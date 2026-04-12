---
title: "Shrinkwrap and Editorial Layout"
---

# Shrinkwrap and Editorial Layout

Two recurring patterns in the sample app are worth copying into your own code.

## Shrinkwrap

Use `WalkLineRanges` or `LayoutWithLines` to find the exact width of the longest line, then size the host element to that width instead of relying on default control measurement.

This pattern is useful for:

- chips
- chat bubbles
- badges
- masonry cards

Typical flow:

1. `PrepareWithSegments` once
2. walk lines at a candidate width
3. compute `MaxLineWidth`
4. add padding and chrome around that width

## Editorial and constrained layouts

Use `LayoutNextLine` when content must flow around obstacles or into fixed slots. Because the API returns one line at a time, you can decide where to place the next line, how much width remains, and whether a region should continue into a second column or frame.

Typical flow:

1. prepare the paragraph once
2. compute the next available slot for the current y-band
3. call `LayoutNextLine(prepared, cursor, slotWidth)`
4. place the line and advance the cursor
5. continue into the next slot, band, or column

## Why these patterns matter

Both patterns depend on the same property of `Pretext`: width-dependent layout is cheap after preparation, so you can build higher-level algorithms without reparsing the paragraph.

## Related helpers

In the Uno companion package, these helpers support the same patterns:

- `PreparedTextMetrics`
- `ColumnFlowLayout`
- `ObstacleLayoutHelper`
