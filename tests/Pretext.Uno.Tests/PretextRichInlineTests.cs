using System.Text;
using Pretext;
using Xunit;

namespace Pretext.Tests;

public sealed class PretextRichInlineTests : IDisposable
{
    private const string Font = "16px Test Sans";

    public PretextRichInlineTests()
    {
        PretextLayout.SetTextMeasurerFactory(new DelegateTextMeasurerFactory(PretextLayoutParityTests_Accessor.MeasureWidth));
        PretextLayout.ClearCache();
    }

    public void Dispose()
    {
        PretextLayout.SetTextMeasurerFactory(null);
        PretextLayout.ClearCache();
    }

    [Fact(DisplayName = "non-materializing rich-inline range walker matches materialization")]
    public void RichInline_NonMaterializingRangeWalkerMatchesMaterialization()
    {
        var prepared = PretextLayout.PrepareRichInline(
        [
            new RichInlineItem("Ship ", Font),
            new RichInlineItem("@maya", "700 12px Test Sans", RichInlineBreakMode.Never, 18),
            new RichInlineItem("'s rich note wraps cleanly", Font),
        ]);

        var rangedLines = new List<(double Width, RichInlineCursor End, RichInlineFragmentRange[] Fragments)>();
        var materializedLines = new List<(double Width, RichInlineCursor End, RichInlineFragment[] Fragments)>();

        var rangeLineCount = PretextLayout.WalkRichInlineLineRanges(prepared, 120, line =>
        {
            rangedLines.Add((line.Width, line.End, line.Fragments));
        });

        var materializedLineCount = PretextLayout.WalkRichInlineLineRanges(prepared, 120, range =>
        {
            var line = PretextLayout.MaterializeRichInlineLineRange(prepared, range);
            materializedLines.Add((line.Width, line.End, line.Fragments));
        });

        Assert.Equal(rangeLineCount, materializedLineCount);
        Assert.Equal(rangedLines.Count, materializedLines.Count);

        var maxWidth = rangedLines.Count == 0 ? 0 : rangedLines.Max(static line => line.Width);
        Assert.Equal(new RichInlineStats(rangeLineCount, maxWidth), PretextLayout.MeasureRichInlineStats(prepared, 120));

        for (var index = 0; index < rangedLines.Count; index++)
        {
            Assert.Equal(rangedLines[index].Width, materializedLines[index].Width);
            Assert.Equal(rangedLines[index].End, materializedLines[index].End);
            Assert.Equal(
                rangedLines[index].Fragments.Select(static fragment => new
                {
                    fragment.ItemIndex,
                    fragment.GapBefore,
                    fragment.OccupiedWidth,
                    fragment.Start,
                    fragment.End,
                }),
                materializedLines[index].Fragments.Select(static fragment => new
                {
                    fragment.ItemIndex,
                    fragment.GapBefore,
                    fragment.OccupiedWidth,
                    fragment.Start,
                    fragment.End,
                }));
        }
    }
}

internal static class PretextLayoutParityTests_Accessor
{
    private const string PunctuationCharacters = ".,!?;:%)]}'\"”’»›…—-";

    public static double MeasureWidth(string text, string font)
    {
        var fontSize = ParseFontSize(font);
        var width = 0d;
        var previousWasDecimalDigit = false;

        foreach (var rune in text.EnumerateRunes())
        {
            var ch = rune.ToString();
            if (ch == " ")
            {
                width += fontSize * 0.33;
                previousWasDecimalDigit = false;
            }
            else if (ch == "\t")
            {
                width += fontSize * 1.32;
                previousWasDecimalDigit = false;
            }
            else if (IsEmojiPresentation(ch) || ch == "\uFE0F")
            {
                width += fontSize;
                previousWasDecimalDigit = false;
            }
            else if (IsDecimalDigit(ch))
            {
                width += fontSize * (previousWasDecimalDigit ? 0.48 : 0.52);
                previousWasDecimalDigit = true;
            }
            else if (IsWideCharacter(ch))
            {
                width += fontSize;
                previousWasDecimalDigit = false;
            }
            else if (IsPunctuation(ch))
            {
                width += fontSize * 0.4;
                previousWasDecimalDigit = false;
            }
            else
            {
                width += fontSize * 0.6;
                previousWasDecimalDigit = false;
            }
        }

        return width;
    }

    private static bool IsDecimalDigit(string ch)
    {
        var enumerator = ch.EnumerateRunes();
        return enumerator.MoveNext() && Rune.IsDigit(enumerator.Current);
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
        return double.TryParse(slice, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var size)
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
            (code >= 0x2EBF0 && code <= 0x2EE5D) ||
            (code >= 0x30000 && code <= 0x3134F) ||
            (code >= 0x31350 && code <= 0x323AF) ||
            (code >= 0x323B0 && code <= 0x33479) ||
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
}
