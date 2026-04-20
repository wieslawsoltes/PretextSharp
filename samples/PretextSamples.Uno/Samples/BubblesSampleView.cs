namespace PretextSamples.Samples;

public sealed class BubblesSampleView : UserControl
{
    private const string Font = "15px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const double LineHeight = 20;
    private const double PaddingX = 12;
    private const double PaddingY = 8;
    private const double BubbleMaxRatio = 0.8;

    private readonly (bool Sent, string Text, PreparedTextWithSegments Prepared)[] _messages =
        BubblesSampleData.Messages
            .Select(static message => (message.Sent, message.Text, PretextLayout.PrepareWithSegments(message.Text, Font)))
            .ToArray();

    private readonly Slider _slider = new() { Minimum = 220, Maximum = 760, Value = 340 };
    private readonly TextBlock _sliderValue = new() { FontFamily = new FontFamily("Consolas"), Foreground = SampleTheme.MutedBrush };
    private readonly TextBlock _cssWaste = new() { FontFamily = new FontFamily("Consolas"), Foreground = SampleTheme.InkBrush };
    private readonly TextBlock _tightWaste = new() { FontFamily = new FontFamily("Consolas"), Foreground = SampleTheme.InkBrush, Text = "0" };
    private readonly StackPanel _cssChat = new() { Spacing = 8 };
    private readonly StackPanel _tightChat = new() { Spacing = 8 };
    private readonly FrameworkElement _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;
    private double _lastChatWidth = -1;

    public BubblesSampleView()
    {
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);
        var stack = SampleUi.CreatePageStack();
        stack.Children.Add(SampleUi.CreateHeader(
            "DEMO",
            "Shrinkwrap showdown",
            "The left column sizes bubbles like fit-content. The right column binary-searches the tightest width that preserves the same line count."));

        _slider.ValueChanged += (_, _) => _renderScheduler.Schedule();

        var controls = new Grid { ColumnSpacing = 12 };
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        controls.Children.Add(new TextBlock
        {
            Text = "Container width",
            FontFamily = new FontFamily("Consolas"),
            Foreground = SampleTheme.MutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(_slider, 1);
        controls.Children.Add(_slider);
        Grid.SetColumn(_sliderValue, 2);
        controls.Children.Add(_sliderValue);
        stack.Children.Add(SampleUi.CreateCard(controls, 16));

        var comparisonGrid = new Grid { ColumnSpacing = 16 };
        comparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        comparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        comparisonGrid.Children.Add(BuildBubblePanel("CSS fit-content", "Sizes to the widest wrapped line, which leaves dead space behind shorter lines.", _cssWaste, _cssChat));
        var tightPanel = BuildBubblePanel("Pretext shrinkwrap", "Walks line ranges and reuses the line count to find the smallest width with the same wraps.", _tightWaste, _tightChat);
        Grid.SetColumn(tightPanel, 1);
        comparisonGrid.Children.Add(tightPanel);
        stack.Children.Add(comparisonGrid);
        stack.Children.Add(BuildWhyCard());

        _pageRoot = SampleUi.CreatePageRoot(stack);
        Content = _pageRoot;
        Loaded += (_, _) => _renderScheduler.Schedule();
        SizeChanged += (_, _) => _renderScheduler.Schedule();
    }

    private static Border BuildBubblePanel(string title, string description, TextBlock metricValue, StackPanel chatPanel)
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = SampleTheme.InkBrush,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
        });
        stack.Children.Add(SampleUi.CreateBodyText(description));

        var metric = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Padding = new Thickness(10, 7, 10, 7),
            Background = SampleTheme.AccentSoftBrush,
        };
        metric.Children.Add(new TextBlock
        {
            Text = "Wasted pixels:",
            FontFamily = new FontFamily("Consolas"),
            Foreground = SampleTheme.InkBrush,
        });
        metric.Children.Add(metricValue);
        stack.Children.Add(metric);

        var shell = new Border
        {
            Background = SampleTheme.ChatBackgroundBrush,
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Child = chatPanel,
        };
        stack.Children.Add(shell);

        return SampleUi.CreateCard(stack);
    }

    private static Border BuildWhyCard()
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(new TextBlock
        {
            Text = "Why can't CSS do this?",
            Foreground = SampleTheme.InkBrush,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Georgia"),
        });
        stack.Children.Add(SampleUi.CreateBodyText(
            "CSS can give you fit-content, which is the width of the widest wrapped line after layout. It cannot search for the narrowest width that preserves the same line count. Pretext can, because it measures the prepared text at multiple candidate widths and compares the resulting wraps without reading the visible tree.",
            15));
        return SampleUi.CreateCard(stack);
    }

    private void Render()
    {
        if (_pageRoot.ActualWidth <= 0)
        {
            return;
        }

        var maxChatWidth = Math.Max(220, Math.Min(760, _pageRoot.ActualWidth - 160));
        var chatWidth = Math.Min(_slider.Value, maxChatWidth);
        if (Math.Abs(chatWidth - _lastChatWidth) < 0.5 && _cssChat.Children.Count == _messages.Length)
        {
            _slider.Maximum = maxChatWidth;
            _sliderValue.Text = $"{Math.Round(chatWidth)}px";
            return;
        }

        _lastChatWidth = chatWidth;
        _slider.Maximum = maxChatWidth;
        _sliderValue.Text = $"{Math.Round(chatWidth)}px";

        var bubbleMaxWidth = Math.Floor(chatWidth * BubbleMaxRatio);
        var contentMaxWidth = Math.Max(1, bubbleMaxWidth - PaddingX * 2);
        var totalWaste = 0d;

        _cssChat.Width = chatWidth;
        _tightChat.Width = chatWidth;
        _cssChat.Children.Clear();
        _tightChat.Children.Clear();

        foreach (var message in _messages)
        {
            var cssMetrics = PreparedTextMetrics.CollectWrapMetrics(message.Prepared, contentMaxWidth, LineHeight);
            var tightMetrics = PreparedTextMetrics.FindTightWrapMetrics(message.Prepared, contentMaxWidth, LineHeight);

            var cssWidth = Math.Ceiling(cssMetrics.MaxLineWidth) + PaddingX * 2;
            var tightWidth = Math.Ceiling(tightMetrics.MaxLineWidth) + PaddingX * 2;
            totalWaste += Math.Max(0, cssWidth - tightWidth) * (cssMetrics.Height + PaddingY * 2);

            _cssChat.Children.Add(CreateBubble(message.Sent, message.Text, cssWidth, bubbleMaxWidth, explicitWidth: false));
            _tightChat.Children.Add(CreateBubble(message.Sent, message.Text, tightWidth, bubbleMaxWidth, explicitWidth: true));
        }

        _cssWaste.Text = Math.Round(totalWaste).ToString("N0");
    }

    private static Border CreateBubble(bool sent, string text, double desiredWidth, double maxWidth, bool explicitWidth)
    {
        var textWidth = Math.Max(40, desiredWidth - PaddingX * 2);
        var border = new Border
        {
            HorizontalAlignment = sent ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Background = sent ? SampleTheme.SentBubbleBrush : SampleTheme.ReceiveBubbleBrush,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(PaddingX, PaddingY, PaddingX, PaddingY),
            MaxWidth = maxWidth,
        };

        if (explicitWidth)
        {
            border.Width = desiredWidth;
        }

        border.Child = new TextBlock
        {
            Text = text,
            Foreground = SampleTheme.WhiteBrush,
            FontSize = 15,
            Width = explicitWidth ? textWidth : double.NaN,
            MaxWidth = explicitWidth ? double.PositiveInfinity : maxWidth - PaddingX * 2,
            TextWrapping = TextWrapping.WrapWholeWords,
        };
        return border;
    }
}
