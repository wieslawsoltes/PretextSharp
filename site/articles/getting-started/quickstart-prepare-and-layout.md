---
title: "Quickstart: Prepare and Layout"
---

# Quickstart: Prepare and Layout

The smallest useful flow is:

```csharp
using Pretext;

const string text = "Hello soft\u00ADwrapped world";
const string font = "16px Inter";
const double lineHeight = 20;

var prepared = PretextLayout.PrepareWithSegments(text, font);
var metrics = PretextLayout.Layout(prepared, maxWidth: 160, lineHeight);
var lines = PretextLayout.LayoutWithLines(prepared, maxWidth: 160, lineHeight);

Console.WriteLine(metrics.LineCount);
Console.WriteLine(metrics.Height);
foreach (var line in lines.Lines)
{
    Console.WriteLine($"{line.Text} ({line.Width})");
}
```

If normalization leaves the prepared text empty, `Layout` returns `new LayoutResult(0, 0)`. When your container sizing wants to reserve at least one visual row, clamp in the caller with `Math.Max(1, metrics.LineCount)` instead of expecting `Pretext` to invent a blank line.

## What this does

- `PrepareWithSegments` analyzes the text, measures it, and preserves segment metadata.
- `Layout` answers the cheapest high-level question: how many lines fit, and how tall is the result if each line uses your chosen `lineHeight`.
- `LayoutWithLines` materializes the text and width of every line.

The same prepared object is reused by both layout calls. That is the main performance model of `Pretext`.

## Why `PrepareWithSegments` here?

This quickstart uses `PrepareWithSegments` because it unlocks all later APIs:

- `LayoutWithLines`
- `LayoutNextLine`
- `WalkLineRanges`
- segment inspection through `Segments`, `Kinds`, and `BreakableWidths`

If you only need `Layout`, use `Prepare` instead and skip the extra segment data.

## Reuse matters

Prepare once and reuse the `PreparedText` object. Recreate it when text, font, whitespace mode, or locale changes. Do **not** recreate it just because width or line height changed.

## Streaming version

When you are building a custom layout engine, iterate line by line instead of materializing the whole paragraph:

```csharp
var cursor = new LayoutCursor(0, 0);

while (true)
{
    var line = PretextLayout.LayoutNextLine(prepared, cursor, maxWidth: 160);
    if (line is null)
    {
        break;
    }

    Console.WriteLine($"{line.Start} -> {line.End}: {line.Text}");
    cursor = line.End;
}
```

## Read next

- [Choosing an API](choosing-an-api)
- [Prepared Text Lifecycle](../concepts/prepared-text-lifecycle)
- [Incremental Layout APIs](../concepts/incremental-layout-apis)
