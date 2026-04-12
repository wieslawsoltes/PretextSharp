namespace Pretext.Uno.Layout;

public static class ColumnFlowLayout
{
    public static List<PositionedLine> LayoutIntoColumns(
        PreparedTextWithSegments prepared,
        IReadOnlyList<RectObstacle> columns,
        IReadOnlyList<RectObstacle> rectangles,
        IReadOnlyList<CircleObstacle> circles,
        double lineHeight,
        double minSlotWidth = 56)
    {
        var lines = new List<PositionedLine>();
        var cursor = new LayoutCursor(0, 0);

        foreach (var column in columns)
        {
            var y = column.Y;
            while (!PreparedTextMetrics.IsEnd(prepared, cursor) && y + lineHeight <= column.Bottom)
            {
                var slot = GetBestSlot(column, y, y + lineHeight, rectangles, circles, minSlotWidth);
                if (slot is null)
                {
                    y += lineHeight;
                    continue;
                }

                var nextLine = PretextLayout.LayoutNextLine(prepared, cursor, slot.Value.Width);
                if (nextLine is null)
                {
                    return lines;
                }

                lines.Add(new PositionedLine(nextLine.Text, slot.Value.Left, y, nextLine.Width));
                cursor = nextLine.End;
                y += lineHeight;
            }
        }

        return lines;
    }

    private static Interval? GetBestSlot(
        RectObstacle column,
        double bandTop,
        double bandBottom,
        IReadOnlyList<RectObstacle> rectangles,
        IReadOnlyList<CircleObstacle> circles,
        double minSlotWidth)
    {
        var slots = new List<Interval> { new(column.X, column.Right) };
        foreach (var rect in rectangles)
        {
            if (bandBottom <= rect.Y || bandTop >= rect.Bottom)
            {
                continue;
            }

            slots = Carve(slots, new Interval(rect.X, rect.Right));
        }

        foreach (var circle in circles)
        {
            var centerY = (bandTop + bandBottom) * 0.5;
            var dy = Math.Abs(centerY - circle.Y);
            if (dy >= circle.Radius)
            {
                continue;
            }

            var dx = Math.Sqrt(circle.Radius * circle.Radius - dy * dy);
            slots = Carve(slots, new Interval(circle.X - dx, circle.X + dx));
        }

        return slots
            .Where(slot => slot.Width >= minSlotWidth)
            .OrderByDescending(slot => slot.Width)
            .FirstOrDefault();
    }

    private static List<Interval> Carve(List<Interval> slots, Interval blocked)
    {
        var next = new List<Interval>();
        foreach (var slot in slots)
        {
            if (blocked.Right <= slot.Left || blocked.Left >= slot.Right)
            {
                next.Add(slot);
                continue;
            }

            if (blocked.Left > slot.Left)
            {
                next.Add(new Interval(slot.Left, blocked.Left));
            }

            if (blocked.Right < slot.Right)
            {
                next.Add(new Interval(blocked.Right, slot.Right));
            }
        }

        return next;
    }
}
