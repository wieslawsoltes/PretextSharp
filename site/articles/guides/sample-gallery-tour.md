---
title: "Sample Gallery Tour"
---

# Sample Gallery Tour

The sample gallery is not just a visual demo. It is a set of concrete integration patterns for the core engine, the `Pretext.Layout` helper layer, and the two sample hosts.

## Hosts

The same sample catalog is presented through:

- `samples/PretextSamples.Uno`, the Uno host
- `samples/PretextSamples.MacOS`, the native AppKit host

Reusable sample data and models live in `samples/PretextSamples.Shared`.

## Overview sample

Shows the high-level surface and how `Pretext` fits into a custom text layout workflow.

## Accordion

Uses `Prepare` and `Layout` to turn width changes into deterministic line counts and section heights.

## Bubbles

Uses `PrepareWithSegments` together with `PreparedTextMetrics` to calculate tight wrap widths for message bubbles.

## Masonry

Uses repeated aggregate layout to size many independent cards cheaply without materializing every line string.

## Rich Text

Uses the core rich-inline helper so code spans, links, and atomic chips share one wrapping flow.

## Markdown Chat

Uses markdown parsing, `PrepareRichInline`, `MeasureRichInlineStats`, and exact-height-first virtualization to render only the visible window of a 10k-message chat surface.

## Dynamic Layout

Shows why prepared text exists: the same prepared object is reused while widths and placements change.

## Editorial Engine

Combines `LayoutNextLine`, obstacle carving, and positioned lines to flow text through constrained columns and pullquote regions.

## Justification Comparison

Uses the same prepared paragraphs with different line-fitting strategies so you can compare greedy, hyphenated, and more global approaches.

## Variable ASCII

Demonstrates width sensitivity and why text layout should stay tied to the real measured font rather than assumptions about character counts.

## How to read the samples

When you open a sample view, ask four questions:

1. Where is text prepared?
2. Which layout API is chosen?
3. Which inputs force a re-prepare?
4. Which inputs only force a relayout?

That pattern generalizes better than the exact UI design of the sample.
The sample hosts are intentionally diverse. They demonstrate that the same core APIs can support very different layout styles.

## Layout-focused samples

- `Accordion`: derive item height from measured line counts
- `Bubbles`: compute exact bubble widths without DOM-style shrinkwrap
- `Masonry`: pack cards after probing text height
- `Dynamic Layout`: react to width changes without reparsing text
- `Markdown Chat`: virtualize a rich block surface from exact predicted heights

## Typography-focused samples

- `Justification Comparison`: compare strategies on the same prepared paragraphs
- `Editorial Engine`: place pull quotes and body copy around constraints
- `Variable ASCII`: show width-sensitive fitting with different glyph runs
- `Rich Text`: keep atomic badges and splittable text in one inline flow

## Where to read next

Start with `samples/PretextSamples.Shared/Samples/SampleCatalog.cs`, then move into the host-specific views for the platform you care about.
