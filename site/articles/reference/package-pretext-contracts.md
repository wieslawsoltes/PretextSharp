---
title: "Package: Pretext.Contracts"
---

# Package: Pretext.Contracts

`Pretext.Contracts` is the backend seam for `Pretext`. It contains the public interfaces and shared font-string parsing helpers used by first-party backends and custom integrations.

## Install it when

- you are implementing your own measurement backend
- you want to ship a backend package independently from the core engine
- you want the shared CSS-like font parser used by the first-party backends

## Public surface

The package exposes:

- `IPretextTextMeasurer`
- `IPretextTextMeasurerFactory`
- `PretextTextMeasurerFactoryAttribute`
- `PretextFontDescriptor`
- `PretextFontParser`

## Backend contract shape

`IPretextTextMeasurerFactory` advertises:

- `Name`
- `IsSupported`
- `Priority`
- `Create(string font)`

`IPretextTextMeasurer` is intentionally small: it measures text width for the prepared segments that the core engine hands to it.

## Shared font parsing

The first-party backends all parse the same CSS-like subset:

- size in `px`
- optional numeric weight or `bold`
- optional `italic` or `oblique`
- family list where the first family wins

Examples:

- `16px Inter`
- `italic 16px Georgia`
- `700 18px "IBM Plex Sans"`

`PretextFontParser.MapGenericFamily(...)` also helps backend authors map `system-ui`, `sans-serif`, `serif`, and `monospace` to host-specific fallback families.

## Discovery advertisement

Backends advertise their factory with an assembly-level `PretextTextMeasurerFactoryAttribute`. `Pretext` scans loaded and probeable `Pretext*.dll` assemblies for those attributes during runtime discovery.

## Read next

- [Package: Pretext](package-pretext)
- [Implementing a Custom Backend](../guides/implementing-a-custom-backend)
- [Backend Discovery and Overrides](../guides/backend-discovery-and-overrides)
