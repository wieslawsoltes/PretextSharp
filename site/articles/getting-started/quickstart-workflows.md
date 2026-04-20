---
title: "Quickstart: Sample Hosts"
---

# Quickstart: Sample Hosts

The sample projects are the fastest way to understand how the new package split is meant to be used in real hosts.

## Project layout

| Project | Purpose |
| --- | --- |
| `samples/PretextSamples.Shared` | Shared sample data, markdown/rich-text models, and catalog definitions |
| `samples/PretextSamples.Uno` | Uno host using `Pretext.Uno` |
| `samples/PretextSamples.MacOS` | Native AppKit host using `Pretext` + `Pretext.CoreText` |

## Shared sample menu

- `Overview`: high-level intro page
- `Accordion`: variable-height sections driven by line counts
- `Bubbles`: shrinkwrapped chat bubbles
- `Masonry`: card sizing in a masonry layout
- `Rich Text`: mixed formatting and inline composition
- `Markdown Chat`: virtualized markdown conversation
- `Dynamic Layout`: layout reacting to width changes
- `Editorial Engine`: page-like placement with constraints
- `Justification Comparison`: greedy vs more advanced line fitting
- `Variable ASCII`: width-sensitive text using variable glyphs

## What to look for

Use the samples as an API map:

| Sample | Main idea | APIs to inspect |
| --- | --- | --- |
| `Accordion` | Repeated line counts during responsive layout | `Prepare`, `Layout` |
| `Bubbles` | Tight wrap width and chat bubble sizing | `PrepareWithSegments`, `PreparedTextMetrics` |
| `Masonry` | Card height estimation without materializing lines | `Prepare`, `Layout` |
| `Rich Text` | Streaming text into an inline composition pipeline | `LayoutNextLine` |
| `Markdown Chat` | Exact-height prediction and virtualization for rich message blocks | `PrepareRichInline`, `MeasureRichInlineStats`, `MeasureLineStats` |
| `Dynamic Layout` | Reusing prepared text while width changes | `PrepareWithSegments`, `WalkLineRanges`, `LayoutNextLine` |
| `Editorial Engine` | Flowing text around obstacles and into columns | `LayoutNextLine`, `ColumnFlowLayout`, `ObstacleLayoutHelper` |
| `Justification Comparison` | Comparing line-fitting strategies on the same prepared data | `PrepareWithSegments`, `WalkLineRanges`, `LayoutNextLine` |
| `Variable ASCII` | Width-sensitive rendering derived from real measured glyph runs | `PrepareWithSegments`, `LayoutWithLines` |

## How to read the hosts

Start from the shared layer and then branch into the host you care about:

1. `samples/PretextSamples.Shared/Samples/SampleCatalog.cs`
2. shared models under `samples/PretextSamples.Shared/Samples`
3. Uno views in `samples/PretextSamples.Uno/Samples`
4. native AppKit pages in `samples/PretextSamples.MacOS/Pages`

That reading order separates reusable layout/data logic from host-specific UI code.

## Read next

- [Sample Gallery Tour](../guides/sample-gallery-tour)
- [Sample Hosts and Shared Assets](../guides/sample-hosts-and-shared-assets)
- [Integrating in Uno](../guides/integrating-in-uno)
