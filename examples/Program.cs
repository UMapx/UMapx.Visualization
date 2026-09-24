using System.Drawing;
using System.Drawing.Imaging;
using UMapx.Core;
using UMapx.Visualization;

var output = Path.GetFullPath(args.Length == 0 ? "figures" : args[0]);
Directory.CreateDirectory(output);
using var style = FigureStyle.Standard;
var range = new RangeFloat(-3, 3);
var surface = SurfaceSeries.Sample((x, y) =>
{
    double r = Math.Sqrt(x * x + y * y);
    return (float)(r == 0 ? 1 : Math.Sin(3 * r) / (3 * r));
}, range, range, 61, 61);
var figure = new Figure(style) { Title = "Surface: sin(3r) / 3r", LabelX = "x", LabelY = "y", LabelZ = "z" };
figure.Grid.Show = true;
surface.Style = SurfaceStyle.Surface; surface.Lighting = true;
figure.Surface(surface); Save(figure, "surface");

figure.Clear(); surface.Style = SurfaceStyle.Mesh; surface.ColorEdges = true;
figure.Title = "Mesh: sin(3r) / 3r";
figure.Surface(surface); Save(figure, "mesh");

figure.Clear(); figure.Title = "Heatmap and contours";
figure.Marks = new PointInt(6, 6); surface.Colormap = Colormap.Viridis;
surface.LineWidth = 1.25f;
figure.Contour(surface, heatmap: true); Save(figure, "contour");

var complex = ComplexSeries.Sample(z => z * z - new Complex32(1, 0),
    new RangeFloat(-2, 2), new RangeFloat(-2, 2), 121, 121);
figure.Clear(); figure.Title = "Complex function: f(z) = z² - 1";
figure.LabelX = "Re(z)"; figure.LabelY = "Im(z)";
figure.Marks = new PointInt(4, 4);
figure.Complex(complex); Save(figure, "complex-domain");

figure.Clear(); figure.Title = "Height: |f(z)| · Color: arg(f(z))";
figure.LabelZ = "|f(z)|"; figure.Colorbar.Label = "Phase, rad";
figure.Surface(complex.ToSurface()); Save(figure, "complex-surface");

figure.Clear(); figure.Title = "Phase: arg(z² - 1)";
figure.Heatmap(complex.ToField(ComplexComponent.Phase)); Save(figure, "phase");

float[] t = Enumerable.Range(0, 41).Select(i => i * 0.25f).ToArray();
float[] signal = t.Select(v => (float)Math.Sin(v)).ToArray();
figure.Clear(); figure.Title = "Line plot";
figure.LabelX = "Time"; figure.LabelY = "Amplitude";
figure.AutoRange = false; figure.RangeX = new RangeFloat(0, 10); figure.RangeY = new RangeFloat(-1, 1);
figure.Marks = new PointInt(5, 4);
figure.Plot(new PlotSeries(t, signal, 2, Color.RoyalBlue, SeriesType.Plot, ShapeType.None, "sin(t)"));
figure.Plot(new PlotSeries(t, t.Select(v => (float)(0.5 * Math.Cos(v))).ToArray(),
    2, Color.OrangeRed, SeriesType.Plot, ShapeType.Circle, "0.5 cos(t)"));
figure.Legend.Anchor = LegendAnchor.TopRight;
Save(figure, "line");

figure.Clear(); figure.Title = "Stem plot";
figure.RangeX = new RangeFloat(0, 20); figure.RangeY = new RangeFloat(-1, 1);
figure.Plot(new PlotSeries(signal.Take(21).ToArray(), 1.5f, Color.RoyalBlue, SeriesType.Stem, ShapeType.Ball, "Samples"));
Save(figure, "stem");

figure.Clear(); figure.Title = "Scatter plot";
figure.RangeX = new RangeFloat(0, 10); figure.RangeY = new RangeFloat(-1, 1);
figure.Plot(new PlotSeries(t, signal, 1.5f, Color.RoyalBlue, SeriesType.Scatter, ShapeType.Circle, "Samples"));
Save(figure, "scatter");

using var source = new Bitmap(64, 48);
for (int y = 0; y < source.Height; y++)
    for (int x = 0; x < source.Width; x++)
        source.SetPixel(x, y, Colormap.Viridis.GetColor((double)(x + y) / (source.Width + source.Height - 2)));
figure.Clear(); figure.Title = "Image"; figure.LabelX = "Column"; figure.LabelY = "Row";
figure.Image(source); Save(figure, "image");

using var overview = new Bitmap(1440, 2800);
using (var g = Graphics.FromImage(overview))
{
    g.Clear(Color.White);
    string[] names = { "line", "surface", "stem", "mesh", "scatter", "contour", "complex-domain", "complex-surface", "image", "phase" };
    for (int i = 0; i < names.Length; i++)
    {
        using var image = new Bitmap(Path.Combine(output, names[i] + ".png"));
        g.DrawImageUnscaled(image, (i % 2) * 720, (i / 2) * 560);
    }
}
overview.Save(Path.Combine(output, "overview.png"), ImageFormat.Png);
Console.WriteLine(output);

void Save(Figure value, string name)
{
    using var bitmap = new Bitmap(720, 560);
    value.To(bitmap);
    bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
}
