namespace PretextSamples.Samples;

public sealed class JustificationComparisonSampleView : UserControl
{
    private readonly JustificationResources _resources = JustificationComparisonModel.CreateResources();
    private readonly StretchScrollHost _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;
    private readonly Slider _widthSlider;
    private readonly CheckBox _indicatorToggle;
    private readonly TextBlock _widthValue;
    private readonly Grid _columnsGrid;
    private readonly JustificationColumnCard _greedyCard;
    private readonly JustificationColumnCard _hyphenCard;
    private readonly JustificationColumnCard _optimalCard;

    public JustificationComparisonSampleView()
    {
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

        var controls = new StackPanel { Spacing = 12 };
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
            ColumnSpacing = JustificationComparisonModel.ColumnGap,
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

        var columnsScroller = new ScrollViewer
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
        stack.Children.Add(columnsScroller);

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
        var columnWidth = Math.Max(220, Math.Round(_widthSlider.Value));
        var showIndicators = _indicatorToggle.IsChecked == true;
        _widthValue.Text = $"{columnWidth:N0}px";
        _columnsGrid.Width = columnWidth * 3 + JustificationComparisonModel.ColumnGap * 2;

        var innerWidth = Math.Max(120, columnWidth - JustificationComparisonModel.Pad * 2);

        var greedy = JustificationComparisonModel.BuildGreedyFrame(
            _resources.BasePreparedParagraphs,
            innerWidth,
            _resources.NaturalSpaceWidth,
            _resources.HyphenWidth);
        var hyphen = JustificationComparisonModel.BuildGreedyFrame(
            _resources.HyphenatedPreparedParagraphs,
            innerWidth,
            _resources.NaturalSpaceWidth,
            _resources.HyphenWidth);
        var optimal = JustificationComparisonModel.BuildOptimalFrame(
            _resources.HyphenatedPreparedParagraphs,
            innerWidth,
            _resources.NaturalSpaceWidth,
            _resources.HyphenWidth);

        _greedyCard.Render(greedy.Paragraphs, columnWidth, _resources.NaturalSpaceWidth, showIndicators, greedy.Metrics);
        _hyphenCard.Render(hyphen.Paragraphs, columnWidth, _resources.NaturalSpaceWidth, showIndicators, hyphen.Metrics);
        _optimalCard.Render(optimal.Paragraphs, columnWidth, _resources.NaturalSpaceWidth, showIndicators, optimal.Metrics);
    }
}
