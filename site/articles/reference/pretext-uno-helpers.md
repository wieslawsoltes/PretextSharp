---
title: "Companion Helpers"
---

# Companion Helpers

The companion surface is now split across two packages:

- `Pretext.Layout` for platform-neutral wrap and obstacle-layout helpers
- `Pretext.Uno` for Uno-specific controls and render scheduling

## `Pretext.Layout`

`Pretext.Layout` builds on the core `Pretext` engine without taking any Uno Platform dependency.

### Data types

#### `WrapMetrics`

Aggregate metrics for a wrapped text block:

- `LineCount`
- `Height`
- `MaxLineWidth`

#### `PositionedLine`

Represents one laid-out line placed into a coordinate system:

- `Text`
- `X`
- `Y`
- `Width`

#### Obstacle primitives

- `RectObstacle`
- `CircleObstacle`
- `Interval`

These are the geometric primitives used by the obstacle-aware helpers.

### Helper types

#### `PreparedTextMetrics`

Utility methods for common wrap calculations:

- `MeasureMaxLineWidth`
- `CollectWrapMetrics`
- `FindTightWrapMetrics`
- `IsEnd`

#### `ColumnFlowLayout`

Flows prepared text through rectangular columns while respecting rectangular and circular obstacles.

#### `ObstacleLayoutHelper`

Lower-level helper methods for carving intervals and selecting usable slots around obstacles.

## `Pretext.Uno`

`Pretext.Uno` now focuses on the Uno-specific pieces.

### `StretchScrollHost`

A simple page-level container that:

- wraps content in a `ScrollViewer`
- stretches the child width to the host width
- exposes `ContentBackground` and `ContentPadding`
- can compute local viewport bounds for a target element

This is useful for sample-like pages or long document surfaces that need viewport-relative calculations.

### `UiRenderScheduler`

A small dispatcher-backed scheduler that coalesces repeated render requests into a single `DispatcherQueue` callback.

Use it when:

- many UI events can trigger a redraw
- you want to avoid stacking redundant render passes
- your custom surface already owns a render action

## When to use which package

Use `Pretext.Layout` when you want the helper patterns from the sample app without taking a UI framework dependency. Add `Pretext.Uno` only when your application already targets Uno and wants the reusable Uno controls as well.
