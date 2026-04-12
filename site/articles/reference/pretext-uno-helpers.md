---
title: "Pretext.Uno Helpers"
---

# Pretext.Uno Helpers

`Pretext.Uno` is the companion package that adds reusable Uno-side helpers on top of the core `Pretext` engine.

## Controls

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

## Layout types

### `WrapMetrics`

Aggregate metrics for a wrapped text block:

- `LineCount`
- `Height`
- `MaxLineWidth`

### `PositionedLine`

Represents one laid-out line placed into a coordinate system:

- `Text`
- `X`
- `Y`
- `Width`

### Obstacle primitives

- `RectObstacle`
- `CircleObstacle`
- `Interval`

These are the geometric primitives used by the obstacle-aware helpers.

## Layout helpers

### `PreparedTextMetrics`

Utility methods for common wrap calculations:

- `MeasureMaxLineWidth`
- `CollectWrapMetrics`
- `FindTightWrapMetrics`
- `IsEnd`

### `ColumnFlowLayout`

Flows prepared text through rectangular columns while respecting rectangular and circular obstacles.

### `ObstacleLayoutHelper`

Lower-level helper methods for carving intervals and selecting usable slots around obstacles.

## When to use the companion package

Use `Pretext.Uno` when your application already targets Uno and wants to reuse the same helper patterns as the sample app. If you only need the text preparation and layout engine, stay on the core `Pretext` package.
