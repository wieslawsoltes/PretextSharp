using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pretext;

public static partial class PretextLayout
{
    private static readonly HashSet<char> ForwardStickyGlue = ['\'', '’'];
    private static readonly HashSet<char> ArabicNoSpaceTrailingPunctuation = [':', '.', '\u060C', '\u061B'];
    private static readonly HashSet<char> MyanmarMedialGlue = ['\u104F'];
    private static readonly HashSet<char> KinsokuStartChars =
    [
        '\uFF0C', '\uFF0E', '\uFF01', '\uFF1A', '\uFF1B', '\uFF1F', '\u3001', '\u3002',
        '\u30FB', '\uFF09', '\u3015', '\u3009', '\u300B', '\u300D', '\u300F', '\u3011',
        '\u3017', '\u3019', '\u301B', '\u30FC', '\u3005', '\u303B', '\u309D', '\u309E',
        '\u30FD', '\u30FE',
    ];
    private static readonly HashSet<char> KinsokuEndChars =
    [
        '"', '(', '[', '{', '“', '‘', '«', '‹', '\uFF08', '\u3014', '\u3008', '\u300A',
        '\u300C', '\u300E', '\u3010', '\u3016', '\u3018', '\u301A',
    ];
    private static readonly HashSet<char> LeftStickyPunctuationChars =
    [
        '.', ',', '!', '?', ':', ';', '\u060C', '\u061B', '\u061F', '\u0964', '\u0965',
        '\u104A', '\u104B', '\u104C', '\u104D', '\u104F', ')', ']', '}', '%', '"',
        '”', '’', '»', '›', '…',
    ];
    private static readonly HashSet<char> ClosingQuotesChars =
    [
        '”', '’', '»', '›', '\u300D', '\u300F', '\u3011', '\u300B', '\u3009', '\u3015', '\uFF09',
    ];
    private static readonly HashSet<char> KeepAllGlueChars = ['\u00A0', '\u202F', '\u2060', '\uFEFF'];

    private readonly record struct EngineProfile(
        double LineFitEpsilon,
        bool CarryCjkAfterClosingQuote,
        bool PreferPrefixWidthsForBreakableRuns,
        bool PreferEarlySoftHyphenBreak);

    private readonly record struct WhiteSpaceProfile(
        WhiteSpaceMode Mode,
        bool PreserveOrdinarySpaces,
        bool PreserveHardBreaks);

    private readonly record struct WordSegment(string Text, bool IsWordLike, int Start);

    private readonly record struct AnalysisToken(string Text, SegmentBreakKind Kind, bool IsWordLike);

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9+.-]*:$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UrlSchemeSegmentRegex();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9+.-]*$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UrlSchemeBareSegmentRegex();

    private static WhiteSpaceProfile GetWhiteSpaceProfile(WhiteSpaceMode whiteSpace)
    {
        return whiteSpace == WhiteSpaceMode.PreWrap
            ? new WhiteSpaceProfile(whiteSpace, PreserveOrdinarySpaces: true, PreserveHardBreaks: true)
            : new WhiteSpaceProfile(whiteSpace, PreserveOrdinarySpaces: false, PreserveHardBreaks: false);
    }

    private static List<AnalysisToken> AnalyzeTokens(string text, WhiteSpaceMode whiteSpace, WordBreakMode wordBreak)
    {
        var whiteSpaceProfile = GetWhiteSpaceProfile(whiteSpace);
        var normalized = whiteSpaceProfile.Mode == WhiteSpaceMode.PreWrap
            ? NormalizeWhitespacePreWrap(text ?? string.Empty)
            : NormalizeWhitespaceNormal(text ?? string.Empty);

        if (normalized.Length == 0)
        {
            return [];
        }

        var initial = BuildInitialTokens(normalized, whiteSpaceProfile);
        var merged = BuildMergedTokens(initial, GetEngineProfile(), whiteSpaceProfile);
        return wordBreak == WordBreakMode.KeepAll
            ? MergeKeepAllTextSegments(merged)
            : merged;
    }

    private static string NormalizeWhitespaceNormal(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        var sawContent = false;

        foreach (var ch in text)
        {
            if (ch is ' ' or '\t' or '\n' or '\r' or '\f')
            {
                if (sawContent)
                {
                    pendingSpace = true;
                }

                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(ch);
            sawContent = true;
        }

        return builder.ToString();
    }

    private static string NormalizeWhitespacePreWrap(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\f', '\n');
    }

    private static List<AnalysisToken> BuildInitialTokens(string text, WhiteSpaceProfile whiteSpaceProfile)
    {
        var tokens = new List<AnalysisToken>();
        if (TryEnumerateLocaleAwareWordSegments(text, out var segments))
        {
            foreach (var segment in segments)
            {
                tokens.AddRange(SplitSegmentByBreakKind(segment.Text, whiteSpaceProfile));
            }

            return tokens;
        }

        SegmentBreakKind? currentKind = null;
        var currentWordLike = false;
        var builder = new StringBuilder();

        void Flush()
        {
            if (builder.Length == 0 || currentKind is null)
            {
                return;
            }

            tokens.Add(new AnalysisToken(builder.ToString(), currentKind.Value, currentWordLike));
            builder.Clear();
            currentKind = null;
            currentWordLike = false;
        }

        foreach (var element in EnumerateTextElements(text))
        {
            var kind = ClassifySegmentBreakTextElement(element, whiteSpaceProfile);
            var isWordLike = kind == SegmentBreakKind.Text && IsWordLikeText(element);

            if (currentKind is not null &&
                currentKind == kind &&
                currentWordLike == isWordLike &&
                CanMergeAdjacentElements(kind))
            {
                builder.Append(element);
                continue;
            }

            Flush();
            currentKind = kind;
            currentWordLike = isWordLike;
            builder.Append(element);
        }

        Flush();
        return tokens;
    }

    private static IEnumerable<AnalysisToken> SplitSegmentByBreakKind(string segment, WhiteSpaceProfile whiteSpaceProfile)
    {
        SegmentBreakKind? currentKind = null;
        var builder = new StringBuilder();

        foreach (var element in EnumerateTextElements(segment))
        {
            var kind = ClassifySegmentBreakTextElement(element, whiteSpaceProfile);

            if (currentKind is not null &&
                currentKind == kind)
            {
                builder.Append(element);
                continue;
            }

            if (builder.Length > 0 && currentKind is not null)
            {
                var text = builder.ToString();
                yield return new AnalysisToken(
                    text,
                    currentKind.Value,
                    currentKind == SegmentBreakKind.Text && IsWordLikeText(text));
            }

            builder.Clear();
            builder.Append(element);
            currentKind = kind;
        }

        if (builder.Length > 0 && currentKind is not null)
        {
            var text = builder.ToString();
            yield return new AnalysisToken(
                text,
                currentKind.Value,
                currentKind == SegmentBreakKind.Text && IsWordLikeText(text));
        }
    }

    private static List<AnalysisToken> BuildMergedTokens(
        List<AnalysisToken> initialTokens,
        EngineProfile profile,
        WhiteSpaceProfile whiteSpaceProfile)
    {
        var merged = new List<AnalysisToken>(initialTokens.Count);

        foreach (var token in initialTokens)
        {
            var isText = token.Kind == SegmentBreakKind.Text;
            if (isText &&
                merged.Count > 0 &&
                merged[^1].Kind == SegmentBreakKind.Text &&
                profile.CarryCjkAfterClosingQuote &&
                ContainsCjk(token.Text) &&
                ContainsCjk(merged[^1].Text) &&
                EndsWithClosingQuote(merged[^1].Text))
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    Text = previous.Text + token.Text,
                    IsWordLike = previous.IsWordLike || token.IsWordLike,
                };
                continue;
            }

            if (isText &&
                merged.Count > 0 &&
                merged[^1].Kind == SegmentBreakKind.Text &&
                IsCjkLineStartProhibitedSegment(token.Text) &&
                ContainsCjk(merged[^1].Text))
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    Text = previous.Text + token.Text,
                    IsWordLike = previous.IsWordLike || token.IsWordLike,
                };
                continue;
            }

            if (isText &&
                merged.Count > 0 &&
                merged[^1].Kind == SegmentBreakKind.Text &&
                EndsWithMyanmarMedialGlue(merged[^1].Text))
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    Text = previous.Text + token.Text,
                    IsWordLike = previous.IsWordLike || token.IsWordLike,
                };
                continue;
            }

            if (isText &&
                merged.Count > 0 &&
                merged[^1].Kind == SegmentBreakKind.Text &&
                token.IsWordLike &&
                ContainsArabicScript(token.Text) &&
                EndsWithArabicNoSpacePunctuation(merged[^1].Text))
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    Text = previous.Text + token.Text,
                    IsWordLike = true,
                };
                continue;
            }

            if (isText &&
                !token.IsWordLike &&
                merged.Count > 0 &&
                merged[^1].Kind == SegmentBreakKind.Text &&
                token.Text.Length == 1 &&
                token.Text != "-" &&
                token.Text != "—" &&
                IsRepeatedSingleCharRun(merged[^1].Text, token.Text[0]))
            {
                var previous = merged[^1];
                merged[^1] = previous with { Text = previous.Text + token.Text };
                continue;
            }

            if (isText &&
                !token.IsWordLike &&
                merged.Count > 0 &&
                merged[^1].Kind == SegmentBreakKind.Text &&
                (IsLeftStickyPunctuationSegment(token.Text) || (token.Text == "-" && merged[^1].IsWordLike)))
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    Text = previous.Text + token.Text,
                    IsWordLike = previous.IsWordLike || token.IsWordLike,
                };
                continue;
            }

            merged.Add(token);
        }

        for (var index = 1; index < merged.Count; index++)
        {
            if (merged[index].Kind == SegmentBreakKind.Text &&
                !merged[index].IsWordLike &&
                IsEscapedQuoteClusterSegment(merged[index].Text) &&
                merged[index - 1].Kind == SegmentBreakKind.Text)
            {
                merged[index - 1] = merged[index - 1] with
                {
                    Text = merged[index - 1].Text + merged[index].Text,
                    IsWordLike = merged[index - 1].IsWordLike || merged[index].IsWordLike,
                };
                merged[index] = merged[index] with { Text = string.Empty };
            }
        }

        for (var index = merged.Count - 2; index >= 0; index--)
        {
            if (merged[index].Kind == SegmentBreakKind.Text &&
                !merged[index].IsWordLike &&
                IsForwardStickyClusterSegment(merged[index].Text))
            {
                var next = index + 1;
                while (next < merged.Count && merged[next].Text.Length == 0)
                {
                    next++;
                }

                if (next < merged.Count && merged[next].Kind == SegmentBreakKind.Text)
                {
                    merged[next] = merged[next] with
                    {
                        Text = merged[index].Text + merged[next].Text,
                        IsWordLike = merged[index].IsWordLike || merged[next].IsWordLike,
                    };
                    merged[index] = merged[index] with { Text = string.Empty };
                }
            }
        }

        merged = CompactTokens(merged);
        merged = MergeCjkBoundaryRuns(merged);
        merged = MergeGlueConnectedTextRuns(merged);
        merged = MergeUrlLikeRuns(merged);
        merged = MergeUrlQueryRuns(merged);
        merged = MergeNumericRuns(merged);
        merged = SplitHyphenatedNumericRuns(merged);
        merged = MergeAsciiPunctuationChains(merged);
        merged = CarryTrailingForwardStickyAcrossCjkBoundary(merged);
        merged = CarryLeadingCjkStartProhibitedAcrossBoundary(merged);
        merged = SplitMyanmarRuns(merged);

        for (var index = 0; index < merged.Count - 1; index++)
        {
            var split = SplitLeadingSpaceAndMarks(merged[index].Text);
            if (split is null)
            {
                continue;
            }

            if (merged[index + 1].Kind != SegmentBreakKind.Text ||
                !ContainsArabicScript(merged[index + 1].Text))
            {
                continue;
            }

            var spaceKind = merged[index].Kind == SegmentBreakKind.PreservedSpace || whiteSpaceProfile.PreserveOrdinarySpaces
                ? SegmentBreakKind.PreservedSpace
                : SegmentBreakKind.Space;

            merged[index] = new AnalysisToken(split.Value.Space, spaceKind, false);
            merged[index + 1] = merged[index + 1] with { Text = split.Value.Marks + merged[index + 1].Text };
        }

        return CompactTokens(merged);
    }

    private static List<AnalysisToken> CompactTokens(List<AnalysisToken> tokens)
    {
        var compacted = new List<AnalysisToken>(tokens.Count);
        foreach (var token in tokens)
        {
            if (token.Text.Length > 0)
            {
                compacted.Add(token);
            }
        }

        return compacted;
    }

    private static List<AnalysisToken> MergeKeepAllTextSegments(List<AnalysisToken> tokens)
    {
        if (tokens.Count <= 1)
        {
            return tokens;
        }

        var merged = new List<AnalysisToken>(tokens.Count);
        string? pendingText = null;
        var pendingWordLike = false;
        var pendingContainsCjk = false;
        var pendingCanContinue = false;

        void FlushPending()
        {
            if (pendingText is null)
            {
                return;
            }

            merged.Add(new AnalysisToken(pendingText, SegmentBreakKind.Text, pendingWordLike));
            pendingText = null;
        }

        foreach (var token in tokens)
        {
            if (token.Kind == SegmentBreakKind.Text)
            {
                var textContainsCjk = ContainsCjk(token.Text);
                var textCanContinue = CanContinueKeepAllTextRun(token.Text);

                if (pendingText is not null && pendingContainsCjk && pendingCanContinue)
                {
                    pendingText += token.Text;
                    pendingWordLike |= token.IsWordLike;
                    pendingContainsCjk |= textContainsCjk;
                    pendingCanContinue = textCanContinue;
                    continue;
                }

                FlushPending();
                pendingText = token.Text;
                pendingWordLike = token.IsWordLike;
                pendingContainsCjk = textContainsCjk;
                pendingCanContinue = textCanContinue;
                continue;
            }

            FlushPending();
            merged.Add(token);
        }

        FlushPending();
        return merged;
    }

    private static List<AnalysisToken> MergeGlueConnectedTextRuns(List<AnalysisToken> tokens)
    {
        var merged = new List<AnalysisToken>(tokens.Count);
        var read = 0;
        while (read < tokens.Count)
        {
            var token = tokens[read];
            if (token.Kind == SegmentBreakKind.Glue)
            {
                var glueText = token.Text;
                read++;
                while (read < tokens.Count && tokens[read].Kind == SegmentBreakKind.Glue)
                {
                    glueText += tokens[read].Text;
                    read++;
                }

                if (read < tokens.Count && tokens[read].Kind == SegmentBreakKind.Text)
                {
                    var next = tokens[read];
                    token = new AnalysisToken(glueText + next.Text, SegmentBreakKind.Text, next.IsWordLike);
                    read++;
                }
                else
                {
                    merged.Add(new AnalysisToken(glueText, SegmentBreakKind.Glue, false));
                    continue;
                }
            }
            else
            {
                read++;
            }

            if (token.Kind == SegmentBreakKind.Text)
            {
                var text = token.Text;
                var isWordLike = token.IsWordLike;
                while (read < tokens.Count && tokens[read].Kind == SegmentBreakKind.Glue)
                {
                    var glueText = string.Empty;
                    while (read < tokens.Count && tokens[read].Kind == SegmentBreakKind.Glue)
                    {
                        glueText += tokens[read].Text;
                        read++;
                    }

                    if (read < tokens.Count && tokens[read].Kind == SegmentBreakKind.Text)
                    {
                        text += glueText + tokens[read].Text;
                        isWordLike |= tokens[read].IsWordLike;
                        read++;
                        continue;
                    }

                    text += glueText;
                }

                merged.Add(new AnalysisToken(text, SegmentBreakKind.Text, isWordLike));
                continue;
            }

            merged.Add(token);
        }

        return merged;
    }

    private static List<AnalysisToken> MergeCjkBoundaryRuns(List<AnalysisToken> tokens)
    {
        var merged = new List<AnalysisToken>(tokens.Count);
        foreach (var token in tokens)
        {
            if (token.Kind == SegmentBreakKind.Text &&
                merged.Count > 0 &&
                merged[^1].Kind == SegmentBreakKind.Text &&
                IsCjkLineStartProhibitedSegment(token.Text) &&
                ContainsCjk(merged[^1].Text))
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    Text = previous.Text + token.Text,
                    IsWordLike = previous.IsWordLike || token.IsWordLike,
                };
                continue;
            }

            merged.Add(token);
        }

        return merged;
    }

    private static List<AnalysisToken> MergeUrlLikeRuns(List<AnalysisToken> tokens)
    {
        var merged = new List<AnalysisToken>(tokens.Count);
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token.Kind == SegmentBreakKind.Text && IsUrlLikeRunStart(tokens, index))
            {
                var text = token.Text;
                var next = index + 1;
                while (next < tokens.Count && !IsTextRunBoundary(tokens[next].Kind))
                {
                    text += tokens[next].Text;
                    var endsQueryPrefix = tokens[next].Text.Contains('?', StringComparison.Ordinal);
                    next++;
                    if (endsQueryPrefix)
                    {
                        break;
                    }
                }

                merged.Add(new AnalysisToken(text, SegmentBreakKind.Text, true));
                index = next - 1;
                continue;
            }

            merged.Add(token);
        }

        return merged;
    }

    private static List<AnalysisToken> MergeUrlQueryRuns(List<AnalysisToken> tokens)
    {
        var merged = new List<AnalysisToken>(tokens.Count);
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            merged.Add(token);

            if (!IsUrlQueryBoundaryToken(token.Text))
            {
                continue;
            }

            var next = index + 1;
            if (next >= tokens.Count || IsTextRunBoundary(tokens[next].Kind))
            {
                continue;
            }

            var queryText = string.Empty;
            var read = next;
            while (read < tokens.Count && !IsTextRunBoundary(tokens[read].Kind))
            {
                queryText += tokens[read].Text;
                read++;
            }

            if (queryText.Length > 0)
            {
                merged.Add(new AnalysisToken(queryText, SegmentBreakKind.Text, true));
                index = read - 1;
            }
        }

        return merged;
    }

    private static List<AnalysisToken> MergeNumericRuns(List<AnalysisToken> tokens)
    {
        var merged = new List<AnalysisToken>(tokens.Count);
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token.Kind == SegmentBreakKind.Text && IsNumericRunSegment(token.Text) && SegmentContainsDecimalDigit(token.Text))
            {
                var text = token.Text;
                var next = index + 1;
                while (next < tokens.Count &&
                       tokens[next].Kind == SegmentBreakKind.Text &&
                       IsNumericRunSegment(tokens[next].Text))
                {
                    text += tokens[next].Text;
                    next++;
                }

                merged.Add(new AnalysisToken(text, SegmentBreakKind.Text, true));
                index = next - 1;
                continue;
            }

            merged.Add(token);
        }

        return merged;
    }

    private static List<AnalysisToken> SplitHyphenatedNumericRuns(List<AnalysisToken> tokens)
    {
        var splitTokens = new List<AnalysisToken>(tokens.Count);
        foreach (var token in tokens)
        {
            if (token.Kind == SegmentBreakKind.Text && token.Text.Contains('-', StringComparison.Ordinal))
            {
                var text = token.Text.AsSpan();
                var start = 0;
                var partCount = 0;
                var shouldSplit = false;

                while (start <= text.Length)
                {
                    var relativeHyphen = text[start..].IndexOf('-');
                    var end = relativeHyphen >= 0 ? start + relativeHyphen : text.Length;
                    var part = text[start..end];

                    if (part.IsEmpty || !SegmentContainsDecimalDigit(part) || !IsNumericRunSegment(part))
                    {
                        shouldSplit = false;
                        break;
                    }

                    partCount++;
                    if (relativeHyphen < 0)
                    {
                        shouldSplit = partCount > 1;
                        break;
                    }

                    start = end + 1;
                }

                if (shouldSplit)
                {
                    start = 0;
                    while (start <= text.Length)
                    {
                        var relativeHyphen = text[start..].IndexOf('-');
                        var end = relativeHyphen >= 0 ? start + relativeHyphen : text.Length;
                        var splitText = relativeHyphen >= 0
                            ? text[start..(end + 1)].ToString()
                            : text[start..].ToString();
                        splitTokens.Add(new AnalysisToken(splitText, SegmentBreakKind.Text, true));

                        if (relativeHyphen < 0)
                        {
                            break;
                        }

                        start = end + 1;
                    }

                    continue;
                }
            }

            splitTokens.Add(token);
        }

        return splitTokens;
    }

    private static List<AnalysisToken> MergeAsciiPunctuationChains(List<AnalysisToken> tokens)
    {
        var merged = new List<AnalysisToken>(tokens.Count);
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token.Kind == SegmentBreakKind.Text &&
                token.IsWordLike &&
                IsAsciiPunctuationChainSegment(token.Text))
            {
                var text = token.Text;
                var next = index + 1;
                while (HasAsciiPunctuationChainTrailingJoiners(text) &&
                       next < tokens.Count &&
                       tokens[next].Kind == SegmentBreakKind.Text &&
                       tokens[next].IsWordLike &&
                       IsAsciiPunctuationChainSegment(tokens[next].Text))
                {
                    text += tokens[next].Text;
                    next++;
                }

                merged.Add(new AnalysisToken(text, SegmentBreakKind.Text, true));
                index = next - 1;
                continue;
            }

            merged.Add(token);
        }

        return merged;
    }

    private static List<AnalysisToken> CarryTrailingForwardStickyAcrossCjkBoundary(List<AnalysisToken> tokens)
    {
        var carried = tokens.ToList();
        for (var index = 0; index < carried.Count - 1; index++)
        {
            if (carried[index].Kind != SegmentBreakKind.Text ||
                carried[index + 1].Kind != SegmentBreakKind.Text ||
                !ContainsCjk(carried[index].Text) ||
                !ContainsCjk(carried[index + 1].Text))
            {
                continue;
            }

            var split = SplitTrailingForwardStickyCluster(carried[index].Text);
            if (split is null)
            {
                continue;
            }

            carried[index] = carried[index] with { Text = split.Value.Head };
            carried[index + 1] = carried[index + 1] with { Text = split.Value.Tail + carried[index + 1].Text };
        }

        return CompactTokens(carried);
    }

    private static List<AnalysisToken> CarryLeadingCjkStartProhibitedAcrossBoundary(List<AnalysisToken> tokens)
    {
        var carried = tokens.ToList();
        for (var index = 1; index < carried.Count; index++)
        {
            if (carried[index - 1].Kind != SegmentBreakKind.Text ||
                carried[index].Kind != SegmentBreakKind.Text ||
                !ContainsCjk(carried[index - 1].Text))
            {
                continue;
            }

            var split = SplitLeadingCjkStartProhibitedPrefix(carried[index].Text);
            if (split is null)
            {
                continue;
            }

            carried[index - 1] = carried[index - 1] with { Text = carried[index - 1].Text + split.Value.Prefix };
            carried[index] = carried[index] with { Text = split.Value.Tail };
        }

        return CompactTokens(carried);
    }

    private static List<AnalysisToken> SplitMyanmarRuns(List<AnalysisToken> tokens)
    {
        var split = new List<AnalysisToken>(tokens.Count);
        foreach (var token in tokens)
        {
            if (token.Kind == SegmentBreakKind.Text &&
                ContainsMyanmarScript(token.Text) &&
                TrySplitMyanmarRun(token.Text, out var parts))
            {
                foreach (var part in parts)
                {
                    split.Add(new AnalysisToken(part, SegmentBreakKind.Text, true));
                }

                continue;
            }

            split.Add(token);
        }

        return split;
    }

    private static SegmentBreakKind ClassifySegmentBreakTextElement(string element, WhiteSpaceProfile whiteSpaceProfile)
    {
        if (whiteSpaceProfile.PreserveOrdinarySpaces || whiteSpaceProfile.PreserveHardBreaks)
        {
            if (element == " ")
            {
                return SegmentBreakKind.PreservedSpace;
            }

            if (element == "\t")
            {
                return SegmentBreakKind.Tab;
            }

            if (whiteSpaceProfile.PreserveHardBreaks && element == "\n")
            {
                return SegmentBreakKind.HardBreak;
            }
        }

        if (element == " ")
        {
            return SegmentBreakKind.Space;
        }

        if (element is "\u00A0" or "\u202F" or "\u2060" or "\uFEFF")
        {
            return SegmentBreakKind.Glue;
        }

        if (element == "\u200B")
        {
            return SegmentBreakKind.ZeroWidthBreak;
        }

        if (element == "\u00AD")
        {
            return SegmentBreakKind.SoftHyphen;
        }

        return SegmentBreakKind.Text;
    }

    private static bool IsWordLikeText(string element)
    {
        var sawWord = false;
        foreach (var rune in element.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.UppercaseLetter or
                UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or
                UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter or
                UnicodeCategory.DecimalDigitNumber or
                UnicodeCategory.LetterNumber or
                UnicodeCategory.OtherNumber or
                UnicodeCategory.NonSpacingMark or
                UnicodeCategory.SpacingCombiningMark or
                UnicodeCategory.EnclosingMark)
            {
                sawWord = true;
                continue;
            }

            if (category == UnicodeCategory.ConnectorPunctuation || rune.Value == '_')
            {
                sawWord = true;
                continue;
            }

            return false;
        }

        return sawWord;
    }

    private static IEnumerable<string> EnumerateTextElements(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            yield return (string)enumerator.Current!;
        }
    }

    private static bool IsTextRunBoundary(SegmentBreakKind kind)
    {
        return kind is SegmentBreakKind.Space or SegmentBreakKind.PreservedSpace or SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.HardBreak;
    }

    private static bool CanMergeAdjacentElements(SegmentBreakKind kind)
    {
        return kind is not (SegmentBreakKind.Tab or SegmentBreakKind.HardBreak or SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.SoftHyphen);
    }

    private static bool IsUrlLikeRunStart(IReadOnlyList<AnalysisToken> tokens, int index)
    {
        var text = tokens[index].Text;
        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (UrlSchemeSegmentRegex().IsMatch(text) &&
            index + 1 < tokens.Count &&
            tokens[index + 1].Kind == SegmentBreakKind.Text &&
            tokens[index + 1].Text == "//")
        {
            return true;
        }

        return UrlSchemeBareSegmentRegex().IsMatch(text) &&
               index + 1 < tokens.Count &&
               tokens[index + 1].Kind == SegmentBreakKind.Text &&
               tokens[index + 1].Text.StartsWith("://", StringComparison.Ordinal);
    }

    private static bool IsUrlQueryBoundaryToken(string text)
    {
        return text.Contains('?', StringComparison.Ordinal) &&
               (text.Contains("://", StringComparison.Ordinal) || text.StartsWith("www.", StringComparison.OrdinalIgnoreCase));
    }

    private static bool SegmentContainsDecimalDigit(string text)
        => SegmentContainsDecimalDigit(text.AsSpan());

    private static bool SegmentContainsDecimalDigit(ReadOnlySpan<char> text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.DecimalDigitNumber)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNumericRunSegment(string text)
        => IsNumericRunSegment(text.AsSpan());

    private static bool IsNumericRunSegment(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return false;
        }

        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.DecimalDigitNumber ||
                NumericJoiners.Contains((char)rune.Value))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsAsciiPunctuationChainSegment(string text)
        => IsAsciiPunctuationChainSegment(text.AsSpan());

    private static bool IsAsciiPunctuationChainSegment(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return false;
        }

        foreach (var ch in text)
        {
            if ((ch >= 'A' && ch <= 'Z') ||
                (ch >= 'a' && ch <= 'z') ||
                (ch >= '0' && ch <= '9') ||
                ch == '_' ||
                ch is ',' or ':' or ';')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool HasAsciiPunctuationChainTrailingJoiners(string text)
        => HasAsciiPunctuationChainTrailingJoiners(text.AsSpan());

    private static bool HasAsciiPunctuationChainTrailingJoiners(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return false;
        }

        var index = text.Length - 1;
        var sawJoiner = false;
        while (index >= 0)
        {
            var ch = text[index];
            if (ch is ',' or ':' or ';')
            {
                sawJoiner = true;
                index--;
                continue;
            }

            break;
        }

        return sawJoiner;
    }

    private static bool ContainsArabicScript(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            var code = rune.Value;
            if ((code >= 0x0600 && code <= 0x06FF) ||
                (code >= 0x0750 && code <= 0x077F) ||
                (code >= 0x08A0 && code <= 0x08FF) ||
                (code >= 0xFB50 && code <= 0xFDFF) ||
                (code >= 0xFE70 && code <= 0xFEFF) ||
                (code >= 0x10E60 && code <= 0x10E7F) ||
                (code >= 0x1EE00 && code <= 0x1EEFF))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMyanmarScript(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            var code = rune.Value;
            if ((code >= 0x1000 && code <= 0x109F) ||
                (code >= 0xA9E0 && code <= 0xA9FF) ||
                (code >= 0xAA60 && code <= 0xAA7F))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCombiningMark(char ch)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(ch);
        return category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;
    }

    private static bool IsEscapedQuoteClusterSegment(string segment)
    {
        var sawQuote = false;
        foreach (var ch in segment)
        {
            if (ch == '\\' || IsCombiningMark(ch))
            {
                continue;
            }

            if (KinsokuEndChars.Contains(ch) || LeftStickyPunctuationChars.Contains(ch) || ForwardStickyGlue.Contains(ch))
            {
                sawQuote = true;
                continue;
            }

            return false;
        }

        return sawQuote;
    }

    private static bool IsLeftStickyPunctuationSegment(string segment)
    {
        if (IsEscapedQuoteClusterSegment(segment))
        {
            return true;
        }

        var sawPunctuation = false;
        foreach (var ch in segment)
        {
            if (LeftStickyPunctuationChars.Contains(ch))
            {
                sawPunctuation = true;
                continue;
            }

            if (sawPunctuation && IsCombiningMark(ch))
            {
                continue;
            }

            return false;
        }

        return sawPunctuation;
    }

    private static bool IsCjkLineStartProhibitedSegment(string segment)
    {
        if (segment.Length == 0)
        {
            return false;
        }

        foreach (var ch in segment)
        {
            if (!KinsokuStartChars.Contains(ch) && !LeftStickyPunctuationChars.Contains(ch))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EndsWithLineStartProhibitedText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var lastScalarValue = GetLastScalarValue(text);
        return lastScalarValue is > 0 and <= char.MaxValue &&
               (KinsokuStartChars.Contains((char)lastScalarValue) || LeftStickyPunctuationChars.Contains((char)lastScalarValue));
    }

    private static bool EndsWithKeepAllGlueText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var lastScalarValue = GetLastScalarValue(text);
        return lastScalarValue is > 0 and <= char.MaxValue &&
               KeepAllGlueChars.Contains((char)lastScalarValue);
    }

    private static bool CanContinueKeepAllTextRun(string previousText)
    {
        return !EndsWithLineStartProhibitedText(previousText) &&
               !EndsWithKeepAllGlueText(previousText);
    }

    private static int GetLastScalarValue(string text)
    {
        var span = text.AsSpan();
        if (span.Length == 0)
        {
            return 0;
        }

        var index = span.Length - 1;
        if (index > 0 &&
            char.IsLowSurrogate(span[index]) &&
            char.IsHighSurrogate(span[index - 1]))
        {
            return char.ConvertToUtf32(span[index - 1], span[index]);
        }

        return span[index];
    }

    private static bool IsForwardStickyClusterSegment(string segment)
    {
        if (IsEscapedQuoteClusterSegment(segment))
        {
            return true;
        }

        if (segment.Length == 0)
        {
            return false;
        }

        foreach (var ch in segment)
        {
            if (KinsokuEndChars.Contains(ch) || ForwardStickyGlue.Contains(ch) || IsCombiningMark(ch))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsRepeatedSingleCharRun(string segment, char ch)
    {
        if (segment.Length == 0)
        {
            return false;
        }

        foreach (var part in segment)
        {
            if (part != ch)
            {
                return false;
            }
        }

        return true;
    }

    private static bool EndsWithArabicNoSpacePunctuation(string segment)
    {
        return segment.Length > 0 &&
               ContainsArabicScript(segment) &&
               ArabicNoSpaceTrailingPunctuation.Contains(segment[^1]);
    }

    private static bool EndsWithMyanmarMedialGlue(string segment)
    {
        return segment.Length > 0 && MyanmarMedialGlue.Contains(segment[^1]);
    }

    private static (string Space, string Marks)? SplitLeadingSpaceAndMarks(string segment)
    {
        if (segment.Length < 2 || segment[0] != ' ')
        {
            return null;
        }

        for (var index = 1; index < segment.Length; index++)
        {
            if (!IsCombiningMark(segment[index]))
            {
                return null;
            }
        }

        return (" ", segment[1..]);
    }

    private static (string Head, string Tail)? SplitTrailingForwardStickyCluster(string text)
    {
        var splitIndex = text.Length;
        while (splitIndex > 0)
        {
            var ch = text[splitIndex - 1];
            if (IsCombiningMark(ch))
            {
                splitIndex--;
                continue;
            }

            if (KinsokuEndChars.Contains(ch) || ForwardStickyGlue.Contains(ch))
            {
                splitIndex--;
                continue;
            }

            break;
        }

        if (splitIndex <= 0 || splitIndex == text.Length)
        {
            return null;
        }

        return (text[..splitIndex], text[splitIndex..]);
    }

    private static (string Prefix, string Tail)? SplitLeadingCjkStartProhibitedPrefix(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var splitIndex = 0;
        while (splitIndex < text.Length)
        {
            var ch = text[splitIndex];
            if (!KinsokuStartChars.Contains(ch) && !LeftStickyPunctuationChars.Contains(ch))
            {
                break;
            }

            splitIndex++;
        }

        if (splitIndex <= 0)
        {
            return null;
        }

        return (text[..splitIndex], text[splitIndex..]);
    }

    private static bool TrySplitMyanmarRun(string text, out IReadOnlyList<string> parts)
    {
        static bool TrySplitBeforeSuffix(string source, string suffix, out IReadOnlyList<string> split)
        {
            split = Array.Empty<string>();
            if (!source.EndsWith(suffix, StringComparison.Ordinal) || source.Length <= suffix.Length)
            {
                return false;
            }

            split = [source[..^suffix.Length], suffix];
            return true;
        }

        parts = Array.Empty<string>();

        if (TrySplitBeforeSuffix(text, "ဖြင့်", out parts) ||
            TrySplitBeforeSuffix(text, "ကြ၏။", out parts))
        {
            return true;
        }

        var index = text.IndexOf("ချီ၍", StringComparison.Ordinal);
        if (index > 0 && index < text.Length)
        {
            parts =
            [
                text[..index],
                text[index..],
            ];
            return true;
        }

        return false;
    }
}
