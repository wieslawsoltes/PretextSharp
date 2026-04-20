using System.Globalization;
using System.Text;
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
        PretextLayout.SetTextMeasurerFactory(new DelegateTextMeasurerFactory(MeasureWidth));
        PretextLayout.SetLocale();
        PretextLayout.ClearCache();
    }

    public void Dispose()
    {
        PretextLayout.SetLocale();
        PretextLayout.SetTextMeasurerFactory(null);
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

    private static double MeasureWidth(string text, string font)
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

    private static double NextTabAdvance(double lineWidth, double spaceWidth, int tabSize = 8)
    {
        var tabStopAdvance = spaceWidth * tabSize;
        var remainder = lineWidth % tabStopAdvance;
        return Math.Abs(remainder) <= 1e-12 ? tabStopAdvance : tabStopAdvance - remainder;
    }

    private static bool IsDecimalDigit(string ch)
    {
        var enumerator = ch.EnumerateRunes();
        return enumerator.MoveNext() && Rune.IsDigit(enumerator.Current);
    }

    private static string[] GetSegmentGraphemes(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var graphemes = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            graphemes.Add(Assert.IsType<string>(enumerator.GetTextElement()));
        }

        return graphemes.ToArray();
    }

    private static string SlicePreparedText(PreparedTextWithSegments prepared, LayoutCursor start, LayoutCursor end)
    {
        if (start.SegmentIndex == end.SegmentIndex)
        {
            var segment = start.SegmentIndex < prepared.Segments.Count
                ? prepared.Segments[start.SegmentIndex]
                : null;
            if (segment is null)
            {
                return string.Empty;
            }

            var graphemes = GetSegmentGraphemes(segment);
            return string.Concat(graphemes.Skip(start.GraphemeIndex).Take(end.GraphemeIndex - start.GraphemeIndex));
        }

        var builder = new StringBuilder();
        for (var segmentIndex = start.SegmentIndex; segmentIndex < end.SegmentIndex; segmentIndex++)
        {
            if (segmentIndex >= prepared.Segments.Count)
            {
                break;
            }

            var segment = prepared.Segments[segmentIndex];
            if (segmentIndex == start.SegmentIndex && start.GraphemeIndex > 0)
            {
                var graphemes = GetSegmentGraphemes(segment);
                for (var graphemeIndex = start.GraphemeIndex; graphemeIndex < graphemes.Length; graphemeIndex++)
                {
                    builder.Append(graphemes[graphemeIndex]);
                }
            }
            else
            {
                builder.Append(segment);
            }
        }

        if (end.GraphemeIndex > 0 && end.SegmentIndex < prepared.Segments.Count)
        {
            var segment = prepared.Segments[end.SegmentIndex];
            var graphemes = GetSegmentGraphemes(segment);
            for (var graphemeIndex = 0; graphemeIndex < end.GraphemeIndex; graphemeIndex++)
            {
                builder.Append(graphemes[graphemeIndex]);
            }
        }

        return builder.ToString();
    }

    private static string ReconstructFromLineBoundaries(PreparedTextWithSegments prepared, IReadOnlyList<LayoutLine> lines)
    {
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            builder.Append(SlicePreparedText(prepared, line.Start, line.End));
        }

        return builder.ToString();
    }

    private static List<LayoutLine> CollectStreamedLines(
        PreparedTextWithSegments prepared,
        double width,
        LayoutCursor? start = null)
    {
        var lines = new List<LayoutLine>();
        var cursor = start ?? new LayoutCursor(0, 0);

        while (true)
        {
            var line = PretextLayout.LayoutNextLine(prepared, cursor, width);
            if (line is null)
            {
                break;
            }

            lines.Add(line);
            cursor = line.End;
        }

        return lines;
    }

    private static List<LayoutLine> CollectStreamedLinesWithWidths(
        PreparedTextWithSegments prepared,
        IReadOnlyList<double> widths,
        LayoutCursor? start = null)
    {
        var lines = new List<LayoutLine>();
        var cursor = start ?? new LayoutCursor(0, 0);
        var widthIndex = 0;

        while (true)
        {
            Assert.True(widthIndex < widths.Count, "CollectStreamedLinesWithWidths requires enough widths to finish the paragraph.");
            var line = PretextLayout.LayoutNextLine(prepared, cursor, widths[widthIndex]);
            if (line is null)
            {
                break;
            }

            lines.Add(line);
            cursor = line.End;
            widthIndex++;
        }

        return lines;
    }

    private static string ReconstructFromWalkedRanges(PreparedTextWithSegments prepared, double width)
    {
        var builder = new StringBuilder();
        PretextLayout.WalkLineRanges(prepared, width, line =>
        {
            builder.Append(SlicePreparedText(prepared, line.Start, line.End));
        });

        return builder.ToString();
    }

    private static int CompareCursors(LayoutCursor left, LayoutCursor right)
    {
        if (left.SegmentIndex != right.SegmentIndex)
        {
            return left.SegmentIndex - right.SegmentIndex;
        }

        return left.GraphemeIndex - right.GraphemeIndex;
    }

    private static LayoutCursor TerminalCursor(PreparedTextWithSegments prepared)
    {
        return new LayoutCursor(prepared.Segments.Count, 0);
    }

    private static IReadOnlyList<(string Text, sbyte Level)> GetNonSpaceSegmentLevels(PreparedTextWithSegments prepared)
    {
        var levels = Assert.IsAssignableFrom<IReadOnlyList<sbyte>>(prepared.SegmentLevels);
        var result = new List<(string Text, sbyte Level)>();

        for (var index = 0; index < prepared.Segments.Count; index++)
        {
            if (prepared.Kinds[index] == SegmentBreakKind.Space)
            {
                continue;
            }

            result.Add((prepared.Segments[index], levels[index]));
        }

        return result;
    }
}
