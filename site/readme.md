---
title: "Pretext"
layout: simple
og_type: website
---

# Pretext

`Pretext` is a deterministic text preparation and line-layout engine with pluggable text-measurement backends. It prepares text once, exposes segment and break metadata, and lets you compute line counts, materialized lines, or streamed line geometry without relying on control reflow.

<div class="d-flex flex-wrap gap-3 mt-4 mb-4">
  <a class="btn btn-primary btn-lg" href="articles/getting-started/overview"><i class="bi bi-rocket-takeoff" aria-hidden="true"></i> Start Here</a>
  <a class="btn btn-outline-secondary btn-lg" href="articles/reference/public-types-and-operations"><i class="bi bi-journal-code" aria-hidden="true"></i> API Reference</a>
  <a class="btn btn-outline-secondary btn-lg" href="articles/guides/sample-gallery-tour"><i class="bi bi-grid-3x3-gap" aria-hidden="true"></i> Sample Tour</a>
  <a class="btn btn-outline-secondary btn-lg" href="https://github.com/wieslawsoltes/PretextSharp"><i class="bi bi-github" aria-hidden="true"></i> Repository</a>
</div>

## What This Library Does

<div class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-4 mb-4">
  <div class="col"><div class="card h-100"><div class="card-body"><h3 class="h5">Prepare once, reuse often</h3><p class="mb-0">`Prepare` and `PrepareWithSegments` precompute widths, break opportunities, and whitespace behavior so repeated layout passes stay cheap.</p></div></div></div>
  <div class="col"><div class="card h-100"><div class="card-body"><h3 class="h5">Batch and streaming layout</h3><p class="mb-0">Use `Layout` for quick metrics, `LayoutWithLines` for materialized lines, or `LayoutNextLine` and `WalkLineRanges` for incremental custom layout engines.</p></div></div></div>
  <div class="col"><div class="card h-100"><div class="card-body"><h3 class="h5">Real text behavior</h3><p class="mb-0">The engine handles grapheme-aware breaks, discretionary hyphens, tabs, preserved whitespace, bidi text, and locale-aware segmentation on desktop targets.</p></div></div></div>
</div>

## Scope

`Pretext` is a layout engine, not a text renderer. It answers questions such as:

- how many lines fit in a given width
- what text ends up on each line
- where the next line should start for a custom flow algorithm
- which whitespace, punctuation, and discretionary break opportunities should stay visible

You still own:

- drawing the text in your host UI
- choosing the line height and baseline policy
- arranging lines into columns, cards, obstacles, or custom panels
- invalidating or repainting your UI when layout inputs change

## Documentation Map

<div class="row row-cols-1 row-cols-md-2 g-4">
  <div class="col"><div class="card h-100"><div class="card-body"><h2 class="h4">Getting Started</h2><p>Install the packages, learn the font string contract, and build the first useful `Prepare` + `Layout` flow.</p><a href="articles/getting-started" class="btn btn-sm btn-primary">Open section</a></div></div></div>
  <div class="col"><div class="card h-100"><div class="card-body"><h2 class="h4">Concepts</h2><p>Understand the prepared-text lifecycle, whitespace modes, break kinds, locale-aware segmentation, bidi, and line fitting.</p><a href="articles/concepts" class="btn btn-sm btn-primary">Open section</a></div></div></div>
  <div class="col"><div class="card h-100"><div class="card-body"><h2 class="h4">Guides</h2><p>Integrate `Pretext` into Uno, native Windows/Linux/macOS hosts, or any SkiaSharp-based renderer, then reuse the sample app patterns for shrinkwrap, editorial, and obstacle-aware layouts.</p><a href="articles/guides" class="btn btn-sm btn-primary">Open section</a></div></div></div>
  <div class="col"><div class="card h-100"><div class="card-body"><h2 class="h4">Reference</h2><p>Browse every public type and operation, the `Pretext.Uno` companion helpers, platform notes, and repository structure.</p><a href="articles/reference" class="btn btn-sm btn-primary">Open section</a></div></div></div>
</div>

## Repository Layout

| Path | Purpose |
| --- | --- |
| `src/Pretext` | The packable library project containing `PretextLayout` and the text preparation/layout pipeline. |
| `src/Pretext.Contracts` | Shared backend contracts and first-party font-string parsing helpers. |
| `src/Pretext.DirectWrite` | Windows DirectWrite measurement backend. |
| `src/Pretext.FreeType` | Linux FreeType + Fontconfig measurement backend. |
| `src/Pretext.CoreText` | macOS CoreText measurement backend. |
| `src/Pretext.Uno` | The source for the `Pretext.Uno` package, with Uno-specific reusable controls and layout helpers. |
| `tests/Pretext.Uno.Tests` | Deterministic parity tests for whitespace handling, break behavior, bidi text, and line walking. |
| `samples/PretextSamples` | A Uno sample app with layout demos including bubbles, masonry, editorial, and justification views. |

## Published Packages

| Package | Purpose |
| --- | --- |
| `Pretext` | Backend-agnostic text preparation and layout engine. |
| `Pretext.Contracts` | Public backend contracts and shared font parsing. |
| `Pretext.DirectWrite` | Windows-native DirectWrite backend. |
| `Pretext.FreeType` | Linux-native FreeType + Fontconfig backend. |
| `Pretext.CoreText` | macOS-native CoreText backend. |
| `Pretext.SkiaSharp` | Portable SkiaSharp backend and fallback. |
| `Pretext.Uno` | Uno-specific controls and helpers layered on top of `Pretext`. |

## Start With These Pages

- [Getting Started Overview](articles/getting-started/overview)
- [Install Packages](articles/getting-started/installation)
- [Quickstart: Prepare and Layout](articles/getting-started/quickstart-prepare-and-layout)
- [Choosing an API](articles/getting-started/choosing-an-api)
- [Prepared Text Lifecycle](articles/concepts/prepared-text-lifecycle)
- [Font Strings and Measurement](articles/concepts/font-strings-and-measurement)
- [Reference: Public Types and Operations](articles/reference/public-types-and-operations)
- [Reference: Prepared Text Types](articles/reference/prepared-text-types)
- [Sample Gallery Tour](articles/guides/sample-gallery-tour)
- [Whitespace and Break Kinds](articles/concepts/whitespace-and-break-kinds)
- [Locale Segmentation and Bidi](articles/concepts/locale-segmentation-and-bidi)

## Repository

- Source code and issues: [github.com/wieslawsoltes/PretextSharp](https://github.com/wieslawsoltes/PretextSharp)
- Sample app: [samples/PretextSamples](https://github.com/wieslawsoltes/PretextSharp/tree/main/samples/PretextSamples)
