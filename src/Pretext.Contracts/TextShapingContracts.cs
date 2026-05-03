namespace Pretext;

public enum PretextGlyphRunKind
{
    Shaped = 0,
    Mapped = 1,
}

public enum PretextTextDirection
{
    Auto = 0,
    LeftToRight = 1,
    RightToLeft = 2,
}

public sealed class PretextShapeOptions
{
    public static PretextShapeOptions Default => new();

    public PretextTextDirection Direction { get; set; }
}

public readonly struct PretextShapedGlyph
{
    public PretextShapedGlyph(
        uint glyphId,
        int cluster,
        double x,
        double y,
        double xAdvance,
        double yAdvance,
        double xOffset,
        double yOffset,
        int fontRunIndex)
    {
        if (cluster < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cluster));
        }

        if (fontRunIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontRunIndex));
        }

        GlyphId = glyphId;
        Cluster = cluster;
        X = x;
        Y = y;
        XAdvance = xAdvance;
        YAdvance = yAdvance;
        XOffset = xOffset;
        YOffset = yOffset;
        FontRunIndex = fontRunIndex;
    }

    public uint GlyphId { get; }

    public int Cluster { get; }

    public double X { get; }

    public double Y { get; }

    public double XAdvance { get; }

    public double YAdvance { get; }

    public double XOffset { get; }

    public double YOffset { get; }

    public int FontRunIndex { get; }
}

public sealed class PretextShapedFontRun
{
    public PretextShapedFontRun(int index, string font, int firstGlyphIndex, int glyphCount)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        if (firstGlyphIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(firstGlyphIndex));
        }

        if (glyphCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(glyphCount));
        }

        Index = index;
        Font = font;
        FirstGlyphIndex = firstGlyphIndex;
        GlyphCount = glyphCount;
    }

    public int Index { get; }

    public string Font { get; }

    public int FirstGlyphIndex { get; }

    public int GlyphCount { get; }
}

public sealed class PretextShapedRun
{
    public PretextShapedRun(
        PretextGlyphRunKind kind,
        IReadOnlyList<PretextShapedGlyph> glyphs,
        IReadOnlyList<PretextShapedFontRun> fontRuns,
        double advanceX,
        double advanceY)
    {
        if (glyphs is null)
        {
            throw new ArgumentNullException(nameof(glyphs));
        }

        if (fontRuns is null)
        {
            throw new ArgumentNullException(nameof(fontRuns));
        }

        var glyphArray = glyphs.ToArray();
        var fontRunArray = fontRuns.ToArray();

        Kind = kind;
        Glyphs = Array.AsReadOnly(glyphArray);
        FontRuns = Array.AsReadOnly(fontRunArray);
        AdvanceX = advanceX;
        AdvanceY = advanceY;
        HasMissingGlyphs = Array.Exists(glyphArray, static glyph => glyph.GlyphId == 0);
    }

    public PretextGlyphRunKind Kind { get; }

    public IReadOnlyList<PretextShapedGlyph> Glyphs { get; }

    public IReadOnlyList<PretextShapedFontRun> FontRuns { get; }

    public double AdvanceX { get; }

    public double AdvanceY { get; }

    public bool HasMissingGlyphs { get; }

    public bool IsShaped => Kind == PretextGlyphRunKind.Shaped;
}

public interface IPretextTextShaper : IDisposable
{
    PretextShapedRun ShapeText(string text, PretextShapeOptions? options = null);
}

public interface IPretextTextShaperFactory
{
    string Name { get; }

    bool IsSupported { get; }

    int Priority { get; }

    IPretextTextShaper CreateShaper(string font);
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class PretextTextShaperFactoryAttribute : Attribute
{
    public PretextTextShaperFactoryAttribute(Type factoryType)
    {
        FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
    }

    public Type FactoryType { get; }
}
