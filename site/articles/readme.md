---
title: "Documentation"
---

# Documentation

This site documents the reusable `Pretext` layout engine, the full package split introduced in this branch, and the shared sample-host patterns built on top of it.

## Sections

- [Getting Started](getting-started)
  - package installation and font strings
  - first `Prepare`, `Layout`, and `LayoutWithLines` flow
  - choosing the right API for a workload
  - sample-host orientation
- [Concepts](concepts)
  - project structure and the core pipeline
  - prepared-text lifecycle and caching model
  - whitespace and break kinds
  - font parsing and measurement behavior
  - locale-aware segmentation and bidi
  - line fitting and incremental layout surfaces
- [Guides](guides)
  - backend discovery, overrides, and custom backends
  - integrating `Pretext` into native Windows/Linux/macOS hosts
  - integrating `Pretext` into Uno code
  - integrating `Pretext` into any SkiaSharp host
  - diagnostics and deterministic testing
  - shared sample-host walkthrough
  - shrinkwrap and editorial layout patterns
- [Reference](reference)
  - package and namespace map
  - per-package reference pages
  - public types, results, and operations
  - companion helper packages
  - scope, limitations, and platform notes
  - docs pipeline and license
