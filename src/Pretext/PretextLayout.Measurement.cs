using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Pretext;

public static partial class PretextLayout
{
    private static readonly HashSet<char> NumericJoiners = ['-', ':', '/', '×', ',', '.', '+', '\u2013', '\u2014'];

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*px", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex FontSizeRegex();

    private static EngineProfile GetEngineProfile()
    {
        if (_cachedEngineProfile is { } cached)
        {
            return cached;
        }

        // This port measures and renders through the same local text stack, so we keep
        // the stable Skia/Desktop defaults while caching the profile the same way TS does.
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
        var currentParts = new List<string> { units[0] };
        var currentContainsCjk = ContainsCjk(units[0]);
        var currentCanContinue = CanContinueKeepAllTextRun(units[0]);

        static string JoinParts(List<string> parts)
            => parts.Count == 1 ? parts[0] : string.Concat(parts);

        void FlushCurrent()
        {
            if (currentParts.Count > 0)
            {
                merged.Add(JoinParts(currentParts));
            }
        }

        for (var index = 1; index < units.Count; index++)
        {
            var next = units[index];
            var nextContainsCjk = ContainsCjk(next);
            var nextCanContinue = CanContinueKeepAllTextRun(next);

            if (currentContainsCjk && currentCanContinue)
            {
                currentParts.Add(next);
                currentContainsCjk |= nextContainsCjk;
                currentCanContinue = nextCanContinue;
                continue;
            }

            FlushCurrent();
            currentParts = [next];
            currentContainsCjk = nextContainsCjk;
            currentCanContinue = nextCanContinue;
        }

        FlushCurrent();
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
        foreach (var rune in text.EnumerateRunes())
        {
            var code = rune.Value;
            if ((code >= 0x4E00 && code <= 0x9FFF) ||
                (code >= 0x3400 && code <= 0x4DBF) ||
                (code >= 0x20000 && code <= 0x2A6DF) ||
                (code >= 0x2A700 && code <= 0x2B73F) ||
                (code >= 0x2B740 && code <= 0x2B81F) ||
                (code >= 0x2B820 && code <= 0x2CEAF) ||
                (code >= 0x2CEB0 && code <= 0x2EBEF) ||
                (code >= 0x2EBF0 && code <= 0x2EE5D) ||
                (code >= 0x30000 && code <= 0x3134F) ||
                (code >= 0x31350 && code <= 0x323AF) ||
                (code >= 0x323B0 && code <= 0x33479) ||
                (code >= 0xF900 && code <= 0xFAFF) ||
                (code >= 0x2F800 && code <= 0x2FA1F) ||
                (code >= 0x3000 && code <= 0x303F) ||
                (code >= 0x3040 && code <= 0x309F) ||
                (code >= 0x30A0 && code <= 0x30FF) ||
                (code >= 0xAC00 && code <= 0xD7AF) ||
                (code >= 0xFF00 && code <= 0xFFEF))
            {
                return true;
            }
        }

        return false;
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
        private FontState(string font, SKFont? skFont, double spaceWidth, double hyphenWidth, Func<string, string, double>? measureTextOverride)
        {
            Font = font;
            SkFont = skFont;
            SpaceWidth = spaceWidth;
            HyphenWidth = hyphenWidth;
            TabStopAdvance = spaceWidth * 8;
            SegmentCache = new Dictionary<MeasurementCacheKey, MeasuredSegment>();
            MeasureTextOverride = measureTextOverride;
        }

        public string Font { get; }

        public SKFont? SkFont { get; }

        public double SpaceWidth { get; }

        public double HyphenWidth { get; }

        public double TabStopAdvance { get; }

        public Dictionary<MeasurementCacheKey, MeasuredSegment> SegmentCache { get; }

        private Func<string, string, double>? MeasureTextOverride { get; }

        public static FontState Create(string font)
        {
            var measureTextOverride = PretextLayout._measureTextOverride;
            var spec = FontSpec.Parse(font);
            SKFont? skFont = null;
            if (measureTextOverride is null)
            {
                skFont = new SKFont
                {
                    Size = spec.Size,
                    Typeface = SKTypeface.FromFamilyName(spec.PrimaryFamily, spec.FontStyle),
                    Subpixel = true,
                };
            }

            var spaceWidth = measureTextOverride?.Invoke(" ", font) ?? skFont!.MeasureText(" ");
            var hyphenWidth = measureTextOverride?.Invoke("-", font) ?? skFont!.MeasureText("-");
            return new FontState(font, skFont, spaceWidth, hyphenWidth, measureTextOverride);
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
                        prefixWidths[i] = MeasureText(text.AsSpan(0, prefixLength));
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
            SkFont?.Dispose();
        }

        private double MeasureText(string text)
        {
            return MeasureTextOverride?.Invoke(text, Font) ?? SkFont!.MeasureText(text);
        }

        private double MeasureText(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return 0;
            }

            return MeasureTextOverride?.Invoke(text.ToString(), Font) ?? SkFont!.MeasureText(text);
        }
    }

    private readonly record struct FontSpec(float Size, string PrimaryFamily, SKFontStyle FontStyle)
    {
        public static FontSpec Parse(string font)
        {
            var match = FontSizeRegex().Match(font);
            if (!match.Success)
            {
                return new FontSpec(16, "Arial", SKFontStyle.Normal);
            }

            var size = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var beforeSize = font.AsSpan(0, match.Index);
            var afterSize = font.AsSpan(match.Index + match.Length).Trim();
            if (!afterSize.IsEmpty && afterSize[0] == '/')
            {
                var nextSpace = afterSize.IndexOf(' ');
                afterSize = nextSpace >= 0 ? afterSize[(nextSpace + 1)..].Trim() : ReadOnlySpan<char>.Empty;
            }

            var primaryFamily = ExtractPrimaryFamily(afterSize);

            if (string.Equals(primaryFamily, "sans-serif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(primaryFamily, "system-ui", StringComparison.OrdinalIgnoreCase))
            {
                primaryFamily = "Arial";
            }
            else if (string.Equals(primaryFamily, "serif", StringComparison.OrdinalIgnoreCase))
            {
                primaryFamily = "Times New Roman";
            }
            else if (string.Equals(primaryFamily, "monospace", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(primaryFamily, "ui-monospace", StringComparison.OrdinalIgnoreCase))
            {
                primaryFamily = "Menlo";
            }

            var italic = false;
            var weight = 400;
            var scan = beforeSize;
            while (!scan.IsEmpty)
            {
                var nextSeparator = scan.IndexOf(' ');
                var token = (nextSeparator >= 0 ? scan[..nextSeparator] : scan).Trim();
                if (!token.IsEmpty)
                {
                    if (token.Equals("italic", StringComparison.OrdinalIgnoreCase) ||
                        token.Equals("oblique", StringComparison.OrdinalIgnoreCase))
                    {
                        italic = true;
                    }

                    if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    {
                        weight = parsed;
                        break;
                    }

                    if (token.Equals("bold", StringComparison.OrdinalIgnoreCase))
                    {
                        weight = 700;
                    }
                }

                if (nextSeparator < 0)
                {
                    break;
                }

                scan = scan[(nextSeparator + 1)..];
            }

            var styleWeight = weight >= 700 ? SKFontStyleWeight.Bold : weight >= 500 ? SKFontStyleWeight.Medium : SKFontStyleWeight.Normal;
            var slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            return new FontSpec(size, primaryFamily, new SKFontStyle(styleWeight, SKFontStyleWidth.Normal, slant));
        }

        private static string ExtractPrimaryFamily(ReadOnlySpan<char> familyList)
        {
            if (familyList.IsEmpty)
            {
                return "Arial";
            }

            var commaIndex = familyList.IndexOf(',');
            var primary = TrimMatchingQuotes((commaIndex >= 0 ? familyList[..commaIndex] : familyList).Trim());
            return primary.IsEmpty ? "Arial" : primary.ToString();
        }

        private static ReadOnlySpan<char> TrimMatchingQuotes(ReadOnlySpan<char> value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                return value[1..^1].Trim();
            }

            return value;
        }
    }
}
