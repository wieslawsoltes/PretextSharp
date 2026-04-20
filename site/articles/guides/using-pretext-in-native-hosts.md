---
title: "Using Pretext in Native Hosts"
---

# Using Pretext in Native Hosts

The new first-party backend split lets a host use the operating system's text stack directly instead of routing everything through SkiaSharp.

## Recommended package sets

```bash
dotnet add package Pretext
dotnet add package Pretext.DirectWrite   # Windows
dotnet add package Pretext.FreeType      # Linux
dotnet add package Pretext.CoreText      # macOS
```

Add `Pretext.Layout` as well when you want shrinkwrap or obstacle-layout helpers outside Uno.

## Generic native-host pattern

1. Pick the OS-native backend package.
2. Prepare text when content or font changes.
3. Re-run layout when width changes.
4. Draw or place the resulting lines in the host UI.

## Auto-discovery vs explicit startup configuration

If only one native backend is referenced for the current OS, discovery is usually enough. Use an explicit factory assignment when your app startup should make the backend obvious in code:

```csharp
using Pretext;
using Pretext.CoreText;

PretextLayout.SetTextMeasurerFactory(new CoreTextTextMeasurerFactory());
```

That is the pattern used by the native macOS sample host in `samples/PretextSamples.MacOS/AppDelegate.cs`.

## Typical native-host uses

- AppKit or interop-based macOS surfaces
- custom Windows desktop controls
- Linux desktop tooling that should follow host-native font lookup
- export pipelines where measurement fidelity should match the current OS

## Helper packages

Pair native backends with:

- `Pretext.Layout` for wrap metrics, columns, and obstacle carving
- `Pretext.Uno` only when your UI host itself is Uno Platform

## Read next

- [Backend Discovery and Overrides](backend-discovery-and-overrides)
- [Sample Hosts and Shared Assets](sample-hosts-and-shared-assets)
