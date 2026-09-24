<p align="center"><img width="25%" src="https://raw.githubusercontent.com/UMapx/UMapx.Visualization/main/docs/umapxnet_big.png" /></p>
<p align="center">UMapx sub-library for plotting data on Windows</p>

# Installation

Install **UMapx.Visualization** using [NuGet](https://www.nuget.org/packages/UMapx.Visualization/):

```shell
dotnet add package UMapx.Visualization
```

The package includes dependencies on UMapx and System.Drawing.Common, which
NuGet restores automatically. The public API is in the `UMapx.Visualization`
namespace.

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

The examples use C# 9 or later and write images to the current working directory.
`Figure.To(bitmap)` renders into the supplied bitmap; `Figure.To(graphics)`
renders into an existing `System.Drawing.Graphics` surface.

# Plotting and drawing

| Component | Purpose |
| --- | --- |
| `Figure` | Cartesian plots with titles, axis labels, automatic or manual ranges, and bitmap display |
| `PlotSeries` | X and Y samples, line width, color, marker shape and legend label |
| `SeriesType` | Line plots (`Plot`), stem plots (`Stem`) and scatter plots (`Scatter`) |
| `FigureStyle` | Colors, fonts and line widths, with presets such as `Standard`, `MATLAB`, `MathCad`, `Excel` and `Black` |
| `ShapeType` | Circle and rectangle markers, with outlined and filled variants |
| `Grid`, `Legend` | Grid patterns, axis marks and legend position, spacing and appearance |
| `Painter`, `PaintData` | Rectangles, titles, text labels and points drawn over images |

Axis ranges and tick counts use `RangeFloat` and `PointInt` from `UMapx.Core`.

# Platform support

The library targets **.NET Standard 2.0** and builds as **AnyCPU**. Drawing uses
[System.Drawing.Common](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/7.0/system-drawing)
and requires **Windows**. Linux and macOS are not supported by this rendering backend.

Regression tests cover Windows with .NET 8 in an x64 process.

# Working with figures

`PlotSeries` accepts `float[]` arrays. X and Y must have equal lengths. The
constructor that takes only Y values generates X coordinates starting at zero.
Call `Figure.Plot()` for each series to display several series on the same axes.
The figure retains the supplied series and arrays; it does not copy their data.

`AutoRange` is enabled by default. Ranges are calculated when the figure is
rendered, using the finite values across all series. Constant values receive
a margin so that single points and constant signals can be displayed.
To set fixed bounds, disable `AutoRange` and assign `RangeX` and `RangeY` using
`RangeFloat`.

Set `Marks` with `PointInt` to choose the number of intervals on each axis.
Enable the grid with `Grid.Show` and choose solid, dashed or dotted lines with
`Grid.Style`. The legend is visible by default; use `Legend.Show` to hide it
or `Legend.Anchor` to change its corner. Each series supplies its legend text
through `PlotSeries.Label`.

`Figure.Image(bitmap)` displays a bitmap inside the plotting area and sets the
axes to its dimensions, even when `AutoRange` is disabled. It retains the bitmap;
keep it alive until rendering is complete. `Clear()` removes all series and the
image, and resets both axis ranges to [-5, 5].

Dispose `FigureStyle` after the last render, and dispose bitmaps and graphics
objects when finished. A figure does not take ownership of these resources.

# Image annotations

Draw a labeled rectangle and points on a blank image:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using UMapx.Visualization;

using var bitmap = new Bitmap(640, 360);
using (var graphics = Graphics.FromImage(bitmap))
using (var painter = new Painter { InsideBox = true, Transparency = 40 })
{
    graphics.Clear(Color.White);
    painter.Draw(graphics, new PaintData
    {
        Title = "Object",
        Rectangle = new Rectangle(120, 80, 320, 220),
        Labels = new[] { "Confidence: 0.98" },
        Points = new[] { new Point(220, 180), new Point(340, 180) }
    });
}
bitmap.Save("annotations.png", ImageFormat.Png);
```

To annotate an existing image, load it with `new Bitmap("input.jpg")` and omit
`graphics.Clear()`. `Painter.Draw()` draws directly onto the supplied graphics
surface and accepts multiple `PaintData` objects in one call. `Painter` owns its
pens and font and disposes them with the painter; the caller owns the image and
graphics surface.

# Build and test

Run from the repository root on Windows with the .NET 8 SDK, or a newer SDK
with the .NET 8 runtime installed:

```shell
dotnet build UMapx.Visualization.sln -c Release
dotnet test UMapx.Visualization.sln -c Release --no-build --no-restore
```

The solution contains the library and its tests. Dependencies are restored from
NuGet during the build. `build.bat` builds the library in Release configuration.

Tests cover automatic ranges for constant and single-point series, combined
bounds across multiple series, manual ranges, and empty or nonfinite data.
They are also discoverable in Visual Studio.

Build outputs:

- Library and XML API documentation: `sources/bin/Release/netstandard2.0/`.
- NuGet package: `sources/bin/Release/UMapx.Visualization.*.nupkg`.

# License

MIT
