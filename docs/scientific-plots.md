# Scientific plots

The plotting API supports Cartesian series, images, 3-D surfaces, scalar fields,
and complex functions. Every view shares one frame and layout system. It uses
the existing Windows `System.Drawing` backend and renders through `To(Bitmap)`
or `To(Graphics)`.

## Common appearance

Line, stem, scatter, image, heatmap, contour, and complex views use the same
Cartesian axes, tick formatting, and grid. Surfaces share the typography, frame,
grid style, and color scale, with their own projected 3-D axes. Line and scatter
markers use the same drawing primitives as their legend symbols.

`FigureStyle.Standard` and `new FigureStyle()` use Arial, a white frame and plot
background, light grid lines, and one-pixel axis strokes. `FigureStyle.MATLAB`
also uses a white frame. Other named themes retain their color schemes; presets
that previously requested Trojan Pro now use Arial. Custom colors, fonts,
line widths, grid options, and legend settings continue to apply.

`Scaling` is the preferred fraction of the output available to the plot. The
layout reserves extra space when tick labels, axis labels, title, or colorbar
require it. Larger fonts and multiline titles therefore reduce the plot area
instead of sharing the same fixed text offsets. Legends measure their text and
clip/ellipsize content that cannot fit within the plotting area.

## Surface data

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
Sampling is explicit and fixed-resolution, not adaptive.

```csharp
var field = SurfaceSeries.Sample(
    (x, y) => (float)(Math.Sin(x) * Math.Cos(y)),
    new RangeFloat(-3, 3), new RangeFloat(-3, 3), 81, 81);
figure.Surface(field);
```

## Surfaces, meshes, and camera

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
wireframe. Set `figure.Colorbar.Show = false` if a color scale is unnecessary,
for example when all mesh edges use one fixed color.

Repeated `Surface()` calls add surfaces to the scene. They share a depth buffer,
so intersecting surfaces occlude each other per pixel. Meshes also participate
in depth testing. The grid uses the existing `Grid` and `FigureStyle` settings.

With `AutoRange = true`, X/Y cover the surface grids and Z covers finite heights
across all surfaces. Constant Z gets a finite margin; all-invalid Z uses [-1, 1].
With `AutoRange = false`, set `RangeX`, `RangeY`, and `RangeZ` explicitly.
Triangles and edges are geometrically clipped to these ranges.

## Colors

Each field has its own `Colormap`. Built-ins are `Colormap.Viridis` (interpolated
representative stops), `Jet`, `Gray`, and cyclic `Phase`. A custom palette takes
two or more opaque color stops:

```csharp
field.Colormap = new Colormap(Color.DarkBlue, Color.White, Color.DarkRed);
field.ColorRange = new RangeFloat(-1, 1);
figure.Colorbar.Label = "Value";
```

Without a fixed `ColorRange`, finite color values determine the scale. By default
Z also supplies colors. Pass `colorValues` to decouple height and color.
For multiple surfaces, the colorbar describes the last added surface.

## Heatmaps and contours

```csharp
figure.Heatmap(field);                       // Nearest sample
figure.Heatmap(field, interpolate: true);    // Bilinear scalar interpolation
figure.Contour(field);                       // Ten automatic contour levels
figure.Contour(field, new[] { -0.5f, 0f, 0.5f }, heatmap: true);
```

The field spans the first and last coordinates of each axis; Y increases upward.
Manual ranges can crop it or show space outside its domain. `EqualFieldAxes`
defaults to true, preserving equal units in X and Y. Set it to false to stretch
the field into the plotting area. This setting affects only the new field views.

Contours trace piecewise-linear Z on the same cell diagonals used by surfaces.
Standalone contour lines use the palette. With `heatmap: true`, lines use
`EdgeColor` for contrast over the interpolated background. This is a heatmap with
isolines, not a discrete filled-contour (`contourf`) implementation.

## Complex functions

`ComplexSeries` uses `UMapx.Core.Complex32`. The input domain consists of real
coordinates and imaginary coordinates; values are indexed `[imaginary, real]`.

```csharp
var domain = new RangeFloat(-2, 2);
var f = ComplexSeries.Sample(z => z * z - new Complex32(1, 0), domain, domain, 121, 121);
figure.Complex(f);
```

`Complex()` renders domain coloring. Hue follows a cyclic palette over phase
[-pi, pi]. Brightness is `2/pi * atan(magnitude)`: zero is black, large values
approach full brightness. The colorbar explains hue/phase only. Pass
`showMagnitude: false` for full-brightness phase colors.

The renderer interpolates real and imaginary components before calculating
phase and magnitude. Cells containing nonfinite samples remain empty.

```csharp
figure.Clear();
figure.Surface(f.ToSurface()); // Height is |f|; color is arg(f)

figure.Clear();
figure.Surface(f.ToSurface(ComplexComponent.Real)); // Height is Re(f), phase colors

figure.Clear();
figure.Heatmap(f.ToField(ComplexComponent.Phase));
```

`ToField()` supports `Real`, `Imaginary`, `Magnitude`, and `Phase`. `ToSurface()`
uses the selected component for height and phase for colors. These conversions
copy a snapshot, including domain coordinates. Reconvert after updating complex
samples. Values outside finite float range become holes rather than overflowing
the renderer. Phase fields use fixed limits [-pi, pi] and a cyclic palette.
Use nearest sampling for phase fields to avoid interpolating across a phase wrap.

For complex vectors, use Cartesian line series:

```csharp
figure.PlotComplex(values, Color.RoyalBlue, label: "Trajectory");
figure.PlotComplex(t, values, ComplexComponent.Magnitude, Color.OrangeRed, label: "Magnitude");
```

The first overload plots imaginary against real values. The second plots one
component against an explicit real argument. Both use the shared Cartesian
renderer and retain the existing accumulation and axis behavior.

## View selection and compatibility

`Surface`, `Heatmap`, `Contour`, and `Complex` explicitly select their respective
views. `Plot`, `PlotComplex`, and `Image` select the Cartesian 2-D view. Switching
views retains the other data. `Heatmap`, `Contour`, and `Complex` replace their
active field; `Surface` appends to the retained surface collection.

`Clear()` removes all data, selects the Cartesian 2-D view, and resets
X/Y/Z ranges to [-5, 5]. As before, labels, style, and other settings are retained.
Automatic ranges are calculated when rendering and are shared Figure state.

Existing public signatures, enum values, series accumulation, zero-based implicit
X coordinates, and range rules are unchanged. `Scatter` with `ShapeType.None`
still connects points, and `Image()` still uses the image dimensions for axis
ranges even when `AutoRange` is disabled. `EqualFieldAxes` continues to apply
only to field views.

The appearance of older 2-D plots is intentionally updated to the common
scientific style. Image plots now show the grid over the image when `Grid.Show`
is enabled. Previously generated images are not expected to be pixel-identical.
No new package dependencies were added.

The implementation separates public figure state, shared presentation, and data
rendering: `Figure.Rendering.cs` provides the common pipeline; `FigureLayout`
reserves space for decorations; `PlotRenderer`, `FieldRenderer`, and
`SurfaceRenderer` draw their respective data. `Figure.Cartesian.cs` retains the
Cartesian range rules and legend, while `Figure.Surface.cs` projects 3-D axes.

## Scope

3-D uses an orthographic, software-rendered view of opaque surfaces on rectangular
grids. Camera changes are programmatic: update `View3D`, then call `To()` again.
This release does not add a window, mouse navigation, perspective, translucent
faces, arbitrary triangulated meshes, or 3-D line plots. Rendering cost grows
with image size and grid density; use modest grids for frequently refreshed plots.

LaTeX and vector export remain outside this change. Existing bitmap export works
as before. Color palettes and shading are provided for scientific use, without
claiming pixel-for-pixel MATLAB rendering.

## Runnable examples

From the repository root on Windows:

```shell
dotnet run --project samples/ScientificFigures -c Release
```

This creates line, stem, scatter, image, Surface, Mesh, Heatmap/Contour, complex
domain, complex surface, phase, and overview PNGs in `artifacts/scientific-figures/`.
Pass an output directory after `--` to change that location.
