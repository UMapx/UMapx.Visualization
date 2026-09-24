using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using UMapx.Core;

namespace UMapx.Visualization
{
    public partial class Figure
    {
        private enum ScientificMode { None, Surface, Heatmap, Contour, Complex }
        [OptionalField] private ScientificMode _scientificMode;
        [OptionalField] private List<SurfaceSeries> _surfaces = new List<SurfaceSeries>();
        [OptionalField] private SurfaceSeries _field;
        [OptionalField] private ComplexSeries _complex;
        [OptionalField] private float[] _contourLevels;
        [OptionalField] private bool _interpolateField, _contourHeatmap, _complexMagnitude;
        [OptionalField] private RangeFloat _rangeZ = new RangeFloat(-5, 5);

        /// <summary>Gets or sets the label of the vertical 3-D axis.</summary>
        [field: OptionalField] public string LabelZ { get; set; } = "Label Z";
        /// <summary>Gets or sets the 3-D height range. AutoRange updates it when rendering surfaces.</summary>
        public RangeFloat RangeZ
        {
            get => _rangeZ;
            set { ScientificData.ValidateRange(value); _rangeZ = value; }
        }
        /// <summary>Gets or sets the camera used for surface plots.</summary>
        [field: OptionalField] public View3D View3D { get; set; } = new View3D();
        /// <summary>Gets or sets the color scale for scientific plots. Null hides the scale.</summary>
        [field: OptionalField] public Colorbar Colorbar { get; set; } = new Colorbar();
        /// <summary>Use equal horizontal and vertical data units in field, contour, and complex views.
        /// Does not affect the existing Plot/Image rendering path.</summary>
        [field: OptionalField] public bool EqualFieldAxes { get; set; } = true;

        // Keep newly added fields optional for existing serialized Figure objects.
        [OnDeserializing]
        private void InitializeScientificDefaults(StreamingContext context)
        {
            _surfaces = new List<SurfaceSeries>(); _rangeZ = new RangeFloat(-5, 5);
            LabelZ = "Label Z"; View3D = new View3D(); Colorbar = new Colorbar(); EqualFieldAxes = true;
        }

        /// <summary>Adds a surface and selects the 3-D view. Existing 2-D data is retained.</summary>
        /// <remarks>Multiple calls share a depth buffer. Clear removes all surfaces.</remarks>
        public void Surface(SurfaceSeries surface)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            surface.Validate();
            _surfaces.Add(surface);
            _scientificMode = ScientificMode.Surface;
        }

        /// <summary>Displays a scalar field in data coordinates. Replaces the active field view.</summary>
        /// <param name="field">Grid samples indexed [y, x].</param>
        /// <param name="interpolate">Bilinearly interpolate values instead of using nearest samples.</param>
        public void Heatmap(SurfaceSeries field, bool interpolate = false)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            field.Validate(); _field = field; _interpolateField = interpolate;
            _scientificMode = ScientificMode.Heatmap;
        }

        /// <summary>Draws isolines of Z using the same triangular grid as Surface.</summary>
        /// <param name="field">Scalar field.</param>
        /// <param name="levels">Finite contour levels, or null for ten automatically selected levels.</param>
        /// <param name="heatmap">Also show the interpolated scalar field behind isolines drawn in EdgeColor.</param>
        public void Contour(SurfaceSeries field, float[] levels = null, bool heatmap = false)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            field.Validate();
            if (levels != null)
            {
                if (levels.Length == 0 || levels.Length > 500) throw new ArgumentOutOfRangeException(nameof(levels));
                foreach (float level in levels)
                    if (!ScientificData.Finite(level)) throw new ArgumentException("Contour levels must be finite.", nameof(levels));
            }
            _field = field; _contourLevels = levels == null ? null : (float[])levels.Clone();
            _contourHeatmap = heatmap; _scientificMode = ScientificMode.Contour;
        }

        /// <summary>Displays domain coloring: cyclic hue is phase; optional brightness is magnitude.</summary>
        /// <remarks>Brightness is 2/pi * atan(|f|). Zero is black and nonfinite samples leave holes.
        /// Coordinates are the complex input plane. The colorbar describes phase, in radians.</remarks>
        public void Complex(ComplexSeries series, bool showMagnitude = true)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));
            ScientificData.ValidateGrid(series.Real, series.Imaginary, series.Values);
            _complex = series; _complexMagnitude = showMagnitude;
            _scientificMode = ScientificMode.Complex;
        }

        /// <summary>Adds a complex trajectory (real versus imaginary) using the existing 2-D plot path.</summary>
        public void PlotComplex(Complex32[] values, Color color, float depth = 1,
            ShapeType shapeType = ShapeType.None, string label = "")
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var x = new float[values.Length]; var y = new float[values.Length];
            for (int i = 0; i < values.Length; i++) { x[i] = values[i].Real; y[i] = values[i].Imag; }
            Plot(new PlotSeries(x, y, depth, color, SeriesType.Plot, shapeType, label));
        }

        /// <summary>Adds a component of a complex signal against an explicit real argument.</summary>
        public void PlotComplex(float[] x, Complex32[] values, ComplexComponent component, Color color,
            float depth = 1, ShapeType shapeType = ShapeType.None, string label = "")
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (x.Length != values.Length) throw new ArgumentException("Vectors must be of the same length.");
            ScientificData.ValidateComponent(component);
            var y = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
                y[i] = ScientificData.ToFloat(ScientificData.Component(values[i], component));
            Plot(new PlotSeries(x, y, depth, color, SeriesType.Plot, shapeType, label));
        }

        private void ClearScientific()
        {
            _scientificMode = ScientificMode.None; _surfaces.Clear();
            _field = null; _complex = null; _contourLevels = null;
            _rangeZ = new RangeFloat(-5, 5);
        }

        private void RenderScientific(Graphics target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            int width = (int)target.VisibleClipBounds.Width, height = (int)target.VisibleClipBounds.Height;
            if (width < 1 || height < 1) throw new ArgumentException("A nonempty drawing surface is required.", nameof(target));
            if (!ScientificData.Finite(Scaling)) throw new InvalidOperationException("Scaling must be finite.");
            ValidateScientific();
            UpdateScientificRanges();
            ScientificData.ValidateRange(RangeX); ScientificData.ValidateRange(RangeY);
            bool colorbar = Colorbar != null && Colorbar.Show;
            float plotWidth = Math.Max(1, width * Scaling - (colorbar ? Math.Min(60, width * 0.12f) : 0));
            float plotHeight = Math.Max(1, height * Scaling);
            var bounds = new RectangleF((width - width * Scaling) / 2, (height - plotHeight) / 2, plotWidth, plotHeight);
            if (_scientificMode != ScientificMode.Surface && EqualFieldAxes)
            {
                double ratio = ((double)RangeX.Max - RangeX.Min) / ((double)RangeY.Max - RangeY.Min);
                if (bounds.Width / bounds.Height > ratio)
                {
                    float w = Math.Max(1, (float)(bounds.Height * ratio));
                    bounds.X += (bounds.Width - w) / 2; bounds.Width = w;
                }
                else
                {
                    float h = Math.Max(1, (float)(bounds.Width / ratio));
                    bounds.Y += (bounds.Height - h) / 2; bounds.Height = h;
                }
            }
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(_style.ColorFrame);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var background = new SolidBrush(_style.ColorBack)) graphics.FillRectangle(background, bounds);
            if (_scientificMode == ScientificMode.Surface)
            {
                var projection = new SurfaceRenderer.Projection(bounds, View3D);
                DrawSurfaceAxes(graphics, projection, bounds);
                var renderer = new SurfaceRenderer(width, height, projection, RangeX, RangeY, RangeZ);
                using var surface = renderer.Render(_surfaces);
                graphics.DrawImageUnscaled(surface, 0, 0);
            }
            else
            {
                if (_scientificMode != ScientificMode.Contour || _contourHeatmap)
                {
                    using var field = FieldRenderer.Render(Math.Max(1, (int)bounds.Width), Math.Max(1, (int)bounds.Height),
                        RangeX, RangeY, _field, _scientificMode == ScientificMode.Complex ? _complex : null,
                        _scientificMode == ScientificMode.Contour || _interpolateField, _complexMagnitude);
                    graphics.DrawImage(field, bounds);
                }
                DrawFieldAxes(graphics, bounds);
                if (_scientificMode == ScientificMode.Contour)
                    FieldRenderer.Contours(graphics, bounds, RangeX, RangeY, _field, _contourLevels, _contourHeatmap);
            }
            using var titleFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(_style.ColorText);
            graphics.DrawString(Title ?? "", _style.FontText, textBrush, new PointF(width / 2f, bounds.Top / 2), titleFormat);
            if (colorbar) DrawColorbar(graphics, bounds, width);
            target.DrawImageUnscaled(bitmap, 0, 0);
        }

        private void ValidateScientific()
        {
            if (_scientificMode == ScientificMode.Surface)
            {
                if (View3D == null) throw new InvalidOperationException("View3D must be set for surface plots.");
                View3D.Validate(); ScientificData.ValidateRange(RangeZ);
                foreach (var surface in _surfaces) surface.Validate();
            }
            else if (_scientificMode == ScientificMode.Complex)
                ScientificData.ValidateGrid(_complex.Real, _complex.Imaginary, _complex.Values);
            else _field.Validate();
        }

        private void UpdateScientificRanges()
        {
            if (!AutoRange) return;
            if (_scientificMode == ScientificMode.Surface)
            {
                double xmin = double.PositiveInfinity, xmax = double.NegativeInfinity;
                double ymin = double.PositiveInfinity, ymax = double.NegativeInfinity;
                double zmin = double.PositiveInfinity, zmax = double.NegativeInfinity;
                foreach (var surface in _surfaces)
                {
                    xmin = Math.Min(xmin, surface.X[0]); xmax = Math.Max(xmax, surface.X[surface.X.Length - 1]);
                    ymin = Math.Min(ymin, surface.Y[0]); ymax = Math.Max(ymax, surface.Y[surface.Y.Length - 1]);
                    foreach (var z in surface.Z)
                        if (ScientificData.Finite(z)) { zmin = Math.Min(zmin, z); zmax = Math.Max(zmax, z); }
                }
                RangeX = ScientificData.Expand(xmin, xmax); RangeY = ScientificData.Expand(ymin, ymax);
                RangeZ = ScientificData.Expand(zmin, zmax);
            }
            else
            {
                var x = _scientificMode == ScientificMode.Complex ? _complex.Real : _field.X;
                var y = _scientificMode == ScientificMode.Complex ? _complex.Imaginary : _field.Y;
                RangeX = new RangeFloat(x[0], x[x.Length - 1]); RangeY = new RangeFloat(y[0], y[y.Length - 1]);
            }
        }

        private void DrawFieldAxes(Graphics g, RectangleF bounds)
        {
            using var pen = new Pen(_style.ColorShapes, _style.DepthShapes);
            using var brush = new SolidBrush(_style.ColorMarks);
            using var center = new StringFormat { Alignment = StringAlignment.Center };
            using var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            using var grid = MakeScientificGridPen();
            var state = g.Save(); g.SetClip(bounds);
            if (Grid != null && Grid.Show)
            {
                if (_style.GridX) for (int i = 1; i < Marks.X; i++)
                {
                    float x = bounds.Left + bounds.Width * i / Marks.X;
                    g.DrawLine(grid, x, bounds.Top, x, bounds.Bottom);
                }
                if (_style.GridY) for (int i = 1; i < Marks.Y; i++)
                {
                    float y = bounds.Top + bounds.Height * i / Marks.Y;
                    g.DrawLine(grid, bounds.Left, y, bounds.Right, y);
                }
            }
            g.Restore(state);
            for (int i = 0; i <= Marks.X; i++)
            {
                float x = bounds.Left + bounds.Width * i / Marks.X;
                if (Grid != null && Grid.Shapes) g.DrawLine(pen, x, bounds.Bottom, x, bounds.Bottom - 4);
                g.DrawString(Tick(RangeX, i, Marks.X), _style.FontMarks, brush, new PointF(x, bounds.Bottom + 6), center);
            }
            for (int i = 0; i <= Marks.Y; i++)
            {
                float y = bounds.Bottom - bounds.Height * i / Marks.Y;
                if (Grid != null && Grid.Shapes) g.DrawLine(pen, bounds.Left, y, bounds.Left + 4, y);
                g.DrawString(Tick(RangeY, i, Marks.Y), _style.FontMarks, brush, new PointF(bounds.Left - 7, y), right);
            }
            if (Grid != null && Grid.Shapes) g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            DrawCentered(g, LabelX, _style.FontText, bounds.Left + bounds.Width / 2, bounds.Bottom + 35);
            var labelState = g.Save();
            g.TranslateTransform(Math.Max(12, bounds.Left - 60), bounds.Top + bounds.Height / 2);
            g.RotateTransform(-90); DrawCentered(g, LabelY, _style.FontText, 0, 0); g.Restore(labelState);
        }

        private Pen MakeScientificGridPen()
        {
            var pen = new Pen(_style.ColorGrid, 1);
            if (Grid != null)
            {
                if (Grid.Style == GridStyle.Dot) { pen.DashStyle = DashStyle.Dot; pen.DashCap = DashCap.Round; }
                else if (Grid.Style == GridStyle.Dashed)
                {
                    pen.DashStyle = DashStyle.Custom;
                    pen.DashPattern = new[] { Math.Max(1, Grid.DashLength), Math.Max(1, Grid.GapLength) };
                }
            }
            return pen;
        }

        private void DrawSurfaceAxes(Graphics g, SurfaceRenderer.Projection projection, RectangleF bounds)
        {
            using var pen = new Pen(_style.ColorShapes, _style.DepthShapes);
            using var grid = MakeScientificGridPen();
            if (Grid != null && Grid.Shapes)
            {
                for (int a = -1; a <= 1; a += 2)
                    for (int b = -1; b <= 1; b += 2)
                    {
                        g.DrawLine(grid, projection.Point(-1, a, b), projection.Point(1, a, b));
                        g.DrawLine(grid, projection.Point(a, -1, b), projection.Point(a, 1, b));
                        g.DrawLine(grid, projection.Point(a, b, -1), projection.Point(a, b, 1));
                    }
            }
            if (Grid != null && Grid.Show)
                for (int i = 1; i < View3D.TickCount; i++)
                {
                    double t = -1 + 2.0 * i / View3D.TickCount;
                    if (_style.GridX) g.DrawLine(grid, projection.Point(t, -1, -1), projection.Point(t, 1, -1));
                    if (_style.GridY) g.DrawLine(grid, projection.Point(-1, t, -1), projection.Point(1, t, -1));
                }
            // Choose the front corner for X/Y ticks, and the leftmost base corner for Z.
            double fx = 1, fy = 1, zx = 1, zy = 1, near = double.NegativeInfinity, left = double.PositiveInfinity;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                {
                    var p = projection.Project(new SurfaceRenderer.Vertex { X = x, Y = y, Z = -1 });
                    if (p.Z > near) { near = p.Z; fx = x; fy = y; }
                    if (p.X < left) { left = p.X; zx = x; zy = y; }
                }
            var xmid = projection.Point(0, fy, -1);
            var ymid = projection.Point(fx, 0, -1);
            float xOffset = xmid.X >= ymid.X ? 14 : -14;
            DrawAxis(RangeX, LabelX, t => projection.Point(t, fy, -1), xOffset, 14);
            DrawAxis(RangeY, LabelY, t => projection.Point(fx, t, -1), -xOffset, 14);
            DrawAxis(RangeZ, LabelZ, t => projection.Point(zx, zy, t), -24, 0);

            void DrawAxis(RangeFloat range, string label, Func<double, PointF> point, float dx, float dy)
            {
                if (Grid != null && Grid.Shapes) g.DrawLine(pen, point(-1), point(1));
                for (int i = 0; i <= View3D.TickCount; i++)
                {
                    var p = point(-1 + 2.0 * i / View3D.TickCount);
                    if (Grid != null && Grid.Shapes) g.DrawLine(pen, p, new PointF(p.X + dx * 0.15f, p.Y + dy * 0.3f));
                    DrawCentered(g, Tick(range, i, View3D.TickCount), _style.FontMarks, p.X + dx, p.Y + dy);
                }
                var middle = point(0);
                if (dy != 0) DrawCentered(g, label, _style.FontText, middle.X, middle.Y + 39);
                else
                {
                    var saved = g.Save(); g.TranslateTransform(middle.X - 61, middle.Y); g.RotateTransform(-90);
                    DrawCentered(g, label, _style.FontText, 0, 0); g.Restore(saved);
                }
            }
        }

        private string Tick(RangeFloat range, int i, int count) =>
            GetNumString((float)(range.Min + ((double)range.Max - range.Min) * i / count));

        private void DrawCentered(Graphics g, string text, Font font, float x, float y)
        {
            using var brush = new SolidBrush(_style.ColorText);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text ?? "", font, brush, new PointF(x, y), format);
        }

        private void DrawColorbar(Graphics g, RectangleF bounds, int width)
        {
            var series = _scientificMode == ScientificMode.Surface ? _surfaces[_surfaces.Count - 1] : _field;
            bool complex = _scientificMode == ScientificMode.Complex;
            var map = complex ? Colormap.Phase : series.Colormap;
            var range = complex ? new RangeFloat(-(float)Math.PI, (float)Math.PI) : series.GetColorRange();
            float x = bounds.Right + 28, y = bounds.Top + bounds.Height * 0.1f;
            float h = bounds.Height * 0.8f;
            if (x + 20 >= width) return;
            int steps = Math.Max(2, (int)h);
            for (int i = 0; i < steps; i++)
            {
                using var brush = new SolidBrush(map.GetColor(1 - (double)i / (steps - 1)));
                g.FillRectangle(brush, x, y + h * i / steps, 14, h / steps + 1);
            }
            using var pen = new Pen(_style.ColorShapes, 1);
            using var text = new SolidBrush(_style.ColorText);
            using var format = new StringFormat { LineAlignment = StringAlignment.Center };
            g.DrawRectangle(pen, x, y, 14, h);
            for (int i = 0; i <= 4; i++)
            {
                float py = y + h - h * i / 4;
                g.DrawLine(pen, x + 14, py, x + 18, py);
                g.DrawString(Tick(range, i, 4), _style.FontMarks, text, new PointF(x + 22, py), format);
            }
            string label = Colorbar.Label;
            if (string.IsNullOrEmpty(label) && complex) label = "arg(f), rad";
            DrawCentered(g, label, _style.FontMarks, x + 7, y - 17);
        }
    }
}
