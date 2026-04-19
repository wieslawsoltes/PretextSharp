using Pretext;

namespace Pretext.Tests;

internal sealed class DelegateTextMeasurerFactory : IPretextTextMeasurerFactory
{
    private readonly Func<string, string, double> _measureText;

    public string Name => "Delegate";

    public bool IsSupported => true;

    public int Priority => int.MaxValue;

    public DelegateTextMeasurerFactory(Func<string, string, double> measureText)
    {
        _measureText = measureText ?? throw new ArgumentNullException(nameof(measureText));
    }

    public IPretextTextMeasurer Create(string font)
    {
        return new DelegateTextMeasurer(font, _measureText);
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
}
