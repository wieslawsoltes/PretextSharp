---
title: "Rich Inline API"
---

# Rich Inline API

`Pretext` keeps the core paragraph APIs low-level. The rich-inline helper is the next layer up: it solves the repeated userland work needed when inline text fragments, links, code spans, and atomic chips must wrap inside one visual flow.

## What it handles

- Collapsed boundary whitespace across item boundaries
- Atomic items that should never split, such as mentions, badges, timestamps, or inline image placeholders
- Per-item extra width for caller-owned chrome such as padding, borders, or pill backgrounds
- Geometry-only walking first, then optional text materialization later

## Main types

| Type | Purpose |
| --- | --- |
| `RichInlineItem` | Raw author item with `Text`, `Font`, optional `Break`, and optional `ExtraWidth`. |
| `PreparedRichInline` | Prepared handle that stores per-item prepared text and cached geometry. |
| `RichInlineCursor` | Cursor used to continue from one streamed line to the next. |
| `RichInlineFragmentRange` | Geometry-only fragment description for one line. |
| `RichInlineLineRange` | Geometry-only line result returned by the range walker. |
| `RichInlineFragment` | Materialized fragment with actual text. |
| `RichInlineLine` | Materialized rich-inline line. |
| `RichInlineStats` | Aggregate line count and widest line width. |

## Basic workflow

1. Build `RichInlineItem` values for your inline runs.
2. Call `PrepareRichInline(...)` once.
3. Use `WalkRichInlineLineRanges(...)` or `LayoutNextRichInlineLineRange(...)` to stream line geometry.
4. Call `MaterializeRichInlineLineRange(...)` only on the lines you actually render.

## Example

```csharp
using Pretext;

var prepared = PretextLayout.PrepareRichInline(
[
    new RichInlineItem("Ship ", "16px Inter"),
    new RichInlineItem("@maya", "700 12px Inter", RichInlineBreakMode.Never, extraWidth: 18),
    new RichInlineItem("'s note wraps cleanly around chips.", "16px Inter"),
]);

PretextLayout.WalkRichInlineLineRanges(prepared, 180, range =>
{
    var line = PretextLayout.MaterializeRichInlineLineRange(prepared, range);
    foreach (var fragment in line.Fragments)
    {
        Console.WriteLine($"{fragment.Text} gap={fragment.GapBefore} width={fragment.OccupiedWidth}");
    }
});
```

## Break modes

| Mode | Meaning |
| --- | --- |
| `RichInlineBreakMode.Normal` | The item may split using the normal prepared-text line breaker. |
| `RichInlineBreakMode.Never` | The item stays atomic. If it starts on a line, it is placed as one unit. |

`Never` is the right mode for pills, badges, mentions, inline timestamps, or synthetic inline media placeholders.

## When to use geometry only

Prefer `RichInlineLineRange` when you are:

- probing for shrinkwrap widths
- precomputing heights
- virtualizing large surfaces
- measuring many candidate widths before committing one

That keeps the expensive step, text materialization, off the hot path until you actually need visible fragment text.

## Samples

- `Rich Text` in `samples/PretextSamples.Uno` uses the rich-inline helper for chips, links, and code spans.
- `Markdown Chat` uses the same helper as the paragraph leaf inside a higher-level block layout model, with shared data/models in `samples/PretextSamples.Shared`.
