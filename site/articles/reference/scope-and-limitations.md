---
title: "Scope and Limitations"
---

# Scope and Limitations

`Pretext` is intentionally focused. It handles text preparation and line fitting; it does not try to replace a full text rendering stack.

## What Pretext does

- analyzes text into reusable segments
- classifies whitespace and break opportunities
- measures text through the configured backend
- computes wrapped line counts, heights, and materialized lines
- supports streamed line walking for custom layouts
- carries locale-aware segmentation and bidi metadata where available

## What Pretext does not do

- draw text for you
- choose line height automatically
- own baseline alignment
- implement selection, caret movement, or hit testing
- expose a ready-made document model

## Important limitations

### Font strings are CSS-like, not full CSS

The font parser only understands a practical subset of CSS-style font strings. Unsupported syntax falls back to `16px Arial`.

### Locale-aware segmentation is desktop-oriented

ICU-backed locale-aware word segmentation is available on desktop targets when the needed ICU libraries are available. If ICU is unavailable, the engine falls back to its internal tokenization path.

### `LayoutWithLines` allocates

If you are on a hot path and only need geometry, prefer `WalkLineRanges` or `LayoutNextLine`.

### Prepared text is input-specific

Prepared objects are valid only for the text, font, white-space mode, and locale they were created with.

### The engine is deterministic, but your host still matters

Rendering quality, baseline handling, and final visuals still depend on your host UI and paint configuration.
