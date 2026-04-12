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
| `WalkLineRanges(PreparedTextWithSegments prepared, double maxWidth, Action<LayoutLineRange> onLine)` | `int` | You need line geometry without allocating line strings. |

## Environment and cache operations

| API | Purpose |
| --- | --- |
| `SetLocale(string? locale = null)` | Overrides locale-sensitive segmentation and clears caches. |
| `ClearCache()` | Clears cached font state and segment text caches. |

## Public type groups

### Enums and options

- `WhiteSpaceMode`
- `SegmentBreakKind`
- `PrepareOptions`

### Prepared data

- `PreparedText`
- `PreparedTextWithSegments`
- `PreparedLineChunk`

### Cursor and result types

- `LayoutCursor`
- `LayoutResult`
- `LayoutLine`
- `LayoutLineRange`
- `LayoutLinesResult`
- `PrepareProfile`

## Read next

- [Prepared Text Types](prepared-text-types)
- [Pretext.Uno Helpers](pretext-uno-helpers)
- [Scope and Limitations](scope-and-limitations)
