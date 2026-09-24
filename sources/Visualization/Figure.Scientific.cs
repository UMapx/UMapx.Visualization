using System;
using System.Collections.Generic;
using System.Drawing;
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
        /// Does not change the aspect ratio of Plot or Image.</summary>
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

        /// <summary>Adds a complex trajectory (real versus imaginary) as a Cartesian line series.</summary>
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

    }
}
