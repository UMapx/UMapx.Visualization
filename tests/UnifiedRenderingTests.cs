using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using UMapx.Core;
using Xunit;

namespace UMapx.Visualization.Tests;

[SupportedOSPlatform("windows")]
public class UnifiedRenderingTests
{
    [Theory]
    [InlineData("heatmap")] [InlineData("contour")] [InlineData("complex")]
    public void CartesianAndFieldViewsShareIdenticalAxesAndGrid(string mode)
    {
        using var style = FigureStyle.Standard;
        style.ColorFrame = Color.AliceBlue; style.ColorBack = Color.WhiteSmoke;
        style.ColorMarks = Color.DarkRed; style.ColorGrid = Color.SlateGray;
        var line = CreateFigure(style);
        var field = CreateFigure(style);
        SelectEmptyField(field, mode);
        using var a = Render(line); using var b = Render(field);
        Assert.Equal(Pixels(a), Pixels(b));
    }

    [Fact]
    public void PlotKeepsAccumulationZeroBasedCoordinatesAndRetainedData()
    {
        using var style = FigureStyle.Standard;
        var figure = CreateFigure(style); figure.AutoRange = true;
        var values = new[] { 2f, 3, 4 };
        var first = new PlotSeries(values, 2, Color.Red, SeriesType.Plot, ShapeType.None, "first");
        Assert.Equal(new[] { 0f, 1, 2 }, first.X);
        figure.Plot(first);
        figure.Plot(new PlotSeries(new[] { 4f, 5 }, new[] { -2f, -1 }, 2, Color.Blue, SeriesType.Plot, ShapeType.None, "second"));
        values[0] = 10;
        using var image = Render(figure);
        Assert.Equal((0f, 5f), (figure.RangeX.Min, figure.RangeX.Max));
        Assert.Equal((-2f, 10f), (figure.RangeY.Min, figure.RangeY.Max));
        Assert.True(Count(image, c => c.B > 200 && c.R < 50) > 50);
        Assert.True(Count(image, c => c.R > 200 && c.B < 50) > 50);
    }

    [Fact]
    public void ScatterWithoutMarkersStillDrawsConnectedLines()
    {
        using var style = FigureStyle.Standard;
        var plot = CreateFigure(style); var scatter = CreateFigure(style);
        var x = new[] { -1f, 0, 1 }; var y = new[] { -0.5f, 0.5f, 0 };
        plot.Plot(new PlotSeries(x, y, 2, Color.Red, SeriesType.Plot, ShapeType.None, "same"));
        scatter.Plot(new PlotSeries(x, y, 2, Color.Red, SeriesType.Scatter, ShapeType.None, "same"));
        using var a = Render(plot); using var b = Render(scatter);
        Assert.Equal(Pixels(a), Pixels(b));
    }

    [Fact]
    public void NaNStillBreaksLinesAndStemStillUsesZeroBaseline()
    {
        using var style = FigureStyle.Standard;
        var line = CreateFigure(style); line.Grid.Show = false;
        line.Plot(new PlotSeries(new[] { -1f, -0.5f, float.NaN, 0.5f, 1 },
            new[] { -0.5f, -0.5f, 0, 0.5f, 0.5f }, 2, Color.Red, SeriesType.Plot, ShapeType.None, ""));
        using var image = Render(line);
        Assert.False(IsRed(image.GetPixel(320, 240)));
        var stem = CreateFigure(style); stem.Grid.Show = false;
        stem.Plot(new PlotSeries(new[] { 0f }, new[] { 0.5f }, 2, Color.Red, SeriesType.Stem, ShapeType.None, ""));
        using var stems = Render(stem);
        Assert.True(IsRed(stems.GetPixel(320, 210)));
        Assert.False(IsRed(stems.GetPixel(320, 270)));
    }

    [Fact]
    public void ImageStillOverridesManualRangesAndPreservesOrientation()
    {
        using var style = FigureStyle.Standard;
        var figure = CreateFigure(style); figure.Grid.Show = false;
        using var source = new Bitmap(2, 2);
        source.SetPixel(0, 0, Color.Red); source.SetPixel(1, 0, Color.Red);
        source.SetPixel(0, 1, Color.Blue); source.SetPixel(1, 1, Color.Blue);
        figure.Image(source);
        using var image = Render(figure);
        Assert.Equal((0f, 2f), (figure.RangeX.Min, figure.RangeX.Max));
        Assert.Equal((0f, 2f), (figure.RangeY.Min, figure.RangeY.Max));
        Assert.True(image.GetPixel(320, 130).R > image.GetPixel(320, 130).B);
        Assert.True(image.GetPixel(320, 350).B > image.GetPixel(320, 350).R);
    }

    [Theory]
    [InlineData("line")] [InlineData("heatmap")] [InlineData("contour")] [InlineData("complex")] [InlineData("surface")]
    public void RenderingPreservesCallerGraphicsState(string mode)
    {
        using var style = FigureStyle.Standard;
        var figure = CreateFigure(style);
        if (mode == "surface") figure.Surface(new SurfaceSeries(new float[,] { { 0, 1 }, { 1, 0 } }));
        else if (mode != "line") SelectEmptyField(figure, mode);
        using var bitmap = new Bitmap(800, 600);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.TranslateTransform(10, 20);
        graphics.SetClip(new Rectangle(0, 0, 640, 480));
        graphics.SmoothingMode = SmoothingMode.None;
        using var transform = graphics.Transform;
        using var clip = graphics.Clip;
        var clippingBounds = clip.GetBounds(graphics);
        figure.To(graphics);
        using var afterTransform = graphics.Transform;
        using var afterClip = graphics.Clip;
        Assert.Equal(transform.Elements, afterTransform.Elements);
        Assert.Equal(clippingBounds, afterClip.GetBounds(graphics));
        Assert.Equal(SmoothingMode.None, graphics.SmoothingMode);
    }

    [Theory]
    [InlineData(LegendAnchor.TopLeft)] [InlineData(LegendAnchor.TopRight)]
    [InlineData(LegendAnchor.BottomLeft)] [InlineData(LegendAnchor.BottomRight)]
    public void LegendFitsLargeFontsAndLongLabelsInsidePlot(LegendAnchor anchor)
    {
        using var style = FigureStyle.Standard;
        style.FontMarks = new Font("Arial", 18);
        style.ColorText = Color.Magenta;
        var figure = CreateFigure(style);
        figure.Title = figure.LabelX = figure.LabelY = "";
        figure.Legend.Show = true; figure.Legend.Anchor = anchor;
        figure.Plot(new PlotSeries(new[] { -1f, 1 }, new[] { -1f, 1 }, 1,
            Color.Blue, SeriesType.Plot, ShapeType.Ball, new string('W', 150)));
        using var bitmap = Render(figure);
        Assert.True(Count(bitmap, c => c.R > 180 && c.B > 180 && c.G < 100) > 100);
        // Decorations use ColorMarks; magenta pixels therefore belong only to the legend.
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.R > 180 && color.B > 180 && color.G < 100)
                    Assert.True(x >= 112 && x <= 528 && y >= 84 && y <= 396);
            }
    }

    [Fact]
    public void HighScalingAndLargeTextKeepSharedLayoutUsable()
    {
        using var style = FigureStyle.Standard;
        style.FontMarks = new Font("Arial", 18); style.FontText = new Font("Arial", 20);
        var figure = CreateFigure(style); figure.Scaling = 1;
        figure.Title = "First line\nSecond line";
        figure.Heatmap(new SurfaceSeries(new float[,] { { 0, 1 }, { 1, 0 } }));
        figure.Colorbar.Show = true; figure.Colorbar.Label = "Scale";
        using var bitmap = Render(figure);
        Assert.True(Count(bitmap, c => Math.Abs(c.R - c.G) + Math.Abs(c.G - c.B) > 100) > 1000);
    }

    private static Figure CreateFigure(FigureStyle style)
    {
        var result = new Figure(style)
        {
            AutoRange = false, RangeX = new RangeFloat(-1, 1), RangeY = new RangeFloat(-1, 1),
            EqualFieldAxes = false, Marks = new PointInt(4, 4), Title = "Shared frame", LabelX = "x", LabelY = "y"
        };
        result.Grid.Show = true; result.Legend.Show = false; result.Colorbar.Show = false;
        return result;
    }

    private static void SelectEmptyField(Figure figure, string mode)
    {
        var axis = new[] { -1f, 1 };
        var values = new float[,] { { float.NaN, float.NaN }, { float.NaN, float.NaN } };
        if (mode == "complex")
            figure.Complex(new ComplexSeries(axis, axis,
                new Complex32[,] { { new(float.NaN, 0), new(float.NaN, 0) }, { new(float.NaN, 0), new(float.NaN, 0) } }));
        else if (mode == "contour") figure.Contour(new SurfaceSeries(axis, axis, values));
        else figure.Heatmap(new SurfaceSeries(axis, axis, values));
    }

    private static Bitmap Render(Figure figure)
    {
        var image = new Bitmap(640, 480);
        try { figure.To(image); return image; }
        catch { image.Dispose(); throw; }
    }
    private static bool IsRed(Color c) => c.R > 200 && c.G < 60 && c.B < 60;
    private static int Count(Bitmap image, Func<Color, bool> predicate)
    {
        int result = 0;
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++) if (predicate(image.GetPixel(x, y))) result++;
        return result;
    }
    private static int[] Pixels(Bitmap image)
    {
        var result = new int[image.Width * image.Height];
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++) result[y * image.Width + x] = image.GetPixel(x, y).ToArgb();
        return result;
    }
}
