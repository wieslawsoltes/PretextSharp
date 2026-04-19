using System.Globalization;

namespace Pretext;

public static partial class PretextLayout
{
    private static readonly HashSet<char> NumericJoiners = ['-', ':', '/', '×', ',', '.', '+', '\u2013', '\u2014'];

    private static EngineProfile GetEngineProfile()
    {
        if (_cachedEngineProfile is { } cached)
        {
            return cached;
        }

        // Keep a stable desktop-oriented profile independent of the active text backend.
        _cachedEngineProfile = new EngineProfile(
            LineFitEpsilon: 0.005,
            CarryCjkAfterClosingQuote: true,
            PreferPrefixWidthsForBreakableRuns: false,
            PreferEarlySoftHyphenBreak: false);

        return _cachedEngineProfile.Value;
    }

    private static IReadOnlyList<string> BuildBaseCjkUnits(string text, bool carryCjkAfterClosingQuote)
    {
        var elements = GetTextElements(text);
        if (elements.Length <= 1)
        {
            return elements;
        }

        var units = new List<string>();
        var currentParts = new List<string> { elements[0] };
        var currentContainsCjk = ContainsCjk(elements[0]);
        var currentEndsWithClosingQuote = EndsWithClosingQuote(elements[0]);
        var currentIsSingleKinsokuEnd = elements[0].Length == 1 && KinsokuEndChars.Contains(elements[0][0]);

        static string JoinParts(List<string> parts)
            => parts.Count == 1 ? parts[0] : string.Concat(parts);

        void PushCurrent()
        {
            if (currentParts.Count == 0)
            {
                return;
            }

            units.Add(JoinParts(currentParts));
        }

        for (var index = 1; index < elements.Length; index++)
        {
            var grapheme = elements[index];
            var graphemeContainsCjk = ContainsCjk(grapheme);

            if (currentIsSingleKinsokuEnd ||
                IsCjkLineStartProhibited(grapheme) ||
                IsLeftStickyCluster(grapheme) ||
                (carryCjkAfterClosingQuote && graphemeContainsCjk && currentEndsWithClosingQuote))
            {
                currentParts.Add(grapheme);
                currentContainsCjk |= graphemeContainsCjk;
                currentEndsWithClosingQuote = currentEndsWithClosingQuote || EndsWithClosingQuote(grapheme);
                currentIsSingleKinsokuEnd = false;
                continue;
            }

            if (!currentContainsCjk && !graphemeContainsCjk)
            {
                currentParts.Add(grapheme);
                currentEndsWithClosingQuote = EndsWithClosingQuote(grapheme);
                currentIsSingleKinsokuEnd = false;
                continue;
            }

            PushCurrent();
            currentParts = [grapheme];
            currentContainsCjk = graphemeContainsCjk;
            currentEndsWithClosingQuote = EndsWithClosingQuote(grapheme);
            currentIsSingleKinsokuEnd = grapheme.Length == 1 && KinsokuEndChars.Contains(grapheme[0]);
        }

        PushCurrent();
        return units;
    }

    private static IReadOnlyList<string> MergeKeepAllTextUnits(IReadOnlyList<string> units)
    {
        if (units.Count <= 1)
        {
            return units;
        }

        var merged = new List<string>();
        var currentText = units[0];
        var currentContainsCjk = ContainsCjk(currentText);
        var currentCanContinue = CanContinueKeepAllTextRun(currentText);

        for (var index = 1; index < units.Count; index++)
        {
            var next = units[index];
            var nextContainsCjk = ContainsCjk(next);

            if (currentContainsCjk && currentCanContinue)
            {
                currentText += next;
                currentContainsCjk |= nextContainsCjk;
                currentCanContinue = CanContinueKeepAllTextRun(currentText);
                continue;
            }

            merged.Add(currentText);
            currentText = next;
            currentContainsCjk = nextContainsCjk;
            currentCanContinue = CanContinueKeepAllTextRun(currentText);
        }

        merged.Add(currentText);
        return merged;
    }

    private static bool IsLeftStickyCluster(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (!LeftStickyPunctuationChars.Contains(ch))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCjkLineStartProhibited(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (!KinsokuStartChars.Contains(ch) && !LeftStickyPunctuationChars.Contains(ch))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsForwardStickyCluster(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (!KinsokuEndChars.Contains(ch) && ch is not '\'' and not '’')
            {
                return false;
            }
        }

        return true;
    }

    private static bool EndsWithClosingQuote(string text)
    {
        for (var index = text.Length - 1; index >= 0; index--)
        {
            var ch = text[index];
            if (ClosingQuotesChars.Contains(ch))
            {
                return true;
            }

            if (!LeftStickyPunctuationChars.Contains(ch))
            {
                return false;
            }
        }

        return false;
    }

    private static bool ContainsCjk(string text)
    {
#if NET6_0_OR_GREATER
        foreach (var rune in text.EnumerateRunes())
        {
            if (IsCjkCodePoint(rune.Value))
            {
                return true;
            }
        }

        return false;
#else
        return UnicodeCompat.AnyCodePoint(text, static codePoint => IsCjkCodePoint(codePoint));
#endif
    }

    private static string[] GetTextElements(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        var elements = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            elements.Add((string)enumerator.Current!);
        }

        return elements.ToArray();
    }

    private readonly record struct MeasurementCacheKey(string Text, SegmentBreakKind Kind, bool IsBreakableRun);

    private sealed class FontState : IDisposable
    {
        private FontState(string font, IPretextTextMeasurer textMeasurer, double spaceWidth, double hyphenWidth)
        {
            Font = font;
            TextMeasurer = textMeasurer;
            SpaceWidth = spaceWidth;
            HyphenWidth = hyphenWidth;
            TabStopAdvance = spaceWidth * 8;
            SegmentCache = new Dictionary<MeasurementCacheKey, MeasuredSegment>();
        }

        public string Font { get; }

        public IPretextTextMeasurer TextMeasurer { get; }

        public double SpaceWidth { get; }

        public double HyphenWidth { get; }

        public double TabStopAdvance { get; }

        public Dictionary<MeasurementCacheKey, MeasuredSegment> SegmentCache { get; }

        public static FontState Create(string font)
        {
            var textMeasurer = PretextLayout.GetTextMeasurerFactory().Create(font);
            var spaceWidth = textMeasurer.MeasureText(" ");
            var hyphenWidth = textMeasurer.MeasureText("-");
            return new FontState(font, textMeasurer, spaceWidth, hyphenWidth);
        }

        public MeasuredSegment MeasureSegment(string text, SegmentBreakKind kind, bool isBreakableRun)
        {
            var cacheKey = new MeasurementCacheKey(text, kind, isBreakableRun);
            if (SegmentCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            double[]? graphemeWidths = null;
            double[]? prefixWidths = null;
            if (isBreakableRun)
            {
                var graphemes = GetTextElements(text);
                if (graphemes.Length > 1)
                {
                    graphemeWidths = new double[graphemes.Length];
                    prefixWidths = new double[graphemes.Length];
                    var prefixLength = 0;
                    for (var i = 0; i < graphemes.Length; i++)
                    {
                        prefixLength += graphemes[i].Length;
                        prefixWidths[i] = MeasureText(text.Substring(0, prefixLength));
                        graphemeWidths[i] = i == 0
                            ? prefixWidths[i]
                            : prefixWidths[i] - prefixWidths[i - 1];
                    }
                }
            }

            var width = MeasureText(text);
            var segment = new MeasuredSegment(text, kind, isBreakableRun, width, graphemeWidths, prefixWidths);
            SegmentCache[cacheKey] = segment;
            return segment;
        }

        public void Dispose()
        {
            TextMeasurer.Dispose();
        }

        private double MeasureText(string text)
        {
            return text.Length == 0 ? 0 : TextMeasurer.MeasureText(text);
        }
    }
}
