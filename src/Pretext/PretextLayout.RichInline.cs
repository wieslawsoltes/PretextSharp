using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
    RichInlineFragment[] Fragments,
    double Width,
    RichInlineCursor End);

public sealed record RichInlineLineRange(
    RichInlineFragmentRange[] Fragments,
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

internal sealed class PreparedRichInlineItem
{
    public PreparedRichInlineItem(
        RichInlineBreakMode @break,
        int endGraphemeIndex,
        int endSegmentIndex,
        double extraWidth,
        double gapBefore,
        double naturalWidth,
        PreparedTextWithSegments prepared,
        int sourceItemIndex)
    {
        Break = @break;
        EndGraphemeIndex = endGraphemeIndex;
        EndSegmentIndex = endSegmentIndex;
        ExtraWidth = extraWidth;
        GapBefore = gapBefore;
        NaturalWidth = naturalWidth;
        Prepared = prepared;
        SourceItemIndex = sourceItemIndex;
    }

    public RichInlineBreakMode Break { get; }

    public int EndGraphemeIndex { get; }

    public int EndSegmentIndex { get; }

    public double ExtraWidth { get; }

    public double GapBefore { get; }

    public double NaturalWidth { get; }

    public PreparedTextWithSegments Prepared { get; }

    public int SourceItemIndex { get; }
}

public static partial class PretextLayout
{
    private readonly record struct BoundaryTrimResult(string TrimmedText, bool HasLeadingWhitespace, bool HasTrailingWhitespace);

    private static readonly LayoutCursor EmptyLayoutCursor = new(0, 0);
    private static readonly RichInlineCursor RichInlineStartCursor = new(0, 0, 0);

    public static PreparedRichInline PrepareRichInline(IReadOnlyList<RichInlineItem> items)
    {
        GuardCompat.ThrowIfNull(items, nameof(items));

        var preparedItems = new List<PreparedRichInlineItem>(items.Count);
        var itemsBySourceItemIndex = new PreparedRichInlineItem?[items.Count];
        var collapsedSpaceWidthCache = new Dictionary<string, double>(StringComparer.Ordinal);
        var pendingGapWidth = 0d;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var trimmed = TrimCollapsibleBoundaryText(item.Text);
            var trimmedText = trimmed.TrimmedText;

            if (trimmedText.Length == 0)
            {
                if ((trimmed.HasLeadingWhitespace || trimmed.HasTrailingWhitespace) && pendingGapWidth == 0)
                {
                    pendingGapWidth = GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache);
                }

                continue;
            }

            var gapBefore = pendingGapWidth > 0
                ? pendingGapWidth
                : trimmed.HasLeadingWhitespace
                    ? GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache)
                    : 0;
            var prepared = PrepareWithSegments(trimmedText, item.Font);
            var wholeLine = LayoutNextLineRange(prepared, EmptyLayoutCursor, double.PositiveInfinity);
            if (wholeLine is null)
            {
                pendingGapWidth = trimmed.HasTrailingWhitespace ? GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache) : 0;
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

            pendingGapWidth = trimmed.HasTrailingWhitespace ? GetCollapsedSpaceWidth(item.Font, collapsedSpaceWidthCache) : 0;
        }

        return new PreparedRichInline(preparedItems.ToArray(), itemsBySourceItemIndex);
    }

    public static RichInlineLineRange? LayoutNextRichInlineLineRange(
        PreparedRichInline prepared,
        double maxWidth,
        RichInlineCursor? start = null)
    {
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));

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

        return new RichInlineLineRange(fragments.ToArray(), width.Value, end);
    }

    public static RichInlineLine MaterializeRichInlineLineRange(PreparedRichInline prepared, RichInlineLineRange line)
    {
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));
        GuardCompat.ThrowIfNull(line, nameof(line));

        var flow = prepared.ItemsBySourceItemIndexInternal;
        var fragments = new RichInlineFragment[line.Fragments.Length];

        for (var index = 0; index < line.Fragments.Length; index++)
        {
            var fragment = line.Fragments[index];
            var item = flow[fragment.ItemIndex] ?? throw new InvalidOperationException("Missing rich-inline item for fragment.");
            fragments[index] = new RichInlineFragment(
                fragment.ItemIndex,
                BuildLineTextFromRange(item.Prepared, fragment.Start.SegmentIndex, fragment.Start.GraphemeIndex, fragment.End.SegmentIndex, fragment.End.GraphemeIndex),
                fragment.GapBefore,
                fragment.OccupiedWidth,
                fragment.Start,
                fragment.End);
        }

        return new RichInlineLine(fragments, line.Width, line.End);
    }

    public static int WalkRichInlineLineRanges(
        PreparedRichInline prepared,
        double maxWidth,
        Action<RichInlineLineRange> onLine)
    {
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));
        GuardCompat.ThrowIfNull(onLine, nameof(onLine));

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
        GuardCompat.ThrowIfNull(prepared, nameof(prepared));

        var cursor = RichInlineStartCursor;
        var lineCount = 0;
        var maxLineWidth = 0d;

        while (true)
        {
            var lineWidth = StepRichInlineLineStats(prepared, maxWidth, ref cursor);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLineStartCursor(LayoutCursor cursor)
    {
        return cursor.SegmentIndex == 0 && cursor.GraphemeIndex == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LayoutCursor CloneCursor(LayoutCursor cursor)
    {
        return new LayoutCursor(cursor.SegmentIndex, cursor.GraphemeIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EndsInsideFirstSegment(LayoutCursor end)
    {
        return end.SegmentIndex == 0 && end.GraphemeIndex > 0;
    }

    private static double GetCollapsedSpaceWidth(string font, Dictionary<string, double> cache)
    {
#if NET6_0_OR_GREATER
        ref var cached = ref CollectionsMarshal.GetValueRefOrAddDefault(cache, font, out var exists);
        if (exists)
        {
            return cached;
        }

        var joinedWidth = MeasureNaturalWidth(PrepareWithSegments("A A", font));
        var compactWidth = MeasureNaturalWidth(PrepareWithSegments("AA", font));
        var collapsedWidth = Math.Max(0, joinedWidth - compactWidth);
        cached = collapsedWidth;
        return cached;
#else
        if (cache.TryGetValue(font, out var cached))
        {
            return cached;
        }

        var joinedWidth = MeasureNaturalWidth(PrepareWithSegments("A A", font));
        var compactWidth = MeasureNaturalWidth(PrepareWithSegments("AA", font));
        var collapsedWidth = Math.Max(0, joinedWidth - compactWidth);
        cache[font] = collapsedWidth;
        return collapsedWidth;
#endif
    }

    private static BoundaryTrimResult TrimCollapsibleBoundaryText(string text)
    {
        var span = text.AsSpan();
        var start = 0;
        while (start < span.Length && IsCollapsibleBoundaryChar(span[start]))
        {
            start++;
        }

        var end = span.Length - 1;
        while (end >= start && IsCollapsibleBoundaryChar(span[end]))
        {
            end--;
        }

        var hasLeadingWhitespace = start > 0;
        var hasTrailingWhitespace = end + 1 < span.Length;
        var trimmedLength = end - start + 1;
        if (trimmedLength <= 0)
        {
            return new BoundaryTrimResult(string.Empty, hasLeadingWhitespace, hasTrailingWhitespace);
        }

        return new BoundaryTrimResult(
            start == 0 && trimmedLength == text.Length
                ? text
                : text.Substring(start, trimmedLength),
            hasLeadingWhitespace,
            hasTrailingWhitespace);
    }

    private static bool IsCollapsibleBoundaryChar(char ch)
    {
        return ch is ' ' or '\t' or '\n' or '\f' or '\r';
    }

    private static double? StepRichInlineLineStats(
        PreparedRichInline prepared,
        double maxWidth,
        ref RichInlineCursor cursor)
    {
        var items = prepared.ItemsInternal;
        if (items.Length == 0 || cursor.ItemIndex >= items.Length)
        {
            return null;
        }

        var safeWidth = maxWidth > 1 ? maxWidth : 1;
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

        var safeWidth = maxWidth > 1 ? maxWidth : 1;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CursorIsAfter(LayoutCursor left, LayoutCursor right)
    {
        return left.SegmentIndex > right.SegmentIndex ||
               (left.SegmentIndex == right.SegmentIndex && left.GraphemeIndex > right.GraphemeIndex);
    }
}
