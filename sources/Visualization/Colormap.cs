using System;
using System.Drawing;

namespace UMapx.Visualization
{
    /// <summary>An immutable, interpolated palette of opaque colors.</summary>
    [Serializable]
    public sealed class Colormap
    {
        private readonly Color[] colors;

        /// <summary>Creates a palette from at least two evenly spaced, opaque color stops.</summary>
        public Colormap(params Color[] colors)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            if (colors.Length < 2) throw new ArgumentException("At least two colors are required.", nameof(colors));
            foreach (var color in colors)
                if (color.A != 255) throw new ArgumentException("Palette colors must be opaque.", nameof(colors));
            this.colors = (Color[])colors.Clone();
        }

        /// <summary>Returns an interpolated color. Finite positions are clamped to [0, 1].</summary>
        public Color GetColor(double position)
        {
            if (!ScientificData.Finite(position)) throw new ArgumentOutOfRangeException(nameof(position));
            double p = Math.Max(0, Math.Min(1, position)) * (colors.Length - 1);
            int i = Math.Min(colors.Length - 2, (int)p);
            double t = p - i;
            var a = colors[i]; var b = colors[i + 1];
            return Color.FromArgb((int)Math.Round(a.R + t * (b.R - a.R)),
                (int)Math.Round(a.G + t * (b.G - a.G)), (int)Math.Round(a.B + t * (b.B - a.B)));
        }

        /// <summary>A palette interpolated from representative viridis color stops.</summary>
        public static Colormap Viridis { get; } = new Colormap(Color.FromArgb(68, 1, 84),
            Color.FromArgb(59, 82, 139), Color.FromArgb(33, 145, 140),
            Color.FromArgb(94, 201, 98), Color.FromArgb(253, 231, 37));
        /// <summary>A blue, cyan, yellow and red palette.</summary>
        public static Colormap Jet { get; } = new Colormap(Color.DarkBlue, Color.Blue, Color.Cyan,
            Color.Yellow, Color.Red, Color.DarkRed);
        /// <summary>A black-to-white palette.</summary>
        public static Colormap Gray { get; } = new Colormap(Color.Black, Color.White);
        /// <summary>A cyclic palette with identical colors at both ends, suitable for phase.</summary>
        public static Colormap Phase { get; } = new Colormap(Color.Red, Color.Yellow, Color.Lime,
            Color.Cyan, Color.Blue, Color.Magenta, Color.Red);
    }
}
