using Pretext;
using Xunit;

namespace Pretext.Tests;

public sealed partial class PretextLayoutParityTests
{
    [Fact(DisplayName = "line count grows monotonically as width shrinks")]
    public void Layout_LineCountGrowsMonotonicallyAsWidthShrinks()
    {
        var prepared = PretextLayout.Prepare("The quick brown fox jumps over the lazy dog", Font);
        var previous = 0;

        foreach (var width in new[] { 320d, 200d, 140d, 90d })
        {
            var lineCount = PretextLayout.Layout(prepared, width, LineHeight).LineCount;
            Assert.True(lineCount >= previous);
            previous = lineCount;
        }
    }

    [Fact(DisplayName = "trailing whitespace hangs past the line edge")]
    public void Layout_TrailingWhitespaceHangsPastLineEdge()
    {
        var prepared = PretextLayout.PrepareWithSegments("Hello ", Font);
        var widthOfHello = prepared.Widths[0];

        Assert.Equal(1, PretextLayout.Layout(prepared, widthOfHello, LineHeight).LineCount);

        var withLines = PretextLayout.LayoutWithLines(prepared, widthOfHello, LineHeight);
        Assert.Equal(1, withLines.LineCount);
        Assert.Equal(
            new[]
            {
                new LayoutLine("Hello", widthOfHello, new LayoutCursor(0, 0), new LayoutCursor(1, 0)),
            },
            withLines.Lines);
    }

    [Fact(DisplayName = "breaks long words at grapheme boundaries and keeps both layout APIs aligned")]
    public void Layout_BreaksLongWordsAtGraphemeBoundariesAndKeepsApisAligned()
    {
        var prepared = PretextLayout.PrepareWithSegments("Superlongword", Font);
        var graphemeWidths = RequireBreakableWidths(prepared.BreakableWidths[0]);
        var maxWidth = graphemeWidths[0] + graphemeWidths[1] + graphemeWidths[2] + 0.1;

        var plain = PretextLayout.Layout(prepared, maxWidth, LineHeight);
        var rich = PretextLayout.LayoutWithLines(prepared, maxWidth, LineHeight);

        Assert.True(plain.LineCount > 1);
        Assert.Equal(plain.LineCount, rich.LineCount);
        Assert.Equal(plain.Height, rich.Height);
        Assert.Equal("Superlongword", string.Concat(rich.Lines.Select(static line => line.Text)));
        Assert.Equal(new LayoutCursor(0, 0), rich.Lines[0].Start);
        Assert.Equal(new LayoutCursor(1, 0), rich.Lines[^1].End);
    }

    [Fact(DisplayName = "mixed-direction text is a stable smoke test")]
    public void Layout_MixedDirectionTextIsStableSmokeTest()
    {
        var prepared = PretextLayout.PrepareWithSegments("According to محمد الأحمد, the results improved.", Font);
        var result = PretextLayout.LayoutWithLines(prepared, 120, LineHeight);

        Assert.True(result.LineCount >= 1);
        Assert.Equal(result.LineCount * LineHeight, result.Height);
        Assert.Equal("According to محمد الأحمد, the results improved.", string.Concat(result.Lines.Select(static line => line.Text)));
    }

    [Fact(DisplayName = "layoutNextLine reproduces layoutWithLines exactly")]
    public void Layout_LayoutNextLineReproducesLayoutWithLinesExactly()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic said \"hello\" to 世界 and waved.", Font);
        var width = prepared.Widths[0] + prepared.Widths[1] + prepared.Widths[2] + RequireBreakableWidths(prepared.BreakableWidths[4])[0] + prepared.DiscretionaryHyphenWidth + 0.1;
        var expected = PretextLayout.LayoutWithLines(prepared, width, LineHeight);

        var actual = new List<LayoutLine>();
        var cursor = new LayoutCursor(0, 0);
        while (true)
        {
            var line = PretextLayout.LayoutNextLine(prepared, cursor, width);
            if (line is null)
            {
                break;
            }

            actual.Add(line);
            cursor = line.End;
        }

        Assert.Equal(expected.Lines, actual);
    }

    [Fact(DisplayName = "pre-wrap mode keeps hanging spaces visible at line end")]
    public void Layout_PreWrapKeepsHangingSpacesVisibleAtLineEnd()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo   bar", Font, PreWrap);
        var width = MeasureWidth("foo", Font) + 0.1;
        var lines = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        Assert.Equal(2, lines.LineCount);
        Assert.Equal(new[] { "foo   ", "bar" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(2, PretextLayout.Layout(prepared, width, LineHeight).LineCount);
    }

    [Fact(DisplayName = "pre-wrap mode treats hard breaks as forced line boundaries")]
    public void Layout_PreWrapTreatsHardBreaksAsForcedLineBoundaries()
    {
        var prepared = PretextLayout.PrepareWithSegments("a\nb", Font, PreWrap);
        var lines = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        Assert.Equal(new[] { "a", "b" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(2, PretextLayout.Layout(prepared, 200, LineHeight).LineCount);
    }

    [Fact(DisplayName = "pre-wrap mode treats tabs as hanging whitespace aligned to tab stops")]
    public void Layout_PreWrapTreatsTabsAsHangingWhitespaceAlignedToTabStops()
    {
        var prepared = PretextLayout.PrepareWithSegments("a\tb", Font, PreWrap);
        var spaceWidth = MeasureWidth(" ", Font);
        var prefixWidth = MeasureWidth("a", Font);
        var tabAdvance = NextTabAdvance(prefixWidth, spaceWidth, 8);
        var textWidth = prefixWidth + tabAdvance + MeasureWidth("b", Font);
        var width = textWidth - 0.1;

        var lines = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        Assert.Equal(new[] { "a\t", "b" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(2, PretextLayout.Layout(prepared, width, LineHeight).LineCount);
    }

    [Fact(DisplayName = "pre-wrap mode treats consecutive tabs as distinct tab stops")]
    public void Layout_PreWrapTreatsConsecutiveTabsAsDistinctTabStops()
    {
        var prepared = PretextLayout.PrepareWithSegments("a\t\tb", Font, PreWrap);
        var spaceWidth = MeasureWidth(" ", Font);
        var prefixWidth = MeasureWidth("a", Font);
        var firstTabAdvance = NextTabAdvance(prefixWidth, spaceWidth, 8);
        var afterFirstTab = prefixWidth + firstTabAdvance;
        var secondTabAdvance = NextTabAdvance(afterFirstTab, spaceWidth, 8);
        var width = prefixWidth + firstTabAdvance + secondTabAdvance - 0.1;

        var lines = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        Assert.Equal(new[] { "a\t\t", "b" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(2, PretextLayout.Layout(prepared, width, LineHeight).LineCount);
    }

    [Fact(DisplayName = "pre-wrap mode keeps whitespace-only middle lines visible")]
    public void Layout_PreWrapKeepsWhitespaceOnlyMiddleLinesVisible()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo\n  \nbar", Font, PreWrap);
        var lines = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        Assert.Equal(new[] { "foo", "  ", "bar" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(new LayoutResult(3, LineHeight * 3), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "pre-wrap mode keeps trailing spaces before a hard break on the current line")]
    public void Layout_PreWrapKeepsTrailingSpacesBeforeHardBreakOnCurrentLine()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo  \nbar", Font, PreWrap);
        var lines = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        Assert.Equal(new[] { "foo  ", "bar" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(new LayoutResult(2, LineHeight * 2), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "pre-wrap mode keeps trailing tabs before a hard break on the current line")]
    public void Layout_PreWrapKeepsTrailingTabsBeforeHardBreakOnCurrentLine()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo\t\nbar", Font, PreWrap);
        var lines = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        Assert.Equal(new[] { "foo\t", "bar" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(new LayoutResult(2, LineHeight * 2), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "pre-wrap mode restarts tab stops after a hard break")]
    public void Layout_PreWrapRestartsTabStopsAfterHardBreak()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo\n\tbar", Font, PreWrap);
        var lines = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        var spaceWidth = MeasureWidth(" ", Font);
        var expectedSecondLineWidth = NextTabAdvance(0, spaceWidth, 8) + MeasureWidth("bar", Font);

        Assert.Equal(new[] { "foo", "\tbar" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.True(Math.Abs(lines.Lines[1].Width - expectedSecondLineWidth) <= 1e-5);
    }

    [Fact(DisplayName = "layoutNextLine stays aligned with layoutWithLines in pre-wrap mode")]
    public void Layout_LayoutNextLineStaysAlignedWithLayoutWithLinesInPreWrapMode()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo\n  bar baz\nquux", Font, PreWrap);
        var width = MeasureWidth("  bar", Font) + 0.1;
        var expected = PretextLayout.LayoutWithLines(prepared, width, LineHeight);

        var actual = new List<LayoutLine>();
        var cursor = new LayoutCursor(0, 0);
        while (true)
        {
            var line = PretextLayout.LayoutNextLine(prepared, cursor, width);
            if (line is null)
            {
                break;
            }

            actual.Add(line);
            cursor = line.End;
        }

        Assert.Equal(expected.Lines, actual);
    }

    [Fact(DisplayName = "pre-wrap mode keeps empty lines from consecutive hard breaks")]
    public void Layout_PreWrapKeepsEmptyLinesFromConsecutiveHardBreaks()
    {
        var prepared = PretextLayout.PrepareWithSegments("\n\n", Font, PreWrap);
        var lines = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        Assert.Equal(new[] { string.Empty, string.Empty }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(new LayoutResult(2, LineHeight * 2), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "pre-wrap mode does not invent an extra trailing empty line")]
    public void Layout_PreWrapDoesNotInventExtraTrailingEmptyLine()
    {
        var prepared = PretextLayout.PrepareWithSegments("a\n", Font, PreWrap);
        var lines = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        Assert.Equal(new[] { "a" }, lines.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(new LayoutResult(1, LineHeight), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "overlong breakable segments wrap onto a fresh line when the current line already has content")]
    public void Layout_OverlongBreakableSegmentsWrapOntoFreshLineWhenCurrentLineHasContent()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo abcdefghijk", Font);
        var prefixWidth = prepared.Widths[0] + prepared.Widths[1];
        var wordBreaks = RequireBreakableWidths(prepared.BreakableWidths[2]);
        var width = prefixWidth + wordBreaks[0] + wordBreaks[1] + 0.1;

        var batched = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        Assert.Equal("foo ", batched.Lines[0].Text);
        Assert.StartsWith("ab", batched.Lines[1].Text, StringComparison.Ordinal);

        var streamed = PretextLayout.LayoutNextLine(prepared, new LayoutCursor(0, 0), width);
        Assert.Equal("foo ", streamed?.Text);
        Assert.Equal(batched.LineCount, PretextLayout.Layout(prepared, width, LineHeight).LineCount);
    }

    [Fact(DisplayName = "walkLineRanges reproduces layoutWithLines geometry without materializing text")]
    public void Layout_WalkLineRangesReproducesGeometryWithoutMaterializingText()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic said \"hello\" to 世界 and waved.", Font);
        var width = prepared.Widths[0] + prepared.Widths[1] + prepared.Widths[2] + RequireBreakableWidths(prepared.BreakableWidths[4])[0] + prepared.DiscretionaryHyphenWidth + 0.1;
        var expected = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        var actual = new List<LineRangeSnapshot>();

        var lineCount = PretextLayout.WalkLineRanges(prepared, width, line =>
        {
            actual.Add(new LineRangeSnapshot(line.Width, line.Start, line.End));
        });

        Assert.Equal(expected.LineCount, lineCount);
        Assert.Equal(
            expected.Lines.Select(static line => new LineRangeSnapshot(line.Width, line.Start, line.End)),
            actual);
    }

    [Fact(DisplayName = "countPreparedLines stays aligned with the walked line counter")]
    public void Layout_CountPreparedLinesStaysAlignedWithWalkedLineCounter()
    {
        var texts = new[]
        {
            "The quick brown fox jumps over the lazy dog.",
            "said \"hello\" to 世界 and waved.",
            "مرحبا، عالم؟",
            "author 7:00-9:00 only",
            "alpha\u200Bbeta gamma",
        };
        var widths = new[] { 40d, 80d, 120d, 200d };

        foreach (var text in texts)
        {
            var prepared = PretextLayout.PrepareWithSegments(text, Font);
            foreach (var width in widths)
            {
                var counted = PretextLayout.CountPreparedLinesForTests(prepared, width);
                var walked = PretextLayout.WalkPreparedLinesForTests(prepared, width);
                Assert.Equal(counted, walked);
            }
        }
    }
}
