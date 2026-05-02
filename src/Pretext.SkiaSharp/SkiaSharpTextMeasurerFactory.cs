using SkiaSharp;

namespace Pretext.SkiaSharp;

public sealed class SkiaSharpTextMeasurerFactory : IPretextTextMeasurerFactory, IPretextTextShaperFactory
{
    public string Name => "SkiaSharp";

    public bool IsSupported => true;

    public int Priority => 0;

    public IPretextTextMeasurer Create(string font)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        var spec = FontSpec.FromDescriptor(PretextFontParser.Parse(font));
        return new SkiaSharpTextMeasurer(spec);
    }

    public IPretextTextShaper CreateShaper(string font)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        var spec = FontSpec.FromDescriptor(PretextFontParser.Parse(font));
        return new SkiaSharpTextMeasurer(spec);
    }

    private sealed class SkiaSharpTextMeasurer : IPretextTextMeasurer, IPretextTextShaper
    {
        private readonly SKFont _font;
        private readonly string _fontIdentity;

        public SkiaSharpTextMeasurer(FontSpec spec)
        {
            var typeface = SKTypeface.FromFamilyName(spec.PrimaryFamily, spec.FontStyle) ?? SKTypeface.Default;
            _font = new SKFont
            {
                Size = spec.Size,
                Typeface = typeface,
                Subpixel = true,
            };
            _fontIdentity = typeface.FamilyName ?? spec.PrimaryFamily;
        }

        public double MeasureText(string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : _font.MeasureText(text);
        }

        public PretextShapedRun ShapeText(string text, PretextShapeOptions? options = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new PretextShapedRun(
                    PretextGlyphRunKind.Mapped,
                    Array.Empty<PretextShapedGlyph>(),
                    new[] { new PretextShapedFontRun(0, _fontIdentity, 0, 0) },
                    0,
                    0);
            }

            var glyphIds = _font.Typeface.GetGlyphs(text);
            if (glyphIds.Length == 0)
            {
                return new PretextShapedRun(
                    PretextGlyphRunKind.Mapped,
                    Array.Empty<PretextShapedGlyph>(),
                    new[] { new PretextShapedFontRun(0, _fontIdentity, 0, 0) },
                    0,
                    0);
            }

            var positions = new SKPoint[glyphIds.Length];
            _font.GetGlyphPositions(glyphIds, positions, SKPoint.Empty);
            var totalWidth = _font.MeasureText(text);
            var glyphs = new PretextShapedGlyph[glyphIds.Length];
            for (var index = 0; index < glyphIds.Length; index++)
            {
                var x = positions[index].X;
                var nextX = index + 1 < positions.Length ? positions[index + 1].X : totalWidth;
                glyphs[index] = new PretextShapedGlyph(
                    glyphIds[index],
                    GetCluster(text, index),
                    x,
                    positions[index].Y,
                    nextX - x,
                    0,
                    0,
                    0,
                    0);
            }

            return new PretextShapedRun(
                PretextGlyphRunKind.Mapped,
                glyphs,
                new[] { new PretextShapedFontRun(0, _fontIdentity, 0, glyphs.Length) },
                totalWidth,
                0);
        }

        public void Dispose()
        {
            _font.Dispose();
        }

        private static int GetCluster(string text, int glyphIndex)
        {
            if (text.Length == 0)
            {
                return 0;
            }

            return Math.Min(glyphIndex, text.Length - 1);
        }
    }

    private readonly struct FontSpec
    {
        public FontSpec(float size, string primaryFamily, SKFontStyle fontStyle)
        {
            Size = size;
            PrimaryFamily = primaryFamily;
            FontStyle = fontStyle;
        }

        public float Size { get; }

        public string PrimaryFamily { get; }

        public SKFontStyle FontStyle { get; }

        public static FontSpec FromDescriptor(PretextFontDescriptor descriptor)
        {
            var primaryFamily = PretextFontParser.MapGenericFamily(
                descriptor.PrimaryFamily,
                sansSerifFallback: "Arial",
                serifFallback: "Times New Roman",
                monospaceFallback: "Menlo");
            var styleWeight = descriptor.Weight >= 700 ? SKFontStyleWeight.Bold : descriptor.Weight >= 500 ? SKFontStyleWeight.Medium : SKFontStyleWeight.Normal;
            var slant = descriptor.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            return new FontSpec((float)descriptor.Size, primaryFamily, new SKFontStyle(styleWeight, SKFontStyleWidth.Normal, slant));
        }
    }
}
