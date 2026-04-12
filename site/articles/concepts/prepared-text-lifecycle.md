---
title: "Prepared Text Lifecycle"
---

# Prepared Text Lifecycle

Prepared text is the core abstraction in `Pretext`. It separates expensive analysis and measurement from repeated width-dependent layout.

## Lifecycle phases

1. **Input**: raw text, font string, whitespace mode, and current locale
2. **Preparation**: segment analysis, break classification, measurement, and derived metadata
3. **Reuse**: repeated layout against different widths and line heights
4. **Invalidation**: recreate when text, font, whitespace mode, or locale changes

## What preparation stores

Depending on whether you choose `Prepare` or `PrepareWithSegments`, the prepared object stores:

- the font string that was measured
- the whitespace mode used
- segment widths
- line-end fit and paint advances
- breakable grapheme widths for overlong runs
- discretionary hyphen width
- tab stop advance
- hard-break chunk boundaries
- optionally, segment text and bidi levels

## When to prepare again

You must recreate prepared text when:

- the input text changes
- the font string changes
- the white-space mode changes
- `SetLocale` is called with a different locale

You should **not** recreate prepared text when only:

- the available width changes
- the line height changes
- the layout container changes
- the same text is placed into a different visual arrangement

## Choosing between `Prepare` and `PrepareWithSegments`

| Type | Best when | Tradeoff |
| --- | --- | --- |
| `PreparedText` | you only need `Layout` | smallest data shape |
| `PreparedTextWithSegments` | you need segment lists, line strings, or streaming layout | richer but larger |

## Caches

`Pretext` keeps internal font-state and segment-text caches so repeated operations stay cheap.

Use `ClearCache()` when you want to:

- reset cached measurement state in tests
- ensure locale or measurement changes take effect immediately
- force a clean profile pass during diagnostics

Calling `SetLocale()` also clears caches because segmentation behavior may change.
