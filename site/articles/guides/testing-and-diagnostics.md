---
title: "Testing and Diagnostics"
---

# Testing and Diagnostics

The test project shows the best patterns for keeping behavior deterministic.

## Useful tools in the library

- `ProfilePrepare` for coarse timing of preparation work
- `SetLocale` to stabilize locale-sensitive segmentation
- `ClearCache` to reset cached font state between tests

## Useful patterns in the repo

- Override measurement in tests so widths are deterministic
- Assert both `Layout` and `LayoutWithLines`
- Compare streamed `LayoutNextLine` output with batched line materialization
- Cover multilingual strings, preserved whitespace, tabs, soft hyphens, and zero-width breaks

## What the parity suite covers

The current parity tests are a useful support matrix for product code and docs:

- collapsed whitespace in `Normal`
- preserved spaces, tabs, and hard breaks in `PreWrap`
- hanging trailing whitespace
- discretionary soft hyphens
- zero-width break opportunities
- URL-like and punctuation-chain tokenization
- Arabic, Devanagari, Myanmar, CJK, and Hangul punctuation attachment
- mixed-direction text
- locale-aware Thai segmentation
- alignment between `Layout`, `LayoutWithLines`, `LayoutNextLine`, and `WalkLineRanges`

## Profiling advice

Use `ProfilePrepare` when you are investigating:

- how much of your cost is in analysis versus measurement
- whether a text source should be prepared once and cached
- whether a host is reparsing or remeasuring too often

If you change tokenization or break behavior, the parity tests are the first place to verify regressions.
