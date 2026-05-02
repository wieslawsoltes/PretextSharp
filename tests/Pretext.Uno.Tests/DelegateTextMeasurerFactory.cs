using Pretext;

namespace Pretext.Tests;

internal sealed class DelegateTextMeasurerFactory : IPretextTextMeasurerFactory, IPretextTextShaperFactory
{
    private readonly Func<string, string, double> _measureText;
    private readonly Func<string, string, PretextShapedRun>? _shapeText;

    public string Name => "Delegate";

    public bool IsSupported => true;

    public int Priority => int.MaxValue;

    public DelegateTextMeasurerFactory(
        Func<string, string, double> measureText,
        Func<string, string, PretextShapedRun>? shapeText = null)
    {
        _measureText = measureText ?? throw new ArgumentNullException(nameof(measureText));
        _shapeText = shapeText;
    }

    public IPretextTextMeasurer Create(string font)
    {
        return new DelegateTextMeasurer(font, _measureText);
    }

    public IPretextTextShaper CreateShaper(string font)
    {
        return new DelegateTextShaper(font, _shapeText);
    }

    private sealed class DelegateTextMeasurer : IPretextTextMeasurer
    {
        private readonly string _font;
        private readonly Func<string, string, double> _measureText;

        public DelegateTextMeasurer(string font, Func<string, string, double> measureText)
        {
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _measureText = measureText;
        }

        public double MeasureText(string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : _measureText(text, _font);
        }

        public void Dispose()
        {
        }
    }

    private sealed class DelegateTextShaper : IPretextTextShaper
    {
        private readonly string _font;
        private readonly Func<string, string, PretextShapedRun>? _shapeText;

        public DelegateTextShaper(string font, Func<string, string, PretextShapedRun>? shapeText)
        {
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _shapeText = shapeText;
        }

        public PretextShapedRun ShapeText(string text, PretextShapeOptions? options = null)
        {
            if (_shapeText is not null)
            {
                return _shapeText(text, _font);
            }

            var glyphs = new PretextShapedGlyph[text.Length];
            double x = 0;
            for (var index = 0; index < text.Length; index++)
            {
                glyphs[index] = new PretextShapedGlyph(text[index], index, x, 0, 1, 0, 0, 0, 0);
                x++;
            }

            return new PretextShapedRun(
                PretextGlyphRunKind.Mapped,
                glyphs,
                new[] { new PretextShapedFontRun(0, _font, 0, glyphs.Length) },
                x,
                0);
        }

        public void Dispose()
        {
        }
    }
}
