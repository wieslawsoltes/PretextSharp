using System.Text;

namespace Pretext;

public static partial class PretextLayout
{
    private readonly record struct InternalLine(LayoutCursor Start, LayoutCursor VisibleEnd, LayoutCursor End, double Width, bool AppendHyphen);

    private static int CountPreparedLines(PreparedText prepared, double maxWidth)
    {
        if (prepared.SimpleLineWalkFastPathInternal)
        {
            return CountPreparedLinesSimple(prepared, maxWidth);
        }

        return WalkPreparedLines(prepared, maxWidth);
    }

    private static int CountPreparedLinesSimple(PreparedText prepared, double maxWidth)
    {
        var widths = prepared.WidthsInternal;
        if (widths.Length == 0)
        {
            return 0;
        }

        var kinds = prepared.KindsInternal;
        var breakableWidths = prepared.BreakableWidthsInternal;
        var breakablePrefixWidths = prepared.BreakablePrefixWidthsInternal;
        var engineProfile = GetEngineProfile();
        var epsilon = engineProfile.LineFitEpsilon;

        var lineCount = 0;
        var lineWidth = 0d;
        var hasContent = false;

        void PlaceOnFreshLine(int segmentIndex)
        {
            var width = widths[segmentIndex];
            var graphemeWidths = breakableWidths[segmentIndex];
            if (width > maxWidth + epsilon && graphemeWidths is not null)
            {
                var prefixWidths = breakablePrefixWidths[segmentIndex];
                lineWidth = 0;
                for (var graphemeIndex = 0; graphemeIndex < graphemeWidths.Length; graphemeIndex++)
                {
                    var graphemeWidth = GetBreakableAdvance(
                        graphemeWidths,
                        prefixWidths,
                        graphemeIndex,
                        engineProfile.PreferPrefixWidthsForBreakableRuns);

                    if (lineWidth > 0 && lineWidth + graphemeWidth > maxWidth + epsilon)
                    {
                        lineCount++;
                        lineWidth = graphemeWidth;
                    }
                    else
                    {
                        if (lineWidth == 0)
                        {
                            lineCount++;
                        }

                        lineWidth += graphemeWidth;
                    }
                }
            }
            else
            {
                lineWidth = width;
                lineCount++;
            }

            hasContent = true;
        }

        for (var segmentIndex = 0; segmentIndex < widths.Length; segmentIndex++)
        {
            var width = widths[segmentIndex];
            var kind = kinds[segmentIndex];

            if (!hasContent)
            {
                PlaceOnFreshLine(segmentIndex);
                continue;
            }

            var newWidth = lineWidth + width;
            if (newWidth > maxWidth + epsilon)
            {
                if (IsSimpleCollapsibleSpace(kind))
                {
                    continue;
                }

                lineWidth = 0;
                hasContent = false;
                PlaceOnFreshLine(segmentIndex);
                continue;
            }

            lineWidth = newWidth;
        }

        return hasContent ? lineCount : lineCount + 1;
    }

    private static int WalkPreparedLinesSimple(PreparedText prepared, double maxWidth, Action<InternalLine>? onLine)
    {
        var widths = prepared.WidthsInternal;
        if (widths.Length == 0)
        {
            return 0;
        }

        var kinds = prepared.KindsInternal;
        var breakableWidths = prepared.BreakableWidthsInternal;
        var breakablePrefixWidths = prepared.BreakablePrefixWidthsInternal;
        var engineProfile = GetEngineProfile();
        var epsilon = engineProfile.LineFitEpsilon;

        var lineCount = 0;
        var lineWidth = 0d;
        var hasContent = false;
        var lineStartSegmentIndex = 0;
        var lineStartGraphemeIndex = 0;
        var lineEndSegmentIndex = 0;
        var lineEndGraphemeIndex = 0;
        var pendingBreakSegmentIndex = -1;
        var pendingBreakPaintWidth = 0d;

        void ClearPendingBreak()
        {
            pendingBreakSegmentIndex = -1;
            pendingBreakPaintWidth = 0;
        }

        void EmitCurrentLine(int endSegmentIndex = -1, int endGraphemeIndex = -1, double? width = null)
        {
            endSegmentIndex = endSegmentIndex >= 0 ? endSegmentIndex : lineEndSegmentIndex;
            endGraphemeIndex = endGraphemeIndex >= 0 ? endGraphemeIndex : lineEndGraphemeIndex;
            var emitWidth = width ?? lineWidth;

            lineCount++;
            var endCursor = new LayoutCursor(endSegmentIndex, endGraphemeIndex);
            onLine?.Invoke(new InternalLine(
                new LayoutCursor(lineStartSegmentIndex, lineStartGraphemeIndex),
                endCursor,
                endCursor,
                emitWidth,
                AppendHyphen: false));
            lineWidth = 0;
            hasContent = false;
            ClearPendingBreak();
        }

        void StartLineAtSegment(int segmentIndex, double width)
        {
            hasContent = true;
            lineStartSegmentIndex = segmentIndex;
            lineStartGraphemeIndex = 0;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
            lineWidth = width;
        }

        void StartLineAtGrapheme(int segmentIndex, int graphemeIndex, double width)
        {
            hasContent = true;
            lineStartSegmentIndex = segmentIndex;
            lineStartGraphemeIndex = graphemeIndex;
            lineEndSegmentIndex = segmentIndex;
            lineEndGraphemeIndex = graphemeIndex + 1;
            lineWidth = width;
        }

        void AppendWholeSegment(int segmentIndex, double width)
        {
            if (!hasContent)
            {
                StartLineAtSegment(segmentIndex, width);
                return;
            }

            lineWidth += width;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
        }

        void UpdatePendingBreak(int segmentIndex, double segmentWidth)
        {
            if (!CanBreakAfter(kinds[segmentIndex]))
            {
                return;
            }

            pendingBreakSegmentIndex = segmentIndex + 1;
            pendingBreakPaintWidth = lineWidth - segmentWidth;
        }

        void AppendBreakableSegmentFrom(int segmentIndex, int startGraphemeIndex)
        {
            var graphemeWidths = breakableWidths[segmentIndex]!;
            var prefixWidths = breakablePrefixWidths[segmentIndex];

            for (var graphemeIndex = startGraphemeIndex; graphemeIndex < graphemeWidths.Length; graphemeIndex++)
            {
                var graphemeWidth = GetBreakableAdvance(
                    graphemeWidths,
                    prefixWidths,
                    graphemeIndex,
                    engineProfile.PreferPrefixWidthsForBreakableRuns);

                if (!hasContent)
                {
                    StartLineAtGrapheme(segmentIndex, graphemeIndex, graphemeWidth);
                    continue;
                }

                if (lineWidth + graphemeWidth > maxWidth + epsilon)
                {
                    EmitCurrentLine();
                    StartLineAtGrapheme(segmentIndex, graphemeIndex, graphemeWidth);
                }
                else
                {
                    lineWidth += graphemeWidth;
                    lineEndSegmentIndex = segmentIndex;
                    lineEndGraphemeIndex = graphemeIndex + 1;
                }
            }

            if (hasContent &&
                lineEndSegmentIndex == segmentIndex &&
                lineEndGraphemeIndex == graphemeWidths.Length)
            {
                lineEndSegmentIndex = segmentIndex + 1;
                lineEndGraphemeIndex = 0;
            }
        }

        var segmentIndex = 0;
        while (segmentIndex < widths.Length)
        {
            var width = widths[segmentIndex];
            var kind = kinds[segmentIndex];

            if (!hasContent)
            {
                if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                {
                    AppendBreakableSegmentFrom(segmentIndex, 0);
                }
                else
                {
                    StartLineAtSegment(segmentIndex, width);
                }

                UpdatePendingBreak(segmentIndex, width);
                segmentIndex++;
                continue;
            }

            var newWidth = lineWidth + width;
            if (newWidth > maxWidth + epsilon)
            {
                if (CanBreakAfter(kind))
                {
                    AppendWholeSegment(segmentIndex, width);
                    EmitCurrentLine(segmentIndex + 1, 0, lineWidth - width);
                    segmentIndex++;
                    continue;
                }

                if (pendingBreakSegmentIndex >= 0)
                {
                    EmitCurrentLine(pendingBreakSegmentIndex, 0, pendingBreakPaintWidth);
                    continue;
                }

                if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                {
                    EmitCurrentLine();
                    AppendBreakableSegmentFrom(segmentIndex, 0);
                    segmentIndex++;
                    continue;
                }

                EmitCurrentLine();
                continue;
            }

            AppendWholeSegment(segmentIndex, width);
            UpdatePendingBreak(segmentIndex, width);
            segmentIndex++;
        }

        if (hasContent)
        {
            EmitCurrentLine();
        }

        return lineCount;
    }

    private static int WalkPreparedLines(PreparedText prepared, double maxWidth, Action<InternalLine>? onLine = null)
    {
        if (prepared.SimpleLineWalkFastPathInternal)
        {
            return WalkPreparedLinesSimple(prepared, maxWidth, onLine);
        }

        var widths = prepared.WidthsInternal;
        var chunks = prepared.ChunksInternal;
        if (widths.Length == 0 || chunks.Length == 0)
        {
            return 0;
        }

        var kinds = prepared.KindsInternal;
        var lineEndFitAdvances = prepared.LineEndFitAdvancesInternal;
        var lineEndPaintAdvances = prepared.LineEndPaintAdvancesInternal;
        var breakableWidths = prepared.BreakableWidthsInternal;
        var breakablePrefixWidths = prepared.BreakablePrefixWidthsInternal;
        var discretionaryHyphenWidth = prepared.DiscretionaryHyphenWidth;
        var tabStopAdvance = prepared.TabStopAdvance;
        var engineProfile = GetEngineProfile();
        var epsilon = engineProfile.LineFitEpsilon;

        var lineCount = 0;
        var lineWidth = 0d;
        var hasContent = false;
        var lineStartSegmentIndex = 0;
        var lineStartGraphemeIndex = 0;
        var lineEndSegmentIndex = 0;
        var lineEndGraphemeIndex = 0;
        var pendingBreakSegmentIndex = -1;
        var pendingBreakFitWidth = 0d;
        var pendingBreakPaintWidth = 0d;
        SegmentBreakKind? pendingBreakKind = null;

        void ClearPendingBreak()
        {
            pendingBreakSegmentIndex = -1;
            pendingBreakFitWidth = 0;
            pendingBreakPaintWidth = 0;
            pendingBreakKind = null;
        }

        void EmitCurrentLine(int endSegmentIndex = -1, int endGraphemeIndex = -1, double? width = null, bool appendHyphen = false)
        {
            endSegmentIndex = endSegmentIndex >= 0 ? endSegmentIndex : lineEndSegmentIndex;
            endGraphemeIndex = endGraphemeIndex >= 0 ? endGraphemeIndex : lineEndGraphemeIndex;
            var emitWidth = width ?? lineWidth;
            var endCursor = new LayoutCursor(endSegmentIndex, endGraphemeIndex);

            lineCount++;
            onLine?.Invoke(new InternalLine(
                new LayoutCursor(lineStartSegmentIndex, lineStartGraphemeIndex),
                endCursor,
                endCursor,
                emitWidth,
                appendHyphen));
            lineWidth = 0;
            hasContent = false;
            ClearPendingBreak();
        }

        void StartLineAtSegment(int segmentIndex, double width)
        {
            hasContent = true;
            lineStartSegmentIndex = segmentIndex;
            lineStartGraphemeIndex = 0;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
            lineWidth = width;
        }

        void StartLineAtGrapheme(int segmentIndex, int graphemeIndex, double width)
        {
            hasContent = true;
            lineStartSegmentIndex = segmentIndex;
            lineStartGraphemeIndex = graphemeIndex;
            lineEndSegmentIndex = segmentIndex;
            lineEndGraphemeIndex = graphemeIndex + 1;
            lineWidth = width;
        }

        void AppendWholeSegment(int segmentIndex, double width)
        {
            if (!hasContent)
            {
                StartLineAtSegment(segmentIndex, width);
                return;
            }

            lineWidth += width;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
        }

        void UpdatePendingBreakForWholeSegment(int segmentIndex, double segmentWidth)
        {
            if (!CanBreakAfter(kinds[segmentIndex]))
            {
                return;
            }

            var fitAdvance = kinds[segmentIndex] == SegmentBreakKind.Tab ? 0 : lineEndFitAdvances[segmentIndex];
            var paintAdvance = kinds[segmentIndex] == SegmentBreakKind.Tab ? segmentWidth : lineEndPaintAdvances[segmentIndex];
            pendingBreakSegmentIndex = segmentIndex + 1;
            pendingBreakFitWidth = lineWidth - segmentWidth + fitAdvance;
            pendingBreakPaintWidth = lineWidth - segmentWidth + paintAdvance;
            pendingBreakKind = kinds[segmentIndex];
        }

        void AppendBreakableSegmentFrom(int segmentIndex, int startGraphemeIndex)
        {
            var graphemeWidths = breakableWidths[segmentIndex]!;
            var prefixWidths = breakablePrefixWidths[segmentIndex];

            for (var graphemeIndex = startGraphemeIndex; graphemeIndex < graphemeWidths.Length; graphemeIndex++)
            {
                var graphemeWidth = GetBreakableAdvance(
                    graphemeWidths,
                    prefixWidths,
                    graphemeIndex,
                    engineProfile.PreferPrefixWidthsForBreakableRuns);

                if (!hasContent)
                {
                    StartLineAtGrapheme(segmentIndex, graphemeIndex, graphemeWidth);
                    continue;
                }

                if (lineWidth + graphemeWidth > maxWidth + epsilon)
                {
                    EmitCurrentLine();
                    StartLineAtGrapheme(segmentIndex, graphemeIndex, graphemeWidth);
                }
                else
                {
                    lineWidth += graphemeWidth;
                    lineEndSegmentIndex = segmentIndex;
                    lineEndGraphemeIndex = graphemeIndex + 1;
                }
            }

            if (hasContent &&
                lineEndSegmentIndex == segmentIndex &&
                lineEndGraphemeIndex == graphemeWidths.Length)
            {
                lineEndSegmentIndex = segmentIndex + 1;
                lineEndGraphemeIndex = 0;
            }
        }

        bool ContinueSoftHyphenBreakableSegment(int segmentIndex)
        {
            if (pendingBreakKind != SegmentBreakKind.SoftHyphen)
            {
                return false;
            }

            var graphemeWidths = breakableWidths[segmentIndex];
            if (graphemeWidths is null)
            {
                return false;
            }

            var fitWidths = engineProfile.PreferPrefixWidthsForBreakableRuns
                ? breakablePrefixWidths[segmentIndex] ?? graphemeWidths
                : graphemeWidths;
            var usesPrefixWidths = !ReferenceEquals(fitWidths, graphemeWidths);
            var (fitCount, fittedWidth) = FitSoftHyphenBreak(
                fitWidths,
                lineWidth,
                maxWidth,
                epsilon,
                discretionaryHyphenWidth,
                usesPrefixWidths);

            if (fitCount == 0)
            {
                return false;
            }

            lineWidth = fittedWidth;
            lineEndSegmentIndex = segmentIndex;
            lineEndGraphemeIndex = fitCount;
            ClearPendingBreak();

            if (fitCount == graphemeWidths.Length)
            {
                lineEndSegmentIndex = segmentIndex + 1;
                lineEndGraphemeIndex = 0;
                return true;
            }

            EmitCurrentLine(segmentIndex, fitCount, fittedWidth + discretionaryHyphenWidth, appendHyphen: true);
            AppendBreakableSegmentFrom(segmentIndex, fitCount);
            return true;
        }

        void EmitEmptyChunk(PreparedLineChunk chunk)
        {
            lineCount++;
            var endCursor = new LayoutCursor(chunk.ConsumedEndSegmentIndex, 0);
            onLine?.Invoke(new InternalLine(
                new LayoutCursor(chunk.StartSegmentIndex, 0),
                endCursor,
                endCursor,
                0,
                AppendHyphen: false));
            ClearPendingBreak();
        }

        for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            var chunk = chunks[chunkIndex];
            if (chunk.StartSegmentIndex == chunk.EndSegmentIndex)
            {
                EmitEmptyChunk(chunk);
                continue;
            }

            hasContent = false;
            lineWidth = 0;
            lineStartSegmentIndex = chunk.StartSegmentIndex;
            lineStartGraphemeIndex = 0;
            lineEndSegmentIndex = chunk.StartSegmentIndex;
            lineEndGraphemeIndex = 0;
            ClearPendingBreak();

            var segmentIndex = chunk.StartSegmentIndex;
            while (segmentIndex < chunk.EndSegmentIndex)
            {
                var kind = kinds[segmentIndex];
                var width = kind == SegmentBreakKind.Tab
                    ? GetTabAdvance(lineWidth, tabStopAdvance)
                    : widths[segmentIndex];

                if (kind == SegmentBreakKind.SoftHyphen)
                {
                    if (hasContent)
                    {
                        lineEndSegmentIndex = segmentIndex + 1;
                        lineEndGraphemeIndex = 0;
                        pendingBreakSegmentIndex = segmentIndex + 1;
                        pendingBreakFitWidth = lineWidth + discretionaryHyphenWidth;
                        pendingBreakPaintWidth = lineWidth + discretionaryHyphenWidth;
                        pendingBreakKind = kind;
                    }

                    segmentIndex++;
                    continue;
                }

                if (!hasContent)
                {
                    if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                    {
                        AppendBreakableSegmentFrom(segmentIndex, 0);
                    }
                    else
                    {
                        StartLineAtSegment(segmentIndex, width);
                    }

                    UpdatePendingBreakForWholeSegment(segmentIndex, width);
                    segmentIndex++;
                    continue;
                }

                var newWidth = lineWidth + width;
                if (newWidth > maxWidth + epsilon)
                {
                    var currentBreakFitWidth = lineWidth + (kind == SegmentBreakKind.Tab ? 0 : lineEndFitAdvances[segmentIndex]);
                    var currentBreakPaintWidth = lineWidth + (kind == SegmentBreakKind.Tab ? width : lineEndPaintAdvances[segmentIndex]);

                    if (pendingBreakKind == SegmentBreakKind.SoftHyphen &&
                        engineProfile.PreferEarlySoftHyphenBreak &&
                        pendingBreakFitWidth <= maxWidth + epsilon)
                    {
                        EmitCurrentLine(pendingBreakSegmentIndex, 0, pendingBreakPaintWidth, appendHyphen: true);
                        continue;
                    }

                    if (pendingBreakKind == SegmentBreakKind.SoftHyphen && ContinueSoftHyphenBreakableSegment(segmentIndex))
                    {
                        segmentIndex++;
                        continue;
                    }

                    if (CanBreakAfter(kind) && currentBreakFitWidth <= maxWidth + epsilon)
                    {
                        AppendWholeSegment(segmentIndex, width);
                        EmitCurrentLine(segmentIndex + 1, 0, currentBreakPaintWidth);
                        segmentIndex++;
                        continue;
                    }

                    if (pendingBreakSegmentIndex >= 0 && pendingBreakFitWidth <= maxWidth + epsilon)
                    {
                        EmitCurrentLine(
                            pendingBreakSegmentIndex,
                            0,
                            pendingBreakPaintWidth,
                            appendHyphen: pendingBreakKind == SegmentBreakKind.SoftHyphen);
                        continue;
                    }

                    if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                    {
                        EmitCurrentLine();
                        AppendBreakableSegmentFrom(segmentIndex, 0);
                        segmentIndex++;
                        continue;
                    }

                    EmitCurrentLine();
                    continue;
                }

                AppendWholeSegment(segmentIndex, width);
                UpdatePendingBreakForWholeSegment(segmentIndex, width);
                segmentIndex++;
            }

            if (hasContent)
            {
                var finalPaintWidth = pendingBreakSegmentIndex == chunk.ConsumedEndSegmentIndex
                    ? pendingBreakPaintWidth
                    : lineWidth;
                EmitCurrentLine(chunk.ConsumedEndSegmentIndex, 0, finalPaintWidth);
            }
        }

        return lineCount;
    }

    private static bool TryStepLine(PreparedText prepared, LayoutCursor start, double maxWidth, out InternalLine line)
    {
        var normalized = NormalizeLineStart(prepared, start);
        if (normalized is null)
        {
            line = default;
            return false;
        }

        return prepared.SimpleLineWalkFastPathInternal
            ? TryStepLineSimple(prepared, normalized.Value, maxWidth, out line)
            : TryStepLineGeneral(prepared, normalized.Value, maxWidth, out line);
    }

    private static bool TryStepLineSimple(PreparedText prepared, LayoutCursor normalizedStart, double maxWidth, out InternalLine line)
    {
        line = default;

        var widths = prepared.WidthsInternal;
        if (widths.Length == 0)
        {
            return false;
        }

        var kinds = prepared.KindsInternal;
        var breakableWidths = prepared.BreakableWidthsInternal;
        var breakablePrefixWidths = prepared.BreakablePrefixWidthsInternal;
        var engineProfile = GetEngineProfile();
        var epsilon = engineProfile.LineFitEpsilon;

        var lineWidth = 0d;
        var hasContent = false;
        var lineStartSegmentIndex = normalizedStart.SegmentIndex;
        var lineStartGraphemeIndex = normalizedStart.GraphemeIndex;
        var lineEndSegmentIndex = lineStartSegmentIndex;
        var lineEndGraphemeIndex = lineStartGraphemeIndex;
        var pendingBreakSegmentIndex = -1;
        var pendingBreakPaintWidth = 0d;

        InternalLine? FinishLine(int? endSegmentIndex = null, int? endGraphemeIndex = null, double? width = null)
        {
            if (!hasContent)
            {
                return null;
            }

            var endCursor = new LayoutCursor(endSegmentIndex ?? lineEndSegmentIndex, endGraphemeIndex ?? lineEndGraphemeIndex);
            return new InternalLine(
                new LayoutCursor(lineStartSegmentIndex, lineStartGraphemeIndex),
                endCursor,
                endCursor,
                width ?? lineWidth,
                AppendHyphen: false);
        }

        void StartLineAtSegment(int segmentIndex, double width)
        {
            hasContent = true;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
            lineWidth = width;
        }

        void StartLineAtGrapheme(int segmentIndex, int graphemeIndex, double width)
        {
            hasContent = true;
            lineEndSegmentIndex = segmentIndex;
            lineEndGraphemeIndex = graphemeIndex + 1;
            lineWidth = width;
        }

        void AppendWholeSegment(int segmentIndex, double width)
        {
            if (!hasContent)
            {
                StartLineAtSegment(segmentIndex, width);
                return;
            }

            lineWidth += width;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
        }

        void UpdatePendingBreak(int segmentIndex, double segmentWidth)
        {
            if (!CanBreakAfter(kinds[segmentIndex]))
            {
                return;
            }

            pendingBreakSegmentIndex = segmentIndex + 1;
            pendingBreakPaintWidth = lineWidth - segmentWidth;
        }

        InternalLine? AppendBreakableSegmentFrom(int segmentIndex, int startGraphemeIndex)
        {
            var graphemeWidths = breakableWidths[segmentIndex]!;
            var prefixWidths = breakablePrefixWidths[segmentIndex];
            for (var graphemeIndex = startGraphemeIndex; graphemeIndex < graphemeWidths.Length; graphemeIndex++)
            {
                var graphemeWidth = GetBreakableAdvance(
                    graphemeWidths,
                    prefixWidths,
                    graphemeIndex,
                    engineProfile.PreferPrefixWidthsForBreakableRuns);

                if (!hasContent)
                {
                    StartLineAtGrapheme(segmentIndex, graphemeIndex, graphemeWidth);
                    continue;
                }

                if (lineWidth + graphemeWidth > maxWidth + epsilon)
                {
                    return FinishLine();
                }

                lineWidth += graphemeWidth;
                lineEndSegmentIndex = segmentIndex;
                lineEndGraphemeIndex = graphemeIndex + 1;
            }

            if (hasContent &&
                lineEndSegmentIndex == segmentIndex &&
                lineEndGraphemeIndex == graphemeWidths.Length)
            {
                lineEndSegmentIndex = segmentIndex + 1;
                lineEndGraphemeIndex = 0;
            }

            return null;
        }

        for (var segmentIndex = normalizedStart.SegmentIndex; segmentIndex < widths.Length; segmentIndex++)
        {
            var width = widths[segmentIndex];
            var kind = kinds[segmentIndex];
            var startGraphemeIndex = segmentIndex == normalizedStart.SegmentIndex ? normalizedStart.GraphemeIndex : 0;

            if (!hasContent)
            {
                InternalLine? emittedLine;
                if (startGraphemeIndex > 0)
                {
                    emittedLine = AppendBreakableSegmentFrom(segmentIndex, startGraphemeIndex);
                    if (emittedLine is not null)
                    {
                        line = emittedLine.Value;
                        return true;
                    }
                }
                else if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                {
                    emittedLine = AppendBreakableSegmentFrom(segmentIndex, 0);
                    if (emittedLine is not null)
                    {
                        line = emittedLine.Value;
                        return true;
                    }
                }
                else
                {
                    StartLineAtSegment(segmentIndex, width);
                }

                UpdatePendingBreak(segmentIndex, width);
                continue;
            }

            var newWidth = lineWidth + width;
            if (newWidth > maxWidth + epsilon)
            {
                if (CanBreakAfter(kind))
                {
                    AppendWholeSegment(segmentIndex, width);
                    line = FinishLine(segmentIndex + 1, 0, lineWidth - width)!.Value;
                    return true;
                }

                if (pendingBreakSegmentIndex >= 0)
                {
                    line = FinishLine(pendingBreakSegmentIndex, 0, pendingBreakPaintWidth)!.Value;
                    return true;
                }

                if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                {
                    var currentLine = FinishLine();
                    if (currentLine is not null)
                    {
                        line = currentLine.Value;
                        return true;
                    }

                    var emittedLine = AppendBreakableSegmentFrom(segmentIndex, 0);
                    if (emittedLine is not null)
                    {
                        line = emittedLine.Value;
                        return true;
                    }
                }

                var finishedLine = FinishLine();
                if (finishedLine is not null)
                {
                    line = finishedLine.Value;
                    return true;
                }

                return false;
            }

            AppendWholeSegment(segmentIndex, width);
            UpdatePendingBreak(segmentIndex, width);
        }

        var finalLine = FinishLine();
        if (finalLine is null)
        {
            return false;
        }

        line = finalLine.Value;
        return true;
    }

    private static bool TryStepLineGeneral(PreparedText prepared, LayoutCursor normalizedStart, double maxWidth, out InternalLine line)
    {
        line = default;

        var chunkIndex = FindChunkIndexForStart(prepared, normalizedStart.SegmentIndex);
        if (chunkIndex < 0)
        {
            return false;
        }

        var chunk = prepared.ChunksInternal[chunkIndex];
        if (chunk.StartSegmentIndex == chunk.EndSegmentIndex)
        {
            var endCursor = new LayoutCursor(chunk.ConsumedEndSegmentIndex, 0);
            line = new InternalLine(
                new LayoutCursor(chunk.StartSegmentIndex, 0),
                endCursor,
                endCursor,
                0,
                AppendHyphen: false);
            return true;
        }

        var widths = prepared.WidthsInternal;
        var lineEndFitAdvances = prepared.LineEndFitAdvancesInternal;
        var lineEndPaintAdvances = prepared.LineEndPaintAdvancesInternal;
        var kinds = prepared.KindsInternal;
        var breakableWidths = prepared.BreakableWidthsInternal;
        var breakablePrefixWidths = prepared.BreakablePrefixWidthsInternal;
        var discretionaryHyphenWidth = prepared.DiscretionaryHyphenWidth;
        var tabStopAdvance = prepared.TabStopAdvance;
        var engineProfile = GetEngineProfile();
        var epsilon = engineProfile.LineFitEpsilon;

        var lineWidth = 0d;
        var hasContent = false;
        var lineStartSegmentIndex = normalizedStart.SegmentIndex;
        var lineStartGraphemeIndex = normalizedStart.GraphemeIndex;
        var lineEndSegmentIndex = lineStartSegmentIndex;
        var lineEndGraphemeIndex = lineStartGraphemeIndex;
        var pendingBreakSegmentIndex = -1;
        var pendingBreakFitWidth = 0d;
        var pendingBreakPaintWidth = 0d;
        SegmentBreakKind? pendingBreakKind = null;

        void ClearPendingBreak()
        {
            pendingBreakSegmentIndex = -1;
            pendingBreakFitWidth = 0;
            pendingBreakPaintWidth = 0;
            pendingBreakKind = null;
        }

        InternalLine? FinishLine(int? endSegmentIndex = null, int? endGraphemeIndex = null, double? width = null, bool appendHyphen = false)
        {
            if (!hasContent)
            {
                return null;
            }

            var endCursor = new LayoutCursor(endSegmentIndex ?? lineEndSegmentIndex, endGraphemeIndex ?? lineEndGraphemeIndex);
            return new InternalLine(
                new LayoutCursor(lineStartSegmentIndex, lineStartGraphemeIndex),
                endCursor,
                endCursor,
                width ?? lineWidth,
                appendHyphen);
        }

        void StartLineAtSegment(int segmentIndex, double width)
        {
            hasContent = true;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
            lineWidth = width;
        }

        void StartLineAtGrapheme(int segmentIndex, int graphemeIndex, double width)
        {
            hasContent = true;
            lineEndSegmentIndex = segmentIndex;
            lineEndGraphemeIndex = graphemeIndex + 1;
            lineWidth = width;
        }

        void AppendWholeSegment(int segmentIndex, double width)
        {
            if (!hasContent)
            {
                StartLineAtSegment(segmentIndex, width);
                return;
            }

            lineWidth += width;
            lineEndSegmentIndex = segmentIndex + 1;
            lineEndGraphemeIndex = 0;
        }

        void UpdatePendingBreakForWholeSegment(int segmentIndex, double segmentWidth)
        {
            if (!CanBreakAfter(kinds[segmentIndex]))
            {
                return;
            }

            var fitAdvance = kinds[segmentIndex] == SegmentBreakKind.Tab ? 0 : lineEndFitAdvances[segmentIndex];
            var paintAdvance = kinds[segmentIndex] == SegmentBreakKind.Tab ? segmentWidth : lineEndPaintAdvances[segmentIndex];
            pendingBreakSegmentIndex = segmentIndex + 1;
            pendingBreakFitWidth = lineWidth - segmentWidth + fitAdvance;
            pendingBreakPaintWidth = lineWidth - segmentWidth + paintAdvance;
            pendingBreakKind = kinds[segmentIndex];
        }

        InternalLine? AppendBreakableSegmentFrom(int segmentIndex, int startGraphemeIndex)
        {
            var graphemeWidths = breakableWidths[segmentIndex]!;
            var prefixWidths = breakablePrefixWidths[segmentIndex];
            for (var graphemeIndex = startGraphemeIndex; graphemeIndex < graphemeWidths.Length; graphemeIndex++)
            {
                var graphemeWidth = GetBreakableAdvance(
                    graphemeWidths,
                    prefixWidths,
                    graphemeIndex,
                    engineProfile.PreferPrefixWidthsForBreakableRuns);

                if (!hasContent)
                {
                    StartLineAtGrapheme(segmentIndex, graphemeIndex, graphemeWidth);
                    continue;
                }

                if (lineWidth + graphemeWidth > maxWidth + epsilon)
                {
                    return FinishLine();
                }

                lineWidth += graphemeWidth;
                lineEndSegmentIndex = segmentIndex;
                lineEndGraphemeIndex = graphemeIndex + 1;
            }

            if (hasContent &&
                lineEndSegmentIndex == segmentIndex &&
                lineEndGraphemeIndex == graphemeWidths.Length)
            {
                lineEndSegmentIndex = segmentIndex + 1;
                lineEndGraphemeIndex = 0;
            }

            return null;
        }

        InternalLine? MaybeFinishAtSoftHyphen(int segmentIndex)
        {
            if (pendingBreakKind != SegmentBreakKind.SoftHyphen || pendingBreakSegmentIndex < 0)
            {
                return null;
            }

            var graphemeWidths = breakableWidths[segmentIndex];
            if (graphemeWidths is not null)
            {
                var fitWidths = engineProfile.PreferPrefixWidthsForBreakableRuns
                    ? breakablePrefixWidths[segmentIndex] ?? graphemeWidths
                    : graphemeWidths;
                var usesPrefixWidths = !ReferenceEquals(fitWidths, graphemeWidths);
                var (fitCount, fittedWidth) = FitSoftHyphenBreak(
                    fitWidths,
                    lineWidth,
                    maxWidth,
                    epsilon,
                    discretionaryHyphenWidth,
                    usesPrefixWidths);

                if (fitCount == graphemeWidths.Length)
                {
                    lineWidth = fittedWidth;
                    lineEndSegmentIndex = segmentIndex + 1;
                    lineEndGraphemeIndex = 0;
                    ClearPendingBreak();
                    return null;
                }

                if (fitCount > 0)
                {
                    return FinishLine(
                        segmentIndex,
                        fitCount,
                        fittedWidth + discretionaryHyphenWidth,
                        appendHyphen: true);
                }
            }

            if (pendingBreakFitWidth <= maxWidth + epsilon)
            {
                return FinishLine(pendingBreakSegmentIndex, 0, pendingBreakPaintWidth, appendHyphen: true);
            }

            return null;
        }

        for (var segmentIndex = normalizedStart.SegmentIndex; segmentIndex < chunk.EndSegmentIndex; segmentIndex++)
        {
            var kind = kinds[segmentIndex];
            var startGraphemeIndex = segmentIndex == normalizedStart.SegmentIndex ? normalizedStart.GraphemeIndex : 0;
            var width = kind == SegmentBreakKind.Tab
                ? GetTabAdvance(lineWidth, tabStopAdvance)
                : widths[segmentIndex];

            if (kind == SegmentBreakKind.SoftHyphen && startGraphemeIndex == 0)
            {
                if (hasContent)
                {
                    lineEndSegmentIndex = segmentIndex + 1;
                    lineEndGraphemeIndex = 0;
                    pendingBreakSegmentIndex = segmentIndex + 1;
                    pendingBreakFitWidth = lineWidth + discretionaryHyphenWidth;
                    pendingBreakPaintWidth = lineWidth + discretionaryHyphenWidth;
                    pendingBreakKind = kind;
                }

                continue;
            }

            if (!hasContent)
            {
                InternalLine? emittedLine;
                if (startGraphemeIndex > 0)
                {
                    emittedLine = AppendBreakableSegmentFrom(segmentIndex, startGraphemeIndex);
                    if (emittedLine is not null)
                    {
                        line = emittedLine.Value;
                        return true;
                    }
                }
                else if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                {
                    emittedLine = AppendBreakableSegmentFrom(segmentIndex, 0);
                    if (emittedLine is not null)
                    {
                        line = emittedLine.Value;
                        return true;
                    }
                }
                else
                {
                    StartLineAtSegment(segmentIndex, width);
                }

                UpdatePendingBreakForWholeSegment(segmentIndex, width);
                continue;
            }

            var newWidth = lineWidth + width;
            if (newWidth > maxWidth + epsilon)
            {
                var currentBreakFitWidth = lineWidth + (kind == SegmentBreakKind.Tab ? 0 : lineEndFitAdvances[segmentIndex]);
                var currentBreakPaintWidth = lineWidth + (kind == SegmentBreakKind.Tab ? width : lineEndPaintAdvances[segmentIndex]);

                if (pendingBreakKind == SegmentBreakKind.SoftHyphen &&
                    engineProfile.PreferEarlySoftHyphenBreak &&
                    pendingBreakFitWidth <= maxWidth + epsilon)
                {
                    line = FinishLine(pendingBreakSegmentIndex, 0, pendingBreakPaintWidth, appendHyphen: true)!.Value;
                    return true;
                }

                var softBreakLine = MaybeFinishAtSoftHyphen(segmentIndex);
                if (softBreakLine is not null)
                {
                    line = softBreakLine.Value;
                    return true;
                }

                if (CanBreakAfter(kind) && currentBreakFitWidth <= maxWidth + epsilon)
                {
                    AppendWholeSegment(segmentIndex, width);
                    line = FinishLine(segmentIndex + 1, 0, currentBreakPaintWidth)!.Value;
                    return true;
                }

                if (pendingBreakSegmentIndex >= 0 && pendingBreakFitWidth <= maxWidth + epsilon)
                {
                    line = FinishLine(
                        pendingBreakSegmentIndex,
                        0,
                        pendingBreakPaintWidth,
                        appendHyphen: pendingBreakKind == SegmentBreakKind.SoftHyphen)!.Value;
                    return true;
                }

                if (width > maxWidth + epsilon && breakableWidths[segmentIndex] is not null)
                {
                    var currentLine = FinishLine();
                    if (currentLine is not null)
                    {
                        line = currentLine.Value;
                        return true;
                    }

                    var emittedLine = AppendBreakableSegmentFrom(segmentIndex, 0);
                    if (emittedLine is not null)
                    {
                        line = emittedLine.Value;
                        return true;
                    }
                }

                var finishedLine = FinishLine();
                if (finishedLine is not null)
                {
                    line = finishedLine.Value;
                    return true;
                }

                return false;
            }

            AppendWholeSegment(segmentIndex, width);
            UpdatePendingBreakForWholeSegment(segmentIndex, width);
        }

        var finalLine = pendingBreakSegmentIndex == chunk.ConsumedEndSegmentIndex && lineEndGraphemeIndex == 0
            ? FinishLine(chunk.ConsumedEndSegmentIndex, 0, pendingBreakPaintWidth)
            : FinishLine(chunk.ConsumedEndSegmentIndex, 0, lineWidth);

        if (finalLine is null)
        {
            return false;
        }

        line = finalLine.Value;
        return true;
    }

    private static int FindChunkIndexForStart(PreparedText prepared, int segmentIndex)
    {
        var chunks = prepared.ChunksInternal;
        for (var index = 0; index < chunks.Length; index++)
        {
            if (segmentIndex < chunks[index].ConsumedEndSegmentIndex)
            {
                return index;
            }
        }

        return -1;
    }

    private static LayoutCursor? NormalizeLineStart(PreparedText prepared, LayoutCursor start)
    {
        var widths = prepared.WidthsInternal;
        if (start.SegmentIndex >= widths.Length)
        {
            return null;
        }

        if (start.GraphemeIndex > 0)
        {
            return start;
        }

        var chunkIndex = FindChunkIndexForStart(prepared, start.SegmentIndex);
        if (chunkIndex < 0)
        {
            return null;
        }

        var chunks = prepared.ChunksInternal;
        var kinds = prepared.KindsInternal;
        var chunk = chunks[chunkIndex];
        var segmentIndex = start.SegmentIndex;

        if (chunk.StartSegmentIndex == chunk.EndSegmentIndex && segmentIndex == chunk.StartSegmentIndex)
        {
            return new LayoutCursor(segmentIndex, 0);
        }

        if (segmentIndex < chunk.StartSegmentIndex)
        {
            segmentIndex = chunk.StartSegmentIndex;
        }

        while (segmentIndex < chunk.EndSegmentIndex)
        {
            var kind = kinds[segmentIndex];
            if (kind is not (SegmentBreakKind.Space or SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.SoftHyphen))
            {
                return new LayoutCursor(segmentIndex, 0);
            }

            segmentIndex++;
        }

        if (chunk.ConsumedEndSegmentIndex >= widths.Length)
        {
            return null;
        }

        return new LayoutCursor(chunk.ConsumedEndSegmentIndex, 0);
    }

    private static double GetBreakableAdvance(
        double[] graphemeWidths,
        double[]? graphemePrefixWidths,
        int graphemeIndex,
        bool preferPrefixWidths)
    {
        if (!preferPrefixWidths || graphemePrefixWidths is null)
        {
            return graphemeWidths[graphemeIndex];
        }

        var end = graphemePrefixWidths[graphemeIndex];
        var start = graphemeIndex > 0 ? graphemePrefixWidths[graphemeIndex - 1] : 0;
        return end - start;
    }

    private static (int FitCount, double FittedWidth) FitSoftHyphenBreak(
        double[] graphemeWidths,
        double initialWidth,
        double maxWidth,
        double epsilon,
        double discretionaryHyphenWidth,
        bool cumulativeWidths)
    {
        var fitCount = 0;
        var fittedWidth = initialWidth;

        while (fitCount < graphemeWidths.Length)
        {
            var nextWidth = cumulativeWidths
                ? initialWidth + graphemeWidths[fitCount]
                : fittedWidth + graphemeWidths[fitCount];
            var nextLineWidth = fitCount + 1 < graphemeWidths.Length
                ? nextWidth + discretionaryHyphenWidth
                : nextWidth;

            if (nextLineWidth > maxWidth + epsilon)
            {
                break;
            }

            fittedWidth = nextWidth;
            fitCount++;
        }

        return (fitCount, fittedWidth);
    }

    private static bool CanBreakAfter(SegmentBreakKind kind)
    {
        return kind is
            SegmentBreakKind.Space or
            SegmentBreakKind.PreservedSpace or
            SegmentBreakKind.Tab or
            SegmentBreakKind.ZeroWidthBreak or
            SegmentBreakKind.SoftHyphen;
    }

    private static bool IsSimpleCollapsibleSpace(SegmentBreakKind kind)
    {
        return kind == SegmentBreakKind.Space;
    }

    private static LayoutLine MaterializeLine(PreparedTextWithSegments prepared, InternalLine line)
    {
        var text = BuildLineText(prepared, line);
        return new LayoutLine(text, line.Width, line.Start, line.End);
    }

    private static string BuildLineText(PreparedTextWithSegments prepared, InternalLine line)
    {
        if (line.Start == line.End && !line.AppendHyphen)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var segmentIndex = line.Start.SegmentIndex;
        var graphemeIndex = line.Start.GraphemeIndex;
        var endSegmentIndex = line.End.SegmentIndex;
        var endGraphemeIndex = line.End.GraphemeIndex;

        while (segmentIndex < prepared.Segments.Count)
        {
            if (segmentIndex > endSegmentIndex)
            {
                break;
            }

            if (segmentIndex == endSegmentIndex && graphemeIndex == endGraphemeIndex)
            {
                break;
            }

            var kind = prepared.KindsInternal[segmentIndex];
            if (kind is SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.SoftHyphen or SegmentBreakKind.HardBreak)
            {
                segmentIndex++;
                graphemeIndex = 0;
                continue;
            }

            if (segmentIndex == endSegmentIndex)
            {
                if (line.AppendHyphen)
                {
                    builder.Append('-');
                }

                builder.Append(GetSegmentSlice(prepared, segmentIndex, graphemeIndex, endGraphemeIndex));
                break;
            }

            builder.Append(GetSegmentSlice(prepared, segmentIndex, graphemeIndex, null));
            segmentIndex++;
            graphemeIndex = 0;
        }

        if (line.AppendHyphen && endGraphemeIndex == 0)
        {
            builder.Append('-');
        }

        return builder.ToString();
    }

    private static string GetSegmentSlice(PreparedTextWithSegments prepared, int segmentIndex, int startGraphemeIndex, int? endGraphemeIndexExclusive)
    {
        var segmentText = prepared.Segments[segmentIndex];
        var graphemes = GetSegmentGraphemes(prepared, segmentIndex);
        var end = endGraphemeIndexExclusive ?? graphemes.Length;

        if (startGraphemeIndex <= 0 && end >= graphemes.Length)
        {
            return segmentText;
        }

        if (startGraphemeIndex >= end)
        {
            return string.Empty;
        }

        return string.Concat(graphemes.Skip(startGraphemeIndex).Take(end - startGraphemeIndex));
    }

    private static string[] GetSegmentGraphemes(PreparedTextWithSegments prepared, int segmentIndex)
    {
        var cache = _segmentTextCaches.GetValue(prepared, static _ => new SegmentTextCache());
        if (cache.GraphemesBySegmentIndex.TryGetValue(segmentIndex, out var graphemes))
        {
            return graphemes;
        }

        graphemes = GetTextElements(prepared.Segments[segmentIndex]);
        cache.GraphemesBySegmentIndex[segmentIndex] = graphemes;
        return graphemes;
    }

    private static double GetTabAdvance(double lineWidth, double tabStopAdvance)
    {
        if (tabStopAdvance <= 0)
        {
            return 0;
        }

        var remainder = lineWidth % tabStopAdvance;
        if (Math.Abs(remainder) <= 1e-6)
        {
            return tabStopAdvance;
        }

        return tabStopAdvance - remainder;
    }
}
