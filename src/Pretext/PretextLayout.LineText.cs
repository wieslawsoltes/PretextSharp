using System.Runtime.InteropServices;
using System.Text;

namespace Pretext;

public static partial class PretextLayout
{
    private static LayoutLine MaterializeLine(PreparedTextWithSegments prepared, InternalLine line)
    {
        var text = BuildLineText(prepared, line);
        return new LayoutLine(text, line.Width, line.Start, line.End);
    }

    private static string BuildLineText(PreparedTextWithSegments prepared, InternalLine line)
    {
        return BuildLineTextFromRange(
            prepared,
            line.Start.SegmentIndex,
            line.Start.GraphemeIndex,
            line.End.SegmentIndex,
            line.End.GraphemeIndex,
            line.AppendHyphen);
    }

    private static string BuildLineTextFromRange(
        PreparedTextWithSegments prepared,
        int startSegmentIndex,
        int startGraphemeIndex,
        int endSegmentIndex,
        int endGraphemeIndex,
        bool appendHyphen = false)
    {
        appendHyphen |= LineHasDiscretionaryHyphen(prepared, startSegmentIndex, startGraphemeIndex, endSegmentIndex);

        if (startSegmentIndex == endSegmentIndex &&
            startGraphemeIndex == endGraphemeIndex &&
            !appendHyphen)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(EstimateLineTextCapacity(
            prepared,
            startSegmentIndex,
            endSegmentIndex,
            appendHyphen));

        for (var segmentIndex = startSegmentIndex; segmentIndex < prepared.Segments.Count; segmentIndex++)
        {
            if (segmentIndex > endSegmentIndex)
            {
                break;
            }

            if (segmentIndex == endSegmentIndex && startGraphemeIndex == endGraphemeIndex)
            {
                break;
            }

            var kind = prepared.KindsInternal[segmentIndex];
            if (kind is SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.SoftHyphen or SegmentBreakKind.HardBreak)
            {
                startGraphemeIndex = 0;
                continue;
            }

            if (segmentIndex == endSegmentIndex)
            {
                if (appendHyphen)
                {
                    builder.Append('-');
                }

                AppendSegmentSlice(builder, prepared, segmentIndex, startGraphemeIndex, endGraphemeIndex);
                break;
            }

            AppendSegmentSlice(builder, prepared, segmentIndex, startGraphemeIndex, null);
            startGraphemeIndex = 0;
        }

        if (appendHyphen && endGraphemeIndex == 0)
        {
            builder.Append('-');
        }

        return builder.ToString();
    }

    private static int EstimateLineTextCapacity(
        PreparedTextWithSegments prepared,
        int startSegmentIndex,
        int endSegmentIndex,
        bool appendHyphen)
    {
        var capacity = appendHyphen ? 1 : 0;
        var lastIndex = Math.Min(endSegmentIndex, prepared.Segments.Count - 1);
        for (var segmentIndex = startSegmentIndex; segmentIndex <= lastIndex; segmentIndex++)
        {
            var kind = prepared.KindsInternal[segmentIndex];
            if (kind is SegmentBreakKind.ZeroWidthBreak or SegmentBreakKind.SoftHyphen or SegmentBreakKind.HardBreak)
            {
                continue;
            }

            capacity += prepared.Segments[segmentIndex].Length;
        }

        return Math.Max(capacity, 0);
    }

    private static bool LineHasDiscretionaryHyphen(
        PreparedTextWithSegments prepared,
        int startSegmentIndex,
        int startGraphemeIndex,
        int endSegmentIndex)
    {
        return endSegmentIndex > 0 &&
               prepared.KindsInternal[endSegmentIndex - 1] == SegmentBreakKind.SoftHyphen &&
               !(startSegmentIndex == endSegmentIndex && startGraphemeIndex > 0);
    }

    private static void AppendSegmentSlice(
        StringBuilder builder,
        PreparedTextWithSegments prepared,
        int segmentIndex,
        int startGraphemeIndex,
        int? endGraphemeIndexExclusive)
    {
        var segmentText = prepared.Segments[segmentIndex];
        var graphemes = GetSegmentGraphemes(prepared, segmentIndex);
        var end = endGraphemeIndexExclusive ?? graphemes.Length;

        if (startGraphemeIndex <= 0 && end >= graphemes.Length)
        {
            builder.Append(segmentText);
            return;
        }

        if (startGraphemeIndex >= end)
        {
            return;
        }

        for (var i = startGraphemeIndex; i < end; i++)
        {
            builder.Append(graphemes[i]);
        }
    }

    private static string[] GetSegmentGraphemes(PreparedTextWithSegments prepared, int segmentIndex)
    {
        var cache = _segmentTextCaches.GetValue(prepared, static _ => new SegmentTextCache());
        ref var graphemes = ref CollectionsMarshal.GetValueRefOrAddDefault(
            cache.GraphemesBySegmentIndex,
            segmentIndex,
            out var exists);
        if (exists && graphemes is not null)
        {
            return graphemes;
        }

        graphemes = GetTextElements(prepared.Segments[segmentIndex]);
        return graphemes!;
    }
}
