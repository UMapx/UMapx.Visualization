using System.Drawing;
using System.Runtime.Versioning;
using Xunit;

namespace UMapx.Visualization.Tests;

[SupportedOSPlatform("windows")]
public class PainterTests
{
    [Fact]
    public void DrawRendersBoxesTitlesLabelsAndStandalonePoints()
    {
        using var image = new Bitmap(320, 240);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.White);
        using var painter = new Painter { TextFont = new Font("Arial", 12), TextColor = Color.Blue };
        painter.Draw(graphics,
            new PaintData(),
            new PaintData { Title = "Object", Rectangle = new Rectangle(40, 70, 180, 80), Labels = new[] { "Score: 0.98" } },
            new PaintData { Points = new[] { new Point(270, 120) } });

        Assert.True(Count(image, new Rectangle(35, 65, 195, 95), c => c.R > 200 && c.G < 60 && c.B < 60) > 500);
        Assert.True(Count(image, new Rectangle(35, 35, 190, 30), IsBlue) > 20);
        Assert.True(Count(image, new Rectangle(35, 155, 195, 35), IsBlue) > 20);
        Assert.True(Count(image, new Rectangle(250, 100, 30, 30), c => c.R > 200 && c.G > 200 && c.B < 60) > 20);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsideBoxControlsWhichSideOfTheBorderContainsLabels(bool inside)
    {
        using var image = new Bitmap(320, 240);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.White);
        using var painter = new Painter { InsideBox = inside, TextFont = new Font("Arial", 12), TextColor = Color.Blue };
        painter.Draw(graphics, new PaintData { Rectangle = new Rectangle(40, 70, 180, 80), Labels = new[] { "First", "Second" } });
        int above = Count(image, new Rectangle(40, 72, 180, 55), IsBlue);
        int below = Count(image, new Rectangle(40, 153, 180, 55), IsBlue);
        Assert.True(inside ? above > 20 && below == 0 : below > 20 && above == 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(255)]
    public void TransparencyBlendsBoxFillWithExistingImage(byte alpha)
    {
        using var image = new Bitmap(200, 160);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.White);
        using var painter = new Painter { Transparency = alpha };
        painter.Draw(graphics, new PaintData { Rectangle = new Rectangle(20, 20, 160, 120) });
        Color center = image.GetPixel(100, 80);
        Assert.Equal(255, center.R);
        Assert.InRange((int)center.G, Math.Max(0, 254 - alpha), Math.Min(255, 256 - alpha));
        Assert.Equal(center.G, center.B);
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void DrawingPreservesCallerClipAndDisposalLeavesGraphicsUsable()
    {
        using var image = new Bitmap(200, 160);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.White);
        graphics.SetClip(new Rectangle(40, 40, 40, 40));
        var clip = graphics.ClipBounds;
        using (var painter = new Painter { Transparency = 255 })
            painter.Draw(graphics, new PaintData { Rectangle = new Rectangle(20, 20, 160, 120) });
        Assert.Equal(clip, graphics.ClipBounds);
        Assert.Equal(Color.Red.ToArgb(), image.GetPixel(60, 60).ToArgb());
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(100, 100).ToArgb());
        graphics.ResetClip();
        graphics.Clear(Color.Blue);
        Assert.Equal(Color.Blue.ToArgb(), image.GetPixel(100, 100).ToArgb());
    }

    private static bool IsBlue(Color color) => color.B > 180 && color.R < 80 && color.G < 80;

    private static int Count(Bitmap image, Rectangle region, Func<Color, bool> predicate)
    {
        int count = 0;
        for (int y = region.Top; y < region.Bottom; y++)
            for (int x = region.Left; x < region.Right; x++)
                if (predicate(image.GetPixel(x, y))) count++;
        return count;
    }
}
