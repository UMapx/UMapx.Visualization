using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UMapx.Core;

namespace UMapx.Visualization
{
    // Rasterizes opaque surfaces with a shared depth buffer, including intersections.
    internal sealed class SurfaceRenderer
    {
        internal struct Vertex
        {
            internal double X, Y, Z, R, G, B;
            internal bool Valid;
            internal static Vertex Lerp(Vertex a, Vertex b, double t) => new Vertex
            {
                X = a.X + (b.X - a.X) * t, Y = a.Y + (b.Y - a.Y) * t,
                Z = a.Z + (b.Z - a.Z) * t, R = a.R + (b.R - a.R) * t,
                G = a.G + (b.G - a.G) * t, B = a.B + (b.B - a.B) * t, Valid = true
            };
        }

        internal sealed class Projection
        {
            private readonly double ca, sa, ce, se, height, scale, centerX, centerY, midU, midV;
            internal Projection(RectangleF bounds, View3D view)
            {
                double a = (view.Azimuth % 360) * Math.PI / 180, e = view.Elevation * Math.PI / 180;
                ca = Math.Cos(a); sa = Math.Sin(a); ce = Math.Cos(e); se = Math.Sin(e);
                height = view.HeightRatio;
                double uMin = double.PositiveInfinity, uMax = double.NegativeInfinity;
                double vMin = double.PositiveInfinity, vMax = double.NegativeInfinity;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var p = Rotate(new Vertex { X = x, Y = y, Z = z });
                            uMin = Math.Min(uMin, p.X); uMax = Math.Max(uMax, p.X);
                            vMin = Math.Min(vMin, p.Y); vMax = Math.Max(vMax, p.Y);
                        }
                scale = Math.Min(bounds.Width / (uMax - uMin), bounds.Height / (vMax - vMin));
                centerX = bounds.Left + bounds.Width / 2; centerY = bounds.Top + bounds.Height / 2;
                midU = (uMin + uMax) / 2; midV = (vMin + vMax) / 2;
            }

            private Vertex Rotate(Vertex v)
            {
                double t = sa * v.X + ca * v.Y, z = v.Z * height;
                v.X = ca * v.X - sa * v.Y;
                v.Y = -se * t + ce * z;
                v.Z = ce * t + se * z;
                return v;
            }

            internal Vertex Project(Vertex v)
            {
                v = Rotate(v);
                v.X = centerX + (v.X - midU) * scale;
                v.Y = centerY - (v.Y - midV) * scale;
                return v;
            }

            internal PointF Point(double x, double y, double z)
            {
                var p = Project(new Vertex { X = x, Y = y, Z = z });
                return new PointF((float)p.X, (float)p.Y);
            }
        }

        private readonly int width, height;
        private readonly double[] depth;
        private readonly int[] pixels;
        private readonly Projection projection;
        private readonly RangeFloat rangeX, rangeY, rangeZ;

        internal SurfaceRenderer(int width, int height, Projection projection,
            RangeFloat rangeX, RangeFloat rangeY, RangeFloat rangeZ)
        {
            this.width = width; this.height = height; this.projection = projection;
            this.rangeX = rangeX; this.rangeY = rangeY; this.rangeZ = rangeZ;
            depth = new double[checked(width * height)]; pixels = new int[depth.Length];
            for (int i = 0; i < depth.Length; i++) depth[i] = double.NegativeInfinity;
        }

        internal Bitmap Render(IReadOnlyList<SurfaceSeries> series)
        {
            // All faces precede all edges so edges respect every surface's depth.
            foreach (var surface in series) DrawFaces(surface);
            foreach (var surface in series)
                if (surface.Style != SurfaceStyle.Surface) DrawEdges(surface);
            return CreateBitmap(width, height, pixels);
        }

        internal static Bitmap CreateBitmap(int width, int height, int[] pixels)
        {
            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            try
            {
                var data = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < height; y++)
                        Marshal.Copy(pixels, y * width, IntPtr.Add(data.Scan0, y * data.Stride), width);
                }
                finally { result.UnlockBits(data); }
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        private Vertex MakeVertex(SurfaceSeries surface, int row, int col, RangeFloat colors)
        {
            double z = surface.Z[row, col], c = (surface.ColorValues ?? surface.Z)[row, col];
            if (!ScientificData.Finite(z) || !ScientificData.Finite(c)) return default;
            var color = surface.Colormap.GetColor(ScientificData.Normalize(c, colors));
            return new Vertex
            {
                X = 2 * ScientificData.Normalize(surface.X[col], rangeX) - 1,
                Y = 2 * ScientificData.Normalize(surface.Y[row], rangeY) - 1,
                Z = 2 * ScientificData.Normalize(z, rangeZ) - 1,
                R = color.R, G = color.G, B = color.B, Valid = true
            };
        }

        private void DrawFaces(SurfaceSeries surface)
        {
            var colors = surface.GetColorRange();
            for (int j = 0; j < surface.Y.Length - 1; j++)
                for (int i = 0; i < surface.X.Length - 1; i++)
                {
                    var a = MakeVertex(surface, j, i, colors);
                    var b = MakeVertex(surface, j, i + 1, colors);
                    var c = MakeVertex(surface, j + 1, i + 1, colors);
                    var d = MakeVertex(surface, j + 1, i, colors);
                    DrawTriangle(a, b, c, surface); DrawTriangle(a, c, d, surface);
                }
        }

        private void DrawTriangle(Vertex a, Vertex b, Vertex c, SurfaceSeries surface)
        {
            if (!a.Valid || !b.Valid || !c.Valid) return;
            if (!surface.InterpolateColors)
            {
                b.R = c.R = a.R; b.G = c.G = a.G; b.B = c.B = a.B;
            }
            double light = 1;
            if (surface.Lighting)
            {
                double ux = b.X - a.X, uy = b.Y - a.Y, uz = b.Z - a.Z;
                double vx = c.X - a.X, vy = c.Y - a.Y, vz = c.Z - a.Z;
                double nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
                double n = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (n > 0) light = 0.35 + 0.65 * Math.Abs((0.3 * nx - 0.4 * ny + 0.8660254 * nz) / n);
            }
            // Clip geometry, not sample coordinates, to preserve intersections at manual limits.
            var polygon = new List<Vertex> { a, b, c };
            for (int axis = 0; axis < 3 && polygon.Count > 0; axis++)
                for (int side = -1; side <= 1 && polygon.Count > 0; side += 2)
                    polygon = Clip(polygon, axis, side);
            if (polygon.Count < 3) return;
            a = projection.Project(polygon[0]);
            for (int i = 1; i < polygon.Count - 1; i++)
                RasterTriangle(a, projection.Project(polygon[i]), projection.Project(polygon[i + 1]),
                    light, surface.Style == SurfaceStyle.Mesh);
        }

        private static double Coordinate(Vertex v, int axis) => axis == 0 ? v.X : axis == 1 ? v.Y : v.Z;

        private static List<Vertex> Clip(List<Vertex> input, int axis, int side)
        {
            var output = new List<Vertex>(input.Count + 1);
            var a = input[input.Count - 1]; double da = 1 - side * Coordinate(a, axis);
            foreach (var b in input)
            {
                double db = 1 - side * Coordinate(b, axis);
                if ((da >= 0) != (db >= 0)) output.Add(Vertex.Lerp(a, b, da / (da - db)));
                if (db >= 0) output.Add(b);
                a = b; da = db;
            }
            return output;
        }

        private void RasterTriangle(Vertex a, Vertex b, Vertex c, double light, bool mesh)
        {
            double den = (b.Y - c.Y) * (a.X - c.X) + (c.X - b.X) * (a.Y - c.Y);
            if (Math.Abs(den) < 1e-12) return;
            int x0 = Math.Max(0, (int)Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X))));
            int x1 = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))));
            int y0 = Math.Max(0, (int)Math.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y))));
            int y1 = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y))));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    double px = x + 0.5, py = y + 0.5;
                    double wa = ((b.Y - c.Y) * (px - c.X) + (c.X - b.X) * (py - c.Y)) / den;
                    double wb = ((c.Y - a.Y) * (px - c.X) + (a.X - c.X) * (py - c.Y)) / den;
                    double wc = 1 - wa - wb;
                    if (wa < -1e-9 || wb < -1e-9 || wc < -1e-9) continue;
                    double z = wa * a.Z + wb * b.Z + wc * c.Z;
                    int index = y * width + x;
                    if (z <= depth[index]) continue;
                    depth[index] = z;
                    pixels[index] = mesh ? 0 : Color.FromArgb(
                        Channel((wa * a.R + wb * b.R + wc * c.R) * light),
                        Channel((wa * a.G + wb * b.G + wc * c.G) * light),
                        Channel((wa * a.B + wb * b.B + wc * c.B) * light)).ToArgb();
                }
        }

        private static int Channel(double value) => (int)Math.Max(0, Math.Min(255, Math.Round(value)));

        private void DrawEdges(SurfaceSeries surface)
        {
            var colors = surface.GetColorRange();
            for (int j = 0; j < surface.Y.Length; j++)
                for (int i = 0; i < surface.X.Length; i++)
                {
                    var a = MakeVertex(surface, j, i, colors);
                    if (i + 1 < surface.X.Length) DrawEdge(a, MakeVertex(surface, j, i + 1, colors), surface);
                    if (j + 1 < surface.Y.Length) DrawEdge(a, MakeVertex(surface, j + 1, i, colors), surface);
                }
        }

        private void DrawEdge(Vertex a, Vertex b, SurfaceSeries surface)
        {
            if (!a.Valid || !b.Valid) return;
            for (int axis = 0; axis < 3; axis++)
                for (int side = -1; side <= 1; side += 2)
                {
                    double da = 1 - side * Coordinate(a, axis), db = 1 - side * Coordinate(b, axis);
                    if (da < 0 && db < 0) return;
                    if (da < 0) a = Vertex.Lerp(a, b, da / (da - db));
                    else if (db < 0) b = Vertex.Lerp(a, b, da / (da - db));
                }
            a = projection.Project(a); b = projection.Project(b);
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y))));
            int radius = Math.Max(0, (int)Math.Ceiling(surface.LineWidth / 2) - 1);
            double bias = 0.002 + Math.Abs(b.Z - a.Z) / steps * (radius + 1);
            for (int i = 0; i <= steps; i++)
            {
                var p = Vertex.Lerp(a, b, (double)i / steps);
                int cx = (int)Math.Floor(p.X), cy = (int)Math.Floor(p.Y);
                for (int y = cy - radius; y <= cy + radius; y++)
                    for (int x = cx - radius; x <= cx + radius; x++)
                        if (x >= 0 && x < width && y >= 0 && y < height && p.Z + bias >= depth[y * width + x])
                            pixels[y * width + x] = surface.ColorEdges
                                ? Color.FromArgb(Channel(p.R), Channel(p.G), Channel(p.B)).ToArgb()
                                : surface.EdgeColor.ToArgb();
            }
        }
    }
}
