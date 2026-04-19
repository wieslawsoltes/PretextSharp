---
title: "Architecture and Packages"
---

# Architecture and Packages

The repository has these main parts:

| Path | Role |
| --- | --- |
| `src/Pretext.Contracts` | Packable contract library for backend implementations |
| `src/Pretext` | Packable library with the preparation, measurement, bidi, and layout pipeline |
| `src/Pretext.DirectWrite` | Packable Windows DirectWrite measurement backend |
| `src/Pretext.FreeType` | Packable Linux FreeType + Fontconfig measurement backend |
| `src/Pretext.CoreText` | Packable macOS CoreText measurement backend |
| `src/Pretext.SkiaSharp` | Packable SkiaSharp measurement backend |
| `src/Pretext.Uno` | Uno companion library with reusable host controls and obstacle-aware flow/layout helpers |
| `tests/Pretext.Uno.Tests` | Deterministic tests covering segmentation, wrapping, pre-wrap behavior, bidi, and line walking |
| `samples/PretextSamples` | Uno sample app that exercises the library in visually different layouts |

## Package mapping

- `Pretext` publishes the core engine from `src/Pretext` and exposes the `Pretext` namespace.
- `Pretext.Contracts` publishes the public contracts for custom measurement backends.
- `Pretext.DirectWrite` publishes the first-party Windows-native backend.
- `Pretext.FreeType` publishes the first-party Linux-native backend.
- `Pretext.CoreText` publishes the first-party macOS-native backend.
- `Pretext.SkiaSharp` publishes the first-party SkiaSharp backend.
- `Pretext.Uno` publishes the Uno-specific controls and helpers from `src/Pretext.Uno`.
- The sample app references the same core, backend, and Uno-layer APIs that ship in the published packages.

## Pipeline shape

At a high level the library does this:

1. Analyze text into tokens and segments.
2. Measure those segments through the configured backend against a font string.
3. Store break opportunities and derived widths.
4. Walk lines repeatedly against different widths.

That split is why `Prepare` is important: it separates expensive setup from repeated layout probes.

## Separation of responsibilities

`Pretext` is responsible for:

- token and segment analysis
- whitespace normalization
- classification into `SegmentBreakKind`
- backend selection and measurement caching
- line fitting, cursor advancement, and materialized line text
- locale-aware segmentation when ICU is available on desktop targets
- segment bidi level computation for mixed-direction text

`Pretext.Contracts` is responsible for:

- defining the measurement contracts
- parsing the shared CSS-like font string used by the first-party backends
- exposing the backend advertisement attribute used for discovery

First-party backend packages are responsible for:

- creating native or renderer-specific font objects
- mapping the shared font descriptor into backend-specific weight/slant/family values
- returning measured widths through the `Pretext.Contracts` interfaces

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
- apps can reference multiple backends while the engine auto-selects the best supported one for the current OS
