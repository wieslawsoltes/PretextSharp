---
title: "Incremental Layout APIs"
---

# Incremental Layout APIs

`Pretext` exposes more than one way to consume layout because different consumers need different output shapes.

## Aggregate result

Use `Layout` when you only need:

- line count
- total height

## Materialized lines

Use `LayoutWithLines` when you need:

- the actual line text
- the painted width of each line
- the start and end cursors for each line

This is the easiest API to use, but it allocates strings for every line.

## Streaming line walk

Use `LayoutNextLine` when you want to drive your own loop and stop early, and use `WalkLineRanges` when you only need geometry without allocating full line strings.

## Decision table

| API | Returns | Best for |
| --- | --- | --- |
| `Layout` | `LayoutResult` | fast measurement passes |
| `LayoutWithLines` | `LayoutLinesResult` | simple rendering and diagnostics |
| `LayoutNextLine` | one `LayoutLine` at a time | columns, obstacles, stop-early custom loops |
| `WalkLineRanges` | widths and cursors only | allocation-sensitive geometry passes |

## Cursor-based layout

`LayoutNextLine` advances through a paragraph using `LayoutCursor`:

- `SegmentIndex` identifies the current prepared segment
- `GraphemeIndex` identifies the offset inside a breakable segment

That is what makes overlong words, discretionary hyphens, and obstacle-aware layout work without losing alignment with `LayoutWithLines`.

## Visible width vs fit width

Some segment kinds behave differently when they are fitted versus painted:

- collapsible spaces may contribute to fit decisions but not remain visible at line end
- tabs and preserved spaces can remain visible in `PreWrap`
- soft hyphens add width only when a break is actually taken there

That difference is already reflected in the public `LayoutLine.Width` and `LayoutLineRange.Width` values.

Those APIs are what make the sample gallery possible: bubbles, masonry cards, and editorial layouts can reuse the same prepared text but consume it differently.
