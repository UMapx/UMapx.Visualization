using System;
using System.Drawing;
using UMapx.Core;

namespace UMapx.Visualization
{
    public partial class Figure
    {
        private void DrawSurfaceAxes(Graphics g, SurfaceRenderer.Projection projection)
        {
            using var pen = new Pen(_style.ColorShapes, _style.DepthShapes);
            using var grid = CreateGridPen();
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
                    DrawCentered(g, Tick(range, i, View3D.TickCount), _style.FontMarks, p.X + dx, p.Y + dy, _style.ColorMarks);
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

    }
}
