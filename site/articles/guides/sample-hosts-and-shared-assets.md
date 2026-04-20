---
title: "Sample Hosts and Shared Assets"
---

# Sample Hosts and Shared Assets

The sample story is no longer a single Uno app. The repository now has a shared sample layer plus two concrete hosts.

## Sample projects

| Project | Role |
| --- | --- |
| `samples/PretextSamples.Shared` | Shared sample catalog, text assets, markdown/chat models, and reusable prepared-layout logic |
| `samples/PretextSamples` | Uno Platform host that uses `Pretext.Uno` |
| `samples/PretextSamples.MacOS` | Native AppKit host on `net10.0-macos` that binds `Pretext` to `Pretext.CoreText` |

None of the sample projects are packable NuGet packages.

## Why the split matters

The shared project keeps the interesting sample logic outside the UI host:

- sample catalog and tags
- text fixtures and JSON assets
- markdown and rich-note models
- comparison and sample data used by multiple hosts

That lets the Uno and macOS hosts demonstrate the same scenarios without duplicating the data/model layer.

## Host responsibilities

The hosts diverge only where they should:

- `PretextSamples` owns Uno navigation, controls, and sample views
- `PretextSamples.MacOS` owns AppKit startup, native shell layout, and AppKit page views

Both hosts still demonstrate the same sample catalog:

- Overview
- Accordion
- Bubbles
- Masonry
- Rich Text
- Markdown Chat
- Dynamic Layout
- Editorial Engine
- Justification Comparison
- Variable ASCII

## Recommended reading order

1. `samples/PretextSamples.Shared/Samples/SampleCatalog.cs`
2. shared models and data under `samples/PretextSamples.Shared/Samples`
3. host-specific views under `samples/PretextSamples/Samples`
4. native AppKit pages under `samples/PretextSamples.MacOS/Pages`

## Read next

- [Sample Gallery Tour](sample-gallery-tour)
- [Using Pretext in Native Hosts](using-pretext-in-native-hosts)
- [Integrating in Uno](integrating-in-uno)
