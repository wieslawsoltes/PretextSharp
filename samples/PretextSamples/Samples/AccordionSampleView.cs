namespace PretextSamples.Samples;

public sealed class AccordionSampleView : UserControl
{
    private const string BodyFont = "16px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const double LineHeight = 26;
    private const double StackBodyPaddingX = 20;
    private const double StackBodyPaddingBottom = 18;

    private readonly (string Title, string Text)[] _items =
    {
        ("Section 1", "Mina cut the release note to three crisp lines, then realized the support caveat still needed one more sentence before it could ship without surprises."),
        ("Section 2", "The handoff doc now reads like a proper morning checklist instead of a diary entry. Restart the worker, verify the queue drains, and only then mark the incident quiet. If the backlog grows again, page the same owner instead of opening a new thread."),
        ("Section 3", "We learned the hard way that a giant native scroll range can dominate everything else. The bug looked like DOM churn, then like pooling, then like rendering pressure, until the repros were stripped down enough to show the real limit. That changed the fix completely: simplify the DOM, keep virtualization honest, and stop hiding the worst-case path behind caches that only make the common frame look cheaper."),
        ("Section 4", "AGI 春天到了. بدأت الرحلة 🚀 and the long URL is https://example.com/reports/q3?lang=ar&mode=full. Nora wrote “please keep 10 000 rows visible,” Mina replied “trans­atlantic labels are still weird.”"),
    };

    private readonly PreparedText[] _prepared;
    private readonly List<TextBlock> _metaBlocks = [];
    private readonly List<Border> _bodyHosts = [];
    private readonly List<RotateTransform> _glyphTransforms = [];
    private readonly List<double> _bodyHeights = [];
    private readonly Border _stackHost;
    private readonly FrameworkElement _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;
    private int _openItemIndex = 0;
    private double _lastMetricWidth = -1;

    public AccordionSampleView()
    {
        _prepared = _items.Select(item => PretextLayout.Prepare(item.Text, BodyFont)).ToArray();
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, UpdateMetrics);

        var stack = SampleUi.CreatePageStack();
        stack.Children.Add(SampleUi.CreateHeader(
            "DEMO",
            "Finally sane accordion",
            "The section heights are predicted by Pretext first, then the accordion opens to those measurements without reading the visible text tree."));

        var accordion = new StackPanel { Spacing = 0 };
        for (var i = 0; i < _items.Length; i++)
        {
            var titleBlock = new TextBlock
            {
                Text = _items[i].Title,
                Foreground = SampleTheme.InkBrush,
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.NoWrap,
            };

            var metaBlock = new TextBlock
            {
                Foreground = SampleTheme.MutedBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
            };
            _metaBlocks.Add(metaBlock);

            var glyphTransform = new RotateTransform { Angle = i == _openItemIndex ? 90 : 0, CenterX = 9, CenterY = 9 };
            _glyphTransforms.Add(glyphTransform);

            var glyph = new Grid
            {
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = glyphTransform,
            };
            glyph.Children.Add(new TextBlock
            {
                Text = "▶",
                Foreground = SampleTheme.AccentBrush,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.Children.Add(titleBlock);
            Grid.SetColumn(metaBlock, 1);
            headerGrid.Children.Add(metaBlock);
            Grid.SetColumn(glyph, 2);
            headerGrid.Children.Add(glyph);

            var button = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(20, 18, 20, 18),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Content = headerGrid,
            };
            var itemIndex = i;
            button.Click += (_, _) =>
            {
                _openItemIndex = _openItemIndex == itemIndex ? -1 : itemIndex;
                ApplyAccordionState();
            };

            var copyBlock = new TextBlock
            {
                Text = _items[i].Text,
                Foreground = SampleTheme.InkBrush,
                FontSize = 16,
                LineHeight = 26,
                TextWrapping = TextWrapping.WrapWholeWords,
            };

            var inner = new Border
            {
                Padding = new Thickness(StackBodyPaddingX, 0, StackBodyPaddingX, StackBodyPaddingBottom),
                Child = copyBlock,
            };

            var bodyHost = new Border
            {
                Height = i == _openItemIndex ? double.NaN : 0,
                VerticalAlignment = VerticalAlignment.Top,
                Child = inner,
            };
            _bodyHosts.Add(bodyHost);

            var itemStack = new StackPanel { Spacing = 0 };
            itemStack.Children.Add(button);
            itemStack.Children.Add(bodyHost);

            accordion.Children.Add(new Border
            {
                BorderBrush = SampleTheme.RuleBrush,
                BorderThickness = new Thickness(0, i == 0 ? 0 : 1, 0, 0),
                Child = itemStack,
            });
        }

        _stackHost = new Border
        {
            Background = SampleTheme.PanelBrush,
            BorderBrush = SampleTheme.RuleBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Child = accordion,
        };

        stack.Children.Add(_stackHost);
        _pageRoot = SampleUi.CreatePageRoot(stack);
        Content = _pageRoot;
        Loaded += (_, _) => _renderScheduler.Schedule();
        SizeChanged += (_, _) => _renderScheduler.Schedule();
    }

    private void UpdateMetrics()
    {
        var width = Math.Max(220, _stackHost.ActualWidth - StackBodyPaddingX * 2);
        if (Math.Abs(width - _lastMetricWidth) < 0.5)
        {
            return;
        }

        _lastMetricWidth = width;
        _bodyHeights.Clear();
        for (var i = 0; i < _prepared.Length; i++)
        {
            var metrics = PretextLayout.Layout(_prepared[i], width, LineHeight);
            _metaBlocks[i].Text = $"Measurement: {metrics.LineCount} lines · {Math.Round(metrics.Height)}px";
            _bodyHeights.Add(Math.Ceiling(metrics.Height + StackBodyPaddingBottom));
        }

        ApplyAccordionState();
    }

    private void ApplyAccordionState()
    {
        for (var i = 0; i < _bodyHosts.Count; i++)
        {
            _glyphTransforms[i].Angle = i == _openItemIndex ? 90 : 0;
            _bodyHosts[i].Height = i == _openItemIndex && i < _bodyHeights.Count ? _bodyHeights[i] : 0;
        }
    }
}
