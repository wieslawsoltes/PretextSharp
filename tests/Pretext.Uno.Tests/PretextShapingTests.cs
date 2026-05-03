using System.Reflection;
using System.Runtime.InteropServices;
using Pretext;
using Xunit;

namespace Pretext.Tests;

public sealed class PretextShapingTests : IDisposable
{
    private const string Font = "16px Test Sans";

    public PretextShapingTests()
    {
        PretextLayout.SetTextMeasurerFactory(new DelegateTextMeasurerFactory(PretextLayoutParityTests_Accessor.MeasureWidth));
        PretextLayout.SetTextShaperFactory(null);
        PretextLayout.ClearCache();
    }

    public void Dispose()
    {
        PretextLayout.SetTextMeasurerFactory(null);
        PretextLayout.SetTextShaperFactory(null);
        PretextLayout.ClearCache();
    }

    [Fact(DisplayName = "shape text uses a shaping-capable configured measurer factory")]
    public void ShapeText_UsesConfiguredMeasurerFactoryWhenItCanShape()
    {
        var shaped = PretextLayout.ShapeText("abc", Font);

        Assert.Equal(PretextGlyphRunKind.Mapped, shaped.Kind);
        Assert.Equal(3, shaped.Glyphs.Count);
        Assert.Single(shaped.FontRuns);
        Assert.Equal(Font, shaped.FontRuns[0].Font);
        Assert.Equal(3, shaped.FontRuns[0].GlyphCount);
        Assert.Equal(3, shaped.AdvanceX);
        Assert.False(shaped.HasMissingGlyphs);
    }

    [Fact(DisplayName = "shape text reuses cached shaped output until cache clear")]
    public void ShapeText_ReusesCachedOutputUntilCacheClear()
    {
        var shapeCalls = 0;
        PretextLayout.SetTextMeasurerFactory(new DelegateTextMeasurerFactory(
            PretextLayoutParityTests_Accessor.MeasureWidth,
            (text, font) =>
            {
                shapeCalls++;
                return CreateSingleGlyphRun(font, shapeCalls);
            }));
        PretextLayout.ClearCache();

        var first = PretextLayout.ShapeText("abc", Font);
        var second = PretextLayout.ShapeText("abc", Font);
        var rtl = PretextLayout.ShapeText(
            "abc",
            Font,
            new PretextShapeOptions { Direction = PretextTextDirection.RightToLeft });

        Assert.Same(first, second);
        Assert.NotSame(first, rtl);
        Assert.Equal(2, shapeCalls);

        PretextLayout.ClearCache();

        var afterClear = PretextLayout.ShapeText("abc", Font);

        Assert.NotSame(first, afterClear);
        Assert.Equal(3, shapeCalls);
    }

    [Fact(DisplayName = "prepared shaped text reuses full prepared shaping for safe line ranges")]
    public void ShapePreparedText_ReusesFullPreparedShapingForSafeLineRanges()
    {
        var shapeCalls = 0;
        PretextLayout.SetTextMeasurerFactory(new DelegateTextMeasurerFactory(
            PretextLayoutParityTests_Accessor.MeasureWidth,
            (text, font) =>
            {
                shapeCalls++;
                return CreateMappedRun(text, font);
            }));
        PretextLayout.ClearCache();

        var prepared = PretextLayout.PrepareWithSegments("hello world", Font);
        var shapedPrepared = PretextLayout.ShapePreparedText(prepared);
        var cachedPrepared = PretextLayout.ShapePreparedText(prepared);
        var line = PretextLayout.LayoutNextLineRange(prepared, new LayoutCursor(0, 0), 1000);
        Assert.NotNull(line);

        var shapedLine = PretextLayout.MaterializeShapedLineRange(shapedPrepared, line!);
        var cachedLine = PretextLayout.MaterializeShapedLineRange(cachedPrepared, line!);

        Assert.Same(shapedPrepared, cachedPrepared);
        Assert.Same(shapedLine.ShapedRun, cachedLine.ShapedRun);
        Assert.Equal(1, shapeCalls);
        Assert.Equal(shapedLine.ShapedRun.Glyphs.Count, cachedLine.ShapedRun.Glyphs.Count);
        Assert.Equal("hello world".Length, shapedLine.ShapedRun.Glyphs.Count);
    }

    [Fact(DisplayName = "prepared shaped line slices full first segment ranges")]
    public void ShapePreparedText_SlicesFullFirstSegmentRanges()
    {
        var shapeCalls = 0;
        PretextLayout.SetTextMeasurerFactory(new DelegateTextMeasurerFactory(
            PretextLayoutParityTests_Accessor.MeasureWidth,
            (text, font) =>
            {
                shapeCalls++;
                return CreateMappedRun(text, font);
            }));
        PretextLayout.ClearCache();

        var prepared = PretextLayout.PrepareWithSegments("abcdef", Font);
        var shapedPrepared = PretextLayout.ShapePreparedText(prepared);
        var line = new LayoutLineRange(6, new LayoutCursor(0, 0), new LayoutCursor(0, 6));

        var shapedLine = PretextLayout.MaterializeShapedLineRange(shapedPrepared, line);

        Assert.Equal(1, shapeCalls);
        Assert.Equal(6, shapedLine.ShapedRun.Glyphs.Count);
        Assert.Equal(6, shapedLine.ShapedRun.AdvanceX);
    }

    [Fact(DisplayName = "prepared shaped line reshapes unsafe partial segment ranges")]
    public void ShapePreparedText_ReshapesUnsafePartialSegmentRanges()
    {
        var shapeCalls = 0;
        PretextLayout.SetTextMeasurerFactory(new DelegateTextMeasurerFactory(
            PretextLayoutParityTests_Accessor.MeasureWidth,
            (text, font) =>
            {
                shapeCalls++;
                return CreateMappedRun(text, font);
            }));
        PretextLayout.ClearCache();

        var prepared = PretextLayout.PrepareWithSegments("abcdef", Font);
        var shapedPrepared = PretextLayout.ShapePreparedText(prepared);
        var partialLine = new LayoutLineRange(2, new LayoutCursor(0, 0), new LayoutCursor(0, 2));

        var shapedLine = PretextLayout.MaterializeShapedLineRange(shapedPrepared, partialLine);
        var cachedLine = PretextLayout.MaterializeShapedLineRange(shapedPrepared, partialLine);

        Assert.Equal(2, shapeCalls);
        Assert.Same(shapedLine.ShapedRun, cachedLine.ShapedRun);
        Assert.Equal(2, shapedLine.ShapedRun.Glyphs.Count);
        Assert.Equal(2, shapedLine.ShapedRun.AdvanceX);
    }

    [Fact(DisplayName = "try shape text returns false when no shaping backend is configured")]
    public void TryShapeText_ReturnsFalseWhenNoShaperIsAvailable()
    {
        PretextLayout.SetTextMeasurerFactory(new MeasurementOnlyFactory());
        PretextLayout.SetTextShaperFactory(new UnsupportedShaperFactory());
        PretextLayout.ClearCache();

        var success = PretextLayout.TryShapeText("abc", Font, out var shaped);

        Assert.False(success);
        Assert.Null(shaped);
    }

    [Fact(DisplayName = "explicit shaper factory survives later measurer factory configuration")]
    public void ShapeText_ExplicitShaperSurvivesMeasurerConfiguration()
    {
        PretextLayout.SetTextShaperFactory(new FixedShaperFactory("ExplicitShaper"));
        PretextLayout.SetTextMeasurerFactory(new MeasurementOnlyFactory());
        PretextLayout.ClearCache();

        var shaped = PretextLayout.ShapeText("abc", Font);

        Assert.Equal("ExplicitShaper", shaped.FontRuns[0].Font);
        Assert.Equal(PretextGlyphRunKind.Mapped, shaped.Kind);
    }

    [Fact(DisplayName = "default shaping options are not shared mutable state")]
    public void ShapeOptions_DefaultReturnsFreshInstance()
    {
        var first = PretextShapeOptions.Default;
        first.Direction = PretextTextDirection.RightToLeft;

        Assert.Equal(PretextTextDirection.Auto, PretextShapeOptions.Default.Direction);
    }

    [Fact(DisplayName = "prepared shape options are returned as snapshots")]
    public void ShapePreparedText_OptionsAreReturnedAsSnapshots()
    {
        var prepared = PretextLayout.PrepareWithSegments("abc", Font);
        var shapedPrepared = PretextLayout.ShapePreparedText(
            prepared,
            new PretextShapeOptions { Direction = PretextTextDirection.LeftToRight });

        var options = shapedPrepared.Options;
        options.Direction = PretextTextDirection.RightToLeft;

        Assert.Equal(PretextTextDirection.LeftToRight, shapedPrepared.Direction);
        Assert.Equal(PretextTextDirection.LeftToRight, shapedPrepared.Options.Direction);
    }

    [Fact(DisplayName = "shape text auto-discovers a shaping backend for the current OS")]
    public void ShapeText_AutoDiscoversShapingBackend()
    {
        PretextLayout.SetTextMeasurerFactory(null);
        PretextLayout.SetTextShaperFactory(null);
        PretextLayout.ClearCache();

        var shaped = PretextLayout.ShapeText("Hello", "16px Arial");
        var selectedFactory = GetSelectedShaperFactory();

        Assert.NotEmpty(shaped.Glyphs);
        Assert.NotEmpty(shaped.FontRuns);
        Assert.True(shaped.AdvanceX > 0);
        Assert.Equal(GetExpectedShaperBackendName(), selectedFactory.Name);
    }

    private static IPretextTextShaperFactory GetSelectedShaperFactory()
    {
        var field = typeof(PretextLayout).GetField("_textShaperFactory", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<IPretextTextShaperFactory>(field!.GetValue(null));
    }

    private static string GetExpectedShaperBackendName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "FreeType";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "CoreText";
        }

        return "SkiaSharp";
    }

    private static PretextShapedRun CreateSingleGlyphRun(string font, double advance)
    {
        var glyph = new PretextShapedGlyph(1, 0, 0, 0, advance, 0, 0, 0, 0);
        return new PretextShapedRun(
            PretextGlyphRunKind.Mapped,
            new[] { glyph },
            new[] { new PretextShapedFontRun(0, font, 0, 1) },
            advance,
            0);
    }

    private static PretextShapedRun CreateMappedRun(string text, string font)
    {
        var glyphs = new PretextShapedGlyph[text.Length];
        for (var index = 0; index < text.Length; index++)
        {
            glyphs[index] = new PretextShapedGlyph(text[index], index, index, 0, 1, 0, 0, 0, 0);
        }

        return new PretextShapedRun(
            PretextGlyphRunKind.Mapped,
            glyphs,
            new[] { new PretextShapedFontRun(0, font, 0, glyphs.Length) },
            glyphs.Length,
            0);
    }

    private sealed class MeasurementOnlyFactory : IPretextTextMeasurerFactory
    {
        public string Name => "MeasurementOnly";

        public bool IsSupported => true;

        public int Priority => int.MaxValue;

        public IPretextTextMeasurer Create(string font)
        {
            return new MeasurementOnlyMeasurer();
        }
    }

    private sealed class MeasurementOnlyMeasurer : IPretextTextMeasurer
    {
        public double MeasureText(string text)
        {
            return text?.Length ?? 0;
        }

        public void Dispose()
        {
        }
    }

    private sealed class UnsupportedShaperFactory : IPretextTextShaperFactory
    {
        public string Name => "Unsupported";

        public bool IsSupported => true;

        public int Priority => int.MaxValue;

        public IPretextTextShaper CreateShaper(string font)
        {
            throw new InvalidOperationException("Test shaper is unavailable.");
        }
    }

    private sealed class FixedShaperFactory : IPretextTextShaperFactory
    {
        private readonly string _fontIdentity;

        public FixedShaperFactory(string fontIdentity)
        {
            _fontIdentity = fontIdentity;
        }

        public string Name => "Fixed";

        public bool IsSupported => true;

        public int Priority => int.MaxValue;

        public IPretextTextShaper CreateShaper(string font)
        {
            return new FixedShaper(_fontIdentity);
        }
    }

    private sealed class FixedShaper : IPretextTextShaper
    {
        private readonly string _fontIdentity;

        public FixedShaper(string fontIdentity)
        {
            _fontIdentity = fontIdentity;
        }

        public PretextShapedRun ShapeText(string text, PretextShapeOptions? options = null)
        {
            var glyph = new PretextShapedGlyph(1, 0, 0, 0, 1, 0, 0, 0, 0);
            return new PretextShapedRun(
                PretextGlyphRunKind.Mapped,
                new[] { glyph },
                new[] { new PretextShapedFontRun(0, _fontIdentity, 0, 1) },
                1,
                0);
        }

        public void Dispose()
        {
        }
    }
}
