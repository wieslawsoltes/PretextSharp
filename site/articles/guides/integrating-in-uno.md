---
title: "Integrating in Uno"
---

# Integrating in Uno

The common integration pattern is:

1. Prepare content when the text or font changes.
2. Reuse the prepared object during measure or arrange.
3. Recompute only the line walk when available width changes.

## Typical spots to call it

- Custom panels
- `MeasureOverride`
- Drawing code for chips, bubbles, and cards
- Responsive layouts that need minimum width or exact line counts

## Example pattern

```csharp
private PreparedTextWithSegments? _prepared;

void UpdateText(string text, string font)
{
    _prepared = PretextLayout.PrepareWithSegments(text, font);
}

Size MeasureBubble(double maxWidth, double lineHeight, Thickness padding)
{
    if (_prepared is null)
    {
        return Size.Empty;
    }

    var layout = PretextLayout.Layout(_prepared, maxWidth, lineHeight);
    return new Size(maxWidth + padding.Left + padding.Right, layout.Height + padding.Top + padding.Bottom);
}
```

## Companion helpers

The `Pretext.Uno` package in this repository adds reusable helpers around the core engine:

- `StretchScrollHost` for page-like scrollable samples and viewport calculations
- `UiRenderScheduler` for coalesced redraw scheduling on `DispatcherQueue`
- `PreparedTextMetrics` for wrap metrics and tight-width calculations
- `ColumnFlowLayout` and `ObstacleLayoutHelper` for flowing lines into constrained regions

Use them when they match your layout model, but keep the core `Pretext` API as the main dependency.

## Uno-specific advice

- Prepare text in view-model or control state, not inside every render callback.
- Reuse prepared text across `SizeChanged` or `MeasureOverride` passes.
- Keep line height in your Uno typography system rather than encoding it into the `font` string.
- Use `Layout` in measurement passes and `LayoutWithLines` or `LayoutNextLine` only when rendering needs richer output.

## Further reading

For broader Uno renderer context, see [How Uno Platform Works](https://platform.uno/docs/articles/how-uno-works.html).
