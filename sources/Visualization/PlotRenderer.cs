using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using UMapx.Core;

namespace UMapx.Visualization
{
    /// <summary>Shared line, stem, scatter, and legend primitives.</summary>
    internal static class PlotRenderer
    {
        internal static void Draw(Graphics graphics, RectangleF bounds, RangeFloat rangeX, RangeFloat rangeY, PlotSeries series)
        {
            if (series.ShapeType < ShapeType.None || series.ShapeType > ShapeType.Polygon) return;
            using var pen = new Pen(series.Color, series.Depth) { LineJoin = LineJoin.Round };
            using var brush = new SolidBrush(series.Color);
            bool stems = series.SeriesType == SeriesType.Stem;
            // Scatter + None intentionally keeps the existing connected-line behavior.
            bool connected = !stems && (series.SeriesType == SeriesType.Plot || series.ShapeType == ShapeType.None);
            float diameter = (series.Depth + 4) * 2;
            var points = connected ? new List<PointF>(series.Y.Length) : null;
            float zero = Device(bounds.Bottom - ScientificData.Normalize(0, rangeY) * bounds.Height);
            for (int i = 0; i < series.Y.Length; i++)
            {
                float x = ClipCoordinate(series.X[i], rangeX.Min, rangeX.Max);
                float y = ClipCoordinate(series.Y[i], rangeY.Min, rangeY.Max);
                if (!ScientificData.Finite(x) || !ScientificData.Finite(y))
                {
                    Flush();
                    continue;
                }
                var p = new PointF(Device(bounds.Left + ScientificData.Normalize(x, rangeX) * bounds.Width),
                    Device(bounds.Bottom - ScientificData.Normalize(y, rangeY) * bounds.Height));
                DrawMarker(graphics, pen, brush, p, diameter, series.ShapeType);
                if (stems) graphics.DrawLine(pen, p.X, zero, p.X, p.Y);
                points?.Add(p);
            }
            Flush();

            void Flush()
            {
                if (points == null) return;
                if (points.Count > 1) graphics.DrawLines(pen, points.ToArray());
                points.Clear();
            }
        }

        internal static void DrawLegendSample(Graphics graphics, RectangleF box, PlotSeries series)
        {
            using var pen = new Pen(series.Color, Math.Max(1, series.Depth));
            using var brush = new SolidBrush(series.Color);
            float x = box.Left + box.Width / 2, y = box.Top + box.Height / 2;
            if (series.SeriesType == SeriesType.Stem)
                graphics.DrawLine(pen, x, y + box.Width / 2, x, y - box.Width / 3);
            else if (series.SeriesType == SeriesType.Plot || series.ShapeType == ShapeType.None)
                graphics.DrawLine(pen, box.Left, y, box.Right, y);
            DrawMarker(graphics, pen, brush, new PointF(x, y), box.Width * 0.7f, series.ShapeType);
        }

        private static void DrawMarker(Graphics graphics, Pen pen, Brush brush, PointF center, float diameter, ShapeType shape)
        {
            var rectangle = new RectangleF(center.X - diameter / 2, center.Y - diameter / 2, diameter, diameter);
            switch (shape)
            {
                case ShapeType.Circle: graphics.DrawEllipse(pen, rectangle); break;
                case ShapeType.Ball: graphics.FillEllipse(brush, rectangle); break;
                case ShapeType.Rectangle: graphics.DrawRectangle(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height); break;
                case ShapeType.Polygon: graphics.FillRectangle(brush, rectangle); break;
            }
        }

        // Preserve the established coordinate handling; this refactor changes presentation.
        private static float ClipCoordinate(float value, float min, float max) =>
            value < min ? min - 1 : value > max ? max + 1 : value;

        private static float Device(double value) => (float)Math.Max(-1000000, Math.Min(1000000, value));
    }
}
