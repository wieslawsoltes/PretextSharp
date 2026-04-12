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

    [Fact(DisplayName = "materializeLineRange reproduces layoutWithLines text from walked ranges")]
    public void Layout_MaterializeLineRangeReproducesLayoutWithLines()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic said \"hello\" to 世界 and waved.", Font);
        var width = prepared.Widths[0] + prepared.Widths[1] + prepared.Widths[2] + RequireBreakableWidths(prepared.BreakableWidths[4])[0] + prepared.DiscretionaryHyphenWidth + 0.1;
        var expected = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        var actual = new List<LayoutLine>();

        PretextLayout.WalkLineRanges(prepared, width, line =>
        {
            actual.Add(PretextLayout.MaterializeLineRange(prepared, line));
        });

        Assert.Equal(expected.Lines, actual);
    }

    [Fact(DisplayName = "measureLineStats matches walked line count and widest line")]
    public void Layout_MeasureLineStatsMatchesWalkedLineCountAndWidestLine()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic said \"hello\" to 世界 and waved.", Font);
        var width = prepared.Widths[0] + prepared.Widths[1] + prepared.Widths[2] + RequireBreakableWidths(prepared.BreakableWidths[4])[0] + prepared.DiscretionaryHyphenWidth + 0.1;
        var walkedLineCount = 0;
        var walkedMaxLineWidth = 0d;

        PretextLayout.WalkLineRanges(prepared, width, line =>
        {
            walkedLineCount++;
            walkedMaxLineWidth = Math.Max(walkedMaxLineWidth, line.Width);
        });

        Assert.Equal(new LineStats(walkedLineCount, walkedMaxLineWidth), PretextLayout.MeasureLineStats(prepared, width));
    }

    [Fact(DisplayName = "measureNaturalWidth matches the widest forced line")]
    public void Layout_MeasureNaturalWidthMatchesWidestForcedLine()
    {
        var prepared = PretextLayout.PrepareWithSegments("alpha beta\n世界 gamma", Font, PreWrap);
        var widest = 0d;

        PretextLayout.WalkLineRanges(prepared, double.PositiveInfinity, line =>
        {
            widest = Math.Max(widest, line.Width);
        });

        Assert.Equal(widest, PretextLayout.MeasureNaturalWidth(prepared), 6);
    }

    [Fact(DisplayName = "layoutNextLineRange stays aligned with range geometry walking")]
    public void Layout_LayoutNextLineRangeStaysAlignedWithRangeGeometryWalking()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic said \"hello\" to 世界 and waved.", Font);

        foreach (var width in new[] { 48d, 72d, 120d })
        {
            var cursor = new LayoutCursor(0, 0);
            var streamed = new List<LineRangeSnapshot>();
            while (true)
            {
                var line = PretextLayout.LayoutNextLineRange(prepared, cursor, width);
                if (line is null)
                {
                    break;
                }

                streamed.Add(new LineRangeSnapshot(line.Width, line.Start, line.End));
                cursor = line.End;
            }

            var walked = new List<LineRangeSnapshot>();
            PretextLayout.WalkLineRanges(prepared, width, line =>
            {
                walked.Add(new LineRangeSnapshot(line.Width, line.Start, line.End));
            });

            Assert.Equal(walked, streamed);
        }
    }

    [Fact(DisplayName = "mixed-script canary keeps layoutWithLines and layoutNextLine aligned across CJK, RTL, and emoji")]
    public void Layout_MixedScriptCanaryKeepsLayoutApisAligned()
    {
        var prepared = PretextLayout.PrepareWithSegments("Hello 世界 مرحبا 🌍 test", Font);
        const double width = 80;
        var expected = PretextLayout.LayoutWithLines(prepared, width, LineHeight);

        Assert.Equal(new[] { "Hello 世", "界 مرحبا ", "🌍 test" }, expected.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(expected.Lines, CollectStreamedLines(prepared, width));
    }

    [Fact(DisplayName = "layout and layoutWithLines stay aligned when ZWSP triggers narrow grapheme breaking")]
    public void Layout_ZwspTriggeredNarrowBreakingKeepsApisAligned()
    {
        foreach (var text in new[] { "alpha\u200Bbeta", "alpha\u200Bbeta\u200Cgamma" })
        {
            var plain = PretextLayout.Prepare(text, Font);
            var rich = PretextLayout.PrepareWithSegments(text, Font);
            const double width = 10;

            Assert.Equal(
                PretextLayout.Layout(plain, width, LineHeight).LineCount,
                PretextLayout.LayoutWithLines(rich, width, LineHeight).LineCount);
        }
    }

    [Fact(DisplayName = "layoutWithLines strips leading collapsible space after a ZWSP break the same way as layoutNextLine")]
    public void Layout_LayoutWithLinesStripsLeadingCollapsibleSpaceAfterZwspBreak()
    {
        var prepared = PretextLayout.PrepareWithSegments("生活就像海洋\u200B 只有意志坚定的人才能到达彼岸", Font);
        var width = prepared.Widths[0] - 1;

        Assert.Equal(
            PretextLayout.LayoutWithLines(prepared, width, LineHeight).Lines,
            CollectStreamedLines(prepared, width));
    }

    [Fact(DisplayName = "layout line count stays aligned after a ZWSP break followed by collapsible space")]
    public void Layout_LineCountStaysAlignedAfterZwspBreakFollowedByCollapsibleSpace()
    {
        var prepared = PretextLayout.PrepareWithSegments("生活就像海洋\u200B 只有意志坚定的人才能到达彼岸", Font);
        var width = prepared.Widths[0] - 1;
        var withLines = PretextLayout.LayoutWithLines(prepared, width, LineHeight);

        Assert.Equal(withLines.LineCount, PretextLayout.Layout(prepared, width, LineHeight).LineCount);
        Assert.Equal(withLines.LineCount, PretextLayout.CountPreparedLinesForTests(prepared, width));
        Assert.Equal(withLines.LineCount, PretextLayout.WalkPreparedLinesForTests(prepared, width));
    }

    [Fact(DisplayName = "adjacent CJK text units stay breakable after visible text, not only after spaces")]
    public void Layout_AdjacentCjkTextUnitsStayBreakableAfterVisibleText()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo 世界 bar", Font);
        Assert.Equal(new[] { "foo", " ", "世", "界", " ", "bar" }, prepared.Segments);

        var width = prepared.Widths[0] + prepared.Widths[1] + prepared.Widths[2] + 0.1;
        var batched = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        Assert.Equal(new[] { "foo 世", "界 bar" }, batched.Lines.Select(static line => line.Text).ToArray());

        Assert.Equal(new[] { "foo 世", "界 bar" }, CollectStreamedLines(prepared, width).Select(static line => line.Text).ToArray());
        Assert.Equal(new LayoutResult(2, LineHeight * 2), PretextLayout.Layout(prepared, width, LineHeight));
    }

    [Fact(DisplayName = "layoutNextLine can resume from any fixed-width line start without hidden state")]
    public void Layout_LayoutNextLineCanResumeFromAnyFixedWidthLineStart()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic said \"hello\" to 世界 and waved. alpha\u200Bbeta 🚀", Font);
        const double width = 90;
        var expected = PretextLayout.LayoutWithLines(prepared, width, LineHeight);

        Assert.True(expected.Lines.Count > 2);

        for (var index = 0; index < expected.Lines.Count; index++)
        {
            Assert.Equal(expected.Lines.Skip(index), CollectStreamedLines(prepared, width, expected.Lines[index].Start));
        }

        Assert.Null(PretextLayout.LayoutNextLine(prepared, TerminalCursor(prepared), width));
    }

    [Fact(DisplayName = "rich line boundary cursors reconstruct normalized source text exactly")]
    public void Layout_LineBoundaryCursorsReconstructNormalizedSourceText()
    {
        var texts = new[]
        {
            "a b c",
            "  Hello\t \n  World  ",
            "foo trans\u00ADatlantic said \"hello\" to 世界 and waved.",
            "According to محمد الأحمد, the results improved.",
            "see https://example.com/reports/q3?lang=ar&mode=full now",
            "alpha\u200Bbeta gamma",
        };

        foreach (var text in texts)
        {
            var prepared = PretextLayout.PrepareWithSegments(text, Font);
            var expected = string.Concat(prepared.Segments);

            foreach (var width in new[] { 40d, 80d, 120d, 200d })
            {
                var batched = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
                var streamed = CollectStreamedLines(prepared, width);

                Assert.Equal(expected, ReconstructFromLineBoundaries(prepared, batched.Lines));
                Assert.Equal(expected, ReconstructFromLineBoundaries(prepared, streamed));
                Assert.Equal(expected, ReconstructFromWalkedRanges(prepared, width));
            }
        }
    }

    [Fact(DisplayName = "soft-hyphen round-trip uses source slices instead of rendered line text")]
    public void Layout_SoftHyphenRoundTripUsesSourceSlices()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic", Font);
        var width =
            prepared.Widths[0] +
            prepared.Widths[1] +
            prepared.Widths[2] +
            RequireBreakableWidths(prepared.BreakableWidths[4])[0] +
            prepared.DiscretionaryHyphenWidth +
            0.1;
        var result = PretextLayout.LayoutWithLines(prepared, width, LineHeight);

        Assert.Equal("foo trans-atlantic", string.Concat(result.Lines.Select(static line => line.Text)));
        Assert.Equal("foo trans\u00ADatlantic", ReconstructFromLineBoundaries(prepared, result.Lines));
    }

    [Fact(DisplayName = "soft-hyphen fallback does not crash when overflow happens on a later space")]
    public void Layout_SoftHyphenFallbackDoesNotCrashWhenOverflowHappensLater()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic labels", Font);
        var width = MeasureWidth("foo transatlantic", Font) + 0.1;
        var result = PretextLayout.LayoutWithLines(prepared, width, LineHeight);

        Assert.Equal(new[] { "foo transatlantic ", "labels" }, result.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(result.LineCount, PretextLayout.Layout(prepared, width, LineHeight).LineCount);
    }

    [Fact(DisplayName = "layoutNextLine variable-width streaming stays contiguous and reconstructs normalized text")]
    public void Layout_VariableWidthStreamingStaysContiguousAndReconstructsText()
    {
        var prepared = PretextLayout.PrepareWithSegments(
            "foo trans\u00ADatlantic said \"hello\" to 世界 and waved. According to محمد الأحمد, alpha\u200Bbeta 🚀",
            Font);
        var widths = new[] { 140d, 72d, 108d, 64d, 160d, 84d, 116d, 70d, 180d, 92d, 128d, 76d };
        var lines = CollectStreamedLinesWithWidths(prepared, widths);
        var expected = string.Concat(prepared.Segments);

        Assert.True(lines.Count > 2);
        Assert.Equal(new LayoutCursor(0, 0), lines[0].Start);

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            Assert.True(CompareCursors(line.End, line.Start) > 0);
            if (index > 0)
            {
                Assert.Equal(lines[index - 1].End, line.Start);
            }
        }

        Assert.Equal(TerminalCursor(prepared), lines[^1].End);
        Assert.Equal(expected, ReconstructFromLineBoundaries(prepared, lines));
        Assert.Null(PretextLayout.LayoutNextLine(prepared, TerminalCursor(prepared), widths[^1]));
    }

    [Fact(DisplayName = "layoutNextLine variable-width streaming stays contiguous in pre-wrap mode")]
    public void Layout_VariableWidthStreamingStaysContiguousInPreWrapMode()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo\n  bar baz\n\tquux quuz", Font, PreWrap);
        var widths = new[] { 200d, 62d, 80d, 200d, 72d, 200d };
        var lines = CollectStreamedLinesWithWidths(prepared, widths);
        var expected = string.Concat(prepared.Segments);

        Assert.True(lines.Count >= 4);
        Assert.Equal(new LayoutCursor(0, 0), lines[0].Start);

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            Assert.True(CompareCursors(line.End, line.Start) > 0);
            if (index > 0)
            {
                Assert.Equal(lines[index - 1].End, line.Start);
            }
        }

        Assert.Equal(TerminalCursor(prepared), lines[^1].End);
        Assert.Equal(expected, ReconstructFromLineBoundaries(prepared, lines));
        Assert.Null(PretextLayout.LayoutNextLine(prepared, TerminalCursor(prepared), widths[^1]));
    }

    [Fact(DisplayName = "mixed CJK-plus-numeric runs use cumulative widths when breaking the numeric suffix")]
    public void Layout_MixedCjkPlusNumericRunsUseCumulativeWidths()
    {
        var prepared = PretextLayout.PrepareWithSegments("中文11111111111111111", Font);
        var width = MeasureWidth("11111", Font) + 0.1;

        Assert.Equal(new[] { "中", "文", "11111111111111111" }, prepared.Segments);

        var batched = PretextLayout.LayoutWithLines(prepared, width, LineHeight);
        Assert.Equal(
            new[] { "中文", "11111", "11111", "11111", "11" },
            batched.Lines.Select(static line => line.Text).ToArray());

        Assert.Equal(batched.Lines, CollectStreamedLines(prepared, width));
        Assert.Equal(new LayoutResult(5, LineHeight * 5), PretextLayout.Layout(prepared, width, LineHeight));
    }

    [Fact(DisplayName = "keep-all suppresses ordinary CJK intra-word breaks after existing line content")]
    public void Layout_KeepAllSuppressesOrdinaryCjkIntraWordBreaksAfterExistingLineContent()
    {
        const string text = "A 中文测试";
        var normal = PretextLayout.PrepareWithSegments(text, Font);
        var keepAll = PretextLayout.PrepareWithSegments(text, Font, new PrepareOptions(WordBreak: WordBreakMode.KeepAll));
        var width = MeasureWidth("A 中", Font) + 0.1;

        Assert.Equal("A 中", PretextLayout.LayoutWithLines(normal, width, LineHeight).Lines[0].Text);
        Assert.Equal("A ", PretextLayout.LayoutWithLines(keepAll, width, LineHeight).Lines[0].Text);
        Assert.True(PretextLayout.Layout(keepAll, width, LineHeight).LineCount > PretextLayout.Layout(normal, width, LineHeight).LineCount);
    }

    [Fact(DisplayName = "keep-all lets mixed no-space CJK runs break through the script boundary")]
    public void Layout_KeepAllLetsMixedNoSpaceCjkRunsBreakThroughScriptBoundary()
    {
        const string text = "日本語foo-bar";
        var normal = PretextLayout.PrepareWithSegments(text, Font);
        var keepAll = PretextLayout.PrepareWithSegments(text, Font, new PrepareOptions(WordBreak: WordBreakMode.KeepAll));
        var width = MeasureWidth("日本語f", Font) + 0.1;

        Assert.Equal("日本語", PretextLayout.LayoutWithLines(normal, width, LineHeight).Lines[0].Text);
        Assert.Equal("日本語f", PretextLayout.LayoutWithLines(keepAll, width, LineHeight).Lines[0].Text);
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
