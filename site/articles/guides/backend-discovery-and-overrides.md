---
title: "Backend Discovery and Overrides"
---

# Backend Discovery and Overrides

The branch split the text-measurement layer out of `Pretext` itself. The engine is now backend-agnostic and resolves measurement through `Pretext.Contracts`.

## Default behavior

If you do not set a factory explicitly, `Pretext`:

1. scans loaded and probeable `Pretext*.dll` assemblies
2. finds assembly-level `PretextTextMeasurerFactoryAttribute` declarations
3. instantiates the advertised factories
4. keeps only factories whose `IsSupported` is `true`
5. selects the supported factory with the highest `Priority`

## First-party priorities

| Backend | OS support | Priority |
| --- | --- | --- |
| `Pretext.DirectWrite` | Windows | `100` |
| `Pretext.FreeType` | Linux | `100` |
| `Pretext.CoreText` | macOS | `100` |
| `Pretext.SkiaSharp` | Any OS | `0` |

That means a host-native backend wins on its matching OS, while `Pretext.SkiaSharp` stays available as the portable fallback.

## Explicit override

Use `PretextLayout.SetTextMeasurerFactory(...)` when:

- you want deterministic startup behavior regardless of discovery
- tests should force a specific backend
- your app ships multiple supported custom backends
- you want to opt into a non-default backend

```csharp
using Pretext;
using Pretext.CoreText;

PretextLayout.SetTextMeasurerFactory(new CoreTextTextMeasurerFactory());
```

Calling `SetTextMeasurerFactory(...)` also clears the cached font state so the new backend takes effect immediately.

## Failure modes

If no supported backend is found, `Pretext` throws and tells you to either:

- reference a backend package such as `Pretext.SkiaSharp`, or
- call `PretextLayout.SetTextMeasurerFactory(...)`

If multiple supported backends exist at the same highest priority, `Pretext` throws and requires an explicit selection.

## Recommended package sets

| Host type | Recommended packages |
| --- | --- |
| Windows-native host | `Pretext` + `Pretext.DirectWrite` |
| Linux-native host | `Pretext` + `Pretext.FreeType` |
| macOS-native host | `Pretext` + `Pretext.CoreText` |
| Generic SkiaSharp host | `Pretext` + `Pretext.SkiaSharp` |
| Uno app | `Pretext.Uno` |

## Read next

- [Using Pretext in Native Hosts](using-pretext-in-native-hosts)
- [Using Pretext in Any SkiaSharp Host](using-pretext-in-any-skiasharp-host)
- [Implementing a Custom Backend](implementing-a-custom-backend)
