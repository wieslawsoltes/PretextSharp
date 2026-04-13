using System.Diagnostics;
using System.Text;
using Pretext;

namespace PretextSamples.Samples;

internal sealed record JustificationResources(
    PreparedTextWithSegments[] BasePreparedParagraphs,
    PreparedTextWithSegments[] HyphenatedPreparedParagraphs,
    double NaturalSpaceWidth,
    double HyphenWidth);

internal readonly record struct BreakCandidate(int SegmentIndex, bool IsSoftHyphen);

internal readonly record struct LineInfo(double WordWidth, int SpaceCount, bool EndsWithHyphen);

internal readonly record struct JustifiedSegment(string Text, double Width, bool IsSpace);

internal sealed record JustifiedLine(IReadOnlyList<JustifiedSegment> Segments, double MaxWidth, bool IsLast, double LineWidth);

internal sealed record QualityMetrics(double AvgDeviation, double MaxDeviation, int RiverCount, int LineCount, double LayoutMs);

internal static partial class JustificationComparisonModel
{
    public const string FontFamilyCss = "Georgia, \"Times New Roman\", serif";
    public const string FontFamilyDisplay = "Georgia";
    public const double BodyFontSize = 15;
    public const double BodyLineHeight = 24;
    public const double Pad = 12;
    public const double ParagraphGap = BodyLineHeight * 0.6;
    public const double ColumnGap = 24;

    public static JustificationResources CreateResources()
    {
        var font = $"{BodyFontSize}px {FontFamilyCss}";
        var paragraphCount = JustificationComparisonData.Paragraphs.Length;
        var basePreparedParagraphs = new PreparedTextWithSegments[paragraphCount];
        var hyphenatedPreparedParagraphs = new PreparedTextWithSegments[paragraphCount];

        for (var index = 0; index < paragraphCount; index++)
        {
            var paragraph = JustificationComparisonData.Paragraphs[index];
            basePreparedParagraphs[index] = PretextLayout.PrepareWithSegments(paragraph, font);
            hyphenatedPreparedParagraphs[index] = PretextLayout.PrepareWithSegments(HyphenateParagraph(paragraph), font);
        }

        return new JustificationResources(
            basePreparedParagraphs,
            hyphenatedPreparedParagraphs,
            MeasureTextWidth("a a", font, " "),
            MeasureSingleRunWidth("-", font));
    }

    public static (IReadOnlyList<IReadOnlyList<JustifiedLine>> Paragraphs, QualityMetrics Metrics) BuildGreedyFrame(
        PreparedTextWithSegments[] paragraphs,
        double innerWidth,
        double naturalSpaceWidth,
        double hyphenWidth)
    {
        var stopwatch = Stopwatch.StartNew();
        var lines = new IReadOnlyList<JustifiedLine>[paragraphs.Length];
        for (var index = 0; index < paragraphs.Length; index++)
        {
            lines[index] = GreedyJustifiedLayout(paragraphs[index], innerWidth, hyphenWidth);
        }

        stopwatch.Stop();
        return (lines, ComputeMetrics(lines, naturalSpaceWidth) with { LayoutMs = stopwatch.Elapsed.TotalMilliseconds });
    }

    public static (IReadOnlyList<IReadOnlyList<JustifiedLine>> Paragraphs, QualityMetrics Metrics) BuildOptimalFrame(
        PreparedTextWithSegments[] paragraphs,
        double innerWidth,
        double naturalSpaceWidth,
        double hyphenWidth)
    {
        var stopwatch = Stopwatch.StartNew();
        var lines = new IReadOnlyList<JustifiedLine>[paragraphs.Length];
        for (var index = 0; index < paragraphs.Length; index++)
        {
            lines[index] = OptimalLayout(paragraphs[index], innerWidth, naturalSpaceWidth, hyphenWidth);
        }

        stopwatch.Stop();
        return (lines, ComputeMetrics(lines, naturalSpaceWidth) with { LayoutMs = stopwatch.Elapsed.TotalMilliseconds });
    }

    public static List<JustifiedLine> GreedyJustifiedLayout(PreparedTextWithSegments prepared, double maxWidth, double hyphenWidth)
    {
        var lines = new List<JustifiedLine>();
        var cursor = new LayoutCursor(0, 0);

        while (true)
        {
            var line = PretextLayout.LayoutNextLine(prepared, cursor, maxWidth);
            if (line is null)
            {
                break;
            }

            var isLast = line.End.SegmentIndex >= prepared.Segments.Count;
            var segments = new List<JustifiedSegment>();
            var endsWithHyphen = false;
            for (var index = line.Start.SegmentIndex; index < line.End.SegmentIndex; index++)
            {
                var text = prepared.Segments[index];
                if (text == "\u00AD")
                {
                    if (index == line.End.SegmentIndex - 1)
                    {
                        endsWithHyphen = true;
                    }

                    continue;
                }

                segments.Add(new JustifiedSegment(text, prepared.Widths[index], string.IsNullOrWhiteSpace(text)));
            }

            if (!endsWithHyphen && line.End.SegmentIndex < prepared.Segments.Count && prepared.Segments[line.End.SegmentIndex] == "\u00AD")
            {
                endsWithHyphen = true;
            }

            if (endsWithHyphen && !isLast)
            {
                segments.Add(new JustifiedSegment("-", hyphenWidth, false));
            }

            while (segments.Count > 0 && segments[^1].IsSpace)
            {
                segments.RemoveAt(segments.Count - 1);
            }

            var naturalWidth = 0d;
            for (var index = 0; index < segments.Count; index++)
            {
                naturalWidth += segments[index].Width;
            }

            lines.Add(new JustifiedLine(segments, maxWidth, isLast, naturalWidth));
            cursor = line.End;
        }

        return lines;
    }

    public static List<JustifiedLine> OptimalLayout(
        PreparedTextWithSegments prepared,
        double maxWidth,
        double naturalSpaceWidth,
        double hyphenWidth)
    {
        var segments = prepared.Segments;
        var widths = prepared.Widths;
        if (segments.Count == 0)
        {
            return [];
        }

        var breakCandidates = new List<BreakCandidate> { new(0, false) };
        for (var index = 0; index < segments.Count; index++)
        {
            var text = segments[index];
            if (text == "\u00AD")
            {
                if (index + 1 < segments.Count)
                {
                    breakCandidates.Add(new BreakCandidate(index + 1, true));
                }
            }
            else if (string.IsNullOrWhiteSpace(text) && index + 1 < segments.Count)
            {
                breakCandidates.Add(new BreakCandidate(index + 1, false));
            }
        }

        breakCandidates.Add(new BreakCandidate(segments.Count, false));

        LineInfo GetLineInfo(int fromIndex, int toIndex)
        {
            var from = breakCandidates[fromIndex].SegmentIndex;
            var to = breakCandidates[toIndex].SegmentIndex;
            var endsWithHyphen = breakCandidates[toIndex].IsSoftHyphen;
            var wordWidth = 0d;
            var spaceCount = 0;

            for (var index = from; index < to; index++)
            {
                var text = segments[index];
                if (text == "\u00AD")
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    spaceCount++;
                }
                else
                {
                    wordWidth += widths[index];
                }
            }

            if (to > from && string.IsNullOrWhiteSpace(segments[to - 1]))
            {
                spaceCount--;
            }

            if (endsWithHyphen)
            {
                wordWidth += hyphenWidth;
            }

            return new LineInfo(wordWidth, spaceCount, endsWithHyphen);
        }

        double LineBadness(LineInfo info, bool isLastLine)
        {
            if (isLastLine)
            {
                return info.WordWidth > maxWidth ? 1e8 : 0;
            }

            if (info.SpaceCount <= 0)
            {
                var slack = maxWidth - info.WordWidth;
                return slack < 0 ? 1e8 : slack * slack * 10;
            }

            var justifiedSpace = (maxWidth - info.WordWidth) / info.SpaceCount;
            if (justifiedSpace < 0 || justifiedSpace < naturalSpaceWidth * 0.4)
            {
                return 1e8;
            }

            var ratio = (justifiedSpace - naturalSpaceWidth) / naturalSpaceWidth;
            var badness = Math.Pow(Math.Abs(ratio), 3) * 1000;
            var riverExcess = justifiedSpace / naturalSpaceWidth - 1.5;
            var riverPenalty = riverExcess > 0 ? 5000 + riverExcess * riverExcess * 10000 : 0;
            var tightThreshold = naturalSpaceWidth * 0.65;
            var tightPenalty = justifiedSpace < tightThreshold
                ? 3000 + Math.Pow(tightThreshold - justifiedSpace, 2) * 10000
                : 0;
            var hyphenPenalty = info.EndsWithHyphen ? 50 : 0;
            return badness + riverPenalty + tightPenalty + hyphenPenalty;
        }

        var candidateCount = breakCandidates.Count;
        var dp = new double[candidateCount];
        Array.Fill(dp, double.PositiveInfinity);
        var previous = new int[candidateCount];
        Array.Fill(previous, -1);
        dp[0] = 0;

        for (var endCandidate = 1; endCandidate < candidateCount; endCandidate++)
        {
            var isLast = endCandidate == candidateCount - 1;
            for (var startCandidate = endCandidate - 1; startCandidate >= 0; startCandidate--)
            {
                if (double.IsPositiveInfinity(dp[startCandidate]))
                {
                    continue;
                }

                var info = GetLineInfo(startCandidate, endCandidate);
                var totalWidth = info.WordWidth + info.SpaceCount * naturalSpaceWidth;
                if (totalWidth > maxWidth * 2)
                {
                    break;
                }

                var total = dp[startCandidate] + LineBadness(info, isLast);
                if (total < dp[endCandidate])
                {
                    dp[endCandidate] = total;
                    previous[endCandidate] = startCandidate;
                }
            }
        }

        var breakIndices = new List<int>();
        for (var current = candidateCount - 1; current > 0;)
        {
            if (previous[current] == -1)
            {
                current--;
                continue;
            }

            breakIndices.Add(current);
            current = previous[current];
        }

        breakIndices.Reverse();

        var lines = new List<JustifiedLine>();
        var fromCandidate = 0;
        foreach (var toCandidate in breakIndices)
        {
            var from = breakCandidates[fromCandidate].SegmentIndex;
            var to = breakCandidates[toCandidate].SegmentIndex;
            var endsWithHyphen = breakCandidates[toCandidate].IsSoftHyphen;
            var isLast = toCandidate == candidateCount - 1;
            var lineSegments = new List<JustifiedSegment>();

            for (var index = from; index < to; index++)
            {
                var text = segments[index];
                if (text == "\u00AD")
                {
                    continue;
                }

                lineSegments.Add(new JustifiedSegment(text, widths[index], string.IsNullOrWhiteSpace(text)));
            }

            if (endsWithHyphen)
            {
                lineSegments.Add(new JustifiedSegment("-", hyphenWidth, false));
            }

            while (lineSegments.Count > 0 && lineSegments[^1].IsSpace)
            {
                lineSegments.RemoveAt(lineSegments.Count - 1);
            }

            var naturalWidth = 0d;
            for (var index = 0; index < lineSegments.Count; index++)
            {
                naturalWidth += lineSegments[index].Width;
            }

            lines.Add(new JustifiedLine(lineSegments, maxWidth, isLast, naturalWidth));
            fromCandidate = toCandidate;
        }

        return lines;
    }

    public static QualityMetrics ComputeMetrics(IEnumerable<IReadOnlyList<JustifiedLine>> paragraphs, double naturalSpaceWidth)
    {
        var totalDeviation = 0d;
        var maxDeviation = 0d;
        var measuredLineCount = 0;
        var riverCount = 0;
        var lineCount = 0;

        foreach (var paragraph in paragraphs)
        {
            lineCount += paragraph.Count;
            foreach (var line in paragraph)
            {
                if (line.IsLast)
                {
                    continue;
                }

                var wordWidth = 0d;
                var spaceCount = 0;
                foreach (var segment in line.Segments)
                {
                    if (segment.IsSpace)
                    {
                        spaceCount++;
                    }
                    else
                    {
                        wordWidth += segment.Width;
                    }
                }

                if (spaceCount <= 0)
                {
                    continue;
                }

                var justifiedSpace = (line.MaxWidth - wordWidth) / spaceCount;
                var deviation = Math.Abs(justifiedSpace - naturalSpaceWidth) / naturalSpaceWidth;
                totalDeviation += deviation;
                maxDeviation = Math.Max(maxDeviation, deviation);
                measuredLineCount++;

                if (justifiedSpace > naturalSpaceWidth * 1.5)
                {
                    riverCount++;
                }
            }
        }

        return new QualityMetrics(
            measuredLineCount > 0 ? totalDeviation / measuredLineCount : 0,
            maxDeviation,
            riverCount,
            lineCount,
            0);
    }

    private static string HyphenateParagraph(string paragraph)
    {
        if (string.IsNullOrEmpty(paragraph))
        {
            return string.Empty;
        }

        var span = paragraph.AsSpan();
        var builder = new StringBuilder(paragraph.Length + Math.Max(8, paragraph.Length / 16));
        var index = 0;

        while (index < span.Length)
        {
            var tokenStart = index;
            while (index < span.Length && !char.IsWhiteSpace(span[index]))
            {
                index++;
            }

            if (index > tokenStart)
            {
                AppendHyphenatedToken(builder, span[tokenStart..index]);
            }

            var whitespaceStart = index;
            while (index < span.Length && char.IsWhiteSpace(span[index]))
            {
                index++;
            }

            if (index > whitespaceStart)
            {
                builder.Append(span[whitespaceStart..index]);
            }
        }

        return builder.ToString();
    }

    private static double MeasureSingleRunWidth(string text, string font)
    {
        var prepared = PretextLayout.PrepareWithSegments(text, font);
        var width = 0d;
        PretextLayout.WalkLineRanges(prepared, 100_000, line => width = line.Width);
        return width;
    }

    private static double MeasureTextWidth(string text, string font, string target)
    {
        var prepared = PretextLayout.PrepareWithSegments(text, font);
        for (var index = 0; index < prepared.Segments.Count; index++)
        {
            if (prepared.Segments[index] == target)
            {
                return prepared.Widths[index];
            }
        }

        return MeasureSingleRunWidth(target, font);
    }

    private static void AppendHyphenatedToken(StringBuilder builder, ReadOnlySpan<char> token)
    {
        var tokenText = token.ToString();
        var parts = JustificationComparisonData.HyphenateWord(tokenText);
        if (parts.Length <= 1)
        {
            builder.Append(token);
            return;
        }

        builder.Append(parts[0]);
        for (var index = 1; index < parts.Length; index++)
        {
            builder.Append('\u00AD');
            builder.Append(parts[index]);
        }
    }
}
