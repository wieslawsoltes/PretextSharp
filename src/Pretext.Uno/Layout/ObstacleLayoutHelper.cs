namespace Pretext.Uno.Layout;

public static class ObstacleLayoutHelper
{
    public static List<Interval> CarveTextLineSlots(Interval baseSlot, IEnumerable<Interval> blocked, double minSlotWidth = 50)
    {
        var slots = new List<Interval> { baseSlot };
        foreach (var interval in blocked)
        {
            var next = new List<Interval>();
            foreach (var slot in slots)
            {
                if (interval.Right <= slot.Left || interval.Left >= slot.Right)
                {
                    next.Add(slot);
                    continue;
                }

                if (interval.Left > slot.Left)
                {
                    next.Add(new Interval(slot.Left, interval.Left));
                }

                if (interval.Right < slot.Right)
                {
                    next.Add(new Interval(interval.Right, slot.Right));
                }
            }

            slots = next;
            if (slots.Count == 0)
            {
                break;
            }
        }

        return slots.Where(slot => slot.Width >= minSlotWidth).ToList();
    }

    public static Interval? CircleIntervalForBand(
        double cx,
        double cy,
        double radius,
        double bandTop,
        double bandBottom,
        double horizontalPadding = 0,
        double verticalPadding = 0)
    {
        var top = bandTop - verticalPadding;
        var bottom = bandBottom + verticalPadding;
        if (top >= cy + radius || bottom <= cy - radius)
        {
            return null;
        }

        var minDy = cy >= top && cy <= bottom ? 0 : cy < top ? top - cy : cy - bottom;
        if (minDy >= radius)
        {
            return null;
        }

        var maxDx = Math.Sqrt(radius * radius - minDy * minDy);
        return new Interval(cx - maxDx - horizontalPadding, cx + maxDx + horizontalPadding);
    }

    public static Interval? EllipseIntervalForBand(
        double cx,
        double cy,
        double radiusX,
        double radiusY,
        double bandTop,
        double bandBottom,
        double horizontalPadding = 0,
        double verticalPadding = 0)
    {
        var top = bandTop - verticalPadding;
        var bottom = bandBottom + verticalPadding;
        if (top >= cy + radiusY || bottom <= cy - radiusY)
        {
            return null;
        }

        var minDy = cy >= top && cy <= bottom ? 0 : cy < top ? top - cy : cy - bottom;
        if (minDy >= radiusY)
        {
            return null;
        }

        var normalizedDy = minDy / radiusY;
        var maxDx = radiusX * Math.Sqrt(1 - normalizedDy * normalizedDy);
        return new Interval(cx - maxDx - horizontalPadding, cx + maxDx + horizontalPadding);
    }

    public static List<Interval> GetRectIntervalsForBand(
        IReadOnlyList<RectObstacle> rects,
        double bandTop,
        double bandBottom,
        double horizontalPadding = 0,
        double verticalPadding = 0)
    {
        var intervals = new List<Interval>(rects.Count);
        foreach (var rect in rects)
        {
            if (bandBottom <= rect.Y - verticalPadding || bandTop >= rect.Bottom + verticalPadding)
            {
                continue;
            }

            intervals.Add(new Interval(rect.X - horizontalPadding, rect.Right + horizontalPadding));
        }

        return intervals;
    }

    public static Interval PickSlot(IReadOnlyList<Interval> slots, bool preferRightOnTie)
    {
        var best = slots[0];
        for (var index = 1; index < slots.Count; index++)
        {
            var candidate = slots[index];
            if (candidate.Width > best.Width)
            {
                best = candidate;
                continue;
            }

            if (candidate.Width < best.Width)
            {
                continue;
            }

            if (preferRightOnTie)
            {
                if (candidate.Left > best.Left)
                {
                    best = candidate;
                }
            }
            else if (candidate.Left < best.Left)
            {
                best = candidate;
            }
        }

        return best;
    }
}
