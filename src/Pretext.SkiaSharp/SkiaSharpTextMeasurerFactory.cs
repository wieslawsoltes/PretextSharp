using SkiaSharp;

namespace Pretext.SkiaSharp;

public sealed class SkiaSharpTextMeasurerFactory : IPretextTextMeasurerFactory
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

    private sealed class SkiaSharpTextMeasurer : IPretextTextMeasurer
    {
        private readonly SKFont _font;

        public SkiaSharpTextMeasurer(FontSpec spec)
        {
            _font = new SKFont
            {
                Size = spec.Size,
                Typeface = SKTypeface.FromFamilyName(spec.PrimaryFamily, spec.FontStyle) ?? SKTypeface.Default,
                Subpixel = true,
            };
        }

        public double MeasureText(string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : _font.MeasureText(text);
        }

        public void Dispose()
        {
            _font.Dispose();
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
