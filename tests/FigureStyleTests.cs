using System.Drawing;
using System.Reflection;
using System.Runtime.Versioning;
using UMapx.Core;
using Xunit;

namespace UMapx.Visualization.Tests;

[SupportedOSPlatform("windows")]
public class FigureStyleTests
{
    public static IEnumerable<object[]> Presets() => typeof(FigureStyle)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(p => p.PropertyType == typeof(FigureStyle))
        .Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(Presets))]
    public void EveryPresetRendersSeriesImagesFieldsAndSurfaces(string preset)
    {
        using var style = (FigureStyle)typeof(FigureStyle).GetProperty(preset)!.GetValue(null)!;
        var figure = new Figure(style) { Title = preset };
        figure.Grid.Show = true;
        using var bitmap = new Bitmap(320, 240);
        foreach (var seriesType in Enum.GetValues<SeriesType>())
        {
            figure.Clear();
            figure.Plot(new PlotSeries(new[] { -1f, 1, 0 }, 2, Color.Red, seriesType, ShapeType.Circle, "Signal"));
            Render();
        }
        using var input = new Bitmap(2, 2);
        input.SetPixel(0, 0, Color.Red); input.SetPixel(1, 1, Color.Blue);
        figure.Clear(); figure.Image(input); Render();
        var field = new SurfaceSeries(new float[,] { { 0, 1 }, { 1, 0 } });
        figure.Clear(); figure.Heatmap(field); Render();
        figure.Clear(); figure.Contour(field, heatmap: true); Render();
        figure.Clear(); figure.Surface(field); Render();
        figure.Clear();
        figure.Complex(ComplexSeries.Sample(z => z * z, new RangeFloat(-1, 1), new RangeFloat(-1, 1), 5, 5));
        Render();

        void Render()
        {
            figure.To(bitmap);
            Assert.Equal(style.ColorFrame.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
            int nonFrame = 0;
            for (int y = 0; y < bitmap.Height; y += 4)
                for (int x = 0; x < bitmap.Width; x += 4)
                    if (bitmap.GetPixel(x, y).ToArgb() != style.ColorFrame.ToArgb()) nonFrame++;
            Assert.True(nonFrame > 50, $"{preset} produced an empty plot.");
        }
    }
}
