---
title: "Locale Segmentation and Bidi"
---

# Locale Segmentation and Bidi

The library is designed for multilingual text, not just ASCII paragraphs.

## Locale-aware segmentation

`PretextLayout.SetLocale` lets you override the locale used for segmentation. On desktop targets the implementation can use ICU word breaking when available, which improves token boundaries for scripts that do not behave well under simple whitespace splitting.

This matters most for:

- Thai and other scripts that do not rely on ordinary spaces between words
- multilingual paragraphs where punctuation attachment differs by script
- tests where you want stable expectations independent of machine culture

If you do not call `SetLocale`, `Pretext` uses the current UI culture when available.

## Bidirectional text

Prepared results can also carry segment levels for mixed-direction text. That matters when a line contains Latin text, Arabic, Hebrew, punctuation, and other content that should still wrap predictably.

When bidi content is present:

- `PreparedTextWithSegments.SegmentLevels` contains one level per segment
- segments can still be line-fit deterministically
- materialized line text remains aligned with the same cursor boundaries used by streamed layout

## Practical advice

- Use the current UI culture unless you have a stronger content-specific locale.
- Call `SetLocale` in tests when you need stable expectations.
- Validate sample strings from your actual product languages, not just English.

## What the current implementation covers

The shipped tests explicitly cover:

- Arabic punctuation attachment
- Arabic punctuation-plus-mark clusters
- Devanagari danda attachment
- Myanmar punctuation and possessive markers
- CJK punctuation and iteration marks
- mixed-direction smoke tests
- Thai locale-sensitive segmentation

## Further reading

The behavior in this area is informed by Unicode guidance:

- [UAX #29: Unicode Text Segmentation](https://www.unicode.org/reports/tr29/)
- [UAX #9: Unicode Bidirectional Algorithm](https://www.unicode.org/reports/tr9/)
- [UAX #14: Unicode Line Breaking Algorithm](https://www.unicode.org/reports/tr14/)
