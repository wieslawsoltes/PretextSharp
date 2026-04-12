---
title: "Quickstart: Sample App Tour"
---

# Quickstart: Sample App Tour

The sample app is the fastest way to understand what the library is good at.

## Sample menu

- `Overview`: high-level intro page
- `Accordion`: variable-height sections driven by line counts
- `Bubbles`: shrinkwrapped chat bubbles
- `Masonry`: card sizing in a masonry layout
- `Rich Text`: mixed formatting and inline composition
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
| `Dynamic Layout` | Reusing prepared text while width changes | `PrepareWithSegments`, `WalkLineRanges`, `LayoutNextLine` |
| `Editorial Engine` | Flowing text around obstacles and into columns | `LayoutNextLine`, `ColumnFlowLayout`, `ObstacleLayoutHelper` |
| `Justification Comparison` | Comparing line-fitting strategies on the same prepared data | `PrepareWithSegments`, `WalkLineRanges`, `LayoutNextLine` |

## How to use it

Start in `samples/PretextSamples/MainPage.xaml.cs` and then inspect the individual sample views in `samples/PretextSamples/Samples`. Those files show how the layout APIs are used in real panels and drawing code rather than in isolated snippets.

## Read next

- [Sample Gallery Tour](../guides/sample-gallery-tour)
- [Integrating in Uno](../guides/integrating-in-uno)
