---
title: "Package: Pretext.FreeType"
---

# Package: Pretext.FreeType

`Pretext.FreeType` is the first-party Linux-native measurement backend built on FreeType and Fontconfig.

## Install it when

- your host runs on Linux and you want host-native font lookup and text metrics
- you want `Pretext` to prefer a Linux-native backend instead of SkiaSharp

## Runtime behavior

The package advertises `FreeTypeTextMeasurerFactory` to the core engine.

- `Name`: `FreeType`
- `IsSupported`: `true` on Linux
- `Priority`: `100`

When it is referenced in a Linux process, auto-discovery prefers it over `Pretext.SkiaSharp`.

## Relationship to the rest of the stack

- depends on `Pretext.Contracts`
- is independent from Uno Platform
- works as a backend for custom or native Linux hosts

## Typical hosts

- GTK, Avalonia, or custom Linux desktop surfaces using host-native text measurement
- export or layout tooling that should follow Linux font discovery rules

## Read next

- [Backend Discovery and Overrides](../guides/backend-discovery-and-overrides)
- [Using Pretext in Native Hosts](../guides/using-pretext-in-native-hosts)
