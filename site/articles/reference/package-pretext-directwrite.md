---
title: "Package: Pretext.DirectWrite"
---

# Package: Pretext.DirectWrite

`Pretext.DirectWrite` is the first-party Windows-native measurement backend.

## Install it when

- your host runs on Windows and you want native DirectWrite metrics
- you want `Pretext` to prefer the Windows-native backend automatically
- you are not using SkiaSharp as your measurement source of truth

## Runtime behavior

The package advertises `DirectWriteTextMeasurerFactory` to the core engine.

- `Name`: `DirectWrite`
- `IsSupported`: `true` on Windows
- `Priority`: `100`

When `Pretext.DirectWrite` and `Pretext` are both referenced, backend discovery will choose it on Windows unless you explicitly configure another factory.

## Relationship to the rest of the stack

- depends on `Pretext.Contracts`
- does not depend on `Pretext.Uno`
- can live beside `Pretext.SkiaSharp`, which remains the lower-priority fallback

## Typical hosts

- WinUI or Uno desktop hosts that want native Windows metrics
- custom Windows controls
- document or editor surfaces that do their own drawing and placement

## Read next

- [Backend Discovery and Overrides](../guides/backend-discovery-and-overrides)
- [Using Pretext in Native Hosts](../guides/using-pretext-in-native-hosts)
