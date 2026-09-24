using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UMapx.Visualization
{
    public partial class Figure
    {
        private void UpdateCartesianRanges()
        {
            if (_imagePane != null)
            {
                _xmin = _ymin = 0;
                _xmax = _imagePane.Width; _ymax = _imagePane.Height;
                return;
            }
            if (!AutoRange || _plotSeries.Count == 0) return;
            float xmin = float.PositiveInfinity, xmax = float.NegativeInfinity;
            float ymin = float.PositiveInfinity, ymax = float.NegativeInfinity;
            foreach (var series in _plotSeries)
            {
                Accumulate(series.X, ref xmin, ref xmax);
                Accumulate(series.Y, ref ymin, ref ymax);
            }
            if (ScientificData.Finite(xmin)) _xmin = xmin;
            if (ScientificData.Finite(xmax)) _xmax = xmax;
            if (ScientificData.Finite(ymin)) _ymin = ymin;
            if (ScientificData.Finite(ymax)) _ymax = ymax;
            // Preserve the established autorange and constant-value margins.
            if (_xmin == _xmax)
            {
                var range = ScientificData.Expand(_xmin, _xmax); _xmin = range.Min; _xmax = range.Max;
            }
            if (_ymin == _ymax)
            {
                var range = ScientificData.Expand(_ymin, _ymax); _ymin = range.Min; _ymax = range.Max;
            }
        }

        private static void Accumulate(float[] values, ref float min, ref float max)
        {
            foreach (float value in values)
                if (ScientificData.Finite(value)) { min = Math.Min(min, value); max = Math.Max(max, value); }
        }

        private void DrawLegend(Graphics graphics, RectangleF bounds)
        {
            if (Legend == null || !Legend.Show || _plotSeries.Count == 0) return;
            var font = _style.FontMarks;
            float marker = Math.Max(1, Legend.MarkerSize);
            float textWidth = 0, rowHeight = Math.Max(Legend.RowHeight, marker);
            foreach (var series in _plotSeries)
            {
                var size = graphics.MeasureString(series.Label ?? "", font);
                textWidth = Math.Max(textWidth, size.Width);
                rowHeight = Math.Max(rowHeight, size.Height + 4);
            }
            float gap = Math.Max(0, Legend.MarkerGap), padding = Math.Max(0, Legend.Padding), inset = 8;
            float width = Math.Min(textWidth + marker + gap + 2 * inset, Math.Max(1, bounds.Width - 2 * padding));
            float height = Math.Min(_plotSeries.Count * rowHeight + 2 * inset, Math.Max(1, bounds.Height - 2 * padding));
            bool left = Legend.Anchor == LegendAnchor.TopLeft || Legend.Anchor == LegendAnchor.BottomLeft;
            bool top = Legend.Anchor == LegendAnchor.TopLeft || Legend.Anchor == LegendAnchor.TopRight;
            var box = new RectangleF(left ? bounds.Left + padding : bounds.Right - padding - width,
                top ? bounds.Top + padding : bounds.Bottom - padding - height, width, height);
            using var background = new SolidBrush(Color.FromArgb((int)(Math.Max(0, Math.Min(1, Legend.Opacity)) * 255), _style.ColorBack));
            using var border = new Pen(_style.ColorShapes, _style.DepthShapes);
            using var text = new SolidBrush(_style.ColorText);
            using var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var state = graphics.Save();
            try
            {
                graphics.SetClip(bounds, CombineMode.Intersect);
                graphics.FillRectangle(background, box);
                if (Legend.Border) graphics.DrawRectangle(border, box.X, box.Y, box.Width, box.Height);
                graphics.SetClip(box, CombineMode.Intersect);
                for (int i = 0; i < _plotSeries.Count; i++)
                {
                    float y = box.Top + inset + rowHeight * i;
                    if (y >= box.Bottom) break;
                    var series = _plotSeries[i];
                    PlotRenderer.DrawLegendSample(graphics, new RectangleF(box.Left + inset, y, marker, rowHeight), series);
                    var label = new RectangleF(box.Left + inset + marker + gap, y, Math.Max(1, width - 2 * inset - marker - gap), rowHeight);
                    graphics.DrawString(series.Label ?? "", font, text, label, format);
                }
            }
            finally { graphics.Restore(state); }
        }
    }
}
