using System.Globalization;
using Pretext;
using Xunit;

namespace Pretext.Tests;

public sealed partial class PretextLayoutParityTests : IDisposable
{
    private const string Font = "16px Test Sans";
    private const double LineHeight = 19;
    private const string PunctuationCharacters = ".,!?;:%)]}'\"”’»›…—-";
    private static readonly PrepareOptions PreWrap = new(WhiteSpaceMode.PreWrap);

    private readonly record struct LineRangeSnapshot(double Width, LayoutCursor Start, LayoutCursor End);

    public PretextLayoutParityTests()
    {
        PretextLayout.SetMeasurementOverrideForTests(MeasureWidth);
        PretextLayout.SetLocale();
        PretextLayout.ClearCache();
    }

    public void Dispose()
    {
        PretextLayout.SetLocale();
        PretextLayout.SetMeasurementOverrideForTests(null);
    }

    private static IReadOnlyList<double> RequireBreakableWidths(IReadOnlyList<double>? widths)
    {
        return Assert.IsAssignableFrom<IReadOnlyList<double>>(widths);
    }

    private static double ParseFontSize(string font)
    {
        var pxIndex = font.IndexOf("px", StringComparison.Ordinal);
        if (pxIndex < 0)
        {
            return 16;
        }

        var end = pxIndex;
        var start = end - 1;
        while (start >= 0 && (char.IsDigit(font[start]) || font[start] == '.'))
        {
            start--;
        }

        var slice = font[(start + 1)..end];
        return double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var size)
            ? size
            : 16;
    }

    private static bool IsWideCharacter(string ch)
    {
        var code = ch.EnumerateRunes().First().Value;
        return
            (code >= 0x4E00 && code <= 0x9FFF) ||
            (code >= 0x3400 && code <= 0x4DBF) ||
            (code >= 0xF900 && code <= 0xFAFF) ||
            (code >= 0x2F800 && code <= 0x2FA1F) ||
            (code >= 0x20000 && code <= 0x2A6DF) ||
            (code >= 0x2A700 && code <= 0x2B73F) ||
            (code >= 0x2B740 && code <= 0x2B81F) ||
            (code >= 0x2B820 && code <= 0x2CEAF) ||
            (code >= 0x2CEB0 && code <= 0x2EBEF) ||
            (code >= 0x30000 && code <= 0x3134F) ||
            (code >= 0x3000 && code <= 0x303F) ||
            (code >= 0x3040 && code <= 0x309F) ||
            (code >= 0x30A0 && code <= 0x30FF) ||
            (code >= 0xAC00 && code <= 0xD7AF) ||
            (code >= 0xFF00 && code <= 0xFFEF);
    }

    private static bool IsEmojiPresentation(string ch)
    {
        var code = ch.EnumerateRunes().First().Value;
        return
            (code >= 0x1F300 && code <= 0x1FAFF) ||
            (code >= 0x2600 && code <= 0x26FF) ||
            (code >= 0x2700 && code <= 0x27BF);
    }

    private static bool IsPunctuation(string ch)
    {
        return PunctuationCharacters.Contains(ch, StringComparison.Ordinal);
    }

    private static double MeasureWidth(string text, string font)
    {
        var fontSize = ParseFontSize(font);
        var width = 0d;

        foreach (var rune in text.EnumerateRunes())
        {
            var ch = rune.ToString();
            if (ch == " ")
            {
                width += fontSize * 0.33;
            }
            else if (ch == "\t")
            {
                width += fontSize * 1.32;
            }
            else if (IsEmojiPresentation(ch) || ch == "\uFE0F")
            {
                width += fontSize;
            }
            else if (IsWideCharacter(ch))
            {
                width += fontSize;
            }
            else if (IsPunctuation(ch))
            {
                width += fontSize * 0.4;
            }
            else
            {
                width += fontSize * 0.6;
            }
        }

        return width;
    }

    private static double NextTabAdvance(double lineWidth, double spaceWidth, int tabSize = 8)
    {
        var tabStopAdvance = spaceWidth * tabSize;
        var remainder = lineWidth % tabStopAdvance;
        return Math.Abs(remainder) <= 1e-12 ? tabStopAdvance : tabStopAdvance - remainder;
    }
}
