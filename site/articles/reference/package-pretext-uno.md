---
title: "Package: Pretext.Uno"
---

# Package: Pretext.Uno

`Pretext.Uno` is the Uno-specific companion package. It no longer owns the platform-neutral layout helpers; those now live in `Pretext.Layout`.

## Install it when

- your application targets Uno Platform
- you want the reusable Uno-side controls and render scheduling helpers from this repository
- you want the core engine, helper layer, and first-party backend set brought in transitively

## What it brings

`Pretext.Uno` depends on:

- `Pretext`
- `Pretext.Layout`
- `Pretext.DirectWrite`
- `Pretext.FreeType`
- `Pretext.CoreText`
- `Pretext.SkiaSharp`

At runtime, `Pretext` then selects the best supported backend for the current OS.

## Public Uno-specific surface

The package exposes:

- `StretchScrollHost`
- `UiRenderScheduler`

Use `StretchScrollHost` for sample-style scroll surfaces that need stretched content and viewport calculations. Use `UiRenderScheduler` when several UI events can trigger a redraw and you want one coalesced dispatch callback.

## What moved out

The wrap and obstacle helpers were extracted into `Pretext.Layout`, so non-Uno hosts can reuse:

- `PreparedTextMetrics`
- `ColumnFlowLayout`
- `ObstacleLayoutHelper`
- related geometry primitives

## Read next

- [Integrating in Uno](../guides/integrating-in-uno)
- [Companion Helpers](pretext-uno-helpers)
- [Package: Pretext.Layout](package-pretext-layout)
