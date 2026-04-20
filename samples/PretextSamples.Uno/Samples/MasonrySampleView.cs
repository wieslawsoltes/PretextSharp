namespace PretextSamples.Samples;

public sealed class MasonrySampleView : UserControl
{
    private const string CardFont = "15px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const double LineHeight = 22;
    private const double CardBorderThickness = 1;
    private const double CardPadding = 16;
    private const double Gap = 12;
    private const double MaxColumnWidth = 400;
    private const double ViewportOverscan = 200;

    private readonly Canvas _canvas = new();
    private readonly TextBlock _status = SampleUi.CreateBodyText("Loading cards…");
    private List<(string Text, PreparedText Prepared)> _cards = [];
    private readonly List<Border> _cardPool = [];
    private readonly StretchScrollHost _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;
    private double _lastAvailableWidth = -1;
    private MasonryLayoutState? _layoutState;
    private bool _scrollViewerHooked;

    public MasonrySampleView()
    {
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);
        var stack = SampleUi.CreatePageStack();
        stack.Children.Add(SampleUi.CreateHeader(
            "DEMO",
            "Masonry without DOM reads",
            "The grid places cards using predicted heights from the shared layout engine. There is no measurement pass over the live card tree."));
        stack.Children.Add(_status);
        stack.Children.Add(SampleUi.CreateCard(_canvas, 0));

        _pageRoot = (StretchScrollHost)SampleUi.CreatePageRoot(stack);
        Content = _pageRoot;
        Loaded += (_, _) =>
        {
            EnsureCards();
            HookScrollViewer();
            _renderScheduler.Schedule();
        };
        SizeChanged += (_, _) => _renderScheduler.Schedule();
    }

    private void EnsureCards()
    {
        if (_cards.Count > 0)
        {
            return;
        }

        _cards = MasonrySampleData.LoadCards()
            .Select(text => (text, PretextLayout.Prepare(text, CardFont)))
            .ToList();
        _status.Text = $"Showing {_cards.Count} cards";
    }

    private void HookScrollViewer()
    {
        if (_scrollViewerHooked)
        {
            return;
        }

        _pageRoot.ScrollViewer.ViewChanged += (_, _) => _renderScheduler.Schedule();
        _scrollViewerHooked = true;
    }

    private void Render()
    {
        if (_cards.Count == 0 || _pageRoot.ActualWidth <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(360, _pageRoot.ActualWidth - 72);
        if (_layoutState is null || Math.Abs(availableWidth - _lastAvailableWidth) >= 0.5)
        {
            _lastAvailableWidth = availableWidth;
            _layoutState = ComputeLayout(availableWidth);
            _canvas.Width = availableWidth;
            _canvas.Height = _layoutState.ContentHeight;
        }

        var scrollViewer = _pageRoot.ScrollViewer;
        var viewportHeight = scrollViewer.ActualHeight > 0 ? scrollViewer.ActualHeight : _pageRoot.ActualHeight;
        var viewportTop = viewportHeight > 0 ? Math.Max(0, scrollViewer.VerticalOffset - ViewportOverscan) : 0;
        var viewportBottom = viewportHeight > 0
            ? scrollViewer.VerticalOffset + viewportHeight + ViewportOverscan
            : double.PositiveInfinity;
        var visibleCards = new List<PositionedCard>(_layoutState.PositionedCards.Count);
        foreach (var card in _layoutState.PositionedCards)
        {
            if (card.Y > viewportBottom || card.Y + card.Height < viewportTop)
            {
                continue;
            }

            visibleCards.Add(card);
        }

        _status.Text = $"Showing {_cards.Count} cards • {visibleCards.Count} visible";
        EnsureCardPool(visibleCards.Count);

        for (var index = 0; index < visibleCards.Count; index++)
        {
            var positioned = visibleCards[index];
            var (text, _) = _cards[positioned.CardIndex];
            var card = _cardPool[index];
            card.Width = _layoutState.ColumnWidth;
            card.Height = positioned.Height;
            if (card.Child is TextBlock textBlock)
            {
                textBlock.Text = text;
                textBlock.Width = GetTextWidth(_layoutState.ColumnWidth);
            }

            Canvas.SetLeft(card, positioned.X);
            Canvas.SetTop(card, positioned.Y);
        }
    }

    private MasonryLayoutState ComputeLayout(double availableWidth)
    {
        int columnCount;
        double columnWidth;
        if (availableWidth <= 520)
        {
            columnCount = 1;
            columnWidth = Math.Min(MaxColumnWidth, availableWidth - Gap * 2);
        }
        else
        {
            var minColumnWidth = 100 + availableWidth * 0.1;
            columnCount = Math.Max(2, (int)Math.Floor((availableWidth + Gap) / (minColumnWidth + Gap)));
            columnWidth = Math.Min(MaxColumnWidth, (availableWidth - (columnCount + 1) * Gap) / columnCount);
        }

        var textWidth = GetTextWidth(columnWidth);
        var contentWidth = columnCount * columnWidth + (columnCount - 1) * Gap;
        var offsetLeft = (availableWidth - contentWidth) / 2;
        var columnHeights = Enumerable.Repeat(Gap, columnCount).ToArray();
        var positionedCards = new List<PositionedCard>(_cards.Count);

        for (var index = 0; index < _cards.Count; index++)
        {
            var (text, prepared) = _cards[index];
            var targetColumn = 0;
            for (var c = 1; c < columnCount; c++)
            {
                if (columnHeights[c] < columnHeights[targetColumn])
                {
                    targetColumn = c;
                }
            }

            var metrics = PretextLayout.Layout(prepared, textWidth, LineHeight);
            var totalHeight = metrics.Height + (CardPadding + CardBorderThickness) * 2;
            var x = offsetLeft + targetColumn * (columnWidth + Gap);
            var y = columnHeights[targetColumn];
            columnHeights[targetColumn] += totalHeight + Gap;
            positionedCards.Add(new PositionedCard(index, x, y, totalHeight));
        }

        return new MasonryLayoutState(columnWidth, columnHeights.Max() + Gap, positionedCards);
    }

    private void EnsureCardPool(int count)
    {
        while (_cardPool.Count < count)
        {
            var card = new Border
            {
                Background = SampleTheme.PanelBrush,
                BorderBrush = SampleTheme.RuleBrush,
                BorderThickness = new Thickness(CardBorderThickness),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(CardPadding),
                Child = new TextBlock
                {
                    Foreground = SampleTheme.InkBrush,
                    FontSize = 15,
                    FontFamily = new FontFamily("Helvetica Neue"),
                    LineHeight = LineHeight,
                    TextWrapping = TextWrapping.WrapWholeWords,
                },
            };
            _cardPool.Add(card);
            _canvas.Children.Add(card);
        }

        for (var index = 0; index < _cardPool.Count; index++)
        {
            _cardPool[index].Visibility = index < count ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static double GetTextWidth(double columnWidth)
    {
        return Math.Max(80, columnWidth - (CardPadding + CardBorderThickness) * 2);
    }

    private readonly record struct PositionedCard(int CardIndex, double X, double Y, double Height);

    private sealed record MasonryLayoutState(double ColumnWidth, double ContentHeight, IReadOnlyList<PositionedCard> PositionedCards);
}
