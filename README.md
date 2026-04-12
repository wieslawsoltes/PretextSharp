# Pretext

[![CI](https://github.com/wieslawsoltes/PretextSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/wieslawsoltes/PretextSharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Pretext.svg)](https://www.nuget.org/packages/Pretext)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.svg)](https://www.nuget.org/packages/Pretext)
[![Docs](https://img.shields.io/badge/docs-github%20pages-0f766e)](https://wieslawsoltes.github.io/PretextSharp/)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
![SkiaSharp 3.119.1](https://img.shields.io/badge/SkiaSharp-3.119.1-16A34A)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Universal text preparation and line layout for any SkiaSharp-based UI, with grapheme-aware wrapping, locale-aware segmentation, bidi support, and streaming line walking.

Documentation site: [wieslawsoltes.github.io/PretextSharp](https://wieslawsoltes.github.io/PretextSharp/)

Key documentation:

- [Getting Started Overview](https://wieslawsoltes.github.io/PretextSharp/articles/getting-started/overview/)
- [Quickstart: Prepare and Layout](https://wieslawsoltes.github.io/PretextSharp/articles/getting-started/quickstart-prepare-and-layout/)
- [Choosing an API](https://wieslawsoltes.github.io/PretextSharp/articles/getting-started/choosing-an-api/)
- [Prepared Text Lifecycle](https://wieslawsoltes.github.io/PretextSharp/articles/concepts/prepared-text-lifecycle/)
- [Public Types and Operations](https://wieslawsoltes.github.io/PretextSharp/articles/reference/public-types-and-operations/)
- [Pretext.Uno Helpers](https://wieslawsoltes.github.io/PretextSharp/articles/reference/pretext-uno-helpers/)

## NuGet Packages

| Package Name | NuGet | Downloads | Description |
| --- | --- | --- | --- |
| `Pretext` | [![NuGet](https://img.shields.io/nuget/v/Pretext.svg)](https://www.nuget.org/packages/Pretext) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.svg)](https://www.nuget.org/packages/Pretext) | Universal text preparation and line layout engine for any SkiaSharp-based UI. |
| `Pretext.Uno` | [![NuGet](https://img.shields.io/nuget/v/Pretext.Uno.svg)](https://www.nuget.org/packages/Pretext.Uno) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.Uno.svg)](https://www.nuget.org/packages/Pretext.Uno) | Uno-specific controls and obstacle-aware layout helpers built on top of `Pretext`. |

## Features

- Prepare text once and reuse the result across repeated layout passes with `Prepare` and `PrepareWithSegments`.
- Compute fast aggregate metrics with `Layout`, or materialize full line data with `LayoutWithLines`.
- Stream line geometry incrementally with `LayoutNextLine` and `WalkLineRanges` for custom layout engines.
- Handle ordinary spaces, preserved spaces, tabs, hard breaks, non-breaking spaces, zero-width breaks, and soft hyphens.
- Support multilingual text with locale-aware segmentation on desktop targets and bidi-aware segment levels.
- Depend only on `SkiaSharp` in the core library so the package can be used outside Uno as well.
- Ship with a published `Pretext.Uno` companion library for reusable Uno host controls and obstacle-aware flow helpers.
- Ship with deterministic parity tests and a Uno sample app that demonstrates bubbles, masonry, editorial, and justification layouts.

## Core API

| API | Purpose |
| --- | --- |
| `Prepare` | Prepare text for repeated layout when you only need aggregate metrics. |
| `PrepareWithSegments` | Prepare text and expose segments, widths, break metadata, and segment levels. |
| `Layout` | Return line count and total height for a given width and line height. |
| `LayoutWithLines` | Return materialized line text and line widths. |
| `LayoutNextLine` | Stream the next line from a given cursor for custom layout flows. |
| `WalkLineRanges` | Iterate line geometry without allocating full line text. |
| `ProfilePrepare` | Measure preparation cost for profiling and diagnostics. |
| `SetLocale` | Override locale-sensitive segmentation behavior when needed. |
| `ClearCache` | Reset cached font state and prepared segment text caches. |

## API Selection

| Need | Start with |
| --- | --- |
| Line count and total height only | `Prepare` + `Layout` |
| Actual line text and widths | `PrepareWithSegments` + `LayoutWithLines` |
| One line at a time in a custom loop | `PrepareWithSegments` + `LayoutNextLine` |
| Geometry only, fewer allocations | `PrepareWithSegments` + `WalkLineRanges` |
| Preparation cost diagnostics | `ProfilePrepare` |

## Quick Start

Install from NuGet:

```bash
dotnet add package Pretext
```

Then prepare and lay out text:

```csharp
using Pretext;

const string text = "Hello soft\u00ADwrapped world";
const string font = "16px Inter";
const double lineHeight = 20;

var prepared = PretextLayout.PrepareWithSegments(text, font);
var metrics = PretextLayout.Layout(prepared, maxWidth: 160, lineHeight);
var lines = PretextLayout.LayoutWithLines(prepared, maxWidth: 160, lineHeight);

Console.WriteLine(metrics.LineCount);
Console.WriteLine(metrics.Height);

foreach (var line in lines.Lines)
{
    Console.WriteLine($"{line.Text} ({line.Width})");
}
```

The core package exposes the `Pretext` namespace and is not tied to Uno. Use it anywhere you use SkiaSharp.

The `font` argument is a CSS-like subset such as `16px Inter`, `italic 16px Georgia`, or `700 18px "IBM Plex Sans"`. Line height is supplied separately to layout calls.

Use `WhiteSpaceMode.PreWrap` when your layout needs preserved spaces, tabs, or hard breaks:

```csharp
var prepared = PretextLayout.PrepareWithSegments(
    "foo\tbar\nbaz",
    "16px Inter",
    new PrepareOptions(WhiteSpaceMode.PreWrap));
```

## Uno Companion

Install the Uno companion package when you want the reusable Uno-specific helpers on top of the core engine:

```bash
dotnet add package Pretext.Uno
```

It brings the `Pretext` core package transitively and exposes:

- `Pretext.PretextLayout`
- `Pretext.Uno.Controls.StretchScrollHost`
- `Pretext.Uno.Controls.UiRenderScheduler`
- `Pretext.Uno.Layout.PreparedTextMetrics`
- `Pretext.Uno.Layout.ColumnFlowLayout`
- `Pretext.Uno.Layout.ObstacleLayoutHelper`

## Sample App

The sample app lives in `samples/PretextSamples` and demonstrates the library in visually different layouts. It uses Uno Platform, exercises the core `Pretext` APIs together with the `Pretext.Uno` companion helpers, and keeps only sample-specific UI/theme code in the sample tree.

- Overview
- Accordion
- Bubbles
- Masonry
- Rich Text
- Dynamic Layout
- Editorial Engine
- Justification Comparison
- Variable ASCII

Run it with:

```bash
dotnet run --project samples/PretextSamples/PretextSamples.csproj -f net10.0-desktop
```

## Building

### Prerequisites

- .NET 10 SDK
- Uno.Sdk 6.5.x for the sample app only

### Build, test, and pack

```bash
dotnet build PretextSamples.slnx
dotnet test tests/Pretext.Uno.Tests/Pretext.Uno.Tests.csproj
dotnet pack src/Pretext/Pretext.csproj -c Release
dotnet pack src/Pretext.Uno/Pretext.Uno.csproj -c Release
```

## Docs and CI

The repository includes:

- `ci.yml` for multi-platform build, test, docs validation, and preview package generation
- `docs.yml` for GitHub Pages deployment
- `release.yml` for tag-driven packing, optional NuGet publication, and GitHub release creation
- a Lunet docs site in `site/`

The docs cover:

- installation and namespace/package selection
- font strings, measurement, and prepared-text lifecycle
- whitespace and break behavior, locale-aware segmentation, and bidi
- practical Uno and generic SkiaSharp integration patterns
- full reference coverage for the public core API and `Pretext.Uno` helpers

## Project Structure

```text
src/
  Pretext/
  Pretext.Uno/
tests/
  Pretext.Uno.Tests/
samples/
  PretextSamples/
site/
```

## License

MIT. See [LICENSE](https://github.com/wieslawsoltes/PretextSharp/blob/main/LICENSE).
