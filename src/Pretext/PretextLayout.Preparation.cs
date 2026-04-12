using System.Text;

namespace Pretext;

public static partial class PretextLayout
{
    private static PreparedText PrepareCore(string text, string font, PrepareOptions options, bool includeSegments)
    {
        var fontState = GetFontState(font);
        var tokens = AnalyzeTokens(text ?? string.Empty, options.WhiteSpace);
        if (tokens.Count == 0)
        {
            return includeSegments
                ? new PreparedTextWithSegments(
                    font,
                    options.WhiteSpace,
                    fontState.HyphenWidth,
                    fontState.TabStopAdvance,
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    true,
                    null)
                : new PreparedText(
                    font,
                    options.WhiteSpace,
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    fontState.HyphenWidth,
                    fontState.TabStopAdvance,
                    [],
                    true);
        }

        var segmentTexts = includeSegments ? new List<string>(tokens.Count) : null;
        var widths = new List<double>(tokens.Count);
        var lineEndFitAdvances = new List<double>(tokens.Count);
        var lineEndPaintAdvances = new List<double>(tokens.Count);
        var kinds = new List<SegmentBreakKind>(tokens.Count);
        var breakableWidths = new List<double[]?>(tokens.Count);
        var breakablePrefixWidths = new List<double[]?>(tokens.Count);
        var normalized = new StringBuilder();
        var starts = new List<int>(tokens.Count);
        var simpleLineWalkFastPath = true;
        var engineProfile = GetEngineProfile();

        foreach (var token in tokens)
        {
            foreach (var segment in ExpandPreparedSegments(token, fontState, engineProfile, options.WordBreak))
            {
                starts.Add(normalized.Length);
                normalized.Append(segment.Text);

                simpleLineWalkFastPath &= segment.Kind is SegmentBreakKind.Text or SegmentBreakKind.Space or SegmentBreakKind.ZeroWidthBreak;

                if (includeSegments)
                {
                    segmentTexts!.Add(segment.Text);
                }

                widths.Add(segment.Width);
                lineEndFitAdvances.Add(GetLineEndFitAdvance(segment, fontState.HyphenWidth));
                lineEndPaintAdvances.Add(GetLineEndPaintAdvance(segment, fontState.HyphenWidth));
                kinds.Add(segment.Kind);
                breakableWidths.Add(segment.BreakableWidths);
                breakablePrefixWidths.Add(segment.BreakablePrefixWidths);
            }
        }

        var chunks = BuildChunks(kinds);
        simpleLineWalkFastPath &= chunks.Length <= 1;

        if (includeSegments)
        {
            var levels = BidiHelper.ComputeSegmentLevels(normalized.ToString(), starts);
            return new PreparedTextWithSegments(
                font,
                options.WhiteSpace,
                fontState.HyphenWidth,
                fontState.TabStopAdvance,
                segmentTexts!.ToArray(),
                widths.ToArray(),
                lineEndFitAdvances.ToArray(),
                lineEndPaintAdvances.ToArray(),
                kinds.ToArray(),
                breakableWidths.ToArray(),
                breakablePrefixWidths.ToArray(),
                chunks,
                simpleLineWalkFastPath,
                levels);
        }

        return new PreparedText(
            font,
            options.WhiteSpace,
            widths.ToArray(),
            lineEndFitAdvances.ToArray(),
            lineEndPaintAdvances.ToArray(),
            kinds.ToArray(),
            breakableWidths.ToArray(),
            breakablePrefixWidths.ToArray(),
            fontState.HyphenWidth,
            fontState.TabStopAdvance,
            chunks,
            simpleLineWalkFastPath);
    }

    private static FontState GetFontState(string font)
    {
        lock (FontStateGate)
        {
            if (FontStates.TryGetValue(font, out var cached))
            {
                return cached;
            }

            var state = FontState.Create(font);
            FontStates[font] = state;
            return state;
        }
    }

    private static IEnumerable<MeasuredSegment> ExpandPreparedSegments(
        AnalysisToken token,
        FontState fontState,
        EngineProfile profile,
        WordBreakMode wordBreak)
    {
        switch (token.Kind)
        {
            case SegmentBreakKind.HardBreak:
                yield return new MeasuredSegment(token.Text, token.Kind, false, 0, null, null);
                yield break;

            case SegmentBreakKind.ZeroWidthBreak:
                yield return new MeasuredSegment(token.Text, token.Kind, false, 0, null, null);
                yield break;

            case SegmentBreakKind.SoftHyphen:
                yield return new MeasuredSegment(token.Text, token.Kind, false, 0, null, null);
                yield break;

            case SegmentBreakKind.Tab:
                yield return new MeasuredSegment(token.Text, token.Kind, false, 0, null, null);
                yield break;
        }

        if (token.Kind == SegmentBreakKind.Text && ContainsCjk(token.Text))
        {
            var units = BuildBaseCjkUnits(token.Text, profile.CarryCjkAfterClosingQuote);
            if (wordBreak == WordBreakMode.KeepAll)
            {
                units = MergeKeepAllTextUnits(units);
            }

            foreach (var unit in units)
            {
                yield return fontState.MeasureSegment(
                    unit,
                    token.Kind,
                    isBreakableRun: wordBreak == WordBreakMode.KeepAll || !ContainsCjk(unit));
            }

            yield break;
        }

        yield return fontState.MeasureSegment(
            token.Text,
            token.Kind,
            isBreakableRun: token.Kind == SegmentBreakKind.Text && token.IsWordLike && token.Text.Length > 1);
    }

    private static double GetLineEndFitAdvance(MeasuredSegment segment, double hyphenWidth)
    {
        return segment.Kind switch
        {
            SegmentBreakKind.Space => 0,
            SegmentBreakKind.PreservedSpace => 0,
            SegmentBreakKind.ZeroWidthBreak => 0,
            SegmentBreakKind.SoftHyphen => hyphenWidth,
            SegmentBreakKind.Tab => 0,
            SegmentBreakKind.HardBreak => 0,
            _ => segment.Width,
        };
    }

    private static double GetLineEndPaintAdvance(MeasuredSegment segment, double hyphenWidth)
    {
        return segment.Kind switch
        {
            SegmentBreakKind.Space => 0,
            SegmentBreakKind.ZeroWidthBreak => 0,
            SegmentBreakKind.SoftHyphen => hyphenWidth,
            SegmentBreakKind.Tab => 0,
            SegmentBreakKind.HardBreak => 0,
            _ => segment.Width,
        };
    }

    private static PreparedLineChunk[] BuildChunks(IReadOnlyList<SegmentBreakKind> kinds)
    {
        var chunks = new List<PreparedLineChunk>();
        if (kinds.Count == 0)
        {
            return [];
        }

        var chunkStart = 0;
        for (var index = 0; index < kinds.Count; index++)
        {
            if (kinds[index] != SegmentBreakKind.HardBreak)
            {
                continue;
            }

            chunks.Add(new PreparedLineChunk(chunkStart, index, index + 1));
            chunkStart = index + 1;
        }

        if (chunkStart < kinds.Count)
        {
            chunks.Add(new PreparedLineChunk(chunkStart, kinds.Count, kinds.Count));
        }

        return chunks.ToArray();
    }
}
