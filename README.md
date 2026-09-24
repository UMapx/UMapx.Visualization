<p align="center"><img width="25%" src="https://raw.githubusercontent.com/UMapx/UMapx.Visualization/main/docs/umapxnet_big.png" /></p>
<p align="center">UMapx sub-library for plotting data and annotating images on Windows</p>

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

The snippets use C# 9 or later and write images to the current working directory.
`Figure.To(bitmap)` renders into the supplied bitmap; `Figure.To(graphics)`
renders into an existing `System.Drawing.Graphics` surface.

# Plotting and drawing

| Component | Purpose |
| --- | --- |
| `Figure` | Line plots, images, surfaces, scalar fields and complex functions with shared axes and styling |
| `PlotSeries` | X and Y samples, line width, color, marker shape and legend label |
| `SeriesType` | Line plots (`Plot`), stem plots (`Stem`) and scatter plots (`Scatter`) |
| `FigureStyle` | Colors, fonts and line widths, with presets such as `Standard`, `MATLAB`, `MathCad`, `Excel` and `Black` |
| `ShapeType` | Circle and rectangle markers, with outlined and filled variants |
| `Grid`, `Legend` | Grid patterns, axis marks and legend position, spacing and appearance |
| `Painter`, `PaintData` | Rectangles, titles, text labels and points drawn over images |
| `SurfaceSeries`, `View3D` | Grid data for surfaces, meshes, heatmaps and contours; camera settings for 3-D views |
| `ComplexSeries`, `ComplexComponent` | Complex functions and real/imaginary/magnitude/phase fields |
| `Colormap`, `Colorbar` | Color palettes and labeled color scales for surfaces and fields |

Axis ranges and tick counts use `RangeFloat` and `PointInt` from `UMapx.Core`.

# Platform support

The library targets **.NET Standard 2.0** and builds as **AnyCPU**. Drawing uses
[System.Drawing.Common](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/7.0/system-drawing)
and requires **Windows**. Linux and macOS are not supported by this rendering backend.

Regression tests cover Windows with .NET 8 in an x64 process.

# Working with figures

All plot types render through `Figure.To(Bitmap)` or `Figure.To(Graphics)` and
share layout, titles, fonts, grid styling and axis formatting. Configure the
figure, supply data, then render it. Update the data or settings and call `To()`
again to redraw.

## View selection and retained data

The last plotting method selects the displayed view:

| Method | View and data behavior |
| --- | --- |
| `Plot`, `PlotComplex` | Line, stem or scatter series; each call adds a series |
| `Image` | Image background for the series view; replaces the retained image |
| `Surface` | 3-D surfaces or meshes; each call adds a surface |
| `Heatmap` | Scalar field; replaces the active field |
| `Contour` | Isolines, optionally over a heatmap; replaces the active field |
| `Complex` | Complex domain coloring; replaces the active complex field |

Switching views retains data belonging to the other views. `Clear()` removes
all series, surfaces, fields and images, returns to the series view, and resets
X/Y/Z ranges to [-5, 5]. Labels, style and other settings are retained.

`AutoRange` is enabled by default. Ranges are calculated when rendering and are
shared figure state. To use fixed bounds, disable `AutoRange` and assign
`RangeX`, `RangeY`, and, for surfaces, `RangeZ` using `UMapx.Core.RangeFloat`.
Image views always use the image dimensions for X/Y ranges.

Dispose `FigureStyle` after the last render, and dispose bitmaps and graphics
objects when finished. A figure does not take ownership of these resources.

## Appearance, axes and legends

`FigureStyle.Standard` and `new FigureStyle()` use Arial, a white frame and plot
background, light grid lines and one-pixel axis strokes. Named themes and
custom colors, fonts and line widths apply to every view.

`Scaling` is the preferred fraction of the output available to the plot. The
layout reserves space for tick labels, axis labels, titles and colorbars.
Larger fonts and multiline titles can reduce the plotting area. Legends measure
their text and clip or ellipsize content that cannot fit within that area.

Set `Marks` with `PointInt` to choose the number of intervals on each Cartesian
axis. For 3-D axes, use `View3D.TickCount`. Enable the grid with `Grid.Show` and
choose solid, dashed or dotted lines with `Grid.Style`. `FigureStyle.GridX` and
`GridY` control the grid directions; `Grid.Shapes` controls the axis outlines
and tick strokes.

The series legend is visible by default. Use `Legend.Show` to hide it and
`Legend.Anchor` to choose its corner. Each series supplies its text through
`PlotSeries.Label`; legend symbols match the series lines and markers.
Surfaces and fields display a color scale controlled by `Colorbar`.

# Lines, stems and scatter plots

`PlotSeries` accepts `float[]` arrays. X and Y must have equal lengths. The
constructor that takes only Y values generates X coordinates starting at zero.
Call `Figure.Plot()` for each series to display several series on the same axes.
The figure retains the supplied series and arrays; it does not copy their data.

Choose `SeriesType.Plot`, `Stem` or `Scatter`, then select a marker with
`ShapeType`. Stems extend from zero to each value. `Scatter` with
`ShapeType.None` connects the points. NaN coordinates break connected lines.

Automatic X/Y ranges cover finite values across all retained series. Constant
values receive a margin so that single points and constant signals can be displayed.

# Images

`Figure.Image(bitmap)` displays a bitmap inside the plotting area and sets the
axes to its dimensions, even when `AutoRange` is disabled. It retains the bitmap;
keep it alive until rendering is complete. Retained line, stem or scatter series
are drawn over the image. The grid appears over the image when `Grid.Show` is enabled.

Use `Clear()` before `Image()` when starting a figure without previous series.

# Surfaces, heatmaps and contours

`SurfaceSeries` holds grid data shared by `Figure.Surface()`, `Heatmap()` and
`Contour()`. A field can be displayed as a 3-D surface, wireframe mesh, heatmap
or contour plot.

## Grid data

`new SurfaceSeries(x, y, z, colorValues)` accepts strictly increasing, finite
`float[]` coordinates. Both axes require at least two samples. Coordinates may
be irregularly spaced. `z` and optional `colorValues` have shape
`[y.Length, x.Length]`: the first index is the row/Y coordinate, the second is
the column/X coordinate. `new SurfaceSeries(z)` generates zero-based coordinates.

Arrays are retained so callers can update samples and render again. Their
dimensions and coordinate ordering are checked on each render. Nonfinite height
or color samples leave holes; invalid grid coordinates cause an exception.

`SurfaceSeries.Sample(function, xRange, yRange, columns, rows)` evaluates a
function on a regular grid. Exceptions from the function propagate to the caller.
Sampling uses the supplied grid resolution.

```csharp
using System;
using System.Drawing;
using System.Drawing.Imaging;
using UMapx.Core;
using UMapx.Visualization;

using var style = FigureStyle.Standard;
var figure = new Figure(style)
{
    Title = "Surface", LabelX = "x", LabelY = "y", LabelZ = "z"
};
var range = new RangeFloat(-3, 3);
var field = SurfaceSeries.Sample(
    (x, y) => (float)(Math.Sin(x) * Math.Cos(y)), range, range, 81, 81);
field.Style = SurfaceStyle.SurfaceWithMesh;
figure.Surface(field);
figure.View3D.Azimuth = -37.5f;
figure.View3D.Elevation = 30;
using var bitmap = new Bitmap(800, 600);
figure.To(bitmap);
bitmap.Save("surface.png", ImageFormat.Png);
```

## Surfaces, meshes and camera

| Setting | Meaning |
| --- | --- |
| `SurfaceStyle.Surface` | Filled faces |
| `SurfaceStyle.Mesh` | Grid edges with hidden-line removal |
| `SurfaceStyle.SurfaceWithMesh` | Filled faces and edges; default |
| `InterpolateColors` | Interpolate vertex colors across triangles; default true |
| `Lighting` | Directional face lighting; default false |
| `EdgeColor`, `LineWidth` | Opaque edge color and width in pixels |
| `ColorEdges` | Color edges with the palette; default false |
| `View3D.Azimuth`, `View3D.Elevation` | Camera angles in degrees |
| `View3D.HeightRatio` | Height of the normalized plotting box; default 0.75 |
| `View3D.TickCount` | Number of intervals on each 3-D axis |

Set `field.Style = SurfaceStyle.Mesh` and `field.ColorEdges = true` for a colored
wireframe. Repeated `Surface()` calls add surfaces to the scene. Surfaces share
a depth buffer, so intersecting surfaces occlude each other per pixel. Meshes
also participate in depth testing.

With `AutoRange = true`, X/Y cover the surface grids and Z covers finite heights
across all surfaces. Constant Z gets a finite margin; all-invalid Z uses [-1, 1].
With `AutoRange = false`, set `RangeX`, `RangeY` and `RangeZ` explicitly.
Triangles and edges are geometrically clipped to these ranges.

## Heatmaps and contours

Using `figure` and `field` from the grid example:

```csharp
figure.Heatmap(field);                       // Nearest sample
figure.Heatmap(field, interpolate: true);    // Bilinear scalar interpolation
figure.Contour(field);                       // Ten automatic contour levels
figure.Contour(field, new[] { -0.5f, 0f, 0.5f }, heatmap: true);
```

Each call selects a view; call `To()` after a call to render that view.
The field spans the first and last coordinates of each axis; Y increases upward.
Manual ranges can crop it or show space outside its domain. `EqualFieldAxes`
defaults to true, preserving equal units in X and Y for heatmaps, contours and
complex domain coloring. Set it to false to stretch the field into the plotting
area. This setting does not change line plots, images or 3-D surfaces.

Contours trace piecewise-linear Z on the same cell diagonals used by surfaces.
Standalone contour lines use the palette. With `heatmap: true`, lines use
`EdgeColor` over the interpolated background. This produces a heatmap with
isolines; discrete filled-contour bands are not supported.

## Colors and color scales

Each field has its own `Colormap`. Built-ins are `Colormap.Viridis` (interpolated
representative stops), `Jet`, `Gray` and cyclic `Phase`. A custom palette takes
two or more opaque color stops:

```csharp
field.Colormap = new Colormap(Color.DarkBlue, Color.White, Color.DarkRed);
field.ColorRange = new RangeFloat(-1, 1);
figure.Colorbar.Label = "Value";
```

Without a fixed `ColorRange`, finite color values determine the scale. By default,
Z also supplies colors. Pass `colorValues` to decouple height and color.
For multiple surfaces, the colorbar describes the last added surface.
Set `figure.Colorbar.Show = false` to hide it, for example when all mesh edges
use one fixed color. The colorbar does not apply to line, stem, scatter or image views.

# Complex functions

`ComplexSeries` uses `UMapx.Core.Complex32`. The input domain consists of real
and imaginary coordinates; values are indexed `[imaginary, real]`.

Using a `Figure` and bitmap as in the examples above:

```csharp
var domain = new RangeFloat(-2, 2);
var f = ComplexSeries.Sample(z => z * z - new Complex32(1, 0), domain, domain, 121, 121);
figure.Clear();
figure.Title = "f(z) = z² - 1";
figure.LabelX = "Re(z)"; figure.LabelY = "Im(z)";
figure.Complex(f);
figure.To(bitmap);
bitmap.Save("complex.png", ImageFormat.Png);
```

`Complex()` renders domain coloring. Hue follows a cyclic palette over phase
[-pi, pi]. Brightness is `2/pi * atan(magnitude)`: zero is black, large values
approach full brightness. The colorbar explains phase. Pass
`showMagnitude: false` for full-brightness phase colors.

The renderer interpolates real and imaginary components before calculating
phase and magnitude. Cells containing nonfinite samples remain empty.

## Components and surfaces

```csharp
figure.Clear();
figure.LabelZ = "|f(z)|";
figure.Surface(f.ToSurface()); // Height is |f|; color is arg(f)

figure.Clear();
figure.LabelZ = "Re(f(z))";
figure.Surface(f.ToSurface(ComplexComponent.Real)); // Height is Re(f), phase colors

figure.Clear();
figure.Heatmap(f.ToField(ComplexComponent.Phase));
```

Render after each selection to save the corresponding view. `ToField()` supports
`Real`, `Imaginary`, `Magnitude` and `Phase`. `ToSurface()` uses the selected
component for height and phase for colors. These conversions copy a snapshot,
including domain coordinates. Reconvert after updating complex samples.
Values outside finite float range become holes. Phase fields use fixed limits
[-pi, pi] and a cyclic palette. Use nearest sampling for phase fields to avoid
interpolating across a phase wrap.

## Trajectories and signal components

For complex vectors, use line series:

```csharp
var values = new[] { new Complex32(1, 0), new Complex32(0, 1), new Complex32(-1, 0) };
var t = new[] { 0f, 1f, 2f };
figure.Clear();
figure.PlotComplex(values, Color.RoyalBlue, label: "Trajectory");

figure.Clear();
figure.PlotComplex(t, values, ComplexComponent.Magnitude, Color.OrangeRed, label: "Magnitude");
```

The first overload plots imaginary against real values. The second plots one
component against an explicit real argument. Both use the series renderer and
its accumulation, range, marker and legend settings.

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

# Examples

The [Figures example](examples/Figures/Program.cs) generates ten plots: line,
stem, scatter, image, surface, mesh, heatmap with contours, complex domain,
complex surface and phase, plus an overview image.

Run from the repository root on Windows:

```shell
dotnet run --project examples/Figures -c Release
```

Images are written to `artifacts/figures/`. Pass an output directory after `--`
to change that location. To open the examples in Visual Studio, use
[UMapx.Visualization.Examples.sln](examples/UMapx.Visualization.Examples.sln).

# Limitations

Rendering produces bitmaps through `System.Drawing`. The library does not
provide a window or mouse navigation. Update settings and call `To()` to redraw.

3-D uses an orthographic, software-rendered view of opaque surfaces on rectangular
grids. Perspective, translucent faces, arbitrary triangulated meshes and 3-D
line plots are not supported. Rendering cost grows with image size and grid
density; use modest grids for frequently refreshed plots. Function sampling
uses fixed grids rather than adaptive refinement.

LaTeX labels and SVG/PDF export are not supported. Save rendered bitmaps using
the formats available through `Bitmap.Save()`.

# Build and test

Run from the repository root on Windows with the .NET 8 SDK, or a newer SDK
with the .NET 8 runtime installed:

```shell
dotnet build UMapx.Visualization.sln -c Release
dotnet test UMapx.Visualization.sln -c Release --no-build --no-restore
```

The solution contains the library and its tests. Dependencies are restored from
NuGet during the build. `build.bat` builds the library in Release configuration.

Tests cover series ranges and accumulation, shared rendering, surface depth and
clipping, field interpolation, complex components, invalid data and view
selection. They are also discoverable in Visual Studio.

Build outputs:

- Library and XML API documentation: `sources/bin/Release/netstandard2.0/`.
- NuGet package: `sources/bin/Release/UMapx.Visualization.*.nupkg`.

# License

MIT
