using System;
using System.Drawing;

namespace UMapx.Visualization
{
    internal sealed class SurfaceProjection
    {
        private readonly double ca, sa, ce, se, height, scale, centerX, centerY, midU, midV;
        internal SurfaceProjection(RectangleF bounds, View3D view)
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
                        var p = Rotate(new SurfaceVertex { X = x, Y = y, Z = z });
                        uMin = Math.Min(uMin, p.X); uMax = Math.Max(uMax, p.X);
                        vMin = Math.Min(vMin, p.Y); vMax = Math.Max(vMax, p.Y);
                    }
            scale = Math.Min(bounds.Width / (uMax - uMin), bounds.Height / (vMax - vMin));
            centerX = bounds.Left + bounds.Width / 2; centerY = bounds.Top + bounds.Height / 2;
            midU = (uMin + uMax) / 2; midV = (vMin + vMax) / 2;
        }

        private SurfaceVertex Rotate(SurfaceVertex v)
        {
            double t = sa * v.X + ca * v.Y, z = v.Z * height;
            v.X = ca * v.X - sa * v.Y;
            v.Y = -se * t + ce * z;
            v.Z = ce * t + se * z;
            return v;
        }

        internal SurfaceVertex Project(SurfaceVertex v)
        {
            v = Rotate(v);
            v.X = centerX + (v.X - midU) * scale;
            v.Y = centerY - (v.Y - midV) * scale;
            return v;
        }

        internal PointF Point(double x, double y, double z)
        {
            var p = Project(new SurfaceVertex { X = x, Y = y, Z = z });
            return new PointF((float)p.X, (float)p.Y);
        }
    }
}
