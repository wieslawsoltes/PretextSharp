# Pretext

[![CI](https://github.com/wieslawsoltes/PretextSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/wieslawsoltes/PretextSharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Pretext.svg)](https://www.nuget.org/packages/Pretext)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.svg)](https://www.nuget.org/packages/Pretext)
[![Docs](https://img.shields.io/badge/docs-github%20pages-0f766e)](https://wieslawsoltes.github.io/PretextSharp/)
![Targets](https://img.shields.io/badge/targets-netstandard2.0%20%7C%20net461%20%7C%20net6.0%20%7C%20net8.0%20%7C%20net10.0-512BD4)
![SkiaSharp 3.119.1](https://img.shields.io/badge/SkiaSharp-3.119.1-16A34A)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Universal text preparation and line layout with grapheme-aware wrapping, locale-aware segmentation, bidi support, and pluggable text-measurement backends.

The core `Pretext` package targets `netstandard2.0`, `net461`, `net6.0`, `net8.0`, and `net10.0`. The `Pretext.Uno` companion package and sample app remain `net10.0-desktop`.

PretextSharp is a .NET/C# port of the original [pretext](https://github.com/chenglou/pretext) project by [Cheng Lou](https://github.com/chenglou).

Documentation site: [wieslawsoltes.github.io/PretextSharp](https://wieslawsoltes.github.io/PretextSharp/)

Key documentation:

- [Getting Started Overview](https://wieslawsoltes.github.io/PretextSharp/articles/getting-started/overview/)
- [Quickstart: Prepare and Layout](https://wieslawsoltes.github.io/PretextSharp/articles/getting-started/quickstart-prepare-and-layout/)
- [Choosing an API](https://wieslawsoltes.github.io/PretextSharp/articles/getting-started/choosing-an-api/)
- [Prepared Text Lifecycle](https://wieslawsoltes.github.io/PretextSharp/articles/concepts/prepared-text-lifecycle/)
- [Public Types and Operations](https://wieslawsoltes.github.io/PretextSharp/articles/reference/public-types-and-operations/)
- [Rich Inline API](https://wieslawsoltes.github.io/PretextSharp/articles/reference/rich-inline-api/)
- [Companion Helpers](https://wieslawsoltes.github.io/PretextSharp/articles/reference/pretext-uno-helpers/)

## NuGet Packages

| Package Name | NuGet | Downloads | Description |
| --- | --- | --- | --- |
| `Pretext` | [![NuGet](https://img.shields.io/nuget/v/Pretext.svg)](https://www.nuget.org/packages/Pretext) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.svg)](https://www.nuget.org/packages/Pretext) | Backend-agnostic text preparation and line layout engine. |
| `Pretext.Contracts` | [![NuGet](https://img.shields.io/nuget/v/Pretext.Contracts.svg)](https://www.nuget.org/packages/Pretext.Contracts) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.Contracts.svg)](https://www.nuget.org/packages/Pretext.Contracts) | Contracts for implementing custom text-measurement backends. |
| `Pretext.Layout` | [![NuGet](https://img.shields.io/nuget/v/Pretext.Layout.svg)](https://www.nuget.org/packages/Pretext.Layout) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.Layout.svg)](https://www.nuget.org/packages/Pretext.Layout) | Platform-neutral wrap and obstacle-layout helpers built on top of `Pretext`. |
| `Pretext.DirectWrite` | [![NuGet](https://img.shields.io/nuget/v/Pretext.DirectWrite.svg)](https://www.nuget.org/packages/Pretext.DirectWrite) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.DirectWrite.svg)](https://www.nuget.org/packages/Pretext.DirectWrite) | First-party DirectWrite backend for Windows hosts. |
| `Pretext.FreeType` | [![NuGet](https://img.shields.io/nuget/v/Pretext.FreeType.svg)](https://www.nuget.org/packages/Pretext.FreeType) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.FreeType.svg)](https://www.nuget.org/packages/Pretext.FreeType) | First-party FreeType + Fontconfig backend for Linux hosts. |
| `Pretext.CoreText` | [![NuGet](https://img.shields.io/nuget/v/Pretext.CoreText.svg)](https://www.nuget.org/packages/Pretext.CoreText) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.CoreText.svg)](https://www.nuget.org/packages/Pretext.CoreText) | First-party CoreText backend for macOS hosts. |
| `Pretext.SkiaSharp` | [![NuGet](https://img.shields.io/nuget/v/Pretext.SkiaSharp.svg)](https://www.nuget.org/packages/Pretext.SkiaSharp) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.SkiaSharp.svg)](https://www.nuget.org/packages/Pretext.SkiaSharp) | First-party SkiaSharp backend for `Pretext`. |
| `Pretext.Uno` | [![NuGet](https://img.shields.io/nuget/v/Pretext.Uno.svg)](https://www.nuget.org/packages/Pretext.Uno) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Pretext.Uno.svg)](https://www.nuget.org/packages/Pretext.Uno) | Uno-specific controls and render scheduling helpers built on top of `Pretext`. |

## Features

- Prepare text once and reuse the result across repeated layout passes with `Prepare` and `PrepareWithSegments`.
- Compute fast aggregate metrics with `Layout`, or materialize full line data with `LayoutWithLines`.
- Stream line geometry incrementally with `LayoutNextLine`, `LayoutNextLineRange`, and `WalkLineRanges` for custom layout engines.
- Re-materialize text lazily with `MaterializeLineRange` after cheap geometry-only probing.
- Measure widest-line geometry without allocating text via `MeasureLineStats` and `MeasureNaturalWidth`.
- Handle ordinary spaces, preserved spaces, tabs, hard breaks, non-breaking spaces, zero-width breaks, and soft hyphens.
- Support `WordBreakMode.KeepAll` for CJK-focused no-space wrapping behavior.
- Build rich inline flows with `PrepareRichInline`, `WalkRichInlineLineRanges`, and `MaterializeRichInlineLineRange`.
- Support multilingual text with locale-aware segmentation on desktop targets and bidi-aware segment levels.
- Keep the core library graphics-backend agnostic through `Pretext.Contracts`.
- Ship first-party native backends for Windows (`Pretext.DirectWrite`), Linux (`Pretext.FreeType`), and macOS (`Pretext.CoreText`), plus the portable `Pretext.SkiaSharp` fallback backend.
- Ship with a published `Pretext.Layout` helper library for platform-neutral wrap and obstacle-layout workflows.
- Ship with a published `Pretext.Uno` companion library for reusable Uno host controls and render scheduling helpers.
- Ship with deterministic parity tests and a Uno sample app that demonstrates bubbles, masonry, editorial, justification, rich-inline, and virtualized markdown chat layouts.

## Core API

| API | Purpose |
| --- | --- |
| `Prepare` | Prepare text for repeated layout when you only need aggregate metrics. |
| `PrepareWithSegments` | Prepare text and expose segments, widths, break metadata, and segment levels. |
| `Layout` | Return line count and total height for a given width and line height. |
| `LayoutWithLines` | Return materialized line text and line widths. |
| `LayoutNextLine` | Stream the next line from a given cursor for custom layout flows. |
| `LayoutNextLineRange` | Stream geometry-only line ranges without materializing text. |
| `MaterializeLineRange` | Turn a `LayoutLineRange` back into a materialized `LayoutLine`. |
| `WalkLineRanges` | Iterate line geometry without allocating full line text. |
| `MeasureLineStats` | Return line count plus widest line width for a prepared block. |
| `MeasureNaturalWidth` | Return the widest unwrapped line width for prepared text. |
| `PrepareRichInline` | Prepare multi-item inline flow with collapsed boundary whitespace and atomic items. |
| `WalkRichInlineLineRanges` | Stream rich-inline line ranges without materializing fragment text. |
| `MaterializeRichInlineLineRange` | Materialize one streamed rich-inline line when you actually need fragment text. |
| `MeasureRichInlineStats` | Measure rich-inline line count and max line width. |
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
| Rich inline fragments with atomic chips or badges | `PrepareRichInline` + `WalkRichInlineLineRanges` |
| Preparation cost diagnostics | `ProfilePrepare` |

## Quick Start

Install the engine plus one or more backends:

```bash
dotnet add package Pretext
dotnet add package Pretext.SkiaSharp
```

Optional host-native backends:

```bash
dotnet add package Pretext.DirectWrite   # Windows
dotnet add package Pretext.FreeType      # Linux
dotnet add package Pretext.CoreText      # macOS
```

Supported target frameworks for the core package:

- `netstandard2.0`
- `net461`
- `net6.0`
- `net8.0`
- `net10.0`

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

If the prepared text is empty after normalization, `Layout` returns `new LayoutResult(0, 0)`. If a container in your UI must still reserve one visual row, clamp with `Math.Max(1, metrics.LineCount)` in the caller instead of expecting `Pretext` to synthesize a blank line.

The core package exposes the `Pretext` namespace and is not tied to Uno. Add one or more backend packages in non-Uno hosts so measurement can be provided automatically. When multiple first-party backends are referenced, `Pretext` prefers the host-native backend on its matching OS and falls back to `Pretext.SkiaSharp` otherwise.

The `font` argument is a CSS-like subset such as `16px Inter`, `italic 16px Georgia`, or `700 18px "IBM Plex Sans"`. Line height is supplied separately to layout calls.

Use `WhiteSpaceMode.PreWrap` when your layout needs preserved spaces, tabs, or hard breaks:

```csharp
var prepared = PretextLayout.PrepareWithSegments(
    "foo\tbar\nbaz",
    "16px Inter",
    new PrepareOptions(WhiteSpaceMode.PreWrap));
```

Use `WordBreakMode.KeepAll` when CJK-heavy text should avoid ordinary intra-run breaks:

```csharp
var prepared = PretextLayout.PrepareWithSegments(
    "日本語foo-bar",
    "16px Inter",
    new PrepareOptions(WordBreak: WordBreakMode.KeepAll));
```

Use the rich-inline helper when paragraph text and atomic inline boxes must share one flow:

```csharp
var flow = PretextLayout.PrepareRichInline(
[
    new RichInlineItem("Ship ", "16px Inter"),
    new RichInlineItem("@maya", "700 12px Inter", RichInlineBreakMode.Never, extraWidth: 18),
    new RichInlineItem("'s note wraps cleanly.", "16px Inter"),
]);

PretextLayout.WalkRichInlineLineRanges(flow, 180, line =>
{
    var materialized = PretextLayout.MaterializeRichInlineLineRange(flow, line);
    Console.WriteLine(string.Join("", materialized.Fragments.Select(f => f.Text)));
});
```

## Companion Packages

Install the platform-neutral layout-helper package when you want reusable wrap-metric and obstacle-flow helpers outside Uno as well:

```bash
dotnet add package Pretext.Layout
```

It exposes:

- `Pretext.Layout.PreparedTextMetrics`
- `Pretext.Layout.ColumnFlowLayout`
- `Pretext.Layout.ObstacleLayoutHelper`
- `Pretext.Layout.WrapMetrics`
- `Pretext.Layout.PositionedLine`

## Uno Companion

Install the Uno companion package when you want the reusable Uno-specific helpers on top of the core engine:

```bash
dotnet add package Pretext.Uno
```

It brings `Pretext`, `Pretext.Layout`, `Pretext.Contracts`, `Pretext.SkiaSharp`, `Pretext.DirectWrite`, `Pretext.FreeType`, and `Pretext.CoreText` transitively, then lets backend discovery choose the best supported backend for the current OS. It exposes:

- `Pretext.PretextLayout`
- `Pretext.Layout.PreparedTextMetrics`
- `Pretext.Layout.ColumnFlowLayout`
- `Pretext.Layout.ObstacleLayoutHelper`
- `Pretext.Uno.Controls.StretchScrollHost`
- `Pretext.Uno.Controls.UiRenderScheduler`

## Sample Apps

The sample hosts share reusable data, prepared-model logic, and sample assets through `samples/PretextSamples.Shared`.

- `samples/PretextSamples` uses Uno Platform and `Pretext.Uno`
- `samples/PretextSamples.MacOS` uses native AppKit on `net10.0-macos` and binds `Pretext` explicitly to `Pretext.CoreText`

- Overview
- Accordion
- Bubbles
- Masonry
- Rich Text
- Markdown Chat
- Dynamic Layout
- Editorial Engine
- Justification Comparison
- Variable ASCII

Run the Uno host with:

```bash
dotnet run --project samples/PretextSamples/PretextSamples.csproj -f net10.0-desktop
```

Run the native macOS host with:

```bash
dotnet run --project samples/PretextSamples.MacOS/PretextSamples.MacOS.csproj -f net10.0-macos
```

## Building

### Prerequisites

- .NET 10 SDK for building this repository
- Uno.Sdk 6.5.x for the sample app only

### Build, test, and pack

```bash
dotnet build PretextSamples.slnx
dotnet build samples/PretextSamples.MacOS/PretextSamples.MacOS.csproj
dotnet test tests/Pretext.Uno.Tests/Pretext.Uno.Tests.csproj
dotnet pack src/Pretext.Contracts/Pretext.Contracts.csproj -c Release
dotnet pack src/Pretext/Pretext.csproj -c Release
dotnet pack src/Pretext.Layout/Pretext.Layout.csproj -c Release
dotnet pack src/Pretext.DirectWrite/Pretext.DirectWrite.csproj -c Release
dotnet pack src/Pretext.FreeType/Pretext.FreeType.csproj -c Release
dotnet pack src/Pretext.CoreText/Pretext.CoreText.csproj -c Release
dotnet pack src/Pretext.SkiaSharp/Pretext.SkiaSharp.csproj -c Release
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
- practical Uno, native host, and generic SkiaSharp integration patterns
- full reference coverage for the public core API and companion helper packages

## Project Structure

```text
src/
  Pretext.Contracts/
  Pretext/
  Pretext.Layout/
  Pretext.DirectWrite/
  Pretext.FreeType/
  Pretext.CoreText/
  Pretext.SkiaSharp/
  Pretext.Uno/
tests/
  Pretext.Uno.Tests/
samples/
  PretextSamples/
  PretextSamples.Shared/
  PretextSamples.MacOS/
site/
```

## Attribution

The core Pretext implementation in this repository is ported from the original [pretext](https://github.com/chenglou/pretext) project by [Cheng Lou](https://github.com/chenglou). This repository adapts that work to .NET, native and SkiaSharp backends, packaging, tests, samples, and companion Uno helpers.

## License

MIT. See [LICENSE](https://github.com/wieslawsoltes/PretextSharp/blob/main/LICENSE).
