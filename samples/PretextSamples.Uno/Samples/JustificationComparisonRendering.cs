using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Shapes;

namespace PretextSamples.Samples;

internal sealed class JustificationColumnCard : StackPanel
{
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

        Children.Add(new TextBlock
        {
            Text = title,
            Foreground = SampleTheme.AccentBrush,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Helvetica Neue"),
            CharacterSpacing = 80,
            TextWrapping = TextWrapping.NoWrap,
        });

        _subtitle = new TextBlock
        {
            Text = subtitle,
            Foreground = SampleTheme.MutedBrush,
            FontSize = 11,
            TextWrapping = TextWrapping.WrapWholeWords,
        };
        Children.Add(_subtitle);

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

        Children.Add(_columnView);
        Children.Add(_metricsHost);
    }

    public void Render(
        IReadOnlyList<IReadOnlyList<JustifiedLine>> paragraphs,
        double width,
        double naturalSpaceWidth,
        bool showIndicators,
        QualityMetrics metrics)
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

internal sealed class JustifiedColumnView : UserControl
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
        var y = JustificationComparisonModel.Pad;

        foreach (var paragraph in paragraphs)
        {
            foreach (var line in paragraph)
            {
                var shouldJustify = !line.IsLast && line.LineWidth >= line.MaxWidth * 0.6;
                if (!shouldJustify)
                {
                    RenderRagged(line, y, words);
                    y += JustificationComparisonModel.BodyLineHeight;
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
                    y += JustificationComparisonModel.BodyLineHeight;
                    continue;
                }

                var rawJustifiedSpace = (line.MaxWidth - wordWidth) / spaceCount;
                if (rawJustifiedSpace < naturalSpaceWidth * 0.2)
                {
                    RenderRagged(line, y, words);
                    y += JustificationComparisonModel.BodyLineHeight;
                    continue;
                }

                var justifiedSpace = Math.Max(rawJustifiedSpace, naturalSpaceWidth * 0.75);
                var isRiver = justifiedSpace > naturalSpaceWidth * 1.5;
                var x = JustificationComparisonModel.Pad;
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
                            indicators.Add(new RenderedIndicator(x + 1, y, Math.Max(0, justifiedSpace - 2), JustificationComparisonModel.BodyLineHeight, ColorHelper.FromArgb(alpha, r, g, b)));
                        }

                        x += justifiedSpace;
                    }
                    else
                    {
                        words.Add(new RenderedWord(segment.Text, x, y));
                        x += segment.Width;
                    }
                }

                y += JustificationComparisonModel.BodyLineHeight;
            }

            y += JustificationComparisonModel.ParagraphGap;
        }

        if (paragraphs.Count > 0)
        {
            y -= JustificationComparisonModel.ParagraphGap;
        }

        var totalHeight = y + JustificationComparisonModel.Pad;
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
            FontFamily = new FontFamily(JustificationComparisonModel.FontFamilyDisplay),
            FontSize = JustificationComparisonModel.BodyFontSize,
            LineHeight = JustificationComparisonModel.BodyLineHeight,
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
        var x = JustificationComparisonModel.Pad;
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
