---
title: "Architecture and Packages"
---

# Architecture and Packages

The repository has four main parts:

| Path | Role |
| --- | --- |
| `src/Pretext` | Packable library with the preparation, measurement, bidi, and layout pipeline |
| `src/Pretext.Uno` | Uno companion library with reusable host controls and obstacle-aware flow/layout helpers |
| `tests/Pretext.Uno.Tests` | Deterministic tests covering segmentation, wrapping, pre-wrap behavior, bidi, and line walking |
| `samples/PretextSamples` | Uno sample app that exercises the library in visually different layouts |

## Package mapping

- `Pretext` publishes the core engine from `src/Pretext` and exposes the `Pretext` namespace.
- `Pretext.Uno` publishes the Uno-specific controls and helpers from `src/Pretext.Uno`.
- The sample app references the same core and Uno-layer APIs that ship in `Pretext` and `Pretext.Uno`.

## Pipeline shape

At a high level the library does this:

1. Analyze text into tokens and segments.
2. Measure those segments against a font string.
3. Store break opportunities and derived widths.
4. Walk lines repeatedly against different widths.

That split is why `Prepare` is important: it separates expensive setup from repeated layout probes.

## Separation of responsibilities

`Pretext` is responsible for:

- token and segment analysis
- whitespace normalization
- classification into `SegmentBreakKind`
- measurement through SkiaSharp
- line fitting, cursor advancement, and materialized line text
- locale-aware segmentation when ICU is available on desktop targets
- segment bidi level computation for mixed-direction text

Your host UI is responsible for:

- drawing the text
- choosing the line height
- baseline alignment
- hit testing and selection
- placing lines into cards, columns, or custom visual containers

## Why the split matters

This architecture gives you deterministic behavior across host controls:

- the expensive part is cached in a prepared object
- width-only changes do not require reanalysis
- you can switch between aggregate, materialized, and streaming APIs without changing the prepared data
- custom containers can reuse the same prepared text as ordinary controls
