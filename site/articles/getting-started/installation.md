---
title: "Installation"
---

# Installation

Install the package set that matches the layer and backend you need.

## Core engine

```bash
dotnet add package Pretext
```

Then import the core namespace:

```csharp
using Pretext;
```

Add `Pretext` plus one or more backends when you want to:

- prepare text once and reuse it across many width probes
- compute line counts or line strings in a custom control
- drive your own native or SkiaSharp drawing code
- use the engine outside Uno entirely

First-party backend packages:

```bash
dotnet add package Pretext.DirectWrite   # Windows
dotnet add package Pretext.FreeType      # Linux
dotnet add package Pretext.CoreText      # macOS
dotnet add package Pretext.SkiaSharp     # portable fallback / Skia hosts
```

The core `Pretext` package targets:

- `netstandard2.0`
- `net461`
- `net6.0`
- `net8.0`
- `net10.0`

`Pretext.Contracts` is available when you want to implement a custom measurement backend instead of using one of the first-party backends.

## Platform-neutral helper package

```bash
dotnet add package Pretext.Layout
```

Import the helper namespace when you want the wrap-metric and obstacle-layout utilities without any Uno dependency:

```csharp
using Pretext.Layout;
```

Use `Pretext.Layout` when you want:

- `PreparedTextMetrics`
- `ColumnFlowLayout`
- `ObstacleLayoutHelper`
- geometry primitives such as `RectObstacle`, `CircleObstacle`, and `Interval`

## Uno companion

```bash
dotnet add package Pretext.Uno
```

`Pretext.Uno` depends on `Pretext`, `Pretext.Layout`, and the first-party backend packages, so the core engine, helper layer, and host-native/portable backend set are brought in transitively. The companion package itself remains `net10.0-desktop`. Backend discovery then prefers the native backend for the current OS and falls back to `Pretext.SkiaSharp` when needed. Import the core namespace and whichever companion namespaces you want:

```csharp
using Pretext;
using Pretext.Layout;
using Pretext.Uno.Controls;
```

Use the companion package when you want the reusable Uno-side controls from this repository:

- `StretchScrollHost`
- `UiRenderScheduler`

## Font string format

Every preparation entry point accepts a `font` string. First-party backends share the same CSS-like parser from `Pretext.Contracts`:

- a size in `px`, for example `16px`
- an optional weight, for example `700 16px Inter`
- optional `italic` or `oblique`
- a family list, where only the first family is used

Examples:

```csharp
"16px Inter"
"italic 16px Georgia"
"700 18px \"IBM Plex Sans\""
"600 15px system-ui"
```

Line height is **not** read from the font string. Pass it separately to `Layout` or `LayoutWithLines`.

## Build the repo

```bash
dotnet build PretextSamples.slnx
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

## Sample app

The interactive demos live in `samples/PretextSamples`. Use that app when you want to see how the library behaves in real layouts before integrating it into your own UI.

## Read next

- [Quickstart: Prepare and Layout](quickstart-prepare-and-layout)
- [Choosing an API](choosing-an-api)
- [Font Strings and Measurement](../concepts/font-strings-and-measurement)
