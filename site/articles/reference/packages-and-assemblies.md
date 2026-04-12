---
title: "Packages and Namespace"
---

# Packages and Namespace

## Published packages

- `Pretext`
  - source: `src/Pretext/Pretext.csproj`
  - primary namespace: `Pretext`
  - contains the core preparation, measurement, bidi, and line-layout pipeline

- `Pretext.Uno`
  - source: `src/Pretext.Uno/Pretext.Uno.csproj`
  - primary namespaces: `Pretext.Uno.Controls`, `Pretext.Uno.Layout`
  - contains reusable Uno host controls and obstacle-aware text-flow helpers layered on top of the core `Pretext` namespace

## Test project

- `tests/Pretext.Uno.Tests/Pretext.Uno.Tests.csproj`
- validates preparation and layout behavior under deterministic measurement

## Sample project

- `samples/PretextSamples/PretextSamples.csproj`
- hosts a Uno UI that demonstrates the library in multiple layout scenarios

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

1. `Pretext` is the engine. It is reusable anywhere SkiaSharp is available.
2. `Pretext.Uno` is a host-specific companion layer. It depends on `Pretext`.

If you are documenting or reviewing behavior, treat the core package as the source of truth for:

- segmentation
- measurement
- wrapping
- streamed line walking

Treat `Pretext.Uno` as a convenience package for host integration and advanced layout helpers.
