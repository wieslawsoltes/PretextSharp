---
title: "Package: Pretext.Layout"
---

# Package: Pretext.Layout

`Pretext.Layout` is the platform-neutral helper package layered on top of `Pretext`. It extracts the reusable non-Uno layout helpers that used to live only in the sample hosts and companion package.

## Install it when

- you want shrinkwrap, wrap-metric, column-flow, or obstacle helpers outside Uno
- you are building a native or custom-rendered host that still wants the higher-level layout utilities
- you want to share layout helpers between multiple UI hosts

## Public surface

The package exposes:

- `PreparedTextMetrics`
- `ColumnFlowLayout`
- `ObstacleLayoutHelper`
- `WrapMetrics`
- `PositionedLine`
- `RectObstacle`
- `CircleObstacle`
- `Interval`

## What the helpers are for

`PreparedTextMetrics` handles common wrap calculations:

- widest visible line
- exact line count plus max line width
- tighter wrap widths via binary search
- end-of-text checks for streamed layout loops

`ColumnFlowLayout` and `ObstacleLayoutHelper` help you:

- carve usable width intervals around obstacles
- place lines into columns or constrained bands
- build editorial or document-like flows on top of `LayoutNextLine`

## Relationship to other packages

- depends on `Pretext`
- has no Uno Platform dependency
- is brought in transitively by `Pretext.Uno`

## Read next

- [Companion Helpers](pretext-uno-helpers)
- [Shrinkwrap and Editorial Layout](../guides/shrinkwrap-and-editorial-layout)
- [Sample Hosts and Shared Assets](../guides/sample-hosts-and-shared-assets)
