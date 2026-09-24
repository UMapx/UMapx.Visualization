<p align="center"><img width="25%" src="https://raw.githubusercontent.com/UMapx/UMapx.Visualization/main/docs/umapxnet_big.png" /></p>
<p align="center">UMapx sub-library for plotting data and annotating images on Windows</p>

# Installation

Install **UMapx.Visualization** using [NuGet](https://www.nuget.org/packages/UMapx.Visualization/):

```shell
dotnet add package UMapx.Visualization
```

NuGet restores the **UMapx** and **System.Drawing.Common** dependencies
automatically. The public API is in the `UMapx.Visualization` namespace.

# Quick start

Plot a series of values and save the figure as a PNG image:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using UMapx.Visualization;

using var style = FigureStyle.Standard;
var figure = new Figure(style)
{
    Title = "Quadratic function",
    LabelX = "X",
    LabelY = "Y"
};
figure.Grid.Show = true;
figure.Plot(new PlotSeries(
    new[] { -2f, -1f, 0f, 1f, 2f },
    new[] { 4f, 1f, 0f, 1f, 4f },
    2, Color.RoyalBlue, SeriesType.Plot, ShapeType.Circle, "y = x²"));

using var bitmap = new Bitmap(800, 600);
figure.To(bitmap);
bitmap.Save("plot.png", ImageFormat.Png);
```

This example uses C# 9 or later and writes `plot.png` to the current working
directory. `Figure.To()` renders into a supplied `Bitmap` or `Graphics` surface.

# Plotting and drawing

| Area | API |
| --- | --- |
| Lines, stems and scatter plots | `PlotSeries`, `SeriesType`, `ShapeType`, `Figure.Plot()` |
| Images | `Figure.Image()` |
| Surfaces and wireframe meshes | `SurfaceSeries`, `SurfaceStyle`, `View3D`, `Figure.Surface()` |
| Heatmaps and contours | `Figure.Heatmap()`, `Figure.Contour()` |
| Complex functions and trajectories | `ComplexSeries`, `ComplexComponent`, `Figure.Complex()`, `Figure.PlotComplex()` |
| Appearance and axes | `FigureStyle`, `Grid`, `Legend`, `Colormap`, `Colorbar` |
| Image annotations | `Painter`, `PaintData` |

All plots share the same styling system. `FigureStyle` includes `Standard`,
`MATLAB`, `MathCad`, `Excel` and other presets. Axis ranges and tick counts use
`RangeFloat` and `PointInt` from `UMapx.Core`.

# Platform support

The library targets **.NET Standard 2.0** and builds as **AnyCPU**. Rendering
requires Windows because it uses `System.Drawing.Common`; Linux and macOS
are not supported. Output is bitmap-based, with orthographic rendering for 3-D
surfaces.

Regression tests cover Windows with .NET 8 in an x64 process. Building and
running the tests and examples requires the .NET 8 SDK, or a newer SDK with
the .NET 8 runtime installed.

# Working with figures

Repeated `Plot()` or `Surface()` calls add series or surfaces. The last plotting
method selects the displayed view; switching views retains the other data.
`Clear()` removes all data and resets X/Y/Z ranges to [-5, 5], retaining labels
and settings.

`AutoRange` follows finite data bounds when rendering and adds a margin for
constant values. Set it to false to use `RangeX`, `RangeY` and `RangeZ`.
`Image()` always sets X/Y ranges to the image dimensions.

`PlotSeries` accepts equally sized X/Y arrays; the Y-only constructor generates
zero-based X coordinates. `SurfaceSeries` uses `float[y, x]` matrices on finite,
strictly increasing coordinate axes with at least two samples each.
`ComplexSeries` uses `Complex32[imaginary, real]` values. Both grid types provide
`Sample()` helpers for evaluating functions.

Series retain their input arrays. Complex `ToField()` and `ToSurface()`
conversions copy a snapshot. Update retained data or settings and call `To()`
again to redraw. Dispose styles, bitmaps and graphics when finished; `Figure`
does not own these resources. `Painter` owns its pens and font.

# Examples

The [plotting example](examples/Program.cs) demonstrates all plot types
and generates PNG images with a combined overview. Run from the repository root:

```shell
dotnet run --project examples/UMapx.Visualization.Example.csproj -c Release
```

Output is written to `figures/` in the current working directory; pass a directory
after `--` to change it. The [example solution](examples/UMapx.Visualization.Example.sln)
opens in Visual Studio.

# Build and test

Run from the repository root on Windows:

```shell
dotnet build UMapx.Visualization.sln -c Release
dotnet test UMapx.Visualization.sln -c Release --no-build --no-restore
```

The solution contains the library and its tests. Tests cover ranges, shared
rendering, surface depth and clipping, fields, complex data and view selection.
The library and XML API documentation are written to
`sources/bin/Release/netstandard2.0/`; the NuGet package is in `sources/bin/Release/`.

# License

MIT
