namespace Pretext;

public static partial class PretextLayout
{
    private static readonly Dictionary<string, ShaperState> ShaperStates = new(StringComparer.Ordinal);

    private readonly record struct ShapingCacheKey(string Text, PretextTextDirection Direction);

    private readonly record struct ShapedLineCacheKey(LayoutCursor Start, LayoutCursor End);

    public sealed record ShapedLayoutLine(PretextShapedRun ShapedRun, double Width, LayoutCursor Start, LayoutCursor End);

    public sealed record ShapedRichInlineFragment(
        int ItemIndex,
        PretextShapedRun ShapedRun,
        double GapBefore,
        double OccupiedWidth,
        LayoutCursor Start,
        LayoutCursor End);

    public sealed record ShapedRichInlineLine(ShapedRichInlineFragment[] Fragments, double Width, RichInlineCursor End);

    public sealed class PreparedShapedText
    {
        private readonly object _lineCacheGate = new();
        private readonly Dictionary<ShapedLineCacheKey, PretextShapedRun> _lineRuns = new();

        internal PreparedShapedText(
            PreparedTextWithSegments prepared,
            PretextTextDirection direction,
            string text,
            PretextShapedRun shapedRun,
            int[][] graphemeBoundaryTextIndexes)
        {
            Prepared = prepared;
            Direction = direction;
            Text = text;
            ShapedRun = shapedRun;
            GraphemeBoundaryTextIndexes = graphemeBoundaryTextIndexes;
        }

        public PreparedTextWithSegments Prepared { get; }

        public PretextTextDirection Direction { get; }

        public PretextShapeOptions Options => CreateShapeOptions(Direction);

        public string Text { get; }

        public PretextShapedRun ShapedRun { get; }

        internal int[][] GraphemeBoundaryTextIndexes { get; }

        internal PretextShapedRun GetOrCreateLineRun(LayoutCursor start, LayoutCursor end)
        {
            var cacheKey = new ShapedLineCacheKey(start, end);
            lock (_lineCacheGate)
            {
                if (_lineRuns.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }

                var shapedRun = TrySlicePreparedShapedRun(this, start, end, out var sliced)
                    ? sliced!
                    : ShapeMaterializedLine(this, start, end);

                _lineRuns[cacheKey] = shapedRun;
                return shapedRun;
            }
        }
    }

    private sealed class PreparedShapedTextCache
    {
        private readonly object _gate = new();
        private readonly Dictionary<PretextTextDirection, PreparedShapedText> _cache = new();

        public PreparedShapedText GetOrCreate(PreparedTextWithSegments prepared, PretextShapeOptions? options)
        {
            var direction = options?.Direction ?? PretextTextDirection.Auto;
            lock (_gate)
            {
                if (_cache.TryGetValue(direction, out var cached))
                {
                    return cached;
                }

                var shaped = CreatePreparedShapedText(prepared, direction);
                _cache[direction] = shaped;
                return shaped;
            }
        }
    }

    public static PretextShapedRun ShapeText(string text, string font, PretextShapeOptions? options = null)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        var state = GetShaperState(font);
        return state.ShapeText(text ?? string.Empty, options);
    }

    public static bool TryShapeText(string text, string font, out PretextShapedRun? shapedRun, PretextShapeOptions? options = null)
    {
        try
        {
            shapedRun = ShapeText(text, font, options);
            return true;
        }
        catch (InvalidOperationException)
        {
            shapedRun = null;
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            shapedRun = null;
            return false;
        }
        catch (DllNotFoundException)
        {
            shapedRun = null;
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            shapedRun = null;
            return false;
        }
    }

    public static PreparedShapedText ShapePreparedText(PreparedTextWithSegments prepared, PretextShapeOptions? options = null)
    {
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));

        var cache = _preparedShapedTextCaches.GetValue(prepared, static _ => new PreparedShapedTextCache());
        return cache.GetOrCreate(prepared, options);
    }

    public static ShapedLayoutLine? LayoutNextShapedLine(
        PreparedShapedText prepared,
        LayoutCursor start,
        double maxWidth)
    {
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));

        var line = LayoutNextLineRange(prepared.Prepared, start, maxWidth);
        return line is null ? null : MaterializeShapedLineRange(prepared, line);
    }

    public static ShapedLayoutLine MaterializeShapedLineRange(PreparedShapedText prepared, LayoutLineRange line)
    {
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));
        GuardCompat.ThrowIfNull(line, nameof(line));

        var shapedRun = prepared.GetOrCreateLineRun(line.Start, line.End);

        return new ShapedLayoutLine(shapedRun, line.Width, line.Start, line.End);
    }

    public static ShapedLayoutLine MaterializeShapedLineRange(
        PreparedTextWithSegments prepared,
        LayoutLineRange line,
        PretextShapeOptions? options = null)
    {
        return MaterializeShapedLineRange(ShapePreparedText(prepared, options), line);
    }

    public static ShapedRichInlineLine MaterializeShapedRichInlineLineRange(
        PreparedRichInline prepared,
        RichInlineLineRange line,
        PretextShapeOptions? options = null)
    {
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));
        GuardCompat.ThrowIfNull(line, nameof(line));

        var flow = prepared.ItemsBySourceItemIndexInternal;
        var fragments = new ShapedRichInlineFragment[line.Fragments.Length];

        for (var index = 0; index < line.Fragments.Length; index++)
        {
            var fragment = line.Fragments[index];
            var item = flow[fragment.ItemIndex] ?? throw new InvalidOperationException("Missing rich-inline item for fragment.");
            var shapedPrepared = ShapePreparedText(item.Prepared, options);
            var shapedRun = shapedPrepared.GetOrCreateLineRun(fragment.Start, fragment.End);

            fragments[index] = new ShapedRichInlineFragment(
                fragment.ItemIndex,
                shapedRun,
                fragment.GapBefore,
                fragment.OccupiedWidth,
                fragment.Start,
                fragment.End);
        }

        return new ShapedRichInlineLine(fragments, line.Width, line.End);
    }

    private static ShaperState GetShaperState(string font)
    {
        lock (FontStateGate)
        {
            if (ShaperStates.TryGetValue(font, out var cached))
            {
                return cached;
            }

            var state = ShaperState.Create(font);
            ShaperStates[font] = state;
            return state;
        }
    }

    private static PreparedShapedText CreatePreparedShapedText(PreparedTextWithSegments prepared, PretextTextDirection direction)
    {
        var text = BuildPreparedShapingText(prepared, out var graphemeBoundaryTextIndexes);
        var options = CreateShapeOptions(direction);
        var shaped = ShapeText(text, prepared.Font, options);
        return new PreparedShapedText(prepared, direction, text, shaped, graphemeBoundaryTextIndexes);
    }

    private static string BuildPreparedShapingText(
        PreparedTextWithSegments prepared,
        out int[][] graphemeBoundaryTextIndexes)
    {
        var builder = new System.Text.StringBuilder(EstimatePreparedShapingTextCapacity(prepared));
        graphemeBoundaryTextIndexes = new int[prepared.Segments.Count][];

        for (var segmentIndex = 0; segmentIndex < prepared.Segments.Count; segmentIndex++)
        {
            var kind = prepared.KindsInternal[segmentIndex];
            var graphemes = GetSegmentGraphemes(prepared, segmentIndex);
            var boundaries = new int[graphemes.Length + 1];
            graphemeBoundaryTextIndexes[segmentIndex] = boundaries;

            for (var index = 0; index < boundaries.Length; index++)
            {
                boundaries[index] = builder.Length;
            }

            if (kind is SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.SoftHyphen or SegmentBreakKind.HardBreak)
            {
                continue;
            }

            for (var graphemeIndex = 0; graphemeIndex < graphemes.Length; graphemeIndex++)
            {
                boundaries[graphemeIndex] = builder.Length;
                builder.Append(graphemes[graphemeIndex]);
            }

            boundaries[^1] = builder.Length;
        }

        return builder.ToString();
    }

    private static bool TrySlicePreparedShapedRun(
        PreparedShapedText prepared,
        LayoutCursor start,
        LayoutCursor end,
        out PretextShapedRun? shapedRun)
    {
        if (!CanSlicePreparedShapedRun(prepared.Prepared, start, end))
        {
            shapedRun = null;
            return false;
        }

        var startTextIndex = GetPreparedShapingTextIndex(prepared, start);
        var endTextIndex = GetPreparedShapingTextIndex(prepared, end);
        shapedRun = SliceShapedRun(prepared.ShapedRun, startTextIndex, endTextIndex);
        return true;
    }

    private static bool CanSlicePreparedShapedRun(PreparedTextWithSegments prepared, LayoutCursor start, LayoutCursor end)
    {
        return !LineHasDiscretionaryHyphen(prepared, start.SegmentIndex, start.GraphemeIndex, end.SegmentIndex) &&
               IsShapingSafeBoundary(prepared, start) &&
               IsShapingSafeBoundary(prepared, end);
    }

    private static bool IsShapingSafeBoundary(PreparedTextWithSegments prepared, LayoutCursor cursor)
    {
        if (cursor.SegmentIndex < 0 || cursor.SegmentIndex >= prepared.Segments.Count)
        {
            return true;
        }

        if (cursor.GraphemeIndex <= 0)
        {
            return !IsInvisibleBreakBoundary(prepared, cursor.SegmentIndex);
        }

        var graphemeCount = GetSegmentGraphemes(prepared, cursor.SegmentIndex).Length;
        return cursor.GraphemeIndex >= graphemeCount &&
               !IsInvisibleBreakBoundary(prepared, cursor.SegmentIndex + 1);
    }

    private static bool IsInvisibleBreakBoundary(PreparedTextWithSegments prepared, int segmentIndex)
    {
        return IsInvisibleBreakSegment(prepared, segmentIndex) ||
               IsInvisibleBreakSegment(prepared, segmentIndex - 1);
    }

    private static bool IsInvisibleBreakSegment(PreparedTextWithSegments prepared, int segmentIndex)
    {
        if ((uint)segmentIndex >= (uint)prepared.KindsInternal.Length)
        {
            return false;
        }

        var kind = prepared.KindsInternal[segmentIndex];
        return kind is SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.SoftHyphen or SegmentBreakKind.HardBreak;
    }

    private static int EstimatePreparedShapingTextCapacity(PreparedTextWithSegments prepared)
    {
        var capacity = 0;
        for (var segmentIndex = 0; segmentIndex < prepared.Segments.Count; segmentIndex++)
        {
            if (!IsInvisibleBreakSegment(prepared, segmentIndex))
            {
                capacity += prepared.Segments[segmentIndex].Length;
            }
        }

        return capacity;
    }

    private static int GetPreparedShapingTextIndex(PreparedShapedText prepared, LayoutCursor cursor)
    {
        if (cursor.SegmentIndex < 0)
        {
            return 0;
        }

        if (cursor.SegmentIndex >= prepared.GraphemeBoundaryTextIndexes.Length)
        {
            return prepared.Text.Length;
        }

        var boundaries = prepared.GraphemeBoundaryTextIndexes[cursor.SegmentIndex];
        if (boundaries.Length == 0)
        {
            return prepared.Text.Length;
        }

        var graphemeIndex = cursor.GraphemeIndex;
        if (graphemeIndex < 0)
        {
            graphemeIndex = 0;
        }
        else if (graphemeIndex >= boundaries.Length)
        {
            graphemeIndex = boundaries.Length - 1;
        }

        return boundaries[graphemeIndex];
    }

    private static PretextShapedRun ShapeMaterializedLine(
        PreparedShapedText prepared,
        LayoutCursor start,
        LayoutCursor end)
    {
        var lineText = BuildLineTextFromRange(
            prepared.Prepared,
            start.SegmentIndex,
            start.GraphemeIndex,
            end.SegmentIndex,
            end.GraphemeIndex);
        return ShapeText(lineText, prepared.Prepared.Font, CreateShapeOptions(prepared.Direction));
    }

    private static PretextShapedRun SliceShapedRun(PretextShapedRun source, int startTextIndex, int endTextIndex)
    {
        if (startTextIndex >= endTextIndex || source.Glyphs.Count == 0)
        {
            return new PretextShapedRun(
                source.Kind,
                Array.Empty<PretextShapedGlyph>(),
                Array.Empty<PretextShapedFontRun>(),
                0,
                0);
        }

        var glyphs = new List<PretextShapedGlyph>();
        var fontRunMap = new Dictionary<int, int>();
        var fontRuns = new List<PretextShapedFontRun>();
        var fontRunGlyphCounts = new List<int>();
        double originX = 0;
        double originY = 0;
        var advanceX = 0d;
        var advanceY = 0d;
        var hasOrigin = false;

        foreach (var glyph in source.Glyphs)
        {
            if (glyph.Cluster < startTextIndex || glyph.Cluster >= endTextIndex)
            {
                continue;
            }

            if (!hasOrigin)
            {
                originX = glyph.X;
                originY = glyph.Y;
                hasOrigin = true;
            }

            if (!fontRunMap.TryGetValue(glyph.FontRunIndex, out var mappedFontRunIndex))
            {
                mappedFontRunIndex = fontRuns.Count;
                fontRunMap[glyph.FontRunIndex] = mappedFontRunIndex;
                var sourceFontRun = FindFontRun(source, glyph.FontRunIndex);
                fontRuns.Add(new PretextShapedFontRun(mappedFontRunIndex, sourceFontRun.Font, glyphs.Count, 0));
                fontRunGlyphCounts.Add(0);
            }

            glyphs.Add(new PretextShapedGlyph(
                glyph.GlyphId,
                glyph.Cluster - startTextIndex,
                glyph.X - originX,
                glyph.Y - originY,
                glyph.XAdvance,
                glyph.YAdvance,
                glyph.XOffset,
                glyph.YOffset,
                mappedFontRunIndex));
            fontRunGlyphCounts[mappedFontRunIndex]++;
            advanceX += glyph.XAdvance;
            advanceY += glyph.YAdvance;
        }

        if (glyphs.Count == 0)
        {
            return new PretextShapedRun(
                source.Kind,
                Array.Empty<PretextShapedGlyph>(),
                Array.Empty<PretextShapedFontRun>(),
                0,
                0);
        }

        var fixedFontRuns = new PretextShapedFontRun[fontRuns.Count];
        for (var fontRunIndex = 0; fontRunIndex < fontRuns.Count; fontRunIndex++)
        {
            var fontRun = fontRuns[fontRunIndex];
            fixedFontRuns[fontRunIndex] = new PretextShapedFontRun(fontRun.Index, fontRun.Font, fontRun.FirstGlyphIndex, fontRunGlyphCounts[fontRunIndex]);
        }

        return new PretextShapedRun(source.Kind, glyphs, fixedFontRuns, advanceX, advanceY);
    }

    private static PretextShapedFontRun FindFontRun(PretextShapedRun source, int fontRunIndex)
    {
        for (var index = 0; index < source.FontRuns.Count; index++)
        {
            if (source.FontRuns[index].Index == fontRunIndex)
            {
                return source.FontRuns[index];
            }
        }

        return source.FontRuns.Count == 0
            ? new PretextShapedFontRun(0, string.Empty, 0, 0)
            : source.FontRuns[0];
    }

    private sealed class ShaperState : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<ShapingCacheKey, PretextShapedRun> _shapeCache;

        private ShaperState(string font, IPretextTextShaper textShaper)
        {
            Font = font;
            TextShaper = textShaper;
            _shapeCache = new Dictionary<ShapingCacheKey, PretextShapedRun>();
        }

        public string Font { get; }

        public IPretextTextShaper TextShaper { get; }

        public static ShaperState Create(string font)
        {
            return new ShaperState(font, PretextLayout.GetTextShaperFactory().CreateShaper(font));
        }

        public PretextShapedRun ShapeText(string text, PretextShapeOptions? options)
        {
            var direction = options?.Direction ?? PretextTextDirection.Auto;
            var cacheKey = new ShapingCacheKey(text, direction);

            lock (_gate)
            {
                if (_shapeCache.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }

                var shaped = TextShaper.ShapeText(text, CreateShapeOptions(direction));
                _shapeCache[cacheKey] = shaped;
                return shaped;
            }
        }

        public void Dispose()
        {
            TextShaper.Dispose();
        }

    }

    private static PretextShapeOptions CreateShapeOptions(PretextTextDirection direction)
    {
        return new PretextShapeOptions { Direction = direction };
    }
}
