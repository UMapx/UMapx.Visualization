using System;
using System.Drawing;
using UMapx.Core;

namespace UMapx.Visualization
{
    internal static class FieldRenderer
    {
        internal static Bitmap Render(int width, int height, RangeFloat rangeX, RangeFloat rangeY,
            SurfaceSeries field, ComplexSeries complex, bool interpolate, bool magnitude)
        {
            var x = complex != null ? complex.Real : field.X;
            var y = complex != null ? complex.Imaginary : field.Y;
            var colors = field != null && complex == null ? field.GetColorRange() : new RangeFloat(-1, 1);
            var pixels = new int[checked(width * height)];
            for (int row = 0; row < height; row++)
            {
                double py = rangeY.Max - ((double)rangeY.Max - rangeY.Min) * (row + 0.5) / height;
                int j = Locate(y, py); if (j < 0) continue;
                double ty = (py - y[j]) / ((double)y[j + 1] - y[j]);
                for (int col = 0; col < width; col++)
                {
                    double px = rangeX.Min + ((double)rangeX.Max - rangeX.Min) * (col + 0.5) / width;
                    int i = Locate(x, px); if (i < 0) continue;
                    double tx = (px - x[i]) / ((double)x[i + 1] - x[i]);
                    Color color;
                    if (complex != null)
                    {
                        // Interpolate real and imaginary components, never wrapped angles.
                        var a = complex.Values[j, i]; var b = complex.Values[j, i + 1];
                        var c = complex.Values[j + 1, i]; var d = complex.Values[j + 1, i + 1];
                        if (!Valid(a) || !Valid(b) || !Valid(c) || !Valid(d)) continue;
                        double re = Bilinear(a.Real, b.Real, c.Real, d.Real, tx, ty);
                        double im = Bilinear(a.Imag, b.Imag, c.Imag, d.Imag, tx, ty);
                        color = Colormap.Phase.GetColor((Math.Atan2(im, re) + Math.PI) / (2 * Math.PI));
                        if (magnitude)
                        {
                            double brightness = 2 / Math.PI * Math.Atan(Math.Sqrt(re * re + im * im));
                            color = Color.FromArgb((int)(color.R * brightness), (int)(color.G * brightness), (int)(color.B * brightness));
                        }
                    }
                    else
                    {
                        var values = field.ColorValues ?? field.Z;
                        double value;
                        if (interpolate)
                        {
                            if (!FiniteCell(field.Z, j, i) || !FiniteCell(values, j, i)) continue;
                            value = Bilinear(values[j, i], values[j, i + 1], values[j + 1, i], values[j + 1, i + 1], tx, ty);
                        }
                        else
                        {
                            int ix = i + (tx >= 0.5 ? 1 : 0), iy = j + (ty >= 0.5 ? 1 : 0);
                            value = values[iy, ix];
                            if (!ScientificData.Finite(value) || !ScientificData.Finite(field.Z[iy, ix])) continue;
                        }
                        color = field.Colormap.GetColor(ScientificData.Normalize(value, colors));
                    }
                    pixels[row * width + col] = color.ToArgb();
                }
            }
            return SurfaceRenderer.CreateBitmap(width, height, pixels);
        }

        private static bool Valid(Complex32 z) => ScientificData.Finite(z.Real) && ScientificData.Finite(z.Imag);
        private static bool FiniteCell(float[,] values, int j, int i) =>
            ScientificData.Finite(values[j, i]) && ScientificData.Finite(values[j, i + 1]) &&
            ScientificData.Finite(values[j + 1, i]) && ScientificData.Finite(values[j + 1, i + 1]);

        private static double Bilinear(double a, double b, double c, double d, double tx, double ty) =>
            (a * (1 - tx) + b * tx) * (1 - ty) + (c * (1 - tx) + d * tx) * ty;

        private static int Locate(float[] coordinates, double value)
        {
            if (value < coordinates[0] || value > coordinates[coordinates.Length - 1]) return -1;
            int left = 0, right = coordinates.Length - 1;
            while (right - left > 1)
            {
                int mid = (left + right) / 2;
                if (coordinates[mid] <= value) left = mid; else right = mid;
            }
            return left;
        }

        internal static void Contours(Graphics g, RectangleF bounds, RangeFloat rangeX, RangeFloat rangeY,
            SurfaceSeries field, float[] levels, bool overlay)
        {
            var colors = field.GetColorRange();
            if (levels == null)
            {
                double min = double.PositiveInfinity, max = double.NegativeInfinity;
                foreach (var z in field.Z)
                    if (ScientificData.Finite(z)) { min = Math.Min(min, z); max = Math.Max(max, z); }
                if (!ScientificData.Finite(min) || min == max) return;
                levels = new float[10];
                for (int i = 0; i < levels.Length; i++) levels[i] = (float)(min + (max - min) * (i + 1) / 11);
            }
            var state = g.Save();
            try
            {
                g.SetClip(bounds);
                foreach (var level in levels)
                {
                    using var pen = new Pen(overlay ? field.EdgeColor : field.Colormap.GetColor(ScientificData.Normalize(level, colors)), field.LineWidth);
                    for (int j = 0; j < field.Y.Length - 1; j++)
                        for (int i = 0; i < field.X.Length - 1; i++)
                        {
                            Triangle(i, j, i + 1, j, i + 1, j + 1);
                            Triangle(i, j, i + 1, j + 1, i, j + 1);
                        }

                    void Triangle(int ax, int ay, int bx, int by, int cx, int cy)
                    {
                        double a = field.Z[ay, ax], b = field.Z[by, bx], c = field.Z[cy, cx];
                        if (!ScientificData.Finite(a) || !ScientificData.Finite(b) || !ScientificData.Finite(c)) return;
                        double firstX = 0, firstY = 0, secondX = 0, secondY = 0; int count = 0;
                        Edge(ax, ay, a, bx, by, b); Edge(bx, by, b, cx, cy, c); Edge(cx, cy, c, ax, ay, a);
                        if (count == 2 && ClipLine(ref firstX, ref firstY, ref secondX, ref secondY))
                            g.DrawLine(pen, (float)(bounds.Left + firstX * bounds.Width), (float)(bounds.Bottom - firstY * bounds.Height),
                                (float)(bounds.Left + secondX * bounds.Width), (float)(bounds.Bottom - secondY * bounds.Height));

                        void Edge(int x0, int y0, double v0, int x1, int y1, double v1)
                        {
                            if ((v0 < level) == (v1 < level)) return;
                            double t = (level - v0) / (v1 - v0);
                            double x = field.X[x0] + ((double)field.X[x1] - field.X[x0]) * t;
                            double y = field.Y[y0] + ((double)field.Y[y1] - field.Y[y0]) * t;
                            double px = ScientificData.Normalize(x, rangeX), py = ScientificData.Normalize(y, rangeY);
                            if (count++ == 0) { firstX = px; firstY = py; }
                            else { secondX = px; secondY = py; }
                        }
                    }
                }
            }
            finally { g.Restore(state); }
        }

        // Clip in double precision before converting to GDI coordinates.
        private static bool ClipLine(ref double ax, ref double ay, ref double bx, ref double by)
        {
            for (int axis = 0; axis < 2; axis++)
                for (int side = 0; side < 2; side++)
                {
                    double a = axis == 0 ? ax : ay, b = axis == 0 ? bx : by;
                    double da = side == 0 ? a : 1 - a, db = side == 0 ? b : 1 - b;
                    if (da < 0 && db < 0) return false;
                    if (da < 0 || db < 0)
                    {
                        double t = da / (da - db);
                        double x = ax + (bx - ax) * t, y = ay + (by - ay) * t;
                        if (axis == 0) x = side; else y = side;
                        if (da < 0) { ax = x; ay = y; } else { bx = x; by = y; }
                    }
                }
            return true;
        }
    }
}
