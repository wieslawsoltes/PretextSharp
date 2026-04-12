---
title: "Font Strings and Measurement"
---

# Font Strings and Measurement

Every preparation API takes a `font` string. The parser accepts a CSS-like subset and maps it into the SkiaSharp font state used for measurement.

## Accepted format

The parser looks for:

- a size in `px`
- an optional weight before the size
- optional `italic` or `oblique`
- a comma-separated family list after the size

Examples:

```csharp
"16px Inter"
"700 18px \"IBM Plex Sans\""
"italic 16px Georgia"
"600 15px system-ui"
"16px Menlo, monospace"
```

## Supported style tokens

| Token | Effect |
| --- | --- |
| `bold` | weight `700` |
| numeric weight such as `500` or `700` | mapped to normal, medium, or bold Skia font weight |
| `italic` or `oblique` | italic slant |

## Family fallback rules

If the primary family is a generic CSS family, `Pretext` maps it to a concrete desktop fallback:

| Input family | Effective family |
| --- | --- |
| `sans-serif`, `system-ui` | `Arial` |
| `serif` | `Times New Roman` |
| `monospace`, `ui-monospace` | `Menlo` |

If the parser cannot find a valid size, it falls back to `16px Arial`.

## What is not parsed

- `%`, `em`, `rem`, and other non-pixel units
- advanced font-variation syntax
- OpenType feature strings
- line height embedded in CSS shorthand

If a `/line-height` segment appears after the size, it is ignored by the parser. You still pass line height explicitly to layout methods.

## Measurement model

`Pretext` measures through SkiaSharp and stores:

- width of each prepared segment
- discretionary hyphen width
- tab stop advance, based on eight space widths
- per-grapheme break widths for breakable runs

Because line height is caller-supplied, you stay in control of:

- baseline policy
- paragraph spacing
- host-specific font metrics
- visual density choices

## Practical advice

- Use the same font string for both preparation and rendering.
- Keep the font string stable while only width changes.
- Pick a line height from your host UI system rather than assuming `font size * constant`.
