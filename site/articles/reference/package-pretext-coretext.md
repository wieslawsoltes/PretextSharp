---
title: "Package: Pretext.CoreText"
---

# Package: Pretext.CoreText

`Pretext.CoreText` is the first-party macOS-native measurement backend.

## Install it when

- your host runs on macOS and you want CoreText metrics
- you are building a native AppKit or interop-based host
- you want `Pretext` to prefer the macOS-native backend automatically

## Runtime behavior

The package advertises `CoreTextTextMeasurerFactory` to the core engine.

- `Name`: `CoreText`
- `IsSupported`: `true` on macOS
- `Priority`: `100`

In the native macOS sample host, the app also sets the factory explicitly during startup so the host behavior is obvious and deterministic.

## Relationship to the rest of the stack

- depends on `Pretext.Contracts`
- pairs naturally with `Pretext` and `Pretext.Layout`
- is one of the transitive backends included by `Pretext.Uno`

## Typical hosts

- AppKit hosts on `net10.0-macos`
- native macOS drawing surfaces
- backend-agnostic tools that should use CoreText when running on macOS

## Read next

- [Using Pretext in Native Hosts](../guides/using-pretext-in-native-hosts)
- [Sample Hosts and Shared Assets](../guides/sample-hosts-and-shared-assets)
