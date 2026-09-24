using System;
using System.Drawing;

namespace UMapx.Visualization
{
    /// <summary>Internal layout shared by line plots, fields, images, and surfaces.</summary>
    internal sealed class FigureLayout
    {
        internal RectangleF PlotBounds, TitleBounds, ColorbarBounds;
        internal float XLabelY, YLabelX;

        internal static FigureLayout Create(int width, int height, float scaling, float tickWidth,
            float markHeight, float textHeight, float titleHeight, bool labelX, bool labelY,
            float colorWidth, double? dataAspect)
        {
            float horizontalMargin = width * (1 - scaling) / 2;
            float verticalMargin = height * (1 - scaling) / 2;
            float left = Math.Max(horizontalMargin, tickWidth + 14 + (labelY ? textHeight + 8 : 0));
            float top = Math.Max(verticalMargin, titleHeight + 16);
            float bottom = Math.Max(verticalMargin, markHeight + 14 + (labelX ? textHeight + 8 : 0));
            float right = Math.Max(horizontalMargin, 16);
            if (colorWidth > 0) right = Math.Max(right + Math.Min(60, width * 0.12f), colorWidth);

            // Even very small targets keep a nonempty content rectangle.
            left = Math.Min(left, Math.Max(0, width - 1));
            top = Math.Min(top, Math.Max(0, height - 1));
            var plot = new RectangleF(left, top, Math.Max(1, width - left - right), Math.Max(1, height - top - bottom));
            if (dataAspect.HasValue)
            {
                double ratio = dataAspect.Value;
                if (plot.Width / plot.Height > ratio)
                {
                    float w = Math.Max(1, (float)(plot.Height * ratio));
                    plot.X += (plot.Width - w) / 2; plot.Width = w;
                }
                else
                {
                    float h = Math.Max(1, (float)(plot.Width / ratio));
                    plot.Y += (plot.Height - h) / 2; plot.Height = h;
                }
            }
            return new FigureLayout
            {
                PlotBounds = plot,
                TitleBounds = new RectangleF(8, 4, Math.Max(1, width - 16), Math.Max(1, top - 8)),
                ColorbarBounds = new RectangleF(plot.Right + 28, plot.Top + plot.Height * 0.1f, 14, plot.Height * 0.8f),
                XLabelY = plot.Bottom + markHeight + 14 + textHeight / 2,
                YLabelX = Math.Max(textHeight / 2, plot.Left - tickWidth - 15 - textHeight / 2)
            };
        }
    }
}
