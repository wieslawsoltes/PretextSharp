---
title: "Implementing a Custom Backend"
---

# Implementing a Custom Backend

`Pretext.Contracts` makes the measurement layer pluggable. A custom backend only needs to measure text widths for a parsed font descriptor and advertise a factory to the core engine.

## When a custom backend makes sense

- you want to bind `Pretext` to another renderer or platform text API
- you need application-specific measurement behavior in tests
- you want to ship a backend package independently from the main repository

## Minimal shape

Implement:

- `IPretextTextMeasurer`
- `IPretextTextMeasurerFactory`

Advertise the factory at assembly level:

```csharp
[assembly: PretextTextMeasurerFactory(typeof(MyTextMeasurerFactory))]
```

## Typical implementation flow

1. Parse the incoming font string with `PretextFontParser.Parse(font)`.
2. Map `PretextFontDescriptor` to the host font API.
3. Return a measurer that can report width for arbitrary segment text.
4. Set `IsSupported` according to the current runtime.
5. Choose a `Priority` that makes sense beside the first-party backends.

## Example skeleton

```csharp
using Pretext;

public sealed class MyTextMeasurerFactory : IPretextTextMeasurerFactory
{
    public string Name => "MyBackend";

    public bool IsSupported => true;

    public int Priority => 50;

    public IPretextTextMeasurer Create(string font)
    {
        var descriptor = PretextFontParser.Parse(font);
        return new MyTextMeasurer(descriptor);
    }
}

public sealed class MyTextMeasurer : IPretextTextMeasurer
{
    public MyTextMeasurer(PretextFontDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public PretextFontDescriptor Descriptor { get; }

    public double MeasureText(string text)
    {
        // Call into the host text API here.
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
```

## Priority guidance

- use a priority above `0` if your backend should beat the SkiaSharp fallback
- use the same top priority as another supported backend only if your app will always configure the backend explicitly
- keep test-only backends explicit rather than relying on discovery

## Read next

- [Package: Pretext.Contracts](../reference/package-pretext-contracts)
- [Backend Discovery and Overrides](backend-discovery-and-overrides)
