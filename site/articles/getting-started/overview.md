---
title: "Getting Started Overview"
---

# Getting Started Overview

`Pretext` is a text preparation and line-layout engine with pluggable text-measurement backends for hosts that need deterministic wrapping and visibility rules without delegating everything to a control-specific text stack.

## What to expect

`Pretext` is built around one idea: expensive text analysis should happen once, and width-dependent layout should be cheap and repeatable after that.

The normal workflow is:

1. Convert raw text into a prepared object with `Prepare` or `PrepareWithSegments`.
2. Reuse that prepared object as widths change.
3. Choose the cheapest layout API that answers your question.
4. Draw or arrange the resulting lines in your own UI.

## Package and repository map

| Item | Use it when |
| --- | --- |
| `Pretext` | You want the reusable layout engine under the `Pretext` namespace. |
| `Pretext.Contracts` | You are implementing or documenting a measurement backend. |
| `Pretext.Layout` | You want platform-neutral wrap and obstacle-layout helpers on top of `Pretext`. |
| `Pretext.DirectWrite` | You want the first-party Windows-native DirectWrite measurement backend. |
| `Pretext.FreeType` | You want the first-party Linux-native FreeType + Fontconfig measurement backend. |
| `Pretext.CoreText` | You want the first-party macOS-native CoreText measurement backend. |
| `Pretext.SkiaSharp` | You want the first-party portable SkiaSharp backend or a cross-platform fallback. |
| `Pretext.Uno` | You want Uno-specific controls layered on top of `Pretext`. |
| `src/Pretext.Contracts` | You are working on backend contracts or building a custom integration. |
| `src/Pretext` | You are working on the core package source in this repository. |
| `src/Pretext.Layout` | You are working on the platform-neutral helper package source. |
| `src/Pretext.DirectWrite` | You are working on the Windows DirectWrite backend source. |
| `src/Pretext.FreeType` | You are working on the Linux FreeType backend source. |
| `src/Pretext.CoreText` | You are working on the macOS CoreText backend source. |
| `src/Pretext.SkiaSharp` | You are working on the SkiaSharp backend source in this repository. |
| `src/Pretext.Uno` | You are working on the Uno companion package source in this repository. |
| `tests/Pretext.Uno.Tests` | You want the deterministic behavior checks and supported-text examples. |
| `samples/PretextSamples.Shared` | You want the shared sample catalog, assets, and data/model layer. |
| `samples/PretextSamples.Uno` | You want the Uno sample host. |
| `samples/PretextSamples.MacOS` | You want the native AppKit sample host that binds `Pretext` to CoreText. |

## The four main API shapes

| API | Use it when |
| --- | --- |
| `Prepare` | You only need aggregate layout metrics and do not need segment data or line text. |
| `PrepareWithSegments` | You need segment metadata, materialized lines, or streamed iteration. |
| `Layout` | You only need line count and total height. |
| `LayoutWithLines` | You need the rendered line text and widths for each line. |
| `LayoutNextLine` | You are flowing one line at a time into a custom container, obstacle map, or multi-column algorithm. |
| `WalkLineRanges` | You need line widths and cursors but want to avoid allocating line strings. |

## When it helps

- Shrinkwrapping cards, chips, and chat bubbles
- Running custom justification or editorial algorithms
- Measuring text repeatedly during responsive layout
- Preserving tabs, hard breaks, and trailing spaces in `PreWrap`
- Handling multilingual content with bidi runs and locale-aware segmentation

## Read next

- [Installation](installation) for packages, namespaces, and build commands
- [Quickstart: Prepare and Layout](quickstart-prepare-and-layout) for the first usable flow
- [Backend Discovery and Overrides](../guides/backend-discovery-and-overrides) for runtime backend selection
- [Choosing an API](choosing-an-api) for a decision table based on output needs and allocation budget
