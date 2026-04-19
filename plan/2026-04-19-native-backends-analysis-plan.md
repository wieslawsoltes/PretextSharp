# Pretext Native Backends Analysis And Implementation Plan

## Goal

Add first-party native text-measurement backends for:

- Windows via DirectWrite
- Linux via FreeType + Fontconfig + optional HarfBuzz shaping
- macOS via CoreText

The new native backends must coexist with the existing `Pretext.SkiaSharp` backend without reintroducing renderer-specific code into the core `Pretext` package.

## MewUI Analysis

### 1. Core/backend split

`MewUI` keeps its backend abstractions in the core assembly and pushes renderer/platform-specific implementations into separate backend packages.

Relevant files:

- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Rendering/IGraphicsFactory.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/PR_SUMMARY_mewvg_skia_platform_packages.md`

Observed pattern:

- core defines backend-neutral contracts
- concrete backends live in separate assemblies
- renderer-specific implementation details are kept out of the core package
- external or optional backends are allowed to evolve independently

### 2. Windows text stack

`MewUI` uses DirectWrite for Windows text measurement and text layout.

Relevant files:

- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/Direct2D/Direct2DGraphicsFactory.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/Direct2D/Direct2DMeasurementContext.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/Direct2D/DirectWriteFont.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Native/Win32/DirectWrite/*`

Observed pattern:

- lazy `DWriteCreateFactory`
- per-font native object storing size/family/weight/italic
- measurement via `CreateTextFormat` + `CreateTextLayout` + `GetMetrics`
- cached font metrics separate from string-measurement calls

### 3. Linux text stack

`MewUI` uses FreeType for font loading and glyph metrics, Fontconfig for font resolution, and HarfBuzz when available for shaping.

Relevant files:

- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/FreeType/FreeTypeFont.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/FreeType/FreeTypeText.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/FreeType/FreeTypeFaceCache.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/FreeType/LinuxFontResolver.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/FreeType/LinuxFontFallbackResolver.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/FreeType/HarfBuzzShaper.cs`

Observed pattern:

- resolve family name to an actual file path with Fontconfig
- cache FreeType faces keyed by path/size/style
- measure with HarfBuzz shaping when available
- fall back to direct glyph advances and kerning when shaping is unavailable
- resolve fallback fonts only when glyphs are missing

### 4. macOS text stack

`MewUI` uses CoreText for fonts and measurement, with CoreFoundation/CoreGraphics interop.

Relevant files:

- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/CoreText/CoreTextFont.cs`
- `/Users/wieslawsoltes/GitHub/MewUI/src/MewUI/Shared/Rendering/CoreText/CoreTextText.cs`

Observed pattern:

- create `CTFont` from family + size + style traits
- query ascent/descent/leading/cap-height from CoreText
- measure text using CoreText line/layout APIs
- register private fonts with CoreText when needed

## Mapping To Pretext

`Pretext` only needs text measurement, not drawing or retained rendering state. That means the Pretext native backends can be materially smaller than the full `MewUI` rendering stacks.

### What Pretext needs from each backend

- construct a measurer from the CSS-like `font` string
- measure arbitrary strings repeatedly
- handle Unicode text acceptably on each host stack
- keep resource lifetime local to the backend instance

### What Pretext does not need

- rasterization
- render targets
- brushes/pens/images
- platform window resources
- full text layout/ellipsis/wrapping APIs from the native stack

## Pretext Design Decisions

### 1. Keep the core package backend-neutral

Continue the current split:

- `Pretext.Contracts`: contracts and shared backend helpers
- `Pretext`: layout engine
- renderer/native packages as separate projects

This follows the same architectural direction visible in `MewUI` and in `PR_SUMMARY_mewvg_skia_platform_packages.md`.

### 2. Add one package per native backend

Planned packages:

- `Pretext.DirectWrite`
- `Pretext.FreeType`
- `Pretext.CoreText`

Rationale:

- clean dependency boundaries
- no Windows/macOS/Linux native interop inside `Pretext`
- mirrors the “separate backend packages” pattern from MewUI
- users can install only the backend they need

### 3. Promote shared font parsing into contracts

All first-party backends should interpret the `font` string the same way.

Move the CSS-like font parser out of the Skia-only backend into `Pretext.Contracts` so:

- SkiaSharp and native backends stay behaviorally aligned
- custom backend implementers can reuse the parser
- docs no longer need to describe SkiaSharp as the canonical parser

### 4. Allow multiple referenced backends to coexist

The current backend discovery assumes exactly one available backend. That becomes too fragile once native packages exist beside `Pretext.SkiaSharp`.

Planned change:

- extend `IPretextTextMeasurerFactory` with backend metadata:
  - display name
  - `IsSupported`
  - `Priority`
- discovery should instantiate candidates, filter by `IsSupported`, then choose the highest-priority supported backend
- native backends should outrank `Pretext.SkiaSharp` on their host OS
- ties at the same priority should still fail fast and require explicit `SetTextMeasurerFactory(...)`

## Implementation Plan

### Step 1. Contracts and discovery

- add shared font-descriptor/parser types to `Pretext.Contracts`
- extend the factory contract with support/priority metadata
- update backend discovery in `PretextLayout.Backends.cs`
- keep explicit `SetTextMeasurerFactory(...)` as the override path

### Step 2. Windows backend

- add `src/Pretext.DirectWrite`
- implement a minimal DirectWrite interop layer:
  - `DWriteCreateFactory`
  - `CreateTextFormat`
  - `CreateTextLayout`
  - `GetMetrics`
- implement `DirectWriteTextMeasurerFactory`
- reuse shared font parsing from contracts

### Step 3. Linux backend

- add `src/Pretext.FreeType`
- implement:
  - Fontconfig family resolution
  - FreeType library/face cache
  - HarfBuzz availability + shaping path
  - glyph-advance fallback path when HarfBuzz is unavailable
- implement `FreeTypeTextMeasurerFactory`

### Step 4. macOS backend

- add `src/Pretext.CoreText`
- implement:
  - CoreText font creation with traits
  - CoreText line measurement for width
  - CoreText metric queries where useful
- implement `CoreTextTextMeasurerFactory`

### Step 5. Verification and documentation

- add the new projects to the solution
- update package docs and installation guidance
- adjust tests so:
  - deterministic fake-measurement tests still use explicit factories
  - auto-discovery works with multiple installed backends
  - current-host smoke tests validate the native backend on the machine running tests
- run:
  - `dotnet build PretextSamples.slnx`
  - `dotnet test ...`
  - `dotnet pack ...` for all packages

## Risks

### Windows

- DirectWrite interop is COM/vtable-based, so bad signatures fail at runtime, not compile time.

Mitigation:

- keep the interop surface minimal
- only implement methods needed for width measurement

### Linux

- shaping quality depends on HarfBuzz availability
- system font availability varies by distro

Mitigation:

- make HarfBuzz optional
- use Fontconfig first, heuristic fallback second
- keep a non-shaped glyph-advance path for environments where HarfBuzz is missing

### macOS

- CoreText trait application is subtle for non-system fonts and font-family aliases

Mitigation:

- follow the `MewUI` pattern of creating a base `CTFont` first, then applying weight/slant traits

## Expected Result

After implementation:

- `Pretext` remains backend-agnostic
- `Pretext.SkiaSharp` remains the existing generic backend
- `Pretext.DirectWrite`, `Pretext.FreeType`, and `Pretext.CoreText` provide first-party native measurement backends
- apps can install native backends per OS and still keep SkiaSharp as an alternative backend
