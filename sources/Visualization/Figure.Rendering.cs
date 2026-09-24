using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using UMapx.Core;

namespace UMapx.Visualization
{
    public partial class Figure
    {
        // Every plot uses this frame, layout, typography, and output path.
        private void RenderFigure(Graphics target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            int width = (int)target.VisibleClipBounds.Width, height = (int)target.VisibleClipBounds.Height;
            if (width < 1 || height < 1) throw new ArgumentException("A nonempty drawing surface is required.", nameof(target));
            if (!ScientificData.Finite(Scaling)) throw new InvalidOperationException("Scaling must be finite.");

            bool scientific = _scientificMode != ScientificMode.None;
            bool surface = _scientificMode == ScientificMode.Surface;
            if (scientific)
            {
                ValidateScientific();
                UpdateScientificRanges();
                ScientificData.ValidateRange(RangeX);
                ScientificData.ValidateRange(RangeY);
            }
            else UpdateCartesianRanges();

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            ConfigureGraphics(graphics);
            graphics.Clear(_style.ColorFrame);
            var colorScale = GetColorScale();
            var layout = CreateLayout(graphics, width, height, colorScale);
            var bounds = layout.PlotBounds;
            using (var background = new SolidBrush(_style.ColorBack)) graphics.FillRectangle(background, bounds);

            if (surface)
            {
                var projection = new SurfaceRenderer.Projection(bounds, View3D);
                DrawSurfaceAxes(graphics, projection);
                var renderer = new SurfaceRenderer(width, height, projection, RangeX, RangeY, RangeZ);
                using var image = renderer.Render(_surfaces);
                graphics.DrawImageUnscaled(image, 0, 0);
            }
            else
            {
                var state = graphics.Save();
                try
                {
                    graphics.SetClip(bounds, CombineMode.Intersect);
                    DrawBackgroundData(graphics, bounds);
                    DrawGrid(graphics, bounds);
                    if (!scientific)
                        foreach (var series in _plotSeries) PlotRenderer.Draw(graphics, bounds, RangeX, RangeY, series);
                    else if (_scientificMode == ScientificMode.Contour)
                        FieldRenderer.Contours(graphics, bounds, RangeX, RangeY, _field, _contourLevels, _contourHeatmap);
                }
                finally { graphics.Restore(state); }
                DrawCartesianAxes(graphics, layout);
                if (!scientific) DrawLegend(graphics, bounds);
            }

            DrawTitle(graphics, layout.TitleBounds);
            if (colorScale != null) DrawColorbar(graphics, layout.ColorbarBounds, colorScale);
            target.DrawImageUnscaled(bitmap, 0, 0);
        }

        private static void ConfigureGraphics(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
        }

        private void DrawBackgroundData(Graphics graphics, RectangleF bounds)
        {
            if (_scientificMode == ScientificMode.None)
            {
                if (_imagePane != null) graphics.DrawImage(_imagePane, bounds);
                return;
            }
            if (_scientificMode == ScientificMode.Contour && !_contourHeatmap) return;
            using var image = FieldRenderer.Render(Math.Max(1, (int)bounds.Width), Math.Max(1, (int)bounds.Height),
                RangeX, RangeY, _field, _scientificMode == ScientificMode.Complex ? _complex : null,
                _scientificMode == ScientificMode.Contour || _interpolateField, _complexMagnitude);
            graphics.DrawImage(image, bounds);
        }

        private FigureLayout CreateLayout(Graphics graphics, int width, int height, ColorScale colorScale)
        {
            bool surface = _scientificMode == ScientificMode.Surface;
            int count = surface ? View3D.TickCount : Marks.Y;
            float tickWidth = 0;
            for (int i = 0; i <= count; i++)
                tickWidth = Math.Max(tickWidth, graphics.MeasureString(Tick(surface ? RangeZ : RangeY, i, count), _style.FontMarks).Width);
            float markHeight = _style.FontMarks.GetHeight(graphics);
            float textHeight = _style.FontText.GetHeight(graphics);
            float titleHeight = string.IsNullOrEmpty(Title) ? 0 : graphics.MeasureString(Title, _style.FontText, Math.Max(1, width - 16)).Height;
            float colorWidth = 0;
            if (colorScale != null)
            {
                for (int i = 0; i <= 4; i++)
                    colorWidth = Math.Max(colorWidth, graphics.MeasureString(Tick(colorScale.Range, i, 4), _style.FontMarks).Width);
                colorWidth = Math.Max(colorWidth + 66, graphics.MeasureString(colorScale.Label, _style.FontMarks).Width + 28);
            }
            bool equal = !surface && _scientificMode != ScientificMode.None && EqualFieldAxes;
            return FigureLayout.Create(width, height, Scaling, tickWidth, markHeight, textHeight, titleHeight,
                !string.IsNullOrEmpty(LabelX), !string.IsNullOrEmpty(surface ? LabelZ : LabelY), colorWidth,
                equal ? ((double)RangeX.Max - RangeX.Min) / ((double)RangeY.Max - RangeY.Min) : (double?)null);
        }

        private void DrawGrid(Graphics graphics, RectangleF bounds)
        {
            if (Grid == null || !Grid.Show) return;
            using var pen = CreateGridPen();
            if (_style.GridX)
                for (int i = 1; i < Marks.X; i++)
                {
                    float x = bounds.Left + bounds.Width * i / Marks.X;
                    graphics.DrawLine(pen, x, bounds.Top, x, bounds.Bottom);
                }
            if (_style.GridY)
                for (int i = 1; i < Marks.Y; i++)
                {
                    float y = bounds.Top + bounds.Height * i / Marks.Y;
                    graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
                }
        }

        private Pen CreateGridPen()
        {
            var pen = new Pen(_style.ColorGrid, _style.DepthShapes);
            if (Grid != null && Grid.Style == GridStyle.Dot)
            {
                pen.DashStyle = DashStyle.Dot;
                pen.DashCap = DashCap.Round;
            }
            else if (Grid != null && Grid.Style == GridStyle.Dashed)
            {
                pen.DashStyle = DashStyle.Custom;
                pen.DashPattern = new[] { Math.Max(1, Grid.DashLength), Math.Max(1, Grid.GapLength) };
            }
            return pen;
        }

        private void DrawCartesianAxes(Graphics graphics, FigureLayout layout)
        {
            var bounds = layout.PlotBounds;
            using var pen = new Pen(_style.ColorShapes, _style.DepthShapes);
            using var brush = new SolidBrush(_style.ColorMarks);
            using var center = new StringFormat { Alignment = StringAlignment.Center };
            using var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            bool shapes = Grid != null && Grid.Shapes;
            for (int i = 0; i <= Marks.X; i++)
            {
                float x = bounds.Left + bounds.Width * i / Marks.X;
                if (shapes) graphics.DrawLine(pen, x, bounds.Bottom, x, bounds.Bottom - 4);
                graphics.DrawString(Tick(RangeX, i, Marks.X), _style.FontMarks, brush, new PointF(x, bounds.Bottom + 6), center);
            }
            for (int i = 0; i <= Marks.Y; i++)
            {
                float y = bounds.Bottom - bounds.Height * i / Marks.Y;
                if (shapes) graphics.DrawLine(pen, bounds.Left, y, bounds.Left + 4, y);
                graphics.DrawString(Tick(RangeY, i, Marks.Y), _style.FontMarks, brush, new PointF(bounds.Left - 7, y), right);
            }
            if (shapes) graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            DrawCentered(graphics, LabelX, _style.FontText, bounds.Left + bounds.Width / 2, layout.XLabelY);
            var state = graphics.Save();
            try
            {
                graphics.TranslateTransform(layout.YLabelX, bounds.Top + bounds.Height / 2);
                graphics.RotateTransform(-90);
                DrawCentered(graphics, LabelY, _style.FontText, 0, 0);
            }
            finally { graphics.Restore(state); }
        }

        private void DrawTitle(Graphics graphics, RectangleF bounds)
        {
            using var brush = new SolidBrush(_style.ColorText);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            graphics.DrawString(Title ?? "", _style.FontText, brush, bounds, format);
        }

        private void DrawCentered(Graphics graphics, string text, Font font, float x, float y, Color? color = null)
        {
            using var brush = new SolidBrush(color ?? _style.ColorText);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(text ?? "", font, brush, new PointF(x, y), format);
        }

        private static string Tick(RangeFloat range, int index, int count)
        {
            float value = (float)(range.Min + ((double)range.Max - range.Min) * index / count);
            if (!ScientificData.Finite(value)) return string.Empty;
            if (value == 0) return "0";
            float magnitude = Math.Abs(value);
            return magnitude >= 1e4f || magnitude < 1e-3f ? value.ToString("0.##E+0") : value.ToString("0.###");
        }

        private sealed class ColorScale
        {
            internal Colormap Map;
            internal RangeFloat Range;
            internal string Label;
        }

        private ColorScale GetColorScale()
        {
            if (_scientificMode == ScientificMode.None || Colorbar == null || !Colorbar.Show) return null;
            if (_scientificMode == ScientificMode.Complex)
                return new ColorScale
                {
                    Map = Colormap.Phase, Range = new RangeFloat(-(float)Math.PI, (float)Math.PI),
                    Label = string.IsNullOrEmpty(Colorbar.Label) ? "arg(f), rad" : Colorbar.Label
                };
            var series = _scientificMode == ScientificMode.Surface ? _surfaces[_surfaces.Count - 1] : _field;
            return new ColorScale { Map = series.Colormap, Range = series.GetColorRange(), Label = Colorbar.Label ?? "" };
        }

        private void DrawColorbar(Graphics graphics, RectangleF bounds, ColorScale scale)
        {
            int steps = Math.Max(2, (int)bounds.Height);
            for (int i = 0; i < steps; i++)
            {
                using var brush = new SolidBrush(scale.Map.GetColor(1 - (double)i / (steps - 1)));
                graphics.FillRectangle(brush, bounds.Left, bounds.Top + bounds.Height * i / steps, bounds.Width, bounds.Height / steps + 1);
            }
            using var pen = new Pen(_style.ColorShapes, _style.DepthShapes);
            using var text = new SolidBrush(_style.ColorMarks);
            using var format = new StringFormat { LineAlignment = StringAlignment.Center };
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            for (int i = 0; i <= 4; i++)
            {
                float y = bounds.Bottom - bounds.Height * i / 4;
                graphics.DrawLine(pen, bounds.Right, y, bounds.Right + 4, y);
                graphics.DrawString(Tick(scale.Range, i, 4), _style.FontMarks, text, new PointF(bounds.Right + 8, y), format);
            }
            DrawCentered(graphics, scale.Label, _style.FontMarks, bounds.Left + bounds.Width / 2, bounds.Top - _style.FontMarks.GetHeight(graphics));
        }
    }
}
