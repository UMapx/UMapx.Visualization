using System.Drawing;
using System.Drawing.Imaging;
using UMapx.Core;
using UMapx.Visualization;

var output = Path.GetFullPath(args.Length == 0 ? "artifacts/scientific-figures" : args[0]);
Directory.CreateDirectory(output);
using var style = FigureStyle.MATLAB;
style.ColorFrame = Color.White;
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

using var overview = new Bitmap(1440, 1680);
using (var g = Graphics.FromImage(overview))
{
    g.Clear(Color.White);
    string[] names = { "surface", "mesh", "contour", "complex-domain", "complex-surface", "phase" };
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
