---
title: "Choosing an API"
---

# Choosing an API

`Pretext` exposes multiple preparation and layout entry points because different workloads need different tradeoffs.

## Step 1: choose the preparation shape

| Use this | When you need | Cost profile |
| --- | --- | --- |
| `Prepare` | Line count and height only | Lowest memory footprint |
| `PrepareWithSegments` | Segment metadata, line strings, or streamed iteration | Higher memory footprint, richer output |

If you are unsure, start with `PrepareWithSegments` during prototyping and move to `Prepare` later if a hot path only needs aggregate metrics.

## Step 2: choose the layout shape

| Use this | Output | Best for |
| --- | --- | --- |
| `Layout` | `LayoutResult` | Measurement passes, card sizing, line count probes |
| `LayoutWithLines` | `LayoutLinesResult` | Rendering full line text, diagnostics, exports |
| `LayoutNextLine` | `LayoutLine?` | Custom loops, obstacles, columns, stop-early flows |
| `WalkLineRanges` | line widths + cursors only | Geometry-only iteration with fewer allocations |

## Typical choices

| Scenario | Recommended flow |
| --- | --- |
| Measure a card height from text and width | `Prepare` -> `Layout` |
| Draw a paragraph line-by-line in a custom canvas | `PrepareWithSegments` -> `LayoutWithLines` |
| Flow text through columns or around obstacles | `PrepareWithSegments` -> repeated `LayoutNextLine` |
| Find the widest visible line without allocating strings | `PrepareWithSegments` -> `WalkLineRanges` |
| Profile how expensive preparation is | `ProfilePrepare` |

## What causes a re-prepare

Recreate prepared text when any of these change:

- the input text
- the `font` string
- the `WhiteSpaceMode`
- the locale selected through `SetLocale`

Do not recreate prepared text when only these change:

- available width
- line height
- the placement of lines in your UI

## Rule of thumb

If your code needs the actual line text, segment kinds, or cursor positions, prepare with segments. Otherwise, stay on the smaller `Prepare` + `Layout` path.
