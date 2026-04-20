---
title: "Package: Pretext"
---

# Package: Pretext

`Pretext` is the backend-agnostic engine package. It owns preparation, segmentation, bidi handling, break analysis, and the line-fitting APIs that the rest of the repository builds on.

## Install it when

- you want deterministic text preparation and line layout without taking a UI framework dependency
- you are building your own control, canvas, export pipeline, or layout engine
- you want to pair the core engine with one or more first-party or custom backends

## Package role

`Pretext` owns:

- `PretextLayout`
- prepared-text creation with `Prepare` and `PrepareWithSegments`
- aggregate layout with `Layout`
- materialized lines with `LayoutWithLines`
- streamed layout with `LayoutNextLine`, `LayoutNextLineRange`, and `WalkLineRanges`
- rich-inline preparation and streamed materialization
- backend discovery and explicit backend override

## What it does not own

`Pretext` does not draw text. Your host still owns:

- painting and antialiasing
- baseline placement
- line-height policy
- hit testing and selection
- scroll/view invalidation

## Package relationship

`Pretext` depends on `Pretext.Contracts` and expects a text-measurement factory to be available at runtime.

Typical pairings:

- `Pretext` + `Pretext.DirectWrite` for Windows-native hosts
- `Pretext` + `Pretext.FreeType` for Linux-native hosts
- `Pretext` + `Pretext.CoreText` for macOS-native hosts
- `Pretext` + `Pretext.SkiaSharp` for portable SkiaSharp hosts or as a fallback

## Key runtime behavior

If you do not call `PretextLayout.SetTextMeasurerFactory(...)`, the engine discovers factories from referenced `Pretext*.dll` assemblies, filters them by `IsSupported`, then picks the supported factory with the highest `Priority`.

That means:

- host-native backends win on their matching OS
- `Pretext.SkiaSharp` stays available as the low-priority portable fallback
- custom or test backends can still be selected explicitly

## Read next

- [Package: Pretext.Contracts](package-pretext-contracts)
- [Backend Discovery and Overrides](../guides/backend-discovery-and-overrides)
- [Public Types and Operations](public-types-and-operations)
