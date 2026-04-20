---
title: "Package: Pretext.SkiaSharp"
---

# Package: Pretext.SkiaSharp

`Pretext.SkiaSharp` is the first-party portable backend for SkiaSharp-based hosts and the fallback backend when no host-native backend applies.

## Install it when

- your app already uses SkiaSharp for drawing
- you want a portable measurement backend that works across OSes
- you want a fallback backend even when native backends are also referenced

## Runtime behavior

The package advertises `SkiaSharpTextMeasurerFactory` to the core engine.

- `Name`: `SkiaSharp`
- `IsSupported`: always `true`
- `Priority`: `0`

That intentionally makes it the portable fallback rather than the preferred backend on Windows, Linux, or macOS when one of the native backends is also present.

## Typical hosts

- `SKCanvasView` or `SKGLView` controls
- SkiaSharp-based desktop applications
- reporting or export flows that already measure and draw through SkiaSharp

## Relationship to other packages

- depends on `Pretext.Contracts` and `SkiaSharp`
- can be used by itself with `Pretext`
- is brought in transitively by `Pretext.Uno`

## Read next

- [Using Pretext in Any SkiaSharp Host](../guides/using-pretext-in-any-skiasharp-host)
- [Backend Discovery and Overrides](../guides/backend-discovery-and-overrides)
