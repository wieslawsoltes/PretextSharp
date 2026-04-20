---
title: "Packages and Namespace"
---

# Packages and Namespace

## Published packages

- `Pretext`
  - source: `src/Pretext/Pretext.csproj`
  - primary namespace: `Pretext`
  - target frameworks: `netstandard2.0`, `net461`, `net6.0`, `net8.0`, `net10.0`
  - contains the core preparation, bidi, and line-layout pipeline plus backend selection

- `Pretext.Contracts`
  - source: `src/Pretext.Contracts/Pretext.Contracts.csproj`
  - primary namespace: `Pretext`
  - target frameworks: `netstandard2.0`, `net461`, `net6.0`, `net8.0`, `net10.0`
  - contains the public backend contracts used by `Pretext` and backend packages

- `Pretext.Layout`
  - source: `src/Pretext.Layout/Pretext.Layout.csproj`
  - primary namespace: `Pretext.Layout`
  - target frameworks: `netstandard2.0`, `net461`, `net6.0`, `net8.0`, `net10.0`
  - contains platform-neutral wrap metrics, column-flow, and obstacle-layout helpers

- `Pretext.DirectWrite`
  - source: `src/Pretext.DirectWrite/Pretext.DirectWrite.csproj`
  - primary namespace: `Pretext.DirectWrite`
  - target frameworks: `netstandard2.0`, `net461`, `net6.0`, `net8.0`, `net10.0`
  - contains the first-party Windows DirectWrite text-measurement backend

- `Pretext.FreeType`
  - source: `src/Pretext.FreeType/Pretext.FreeType.csproj`
  - primary namespace: `Pretext.FreeType`
  - target frameworks: `netstandard2.0`, `net461`, `net6.0`, `net8.0`, `net10.0`
  - contains the first-party Linux FreeType + Fontconfig text-measurement backend

- `Pretext.CoreText`
  - source: `src/Pretext.CoreText/Pretext.CoreText.csproj`
  - primary namespace: `Pretext.CoreText`
  - target frameworks: `netstandard2.0`, `net461`, `net6.0`, `net8.0`, `net10.0`
  - contains the first-party macOS CoreText text-measurement backend

- `Pretext.SkiaSharp`
  - source: `src/Pretext.SkiaSharp/Pretext.SkiaSharp.csproj`
  - primary namespace: `Pretext.SkiaSharp`
  - target frameworks: `netstandard2.0`, `net461`, `net6.0`, `net8.0`, `net10.0`
  - contains the portable SkiaSharp text-measurement backend and fallback

- `Pretext.Uno`
  - source: `src/Pretext.Uno/Pretext.Uno.csproj`
  - primary namespace: `Pretext.Uno.Controls`
  - target framework: `net10.0-desktop`
  - contains reusable Uno host controls layered on top of the core `Pretext` namespace and `Pretext.Layout`

## Test project

- `tests/Pretext.Uno.Tests/Pretext.Uno.Tests.csproj`
- validates preparation and layout behavior under deterministic measurement

## Sample project

- `samples/PretextSamples.Shared/PretextSamples.Shared.csproj`
- `samples/PretextSamples/PretextSamples.csproj`
- `samples/PretextSamples.MacOS/PretextSamples.MacOS.csproj`
- together they provide the shared sample layer plus Uno and native macOS hosts

## Core entry point

The public API is centered on `PretextLayout` and the data types around it:

- `PreparedText`
- `PreparedTextWithSegments`
- `LayoutResult`
- `LayoutLinesResult`
- `LayoutLine`
- `LayoutLineRange`

## Relationship between the packages

The packages are layered:

1. `Pretext` is the engine. It is reusable with any measurement backend that implements `Pretext.Contracts`.
2. `Pretext.Contracts` defines the public backend seam for custom measurement integrations.
3. `Pretext.Layout` is the platform-neutral helper layer for wrap metrics and obstacle-aware flows.
4. `Pretext.DirectWrite`, `Pretext.FreeType`, and `Pretext.CoreText` are the first-party host-native backends.
5. `Pretext.SkiaSharp` is the portable first-party fallback backend.
6. `Pretext.Uno` is a host-specific companion layer. It depends on `Pretext`, `Pretext.Layout`, and the first-party backend packages.

For package-specific installation and usage details, continue into the dedicated package pages in this section.

If you are documenting or reviewing behavior, treat the core package as the source of truth for:

- segmentation
- backend-independent layout behavior
- wrapping
- streamed line walking

Treat `Pretext.Layout` as the reusable helper package for higher-level layout workflows, and `Pretext.Uno` as the convenience package for Uno-specific host integration.
