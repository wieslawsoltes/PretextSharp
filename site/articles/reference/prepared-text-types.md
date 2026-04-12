---
title: "Prepared Text Types"
---

# Prepared Text Types

Prepared data is the main value produced by the core engine. These types describe what was measured and how later line walking can reuse it.

## `PreparedText`

Use `PreparedText` when you created it with `Prepare` and only need aggregate layout operations.

| Member | Meaning |
| --- | --- |
| `Font` | The font string used for measurement. |
| `WhiteSpace` | The white-space mode used during preparation. |
| `DiscretionaryHyphenWidth` | Width of the inserted hyphen when a soft hyphen break is taken. |
| `TabStopAdvance` | Width of one tab stop, derived from eight measured spaces. |

## `PreparedTextWithSegments`

Use `PreparedTextWithSegments` when you created it with `PrepareWithSegments` and need richer output.

| Member | Meaning |
| --- | --- |
| `Segments` | Segment text after analysis and normalization. |
| `Widths` | Measured width of each segment. |
| `LineEndFitAdvances` | Width contribution used when deciding whether a segment still fits at line end. |
| `LineEndPaintAdvances` | Width contribution used when reporting painted width at line end. |
| `Kinds` | `SegmentBreakKind` for each segment. |
| `BreakableWidths` | Per-grapheme widths for breakable runs that can wrap internally. |
| `BreakablePrefixWidths` | Prefix widths for the same breakable runs. |
| `Chunks` | Hard-break chunk boundaries. |
| `SimpleLineWalkFastPath` | Indicates whether the simplified line walker can be used internally. |
| `SegmentLevels` | Optional bidi level per segment for mixed-direction text. |

## `PreparedLineChunk`

`PreparedLineChunk` is an advanced type that reflects hard-break chunk boundaries:

| Member | Meaning |
| --- | --- |
| `StartSegmentIndex` | First segment in the chunk |
| `EndSegmentIndex` | End of the chunk before the hard break |
| `ConsumedEndSegmentIndex` | The cursor position after the break is consumed |

Most callers do not need `Chunks` directly, but they are useful when reasoning about forced line boundaries and diagnostics.

## Results and cursors

| Type | Meaning |
| --- | --- |
| `LayoutCursor` | Segment + grapheme position for streaming layout |
| `LayoutResult` | Aggregate line count and height |
| `LayoutLine` | Materialized text, width, start cursor, and end cursor for one line |
| `LayoutLineRange` | Width and cursor range for one line without materialized text |
| `LayoutLinesResult` | Aggregate result plus a line collection |
| `PrepareProfile` | Coarse timing split for analysis and measurement |
