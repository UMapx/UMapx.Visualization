using System.Drawing;
using System.Runtime.Versioning;
using UMapx.Core;
using Xunit;

namespace UMapx.Visualization.Tests;

[SupportedOSPlatform("windows")]
public class ScientificFigureTests
{
    private static readonly float[] Axis = { -1, 0, 1 };

    [Fact]
    public void SurfaceGridRejectsMismatchedNonfiniteOrUnorderedCoordinates()
    {
        Assert.Throws<ArgumentException>(() => new SurfaceSeries(Axis, Axis, new float[2, 3]));
        Assert.Throws<ArgumentException>(() => new SurfaceSeries(new[] { 0f, 0 }, new[] { 0f, 1 }, new float[2, 2]));
        Assert.Throws<ArgumentException>(() => new SurfaceSeries(new[] { 0f, float.NaN }, new[] { 0f, 1 }, new float[2, 2]));
        Assert.Throws<ArgumentException>(() => new SurfaceSeries(Axis, Axis, new float[3, 3], new float[3, 2]));
        Assert.Throws<ArgumentNullException>(() => new SurfaceSeries(null!));
        Assert.Throws<ArgumentException>(() => new SurfaceSeries(new float[1, 1]));
    }

    [Fact]
    public void SamplingUsesRowsForYAndColumnsForX()
    {
        var field = SurfaceSeries.Sample((x, y) => 10 * y + x, new RangeFloat(-1, 1), new RangeFloat(2, 4), 3, 2);
        Assert.Equal(2, field.Z.GetLength(0)); Assert.Equal(3, field.Z.GetLength(1));
        Assert.Equal(19, field.Z[0, 0]); Assert.Equal(41, field.Z[1, 2]);
        var complex = ComplexSeries.Sample(z => new Complex32(z.Real, 2 * z.Imag),
            new RangeFloat(-1, 1), new RangeFloat(2, 4), 3, 2);
        Assert.Equal(-1, complex.Values[0, 0].Real); Assert.Equal(8, complex.Values[1, 2].Imag);
        Assert.Throws<ArgumentOutOfRangeException>(() => SurfaceSeries.Sample((x, y) => x,
            new RangeFloat(0, 1), new RangeFloat(0, 1), 1));
        Assert.Throws<InvalidOperationException>(() => ComplexSeries.Sample(_ => throw new InvalidOperationException(),
            new RangeFloat(0, 1), new RangeFloat(0, 1)));
    }

    [Theory]
    [InlineData(0f)] [InlineData(5f)] [InlineData(float.MaxValue)] [InlineData(-float.MaxValue)]
    public void ConstantSurfacesReceiveFiniteHeightAndColorRanges(float z)
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style); figure.Colorbar.Show = false;
        var data = new float[,] { { z, z }, { z, z } };
        figure.Surface(new SurfaceSeries(data) { Colormap = Solid(Color.Red), Style = SurfaceStyle.Surface });
        using var bitmap = Render(figure);
        Assert.True(float.IsFinite(figure.RangeZ.Min) && float.IsFinite(figure.RangeZ.Max));
        Assert.True(figure.RangeZ.Min < figure.RangeZ.Max);
        Assert.True(Count(bitmap, c => c.R > 220 && c.G < 30 && c.B < 30) > 1000);
    }

    [Fact]
    public void SurfaceRangesCombineAllGridsAndIgnoreInvalidHeights()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style);
        figure.Surface(new SurfaceSeries(new[] { -3f, 0 }, new[] { -1f, 2 }, new float[,] { { -2, float.NaN }, { 0, 1 } }));
        figure.Surface(new SurfaceSeries(new[] { 2f, 5 }, new[] { 0f, 4 }, new float[,] { { 3, 4 }, { float.PositiveInfinity, 6 } }));
        using var bitmap = Render(figure);
        Assert.Equal((-3f, 5f), (figure.RangeX.Min, figure.RangeX.Max));
        Assert.Equal((-1f, 4f), (figure.RangeY.Min, figure.RangeY.Max));
        Assert.Equal((-2f, 6f), (figure.RangeZ.Min, figure.RangeZ.Max));
    }

    [Fact]
    public void IntersectingSurfacesUseDepthAndAreIndependentOfSubmissionOrder()
    {
        using var style = FigureStyle.Standard;
        var red = SurfaceSeries.Sample((x, y) => x, new RangeFloat(-1, 1), new RangeFloat(-1, 1), 3, 3);
        var blue = SurfaceSeries.Sample((x, y) => -x, new RangeFloat(-1, 1), new RangeFloat(-1, 1), 3, 3);
        red.Colormap = Solid(Color.Red); blue.Colormap = Solid(Color.Blue);
        red.Style = blue.Style = SurfaceStyle.Surface;
        var first = TopView(style); first.Surface(red); first.Surface(blue);
        var second = TopView(style); second.Surface(blue); second.Surface(red);
        using var a = Render(first); using var b = Render(second);
        Assert.True(a.GetPixel(210, 240).B > 240);
        Assert.True(a.GetPixel(430, 240).R > 240);
        Assert.Equal(Pixels(a), Pixels(b));
    }

    [Fact]
    public void ManualLimitsClipTrianglesAtTheActualIntersection()
    {
        using var style = FigureStyle.Standard;
        var figure = TopView(style); figure.AutoRange = false;
        figure.RangeX = figure.RangeY = figure.RangeZ = new RangeFloat(-1, 1);
        figure.Surface(new SurfaceSeries(new[] { -10f, 10 }, new[] { -10f, 10 },
            new float[,] { { -10, 10 }, { -10, 10 } }) { Colormap = Solid(Color.Red), Style = SurfaceStyle.Surface });
        using var bitmap = Render(figure);
        Assert.True(bitmap.GetPixel(220, 240).R > 240 && bitmap.GetPixel(220, 240).G < 20);
        Assert.True(bitmap.GetPixel(420, 240).R > 240 && bitmap.GetPixel(420, 240).G < 20);
        Assert.Equal((-1f, 1f), (figure.RangeZ.Min, figure.RangeZ.Max));
    }

    [Fact]
    public void NonfiniteSurfaceVerticesCreateHoles()
    {
        using var style = FigureStyle.Standard;
        var figure = TopView(style);
        var surface = new SurfaceSeries(new float[,] { { float.NaN, float.PositiveInfinity }, { float.NaN, float.NaN } })
            { Colormap = Solid(Color.Red), Style = SurfaceStyle.Surface };
        figure.Surface(surface);
        using var bitmap = Render(figure);
        Assert.Equal(0, Count(bitmap, c => c.R > 220 && c.G < 30 && c.B < 30));
        Assert.True(float.IsFinite(figure.RangeZ.Min) && figure.RangeZ.Min < figure.RangeZ.Max);
    }

    [Fact]
    public void MutableGridAndCameraAreValidatedAtRenderTime()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style);
        var surface = new SurfaceSeries(new float[2, 2]); figure.Surface(surface);
        figure.View3D.Elevation = float.NaN;
        Assert.Throws<ArgumentOutOfRangeException>(() => { using var bitmap = Render(figure); });
        figure.View3D.Elevation = 30; surface.X[1] = surface.X[0];
        Assert.Throws<ArgumentException>(() => { using var bitmap = Render(figure); });
    }

    [Fact]
    public void MeshDrawsEdgesWithoutFillingFaces()
    {
        using var style = FigureStyle.Standard;
        var figure = TopView(style);
        figure.Surface(new SurfaceSeries(Axis, Axis, new float[3, 3]) { Style = SurfaceStyle.Mesh, EdgeColor = Color.Red });
        using var bitmap = Render(figure);
        int red = Count(bitmap, c => c.R > 220 && c.G < 30 && c.B < 30);
        Assert.InRange(red, 300, 6000);
        Assert.Equal(style.ColorBack.ToArgb(), bitmap.GetPixel(220, 200).ToArgb());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HeatmapUsesIncreasingYUpwardAndSupportsIrregularCoordinates(bool interpolate)
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style) { Title = "", LabelX = "", LabelY = "", Grid = null!, Colorbar = null! };
        figure.Heatmap(new SurfaceSeries(new[] { -2f, -1, 4 }, new[] { -3f, 3 },
            new float[,] { { 0, 0, 0 }, { 1, 1, 1 } }) { Colormap = Colormap.Gray }, interpolate);
        using var bitmap = Render(figure);
        Assert.True(bitmap.GetPixel(320, 115).R > 220);
        Assert.True(bitmap.GetPixel(320, 365).R < 35);
        Assert.Equal((-2f, 4f), (figure.RangeX.Min, figure.RangeX.Max));
    }

    [Fact]
    public void ContourOfLinearFieldIsAtRequestedDataCoordinate()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style) { Grid = null!, Colorbar = null! };
        var field = SurfaceSeries.Sample((x, y) => x, new RangeFloat(-1, 1), new RangeFloat(-1, 1), 3, 3);
        field.Colormap = Solid(Color.Red); field.LineWidth = 2;
        figure.Contour(field, new[] { 0f });
        using var bitmap = Render(figure);
        Assert.True(bitmap.GetPixel(320, 200).R > 220 && bitmap.GetPixel(320, 200).G < 30);
        Assert.Equal(style.ColorBack.ToArgb(), bitmap.GetPixel(210, 200).ToArgb());
    }

    [Fact]
    public void ComplexComponentsAndPhaseColorsAreIndependent()
    {
        var values = new Complex32[,] { { new(3, 4), new(-3, 4) }, { new(0, 0), new(float.NaN, 1) } };
        var complex = new ComplexSeries(new[] { -1f, 1 }, new[] { -1f, 1 }, values);
        Assert.Equal(3, complex.ToField(ComplexComponent.Real).Z[0, 0]);
        Assert.Equal(4, complex.ToField(ComplexComponent.Imaginary).Z[0, 0]);
        var surface = complex.ToSurface();
        Assert.Equal(5, surface.Z[0, 0]); Assert.Equal(0, surface.Z[1, 0]);
        Assert.Equal(Math.Atan2(4, 3), surface.ColorValues[0, 0], 6);
        Assert.True(float.IsNaN(surface.Z[1, 1]));
        Assert.Equal(-(float)Math.PI, surface.ColorRange!.Value.Min);
        Assert.Equal(Colormap.Phase.GetColor(0), Colormap.Phase.GetColor(1));
        values[0, 0] = new Complex32(100, 100);
        Assert.Equal(5, surface.Z[0, 0]); // Conversion is a snapshot.
    }

    [Fact]
    public void DomainColoringShowsPhaseAndDarkensTheZero()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style) { Grid = null!, Colorbar = null! };
        figure.Complex(ComplexSeries.Sample(z => z, new RangeFloat(-1, 1), new RangeFloat(-1, 1), 3, 3));
        using var bitmap = Render(figure);
        var positiveReal = bitmap.GetPixel(425, 240); var negativeReal = bitmap.GetPixel(215, 240);
        Assert.True(positiveReal.G > 50 && positiveReal.B > 50 && positiveReal.R < 10);
        Assert.True(negativeReal.R > 50 && negativeReal.G < 10 && negativeReal.B < 10);
        var zero = bitmap.GetPixel(320, 240);
        Assert.True(zero.R < 5 && zero.G < 5 && zero.B < 5);
    }

    [Fact]
    public void ComplexTrajectoriesAndSignalsUseExistingPlotSemantics()
    {
        using var style = FigureStyle.Standard;
        var values = new[] { new Complex32(3, 4), new Complex32(-3, 4) };
        var figure = new Figure(style);
        figure.PlotComplex(values, Color.Red);
        using (var bitmap = Render(figure)) Assert.Equal((-3f, 3f), (figure.RangeX.Min, figure.RangeX.Max));
        figure.Clear(); figure.PlotComplex(new[] { 0f, 1 }, values, ComplexComponent.Magnitude, Color.Blue);
        using (var bitmap = Render(figure)) Assert.True(figure.RangeY.Min < 5 && figure.RangeY.Max > 5);
    }

    [Fact]
    public void ClearAndLegacyPlotRestoreTheOriginalRenderingPath()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style);
        figure.Surface(new SurfaceSeries(new float[2, 2]));
        using (var image = Render(figure)) { }
        figure.Clear();
        using var cleared = Render(figure); using var expected = Render(new Figure(style));
        Assert.Equal(Pixels(expected), Pixels(cleared));
        Assert.Equal((-5f, 5f), (figure.RangeZ.Min, figure.RangeZ.Max));
        figure.Surface(new SurfaceSeries(new float[2, 2]));
        var plot = new PlotSeries(new[] { 1f, 3, 2 }, 2, Color.Red, SeriesType.Plot, ShapeType.Circle, "line");
        figure.Plot(plot);
        var legacy = new Figure(style); legacy.Plot(plot);
        using var actual = Render(figure); using var reference = Render(legacy);
        Assert.Equal(Pixels(reference), Pixels(actual));
    }

    [Fact]
    public void PalettesCopyTheirInputAndValidateValues()
    {
        var input = new[] { Color.Black, Color.White };
        var map = new Colormap(input); input[0] = Color.Red;
        Assert.Equal(Color.Black.ToArgb(), map.GetColor(-1).ToArgb());
        Assert.Equal(Color.White.ToArgb(), map.GetColor(2).ToArgb());
        Assert.Equal(128, map.GetColor(0.5).R);
        Assert.Throws<ArgumentOutOfRangeException>(() => map.GetColor(double.NaN));
        Assert.Throws<ArgumentException>(() => new Colormap(Color.Transparent, Color.Red));
    }

    [Fact]
    public void SurfaceColorDataDoesNotChangeGeometryAndHonorsFixedLimits()
    {
        using var style = FigureStyle.Standard;
        var figure = TopView(style);
        figure.Surface(new SurfaceSeries(new[] { -1f, 1 }, new[] { -1f, 1 }, new float[2, 2],
            new float[,] { { 10, 10 }, { 10, 10 } })
        {
            Colormap = Colormap.Gray, ColorRange = new RangeFloat(0, 20), Style = SurfaceStyle.Surface
        });
        using var bitmap = Render(figure);
        Assert.InRange(bitmap.GetPixel(320, 240).R, 127, 129);
        Assert.True(figure.RangeZ.Min < 0 && figure.RangeZ.Max > 0 && figure.RangeZ.Max < 1);
    }

    [Fact]
    public void HeatmapClipsToManualLimitsAndLeavesInvalidValuesEmpty()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style)
        {
            Grid = null!, Colorbar = null!, AutoRange = false, EqualFieldAxes = false,
            RangeX = new RangeFloat(-2, 2), RangeY = new RangeFloat(-1, 1)
        };
        figure.Heatmap(new SurfaceSeries(new[] { -1f, 0, 1 }, new[] { -1f, 1 },
            new float[,] { { 1, float.NaN, 1 }, { 1, float.NaN, 1 } }) { Colormap = Solid(Color.Red) });
        using var bitmap = Render(figure);
        Assert.Equal(style.ColorBack.ToArgb(), bitmap.GetPixel(140, 240).ToArgb());
        Assert.Equal(style.ColorBack.ToArgb(), bitmap.GetPixel(320, 240).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(235, 240).ToArgb());
    }

    [Fact]
    public void ContoursClipExtremeCoordinatesBeforeDrawing()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style)
        {
            Grid = null!, Colorbar = null!, AutoRange = false,
            RangeX = new RangeFloat(-1, 1), RangeY = new RangeFloat(-1, 1)
        };
        var field = new SurfaceSeries(new[] { -float.MaxValue, float.MaxValue }, new[] { -float.MaxValue, float.MaxValue },
            new float[,] { { -1, 1 }, { -1, 1 } }) { Colormap = Solid(Color.Red), LineWidth = 2 };
        figure.Contour(field, new[] { 0f });
        using var bitmap = Render(figure);
        Assert.True(bitmap.GetPixel(320, 200).R > 220 && bitmap.GetPixel(320, 200).G < 30);
    }

    [Fact]
    public void ContourOverlayUsesContrastingEdgeColor()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style) { Grid = null!, Colorbar = null! };
        var field = SurfaceSeries.Sample((x, y) => x, new RangeFloat(-1, 1), new RangeFloat(-1, 1), 3, 3);
        field.Colormap = Solid(Color.Blue); field.EdgeColor = Color.Red; field.LineWidth = 2;
        figure.Contour(field, new[] { 0f }, heatmap: true);
        using var bitmap = Render(figure);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(240, 200).ToArgb());
        Assert.True(bitmap.GetPixel(320, 200).R > 220);
    }

    [Fact]
    public void ComplexInvalidSamplesRemainEmptyAndPurePhaseCanBeSelected()
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style) { Grid = null!, Colorbar = null! };
        var values = new Complex32[,] { { new(float.NaN, 0), new(1, 0) }, { new(1, 0), new(1, 0) } };
        var data = new ComplexSeries(new[] { -1f, 1 }, new[] { -1f, 1 }, values);
        figure.Complex(data);
        using (var bitmap = Render(figure)) Assert.Equal(style.ColorBack.ToArgb(), bitmap.GetPixel(320, 240).ToArgb());
        values[0, 0] = new Complex32(1, 0);
        figure.Complex(data, showMagnitude: false);
        using var phase = Render(figure);
        Assert.Equal(Color.Cyan.ToArgb(), phase.GetPixel(320, 240).ToArgb());
    }

    [Theory]
    [InlineData(-90f)] [InlineData(0f)] [InlineData(90f)]
    public void CameraExtremesLightingAndColoredEdgesRenderWithoutInvalidPixels(float elevation)
    {
        using var style = FigureStyle.Standard;
        var figure = new Figure(style); figure.View3D.Elevation = elevation;
        var surface = SurfaceSeries.Sample((x, y) => x * y, new RangeFloat(-1, 1), new RangeFloat(-1, 1), 11, 11);
        surface.ColorEdges = true; surface.Lighting = true; surface.InterpolateColors = false;
        figure.Surface(surface);
        using var bitmap = Render(figure);
        Assert.True(Count(bitmap, c => Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) > 50) > 100);
    }

    private static Figure TopView(FigureStyle style) => new Figure(style)
    {
        Title = "", LabelX = "", LabelY = "", LabelZ = "", Grid = null!, Colorbar = null!,
        View3D = new View3D { Azimuth = 0, Elevation = 90 }
    };
    private static Colormap Solid(Color color) => new(color, color);
    private static Bitmap Render(Figure figure)
    {
        var bitmap = new Bitmap(640, 480);
        try { figure.To(bitmap); return bitmap; }
        catch { bitmap.Dispose(); throw; }
    }
    private static int Count(Bitmap bitmap, Func<Color, bool> predicate)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++) if (predicate(bitmap.GetPixel(x, y))) count++;
        return count;
    }
    private static int[] Pixels(Bitmap bitmap)
    {
        var pixels = new int[bitmap.Width * bitmap.Height];
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++) pixels[y * bitmap.Width + x] = bitmap.GetPixel(x, y).ToArgb();
        return pixels;
    }
}
