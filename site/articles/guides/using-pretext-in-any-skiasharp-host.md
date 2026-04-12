---
title: "Using Pretext in Any SkiaSharp Host"
---

# Using Pretext in Any SkiaSharp Host

The core `Pretext` package does not depend on Uno. If your host can measure and draw text through SkiaSharp, the integration pattern stays the same.

## Generic host pattern

1. Prepare text when content or font changes.
2. Keep the prepared object in control or view-model state.
3. Re-run layout when available width changes.
4. Draw the resulting lines with your host's SkiaSharp surface.

## Minimal drawing example

```csharp
using Pretext;
using SkiaSharp;

var prepared = PretextLayout.PrepareWithSegments(
    "Hello soft\u00ADwrapped world",
    "16px Inter");

var lines = PretextLayout.LayoutWithLines(prepared, maxWidth: 220, lineHeight: 22);

using var paint = new SKPaint
{
    Typeface = SKTypeface.FromFamilyName("Inter"),
    TextSize = 16,
    IsAntialias = true
};

var y = 24f;
foreach (var line in lines.Lines)
{
    canvas.DrawText(line.Text, 0, y, paint);
    y += 22f;
}
```

## Important host decisions

`Pretext` gives you line text and widths, but your host still decides:

- baseline position
- clipping and scrolling
- selection and hit testing
- antialiasing and paint configuration
- line spacing beyond the supplied `lineHeight`

## When to prefer streamed layout

Use `LayoutNextLine` instead of `LayoutWithLines` when:

- you may stop early
- lines flow into changing slots or obstacles
- you want to avoid materializing every line string up front

## Typical non-Uno uses

- custom `SKCanvasView` or `SKGLView` controls
- desktop editors or note surfaces built directly on SkiaSharp
- design tools that need repeated text measurement during resize
- reporting or export pipelines that place text into rectangles manually
