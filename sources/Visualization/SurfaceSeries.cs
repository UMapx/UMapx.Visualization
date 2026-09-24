using System;
using System.Drawing;
using UMapx.Core;

namespace UMapx.Visualization
{
    /// <summary>Controls whether a surface displays filled faces, grid edges, or both.</summary>
    public enum SurfaceStyle
    {
        /// <summary>Filled faces.</summary>
        Surface,
        /// <summary>Grid edges with hidden-line removal.</summary>
        Mesh,
        /// <summary>Filled faces and grid edges.</summary>
        SurfaceWithMesh
    }

    /// <summary>A scalar field on a rectangular grid. Arrays are retained, not copied.</summary>
    [Serializable]
    public sealed class SurfaceSeries
    {
        /// <summary>Creates a field. Z and optional color values are indexed [y, x].</summary>
        public SurfaceSeries(float[] x, float[] y, float[,] z, float[,] colorValues = null)
        {
            X = x; Y = y; Z = z; ColorValues = colorValues;
            Validate();
        }

        /// <summary>Creates a field with zero-based column and row coordinates.</summary>
        public SurfaceSeries(float[,] z) : this(
            ScientificData.Indices(z?.GetLength(1) ?? 0),
            ScientificData.Indices(z?.GetLength(0) ?? 0), z) { }

        /// <summary>Strictly increasing horizontal grid coordinates.</summary>
        public float[] X { get; }
        /// <summary>Strictly increasing vertical grid coordinates.</summary>
        public float[] Y { get; }
        /// <summary>Heights or field values, indexed [y, x]. Nonfinite vertices leave holes.</summary>
        public float[,] Z { get; }
        /// <summary>Optional independent color values, indexed [y, x]. Null uses Z.</summary>
        public float[,] ColorValues { get; }
        /// <summary>Color palette.</summary>
        public Colormap Colormap { get; set; } = Colormap.Viridis;
        /// <summary>Fixed color limits. Null computes limits from finite color values.</summary>
        public RangeFloat? ColorRange { get; set; }
        /// <summary>Filled surface, mesh, or both.</summary>
        public SurfaceStyle Style { get; set; } = SurfaceStyle.SurfaceWithMesh;
        /// <summary>Grid edge color.</summary>
        public Color EdgeColor { get; set; } = Color.FromArgb(45, 45, 45);
        /// <summary>Use interpolated palette colors for mesh edges instead of EdgeColor.</summary>
        public bool ColorEdges { get; set; }
        /// <summary>Grid edge width in pixels.</summary>
        public float LineWidth { get; set; } = 0.75f;
        /// <summary>Interpolate vertex colors across each triangle.</summary>
        public bool InterpolateColors { get; set; } = true;
        /// <summary>Apply directional lighting to filled faces.</summary>
        public bool Lighting { get; set; }

        /// <summary>Samples a real-valued function on an evenly spaced grid.</summary>
        public static SurfaceSeries Sample(Func<float, float, float> function, RangeFloat xRange,
            RangeFloat yRange, int columns = 81, int rows = 81)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));
            var x = ScientificData.Coordinates(xRange, columns);
            var y = ScientificData.Coordinates(yRange, rows);
            var z = new float[rows, columns];
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < columns; i++) z[j, i] = function(x[i], y[j]);
            return new SurfaceSeries(x, y, z);
        }

        internal void Validate()
        {
            ScientificData.ValidateGrid(X, Y, Z);
            if (ColorValues != null) ScientificData.ValidateGrid(X, Y, ColorValues);
            if (Colormap == null) throw new ArgumentNullException(nameof(Colormap));
            if (ColorRange.HasValue) ScientificData.ValidateRange(ColorRange.Value);
            if (!Enum.IsDefined(typeof(SurfaceStyle), Style)) throw new ArgumentOutOfRangeException(nameof(Style));
            if (!ScientificData.Finite(LineWidth) || LineWidth <= 0 || LineWidth > 20)
                throw new ArgumentOutOfRangeException(nameof(LineWidth), "Line width must be in (0, 20].");
            if (EdgeColor.A != 255) throw new ArgumentException("Surface edges must be opaque.", nameof(EdgeColor));
        }

        internal RangeFloat GetColorRange()
        {
            if (ColorRange.HasValue) return ColorRange.Value;
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            var values = ColorValues ?? Z;
            for (int j = 0; j < Y.Length; j++)
                for (int i = 0; i < X.Length; i++)
                {
                    var value = values[j, i];
                    if (!ScientificData.Finite(value) || !ScientificData.Finite(Z[j, i])) continue;
                    min = Math.Min(min, value); max = Math.Max(max, value);
                }
            return ScientificData.Expand(min, max);
        }
    }
}
