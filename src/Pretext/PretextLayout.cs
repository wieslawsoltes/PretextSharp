using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Pretext;

public enum WhiteSpaceMode
{
    Normal,
    PreWrap,
}

public enum WordBreakMode
{
    Normal,
    KeepAll,
}

public enum SegmentBreakKind
{
    Text,
    Space,
    PreservedSpace,
    Tab,
    Glue,
    ZeroWidthBreak,
    SoftHyphen,
    HardBreak,
}

public readonly record struct PrepareOptions(
    WhiteSpaceMode WhiteSpace = WhiteSpaceMode.Normal,
    WordBreakMode WordBreak = WordBreakMode.Normal);

public readonly record struct LayoutCursor(int SegmentIndex, int GraphemeIndex);

public readonly record struct LayoutResult(int LineCount, double Height);

public readonly record struct LineStats(int LineCount, double MaxLineWidth);

public readonly record struct PrepareProfile(
    double AnalysisMs,
    double MeasureMs,
    double TotalMs,
    int AnalysisSegments,
    int PreparedSegments,
    int BreakableSegments);

public readonly record struct PreparedLineChunk(int StartSegmentIndex, int EndSegmentIndex, int ConsumedEndSegmentIndex);

public sealed record LayoutLine(string Text, double Width, LayoutCursor Start, LayoutCursor End);

public sealed record LayoutLineRange(double Width, LayoutCursor Start, LayoutCursor End);

public sealed record LayoutLinesResult(int LineCount, double Height, IReadOnlyList<LayoutLine> Lines);

internal readonly record struct MeasuredSegment(
    string Text,
    SegmentBreakKind Kind,
    bool IsBreakableRun,
    double Width,
    double[]? BreakableWidths,
    double[]? BreakablePrefixWidths);

internal sealed class SegmentTextCache
{
    public Dictionary<int, string[]> GraphemesBySegmentIndex { get; } = new();
}

public class PreparedText
{
    internal PreparedText(
        string font,
        WhiteSpaceMode whiteSpace,
        double[] widths,
        double[] lineEndFitAdvances,
        double[] lineEndPaintAdvances,
        SegmentBreakKind[] kinds,
        double[]?[] breakableWidths,
        double[]?[] breakablePrefixWidths,
        double hyphenWidth,
        double tabStopAdvance,
        PreparedLineChunk[] chunks,
        bool simpleLineWalkFastPath)
    {
        Font = font;
        WhiteSpace = whiteSpace;
        WidthsInternal = widths;
        LineEndFitAdvancesInternal = lineEndFitAdvances;
        LineEndPaintAdvancesInternal = lineEndPaintAdvances;
        KindsInternal = kinds;
        BreakableWidthsInternal = breakableWidths;
        BreakablePrefixWidthsInternal = breakablePrefixWidths;
        DiscretionaryHyphenWidth = hyphenWidth;
        TabStopAdvance = tabStopAdvance;
        ChunksInternal = chunks;
        SimpleLineWalkFastPathInternal = simpleLineWalkFastPath;
    }

    public string Font { get; }

    public WhiteSpaceMode WhiteSpace { get; }

    internal double[] WidthsInternal { get; }

    internal double[] LineEndFitAdvancesInternal { get; }

    internal double[] LineEndPaintAdvancesInternal { get; }

    internal SegmentBreakKind[] KindsInternal { get; }

    internal double[]?[] BreakableWidthsInternal { get; }

    internal double[]?[] BreakablePrefixWidthsInternal { get; }

    internal PreparedLineChunk[] ChunksInternal { get; }

    internal bool SimpleLineWalkFastPathInternal { get; }

    public double DiscretionaryHyphenWidth { get; }

    public double TabStopAdvance { get; }
}

public sealed class PreparedTextWithSegments : PreparedText
{
    internal PreparedTextWithSegments(
        string font,
        WhiteSpaceMode whiteSpace,
        double hyphenWidth,
        double tabStopAdvance,
        string[] segmentTexts,
        double[] widths,
        double[] lineEndFitAdvances,
        double[] lineEndPaintAdvances,
        SegmentBreakKind[] kinds,
        double[]?[] breakableWidths,
        double[]?[] breakablePrefixWidths,
        PreparedLineChunk[] chunks,
        bool simpleLineWalkFastPath,
        sbyte[]? segmentLevels)
        : base(font, whiteSpace, widths, lineEndFitAdvances, lineEndPaintAdvances, kinds, breakableWidths, breakablePrefixWidths, hyphenWidth, tabStopAdvance, chunks, simpleLineWalkFastPath)
    {
        Segments = segmentTexts;
        Widths = widths;
        LineEndFitAdvances = lineEndFitAdvances;
        LineEndPaintAdvances = lineEndPaintAdvances;
        Kinds = kinds;
        BreakableWidths = breakableWidths;
        BreakableFitAdvances = breakableWidths;
        BreakablePrefixWidths = breakablePrefixWidths;
        Chunks = chunks;
        SimpleLineWalkFastPath = simpleLineWalkFastPath;
        SegmentLevels = segmentLevels;
    }

    public IReadOnlyList<string> Segments { get; }

    public IReadOnlyList<double> Widths { get; }

    public IReadOnlyList<double> LineEndFitAdvances { get; }

    public IReadOnlyList<double> LineEndPaintAdvances { get; }

    public IReadOnlyList<SegmentBreakKind> Kinds { get; }

    public IReadOnlyList<double[]?> BreakableWidths { get; }

    public IReadOnlyList<double[]?> BreakableFitAdvances { get; }

    public IReadOnlyList<double[]?> BreakablePrefixWidths { get; }

    public IReadOnlyList<PreparedLineChunk> Chunks { get; }

    public bool SimpleLineWalkFastPath { get; }

    public IReadOnlyList<sbyte>? SegmentLevels { get; }
}

public static partial class PretextLayout
{
    private static readonly Dictionary<string, FontState> FontStates = new(StringComparer.Ordinal);
    private static readonly object FontStateGate = new();
    private static ConditionalWeakTable<PreparedTextWithSegments, SegmentTextCache> _segmentTextCaches = new();
    private static string? _locale;
    private static Func<string, string, double>? _measureTextOverride;
    private static EngineProfile? _cachedEngineProfile;

    public static PreparedText Prepare(string text, string font, PrepareOptions? options = null)
    {
        return PrepareCore(text, font, options ?? new PrepareOptions(), includeSegments: false);
    }

    public static PreparedTextWithSegments PrepareWithSegments(string text, string font, PrepareOptions? options = null)
    {
        return (PreparedTextWithSegments)PrepareCore(text, font, options ?? new PrepareOptions(), includeSegments: true);
    }

    public static PrepareProfile ProfilePrepare(string text, string font, PrepareOptions? options = null)
    {
        var effectiveOptions = options ?? new PrepareOptions();
        var stopwatch = Stopwatch.StartNew();
        var tokens = AnalyzeTokens(text ?? string.Empty, effectiveOptions.WhiteSpace);
        var analysisMs = stopwatch.Elapsed.TotalMilliseconds;

        var fontState = GetFontState(font);
        var engineProfile = GetEngineProfile();
        var breakableSegments = 0;
        var preparedSegments = 0;
        foreach (var token in tokens)
        {
            foreach (var segment in ExpandPreparedSegments(token, fontState, engineProfile, effectiveOptions.WordBreak))
            {
                preparedSegments++;
                if (segment.IsBreakableRun && segment.BreakableWidths is { Length: > 1 })
                {
                    breakableSegments++;
                }
            }
        }

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        return new PrepareProfile(
            analysisMs,
            totalMs - analysisMs,
            totalMs,
            tokens.Count,
            preparedSegments,
            breakableSegments);
    }

    public static LayoutResult Layout(PreparedText prepared, double maxWidth, double lineHeight)
    {
        var lineCount = CountPreparedLines(prepared, maxWidth);
        return new LayoutResult(lineCount, lineCount * lineHeight);
    }

    public static LayoutLinesResult LayoutWithLines(PreparedTextWithSegments prepared, double maxWidth, double lineHeight)
    {
        var lines = new List<LayoutLine>();
        var lineCount = WalkPreparedLines(prepared, maxWidth, line => lines.Add(MaterializeLine(prepared, line)));
        return new LayoutLinesResult(lineCount, lineCount * lineHeight, new ReadOnlyCollection<LayoutLine>(lines));
    }

    public static LayoutLine? LayoutNextLine(PreparedTextWithSegments prepared, LayoutCursor start, double maxWidth)
    {
        return TryStepLine(prepared, start, maxWidth, out var line)
            ? MaterializeLine(prepared, line)
            : null;
    }

    public static LayoutLineRange? LayoutNextLineRange(PreparedTextWithSegments prepared, LayoutCursor start, double maxWidth)
    {
        return TryStepLine(prepared, start, maxWidth, out var line)
            ? new LayoutLineRange(line.Width, line.Start, line.End)
            : null;
    }

    public static LayoutLine MaterializeLineRange(PreparedTextWithSegments prepared, LayoutLineRange line)
    {
        return MaterializeLine(prepared, new InternalLine(line.Start, line.End, line.End, line.Width, AppendHyphen: false));
    }

    public static int WalkLineRanges(PreparedTextWithSegments prepared, double maxWidth, Action<LayoutLineRange> onLine)
    {
        ArgumentNullException.ThrowIfNull(onLine);

        return WalkPreparedLines(prepared, maxWidth, line => onLine(new LayoutLineRange(line.Width, line.Start, line.End)));
    }

    public static LineStats MeasureLineStats(PreparedTextWithSegments prepared, double maxWidth)
    {
        var maxLineWidth = 0d;
        var lineCount = WalkPreparedLines(prepared, maxWidth, line =>
        {
            if (line.Width > maxLineWidth)
            {
                maxLineWidth = line.Width;
            }
        });

        return new LineStats(lineCount, maxLineWidth);
    }

    public static double MeasureNaturalWidth(PreparedTextWithSegments prepared)
    {
        var maxLineWidth = 0d;
        WalkPreparedLines(prepared, double.PositiveInfinity, line =>
        {
            if (line.Width > maxLineWidth)
            {
                maxLineWidth = line.Width;
            }
        });

        return maxLineWidth;
    }

    public static void ClearCache()
    {
        lock (FontStateGate)
        {
            foreach (var state in FontStates.Values)
            {
                state.Dispose();
            }

            FontStates.Clear();
        }

        _cachedEngineProfile = null;
        _segmentTextCaches = new ConditionalWeakTable<PreparedTextWithSegments, SegmentTextCache>();
    }

    public static void SetLocale(string? locale = null)
    {
        _locale = locale;
        ClearCache();
    }

    internal static void SetMeasurementOverrideForTests(Func<string, string, double>? measureText)
    {
        _measureTextOverride = measureText;
        ClearCache();
    }

    internal static int CountPreparedLinesForTests(PreparedText prepared, double maxWidth)
    {
        return CountPreparedLines(prepared, maxWidth);
    }

    internal static int WalkPreparedLinesForTests(PreparedText prepared, double maxWidth)
    {
        return WalkPreparedLines(prepared, maxWidth);
    }
}
