namespace Pretext.Layout;

public static class PreparedTextMetrics
{
    public static double MeasureMaxLineWidth(PreparedTextWithSegments prepared)
    {
        var max = 0d;
        PretextLayout.WalkLineRanges(prepared, 100_000, line =>
        {
            if (line.Width > max)
            {
                max = line.Width;
            }
        });
        return max;
    }

    public static WrapMetrics CollectWrapMetrics(PreparedTextWithSegments prepared, double maxWidth, double lineHeight)
    {
        var max = 0d;
        var count = PretextLayout.WalkLineRanges(prepared, maxWidth, line =>
        {
            if (line.Width > max)
            {
                max = line.Width;
            }
        });

        return new WrapMetrics(count, count * lineHeight, max);
    }

    public static WrapMetrics FindTightWrapMetrics(PreparedTextWithSegments prepared, double maxWidth, double lineHeight)
    {
        var initial = CollectWrapMetrics(prepared, maxWidth, lineHeight);
        var lo = 1;
        var hi = Math.Max(1, (int)Math.Ceiling(maxWidth));

        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            var result = PretextLayout.Layout(prepared, mid, lineHeight);
            if (result.LineCount <= initial.LineCount)
            {
                hi = mid;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return CollectWrapMetrics(prepared, lo, lineHeight);
    }

    public static bool IsEnd(PreparedTextWithSegments prepared, LayoutCursor cursor)
    {
        return cursor.SegmentIndex >= prepared.Segments.Count;
    }
}
