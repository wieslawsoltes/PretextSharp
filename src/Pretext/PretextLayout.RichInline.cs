using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Pretext;

public enum RichInlineBreakMode
{
    Normal,
    Never,
}

public readonly record struct RichInlineItem(
    string Text,
    string Font,
    RichInlineBreakMode Break = RichInlineBreakMode.Normal,
    double ExtraWidth = 0);

public readonly record struct RichInlineCursor(int ItemIndex, int SegmentIndex, int GraphemeIndex);

public readonly record struct RichInlineStats(int LineCount, double MaxLineWidth);

public sealed record RichInlineFragment(
    int ItemIndex,
    string Text,
    double GapBefore,
    double OccupiedWidth,
    LayoutCursor Start,
    LayoutCursor End);

public sealed record RichInlineFragmentRange(
    int ItemIndex,
    double GapBefore,
    double OccupiedWidth,
    LayoutCursor Start,
    LayoutCursor End);

public sealed record RichInlineLine(
    IReadOnlyList<RichInlineFragment> Fragments,
    double Width,
    RichInlineCursor End);

public sealed record RichInlineLineRange(
    IReadOnlyList<RichInlineFragmentRange> Fragments,
    double Width,
    RichInlineCursor End);

public sealed class PreparedRichInline
{
    internal PreparedRichInline(
        PreparedRichInlineItem[] items,
        PreparedRichInlineItem?[] itemsBySourceItemIndex)
    {
        ItemsInternal = items;
        ItemsBySourceItemIndexInternal = itemsBySourceItemIndex;
    }

    internal PreparedRichInlineItem[] ItemsInternal { get; }

    internal PreparedRichInlineItem?[] ItemsBySourceItemIndexInternal { get; }
}

internal sealed record PreparedRichInlineItem(
    RichInlineBreakMode Break,
    int EndGraphemeIndex,
    int EndSegmentIndex,
    double ExtraWidth,
    double GapBefore,
    double NaturalWidth,
    PreparedTextWithSegments Prepared,
    int SourceItemIndex);

public static partial class PretextLayout
{
    private static readonly Regex LeadingCollapsibleBoundaryRegex = new("^[ \\t\\n\\f\\r]+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex TrailingCollapsibleBoundaryRegex = new("[ \\t\\n\\f\\r]+$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex CollapsibleBoundaryRegex = new("[ \\t\\n\\f\\r]+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly LayoutCursor EmptyLayoutCursor = new(0, 0);
    private static readonly RichInlineCursor RichInlineStartCursor = new(0, 0, 0);

    public static PreparedRichInline PrepareRichInline(IReadOnlyList<RichInlineItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var preparedItems = new List<PreparedRichInlineItem>(items.Count);
        var itemsBySourceItemIndex = new PreparedRichInlineItem?[items.Count];
        var collapsedSpaceWidthCache = new Dictionary<string, double>(StringComparer.Ordinal);
        var pendingGapWidth = 0d;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var hasLeadingWhitespace = LeadingCollapsibleBoundaryRegex.IsMatch(item.Text);
            var hasTrailingWhitespace = TrailingCollapsibleBoundaryRegex.IsMatch(item.Text);
            var trimmedText = TrailingCollapsibleBoundaryRegex.Replace(
                LeadingCollapsibleBoundaryRegex.Replace(item.Text, string.Empty),
                string.Empty);

            if (trimmedText.Length == 0)
            {
                if (CollapsibleBoundaryRegex.IsMatch(item.Text) && pendingGapWidth == 0)
                {
                    pendingGapWidth = GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache);
                }

                continue;
            }

            var gapBefore = pendingGapWidth > 0
                ? pendingGapWidth
                : hasLeadingWhitespace
                    ? GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache)
                    : 0;
            var prepared = PrepareWithSegments(trimmedText, item.Font);
            var wholeLine = LayoutNextLineRange(prepared, EmptyLayoutCursor, double.PositiveInfinity);
            if (wholeLine is null)
            {
                pendingGapWidth = hasTrailingWhitespace ? GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache) : 0;
                continue;
            }

            var preparedItem = new PreparedRichInlineItem(
                item.Break,
                wholeLine.End.GraphemeIndex,
                wholeLine.End.SegmentIndex,
                item.ExtraWidth,
                gapBefore,
                wholeLine.Width,
                prepared,
                index);
            preparedItems.Add(preparedItem);
            itemsBySourceItemIndex[index] = preparedItem;

            pendingGapWidth = hasTrailingWhitespace ? GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache) : 0;
        }

        return new PreparedRichInline(preparedItems.ToArray(), itemsBySourceItemIndex);
    }

    public static RichInlineLineRange? LayoutNextRichInlineLineRange(
        PreparedRichInline prepared,
        double maxWidth,
        RichInlineCursor? start = null)
    {
        ArgumentNullException.ThrowIfNull(prepared);

        var end = start ?? RichInlineStartCursor;
        var fragments = new List<RichInlineFragmentRange>();
        var width = StepRichInlineLine(prepared, maxWidth, ref end, static (fragmentRanges, item, gapBefore, occupiedWidth, fragmentStart, fragmentEnd) =>
        {
            fragmentRanges.Add(new RichInlineFragmentRange(
                item.SourceItemIndex,
                gapBefore,
                occupiedWidth,
                fragmentStart,
                fragmentEnd));
        }, fragments);

        if (width is null)
        {
            return null;
        }

        return new RichInlineLineRange(new ReadOnlyCollection<RichInlineFragmentRange>(fragments), width.Value, end);
    }

    public static RichInlineLine MaterializeRichInlineLineRange(PreparedRichInline prepared, RichInlineLineRange line)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        ArgumentNullException.ThrowIfNull(line);

        var flow = prepared.ItemsBySourceItemIndexInternal;
        var fragments = new List<RichInlineFragment>(line.Fragments.Count);

        for (var index = 0; index < line.Fragments.Count; index++)
        {
            var fragment = line.Fragments[index];
            var item = flow[fragment.ItemIndex] ?? throw new InvalidOperationException("Missing rich-inline item for fragment.");
            fragments.Add(new RichInlineFragment(
                fragment.ItemIndex,
                BuildLineTextFromRange(item.Prepared, fragment.Start.SegmentIndex, fragment.Start.GraphemeIndex, fragment.End.SegmentIndex, fragment.End.GraphemeIndex),
                fragment.GapBefore,
                fragment.OccupiedWidth,
                fragment.Start,
                fragment.End));
        }

        return new RichInlineLine(new ReadOnlyCollection<RichInlineFragment>(fragments), line.Width, line.End);
    }

    public static int WalkRichInlineLineRanges(
        PreparedRichInline prepared,
        double maxWidth,
        Action<RichInlineLineRange> onLine)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        ArgumentNullException.ThrowIfNull(onLine);

        var lineCount = 0;
        var cursor = RichInlineStartCursor;

        while (true)
        {
            var line = LayoutNextRichInlineLineRange(prepared, maxWidth, cursor);
            if (line is null)
            {
                return lineCount;
            }

            onLine(line);
            lineCount++;
            cursor = line.End;
        }
    }

    public static RichInlineStats MeasureRichInlineStats(PreparedRichInline prepared, double maxWidth)
    {
        ArgumentNullException.ThrowIfNull(prepared);

        var cursor = RichInlineStartCursor;
        var lineCount = 0;
        var maxLineWidth = 0d;

        while (true)
        {
            var lineWidth = StepRichInlineLine(prepared, maxWidth, ref cursor, onFragment: null, state: 0);
            if (lineWidth is null)
            {
                return new RichInlineStats(lineCount, maxLineWidth);
            }

            lineCount++;
            if (lineWidth.Value > maxLineWidth)
            {
                maxLineWidth = lineWidth.Value;
            }
        }
    }

    private static bool IsLineStartCursor(LayoutCursor cursor)
    {
        return cursor.SegmentIndex == 0 && cursor.GraphemeIndex == 0;
    }

    private static LayoutCursor CloneCursor(LayoutCursor cursor)
    {
        return new LayoutCursor(cursor.SegmentIndex, cursor.GraphemeIndex);
    }

    private static bool EndsInsideFirstSegment(LayoutCursor end)
    {
        return end.SegmentIndex == 0 && end.GraphemeIndex > 0;
    }

    private static double GetCollapsedSpaceWidth(string font, Dictionary<string, double> cache)
    {
        if (cache.TryGetValue(font, out var cached))
        {
            return cached;
        }

        var joinedWidth = MeasureNaturalWidth(PrepareWithSegments("A A", font));
        var compactWidth = MeasureNaturalWidth(PrepareWithSegments("AA", font));
        var collapsedWidth = Math.Max(0, joinedWidth - compactWidth);
        cache[font] = collapsedWidth;
        return collapsedWidth;
    }

    private static double? StepRichInlineLine<TState>(
        PreparedRichInline prepared,
        double maxWidth,
        ref RichInlineCursor cursor,
        Action<TState, PreparedRichInlineItem, double, double, LayoutCursor, LayoutCursor>? onFragment,
        TState state)
    {
        var items = prepared.ItemsInternal;
        if (items.Length == 0 || cursor.ItemIndex >= items.Length)
        {
            return null;
        }

        var safeWidth = Math.Max(1, maxWidth);
        var lineWidth = 0d;
        var remainingWidth = safeWidth;
        var itemIndex = cursor.ItemIndex;
        var textCursor = new LayoutCursor(cursor.SegmentIndex, cursor.GraphemeIndex);

        while (itemIndex < items.Length)
        {
            var item = items[itemIndex];
            if (!IsLineStartCursor(textCursor) &&
                textCursor.SegmentIndex == item.EndSegmentIndex &&
                textCursor.GraphemeIndex == item.EndGraphemeIndex)
            {
                itemIndex++;
                textCursor = EmptyLayoutCursor;
                continue;
            }

            var gapBefore = lineWidth == 0 ? 0 : item.GapBefore;
            var atItemStart = IsLineStartCursor(textCursor);

            if (item.Break == RichInlineBreakMode.Never)
            {
                if (!atItemStart)
                {
                    itemIndex++;
                    textCursor = EmptyLayoutCursor;
                    continue;
                }

                var occupiedWidth = item.NaturalWidth + item.ExtraWidth;
                var totalWidth = gapBefore + occupiedWidth;
                if (lineWidth > 0 && totalWidth > remainingWidth)
                {
                    break;
                }

                onFragment?.Invoke(
                    state,
                    item,
                    gapBefore,
                    occupiedWidth,
                    EmptyLayoutCursor,
                    new LayoutCursor(item.EndSegmentIndex, item.EndGraphemeIndex));
                lineWidth += totalWidth;
                remainingWidth = Math.Max(0, safeWidth - lineWidth);
                itemIndex++;
                textCursor = EmptyLayoutCursor;
                continue;
            }

            var reservedWidth = gapBefore + item.ExtraWidth;
            if (lineWidth > 0 && reservedWidth >= remainingWidth)
            {
                break;
            }

            if (atItemStart)
            {
                var totalWidth = reservedWidth + item.NaturalWidth;
                if (totalWidth <= remainingWidth)
                {
                    onFragment?.Invoke(
                        state,
                        item,
                        gapBefore,
                        item.NaturalWidth + item.ExtraWidth,
                        EmptyLayoutCursor,
                        new LayoutCursor(item.EndSegmentIndex, item.EndGraphemeIndex));
                    lineWidth += totalWidth;
                    remainingWidth = Math.Max(0, safeWidth - lineWidth);
                    itemIndex++;
                    textCursor = EmptyLayoutCursor;
                    continue;
                }
            }

            var availableWidth = Math.Max(1, remainingWidth - reservedWidth);
            var line = LayoutNextLineRange(item.Prepared, textCursor, availableWidth);
            if (line is null)
            {
                itemIndex++;
                textCursor = EmptyLayoutCursor;
                continue;
            }

            if (textCursor == line.End)
            {
                itemIndex++;
                textCursor = EmptyLayoutCursor;
                continue;
            }

            if (lineWidth > 0 &&
                atItemStart &&
                gapBefore > 0 &&
                EndsInsideFirstSegment(line.End))
            {
                var freshLine = LayoutNextLineRange(item.Prepared, EmptyLayoutCursor, Math.Max(1, safeWidth - item.ExtraWidth));
                if (freshLine is not null && CursorIsAfter(freshLine.End, line.End))
                {
                    break;
                }
            }

            onFragment?.Invoke(
                state,
                item,
                gapBefore,
                line.Width + item.ExtraWidth,
                CloneCursor(textCursor),
                line.End);
            lineWidth += gapBefore + line.Width + item.ExtraWidth;
            remainingWidth = Math.Max(0, safeWidth - lineWidth);

            if (line.End.SegmentIndex == item.EndSegmentIndex && line.End.GraphemeIndex == item.EndGraphemeIndex)
            {
                itemIndex++;
                textCursor = EmptyLayoutCursor;
                continue;
            }

            textCursor = line.End;
            break;
        }

        if (lineWidth == 0)
        {
            return null;
        }

        cursor = new RichInlineCursor(itemIndex, textCursor.SegmentIndex, textCursor.GraphemeIndex);
        return lineWidth;
    }

    private static bool CursorIsAfter(LayoutCursor left, LayoutCursor right)
    {
        return left.SegmentIndex > right.SegmentIndex ||
               (left.SegmentIndex == right.SegmentIndex && left.GraphemeIndex > right.GraphemeIndex);
    }
}
