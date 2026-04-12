using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Pretext;

namespace PretextSamples.Samples;

public sealed class JustificationComparisonSampleView : UserControl
{
    private const string FontFamilyCss = "Georgia, \"Times New Roman\", serif";
    private const string FontFamilyDisplay = "Georgia";
    private const double BodyFontSize = 15;
    private const double BodyLineHeight = 24;
    private const double Pad = 12;
    private const double ParagraphGap = BodyLineHeight * 0.6;
    private const double ColumnGap = 24;

    private readonly PreparedTextWithSegments[] _preparedParagraphs = JustificationComparisonData.Paragraphs
        .Select(paragraph => PretextLayout.PrepareWithSegments(paragraph, $"{BodyFontSize}px {FontFamilyCss}"))
        .ToArray();
    private readonly PreparedTextWithSegments[] _hyphenatedPreparedParagraphs;
    private readonly StretchScrollHost _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;
    private readonly Slider _widthSlider;
    private readonly CheckBox _indicatorToggle;
    private readonly TextBlock _widthValue;
    private readonly Grid _columnsGrid;
    private readonly ScrollViewer _columnsScroller;
    private readonly JustificationColumnCard _greedyCard;
    private readonly JustificationColumnCard _hyphenCard;
    private readonly JustificationColumnCard _optimalCard;
    private readonly double _naturalSpaceWidth;
    private readonly double _hyphenWidth;

    public JustificationComparisonSampleView()
    {
        var font = $"{BodyFontSize}px {FontFamilyCss}";
        _hyphenatedPreparedParagraphs = JustificationComparisonData.Paragraphs
            .Select(paragraph => PretextLayout.PrepareWithSegments(HyphenateParagraph(paragraph), font))
            .ToArray();
        _naturalSpaceWidth = MeasureTextWidth("a a", font, " ");
        _hyphenWidth = MeasureSingleRunWidth("-", font);

        _greedyCard = new JustificationColumnCard("CSS / Greedy", "Browser-style line-by-line justification");
        _hyphenCard = new JustificationColumnCard("Pretext (Hyphenation)", "Greedy with syllable-level hyphenation");
        _optimalCard = new JustificationColumnCard("Pretext (Knuth-Plass)", "Global line-breaking with syllable hyphenation");

        _widthSlider = new Slider
        {
            Minimum = 200,
            Maximum = 600,
            Value = 364,
            StepFrequency = 1,
            SmallChange = 1,
            LargeChange = 16,
            Width = 220,
        };
        _widthValue = new TextBlock
        {
            Foreground = SampleTheme.AccentBrush,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _indicatorToggle = new CheckBox
        {
            Content = "Toggle red visualizers",
            IsChecked = true,
            Foreground = SampleTheme.MutedBrush,
            FontSize = 13,
        };

        var controls = new StackPanel
        {
            Spacing = 12,
        };

        controls.Children.Add(new TextBlock
        {
            Text = "Column width",
            Foreground = SampleTheme.MutedBrush,
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            CharacterSpacing = 60,
            TextWrapping = TextWrapping.NoWrap,
        });

        var sliderRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 14,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sliderRow.Children.Add(_widthSlider);
        sliderRow.Children.Add(_widthValue);
        controls.Children.Add(sliderRow);
        controls.Children.Add(_indicatorToggle);

        _columnsGrid = new Grid
        {
            ColumnSpacing = ColumnGap,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        _columnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _columnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _columnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _columnsGrid.Children.Add(_greedyCard);
        _columnsGrid.Children.Add(_hyphenCard);
        _columnsGrid.Children.Add(_optimalCard);
        Grid.SetColumn(_greedyCard, 0);
        Grid.SetColumn(_hyphenCard, 1);
        Grid.SetColumn(_optimalCard, 2);

        _columnsScroller = new ScrollViewer
        {
            HorizontalScrollMode = ScrollMode.Enabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _columnsGrid,
        };

        var stack = SampleUi.CreatePageStack();
        stack.Children.Add(SampleUi.CreateHeader(
            "DEMO",
            "Justification Algorithms Compared",
            "Side-by-side comparison of greedy justification, greedy hyphenation, and optimal Knuth-Plass breaking. The sample visualizes rivers and spacing variance so the typography tradeoffs are obvious."));
        stack.Children.Add(SampleUi.CreateCard(controls));
        stack.Children.Add(_columnsScroller);

        _pageRoot = (StretchScrollHost)SampleUi.CreatePageRoot(stack);
        Content = _pageRoot;

        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);
        Loaded += (_, _) => _renderScheduler.Schedule();
        SizeChanged += (_, _) => _renderScheduler.Schedule();
        _widthSlider.ValueChanged += (_, _) => _renderScheduler.Schedule();
        _indicatorToggle.Checked += (_, _) => _renderScheduler.Schedule();
        _indicatorToggle.Unchecked += (_, _) => _renderScheduler.Schedule();
    }

    private void Render()
    {
        var requestedWidth = Math.Round(_widthSlider.Value);
        var columnWidth = Math.Max(220, requestedWidth);
        var showIndicators = _indicatorToggle.IsChecked == true;
        _widthValue.Text = $"{columnWidth:N0}px";

        _columnsGrid.Width = columnWidth * 3 + ColumnGap * 2;

        var innerWidth = Math.Max(120, columnWidth - Pad * 2);

        var stopwatch = Stopwatch.StartNew();
        var greedyParagraphs = _preparedParagraphs.Select(prepared => GreedyJustifiedLayout(prepared, innerWidth)).ToList();
        stopwatch.Stop();
        var greedyMetrics = ComputeMetrics(greedyParagraphs, _naturalSpaceWidth) with { LayoutMs = stopwatch.Elapsed.TotalMilliseconds };

        stopwatch.Restart();
        var hyphenParagraphs = _hyphenatedPreparedParagraphs.Select(prepared => GreedyJustifiedLayout(prepared, innerWidth)).ToList();
        stopwatch.Stop();
        var hyphenMetrics = ComputeMetrics(hyphenParagraphs, _naturalSpaceWidth) with { LayoutMs = stopwatch.Elapsed.TotalMilliseconds };

        stopwatch.Restart();
        var optimalParagraphs = _hyphenatedPreparedParagraphs.Select(prepared => OptimalLayout(prepared, innerWidth)).ToList();
        stopwatch.Stop();
        var optimalMetrics = ComputeMetrics(optimalParagraphs, _naturalSpaceWidth) with { LayoutMs = stopwatch.Elapsed.TotalMilliseconds };

        _greedyCard.Render(greedyParagraphs, columnWidth, _naturalSpaceWidth, showIndicators, greedyMetrics);
        _hyphenCard.Render(hyphenParagraphs, columnWidth, _naturalSpaceWidth, showIndicators, hyphenMetrics);
        _optimalCard.Render(optimalParagraphs, columnWidth, _naturalSpaceWidth, showIndicators, optimalMetrics);
    }

    private List<JustifiedLine> GreedyJustifiedLayout(PreparedTextWithSegments prepared, double maxWidth)
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
                segments.Add(new JustifiedSegment("-", _hyphenWidth, false));
            }

            while (segments.Count > 0 && segments[^1].IsSpace)
            {
                segments.RemoveAt(segments.Count - 1);
            }

            var naturalWidth = segments.Sum(static segment => segment.Width);
            lines.Add(new JustifiedLine(segments, maxWidth, isLast, naturalWidth));
            cursor = line.End;
        }

        return lines;
    }

    private List<JustifiedLine> OptimalLayout(PreparedTextWithSegments prepared, double maxWidth)
    {
        var segments = prepared.Segments;
        var widths = prepared.Widths;
        var segmentCount = segments.Count;
        if (segmentCount == 0)
        {
            return [];
        }

        var breakCandidates = new List<BreakCandidate> { new(0, false) };
        for (var index = 0; index < segmentCount; index++)
        {
            var text = segments[index];
            if (text == "\u00AD")
            {
                if (index + 1 < segmentCount)
                {
                    breakCandidates.Add(new BreakCandidate(index + 1, true));
                }
            }
            else if (string.IsNullOrWhiteSpace(text) && index + 1 < segmentCount)
            {
                breakCandidates.Add(new BreakCandidate(index + 1, false));
            }
        }

        breakCandidates.Add(new BreakCandidate(segmentCount, false));

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
                wordWidth += _hyphenWidth;
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
            if (justifiedSpace < 0)
            {
                return 1e8;
            }

            if (justifiedSpace < _naturalSpaceWidth * 0.4)
            {
                return 1e8;
            }

            var ratio = (justifiedSpace - _naturalSpaceWidth) / _naturalSpaceWidth;
            var badness = Math.Pow(Math.Abs(ratio), 3) * 1000;
            var riverExcess = justifiedSpace / _naturalSpaceWidth - 1.5;
            var riverPenalty = riverExcess > 0 ? 5000 + riverExcess * riverExcess * 10000 : 0;
            var tightThreshold = _naturalSpaceWidth * 0.65;
            var tightPenalty = justifiedSpace < tightThreshold
                ? 3000 + Math.Pow(tightThreshold - justifiedSpace, 2) * 10000
                : 0;
            var hyphenPenalty = info.EndsWithHyphen ? 50 : 0;
            return badness + riverPenalty + tightPenalty + hyphenPenalty;
        }

        var candidateCount = breakCandidates.Count;
        var dp = Enumerable.Repeat(double.PositiveInfinity, candidateCount).ToArray();
        var previous = Enumerable.Repeat(-1, candidateCount).ToArray();
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
                var totalWidth = info.WordWidth + info.SpaceCount * _naturalSpaceWidth;
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
                lineSegments.Add(new JustifiedSegment("-", _hyphenWidth, false));
            }

            while (lineSegments.Count > 0 && lineSegments[^1].IsSpace)
            {
                lineSegments.RemoveAt(lineSegments.Count - 1);
            }

            var naturalWidth = lineSegments.Sum(static segment => segment.Width);
            lines.Add(new JustifiedLine(lineSegments, maxWidth, isLast, naturalWidth));
            fromCandidate = toCandidate;
        }

        return lines;
    }

    private static QualityMetrics ComputeMetrics(IEnumerable<IReadOnlyList<JustifiedLine>> paragraphs, double naturalSpaceWidth)
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
        var tokens = Regex.Split(paragraph, @"(\s+)");
        return string.Concat(tokens.Select(static token =>
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            var parts = JustificationComparisonData.HyphenateWord(token);
            return parts.Length <= 1 ? token : string.Join('\u00AD', parts);
        }));
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

    private readonly record struct BreakCandidate(int SegmentIndex, bool IsSoftHyphen);

    private readonly record struct LineInfo(double WordWidth, int SpaceCount, bool EndsWithHyphen);

    private readonly record struct JustifiedSegment(string Text, double Width, bool IsSpace);

    private sealed record JustifiedLine(IReadOnlyList<JustifiedSegment> Segments, double MaxWidth, bool IsLast, double LineWidth);

    private sealed record QualityMetrics(double AvgDeviation, double MaxDeviation, int RiverCount, int LineCount, double LayoutMs);

    private sealed class JustificationColumnCard : StackPanel
    {
        private readonly TextBlock _title;
        private readonly TextBlock _subtitle;
        private readonly JustifiedColumnView _columnView = new();
        private readonly Border _metricsHost;
        private readonly TextBlock _linesValue;
        private readonly TextBlock _avgValue;
        private readonly TextBlock _maxValue;
        private readonly TextBlock _riverValue;

        public JustificationColumnCard(string title, string subtitle)
        {
            Spacing = 8;
            HorizontalAlignment = HorizontalAlignment.Left;

            _title = new TextBlock
            {
                Text = title,
                Foreground = SampleTheme.AccentBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Helvetica Neue"),
                CharacterSpacing = 80,
                TextWrapping = TextWrapping.NoWrap,
            };
            _subtitle = new TextBlock
            {
                Text = subtitle,
                Foreground = SampleTheme.MutedBrush,
                FontSize = 11,
                TextWrapping = TextWrapping.WrapWholeWords,
            };

            var metricsStack = new StackPanel { Spacing = 4 };
            metricsStack.Children.Add(CreateMetricRow("Lines", out _linesValue));
            metricsStack.Children.Add(CreateMetricRow("Avg deviation", out _avgValue));
            metricsStack.Children.Add(CreateMetricRow("Max deviation", out _maxValue));
            metricsStack.Children.Add(CreateMetricRow("River spaces", out _riverValue));

            _metricsHost = new Border
            {
                Background = SampleTheme.Brush(0xF5, 0xF2, 0xED),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10, 8, 10, 8),
                Child = metricsStack,
            };

            Children.Add(_title);
            Children.Add(_subtitle);
            Children.Add(_columnView);
            Children.Add(_metricsHost);
        }

        public void Render(IReadOnlyList<IReadOnlyList<JustifiedLine>> paragraphs, double width, double naturalSpaceWidth, bool showIndicators, QualityMetrics metrics)
        {
            Width = width;
            _subtitle.Width = width;
            _columnView.Render(paragraphs, width, naturalSpaceWidth, showIndicators);
            _metricsHost.Width = width;

            _linesValue.Text = metrics.LineCount.ToString();
            _avgValue.Text = $"{metrics.AvgDeviation * 100:0.0}%";
            _avgValue.Foreground = GetQualityBrush(metrics.AvgDeviation);
            _maxValue.Text = $"{metrics.MaxDeviation * 100:0.0}%";
            _maxValue.Foreground = GetQualityBrush(metrics.MaxDeviation / 2);
            _riverValue.Text = metrics.RiverCount.ToString();
            _riverValue.Foreground = metrics.RiverCount > 0 ? SampleTheme.Brush(0xC4, 0x44, 0x44) : SampleTheme.Brush(0x2A, 0x8A, 0x4A);
        }

        private static Grid CreateMetricRow(string label, out TextBlock valueBlock)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = SampleTheme.MutedBrush,
                FontSize = 11,
            });

            valueBlock = new TextBlock
            {
                Foreground = SampleTheme.AccentBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);
            return grid;
        }

        private static Brush GetQualityBrush(double deviation)
        {
            if (deviation < 0.15)
            {
                return SampleTheme.Brush(0x2A, 0x8A, 0x4A);
            }

            if (deviation < 0.35)
            {
                return SampleTheme.Brush(0xB8, 0x70, 0x20);
            }

            return SampleTheme.Brush(0xC4, 0x44, 0x44);
        }
    }

    private sealed class JustifiedColumnView : UserControl
    {
        private readonly Canvas _indicatorLayer = new();
        private readonly Canvas _textLayer = new();
        private readonly Grid _root = new();
        private readonly List<Rectangle> _indicatorPool = [];
        private readonly List<TextBlock> _textPool = [];

        public JustifiedColumnView()
        {
            var surface = new Border
            {
                Background = SampleTheme.WhiteBrush,
                BorderBrush = SampleTheme.Brush(0xE8, 0xE0, 0xD4),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Child = _root,
            };

            _root.Children.Add(_indicatorLayer);
            _root.Children.Add(_textLayer);
            Content = surface;
        }

        public void Render(IReadOnlyList<IReadOnlyList<JustifiedLine>> paragraphs, double columnWidth, double naturalSpaceWidth, bool showIndicators)
        {
            var words = new List<RenderedWord>();
            var indicators = new List<RenderedIndicator>();
            var y = Pad;

            foreach (var paragraph in paragraphs)
            {
                foreach (var line in paragraph)
                {
                    var shouldJustify = !line.IsLast && line.LineWidth >= line.MaxWidth * 0.6;
                    if (!shouldJustify)
                    {
                        RenderRagged(line, y, words);
                        y += BodyLineHeight;
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
                        RenderRagged(line, y, words);
                        y += BodyLineHeight;
                        continue;
                    }

                    var rawJustifiedSpace = (line.MaxWidth - wordWidth) / spaceCount;
                    if (rawJustifiedSpace < naturalSpaceWidth * 0.2)
                    {
                        RenderRagged(line, y, words);
                        y += BodyLineHeight;
                        continue;
                    }

                    var justifiedSpace = Math.Max(rawJustifiedSpace, naturalSpaceWidth * 0.75);
                    var isRiver = justifiedSpace > naturalSpaceWidth * 1.5;
                    var x = Pad;
                    foreach (var segment in line.Segments)
                    {
                        if (segment.IsSpace)
                        {
                            if (showIndicators && isRiver)
                            {
                                var intensity = Math.Min(1, (justifiedSpace / naturalSpaceWidth - 1.5) / 1.5);
                                var r = (byte)Math.Round(220 + intensity * 35);
                                var g = (byte)Math.Round(180 - intensity * 80);
                                var b = (byte)Math.Round(180 - intensity * 80);
                                var alpha = (byte)Math.Round((0.25 + intensity * 0.35) * 255);
                                indicators.Add(new RenderedIndicator(x + 1, y, Math.Max(0, justifiedSpace - 2), BodyLineHeight, ColorHelper.FromArgb(alpha, r, g, b)));
                            }

                            x += justifiedSpace;
                        }
                        else
                        {
                            words.Add(new RenderedWord(segment.Text, x, y));
                            x += segment.Width;
                        }
                    }

                    y += BodyLineHeight;
                }

                y += ParagraphGap;
            }

            if (paragraphs.Count > 0)
            {
                y -= ParagraphGap;
            }

            var totalHeight = y + Pad;
            Width = columnWidth;
            Height = totalHeight;
            _root.Width = columnWidth;
            _root.Height = totalHeight;
            _indicatorLayer.Width = columnWidth;
            _indicatorLayer.Height = totalHeight;
            _textLayer.Width = columnWidth;
            _textLayer.Height = totalHeight;

            SampleUi.EnsurePool(_indicatorLayer, _indicatorPool, indicators.Count, () => new Rectangle { IsHitTestVisible = false });
            for (var index = 0; index < indicators.Count; index++)
            {
                var indicator = indicators[index];
                var rectangle = _indicatorPool[index];
                rectangle.Fill = new SolidColorBrush(indicator.Color);
                rectangle.Width = indicator.Width;
                rectangle.Height = indicator.Height;
                Canvas.SetLeft(rectangle, indicator.X);
                Canvas.SetTop(rectangle, indicator.Y);
            }

            SampleUi.EnsurePool(_textLayer, _textPool, words.Count, () => new TextBlock
            {
                Foreground = SampleTheme.InkBrush,
                FontFamily = new FontFamily(FontFamilyDisplay),
                FontSize = BodyFontSize,
                LineHeight = BodyLineHeight,
                TextWrapping = TextWrapping.NoWrap,
                IsHitTestVisible = false,
            });
            for (var index = 0; index < words.Count; index++)
            {
                var word = words[index];
                var textBlock = _textPool[index];
                textBlock.Text = word.Text;
                Canvas.SetLeft(textBlock, word.X);
                Canvas.SetTop(textBlock, word.Y);
            }
        }

        private static void RenderRagged(JustifiedLine line, double y, ICollection<RenderedWord> words)
        {
            var x = Pad;
            foreach (var segment in line.Segments)
            {
                if (!segment.IsSpace)
                {
                    words.Add(new RenderedWord(segment.Text, x, y));
                }

                x += segment.Width;
            }
        }

        private readonly record struct RenderedWord(string Text, double X, double Y);

        private readonly record struct RenderedIndicator(double X, double Y, double Width, double Height, Windows.UI.Color Color);
    }
}
