---
title: "Public Types and Operations"
---

# Public Types and Operations

This page is the top-level map of the public surface. Use the linked pages for deeper details on types and companion helpers.

## Preparation entry points

| API | Returns | Use it when |
| --- | --- | --- |
| `Prepare(string text, string font, PrepareOptions? options = null)` | `PreparedText` | You only need aggregate layout metrics. |
| `PrepareWithSegments(string text, string font, PrepareOptions? options = null)` | `PreparedTextWithSegments` | You need segments, materialized line text, or streamed iteration. |
| `ProfilePrepare(string text, string font, PrepareOptions? options = null)` | `PrepareProfile` | You want coarse profiling for preparation cost. |

## Layout entry points

| API | Returns | Use it when |
| --- | --- | --- |
| `Layout(PreparedText prepared, double maxWidth, double lineHeight)` | `LayoutResult` | You only need line count and total height. |
| `LayoutWithLines(PreparedTextWithSegments prepared, double maxWidth, double lineHeight)` | `LayoutLinesResult` | You need materialized line strings and widths. |
| `LayoutNextLine(PreparedTextWithSegments prepared, LayoutCursor start, double maxWidth)` | `LayoutLine?` | You are flowing one line at a time through a custom loop. |
| `LayoutNextLineRange(PreparedTextWithSegments prepared, LayoutCursor start, double maxWidth)` | `LayoutLineRange?` | You want streamed geometry without line-text allocation. |
| `MaterializeLineRange(PreparedTextWithSegments prepared, LayoutLineRange line)` | `LayoutLine` | You first walked geometry and only now need text for one line. |
| `WalkLineRanges(PreparedTextWithSegments prepared, double maxWidth, Action<LayoutLineRange> onLine)` | `int` | You need line geometry without allocating line strings. |
| `MeasureLineStats(PreparedTextWithSegments prepared, double maxWidth)` | `LineStats` | You need line count plus the widest wrapped line. |
| `MeasureNaturalWidth(PreparedTextWithSegments prepared)` | `double` | You need the widest unwrapped line width. |

## Rich inline entry points

| API | Returns | Use it when |
| --- | --- | --- |
| `PrepareRichInline(IReadOnlyList<RichInlineItem> items)` | `PreparedRichInline` | You want paragraph text and atomic inline items in one flow. |
| `LayoutNextRichInlineLineRange(PreparedRichInline prepared, double maxWidth, RichInlineCursor? start = null)` | `RichInlineLineRange?` | You want one streamed rich-inline line at a time. |
| `MaterializeRichInlineLineRange(PreparedRichInline prepared, RichInlineLineRange line)` | `RichInlineLine` | You want fragment text only after a geometry pass. |
| `WalkRichInlineLineRanges(PreparedRichInline prepared, double maxWidth, Action<RichInlineLineRange> onLine)` | `int` | You want to stream all rich-inline lines without per-line text churn. |
| `MeasureRichInlineStats(PreparedRichInline prepared, double maxWidth)` | `RichInlineStats` | You need rich-inline line count plus the widest line. |

## Environment and cache operations

| API | Purpose |
| --- | --- |
| `SetLocale(string? locale = null)` | Overrides locale-sensitive segmentation and clears caches. |
| `ClearCache()` | Clears cached font state and segment text caches. |

## Public type groups

### Enums and options

- `WhiteSpaceMode`
- `WordBreakMode`
- `SegmentBreakKind`
- `PrepareOptions`

### Prepared data

- `PreparedText`
- `PreparedTextWithSegments`
- `PreparedLineChunk`

### Cursor and result types

- `LayoutCursor`
- `LayoutResult`
- `LineStats`
- `LayoutLine`
- `LayoutLineRange`
- `LayoutLinesResult`
- `PrepareProfile`

### Rich inline types

- `RichInlineBreakMode`
- `RichInlineItem`
- `PreparedRichInline`
- `RichInlineCursor`
- `RichInlineFragment`
- `RichInlineFragmentRange`
- `RichInlineLine`
- `RichInlineLineRange`
- `RichInlineStats`

## Read next

- [Prepared Text Types](prepared-text-types)
- [Rich Inline API](rich-inline-api)
- [Pretext.Uno Helpers](pretext-uno-helpers)
- [Scope and Limitations](scope-and-limitations)
