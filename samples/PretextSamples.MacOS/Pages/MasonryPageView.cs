namespace PretextSamples.MacOS;

internal sealed class MasonryPageView : SamplePageView
{
    private const string CardFont = "15px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const double LineHeight = 22;
    private const double CardBorderThickness = 1;
    private const double CardPadding = 16;
    private const double Gap = 12;
    private const double MaxColumnWidth = 400;

    private readonly List<(string Text, PreparedText Prepared)> _cards = MasonrySampleData.LoadCards()
        .Select(text => (text, PretextLayout.Prepare(text, CardFont)))
        .ToList();

    private MasonryLayoutState? _state;
    private nfloat _headerBottom;

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(760), availableSize.Width);
        _headerBottom = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Masonry without DOM reads", "The grid places cards using predicted heights from the shared layout engine. There is no measurement pass over the live card tree.");
        _state = ComputeLayout(contentWidth - MacTheme.PageMargin * 2);
        return new CGSize(contentWidth, _headerBottom + 24 + _state.ContentHeight + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Masonry without DOM reads", "The grid places cards using predicted heights from the shared layout engine. There is no measurement pass over the live card tree.");

        if (_state is null)
        {
            return;
        }

        MacTheme.DrawWrappedString($"Showing {_cards.Count} cards", new CGRect(MacTheme.PageMargin, _headerBottom + 2, 220, 18), MacTheme.CreateAttributes(MacTheme.Sans(15), MacTheme.MutedBrush));
        var textAttributes = MacTheme.CreateCssAttributes(CardFont, MacTheme.InkBrush, MacTheme.N(LineHeight));

        foreach (var card in _state.PositionedCards)
        {
            var cardRect = new CGRect(MacTheme.PageMargin + card.X, _headerBottom + 24 + card.Y, _state.ColumnWidth, card.Height);
            if (!dirtyRect.IntersectsWith(cardRect))
            {
                continue;
            }

            MacTheme.FillRoundedRect(cardRect, 18, MacTheme.PanelBrush, MacTheme.RuleBrush);
            var textRect = new CGRect(cardRect.X + MacTheme.N(CardPadding), cardRect.Y + MacTheme.N(CardPadding), cardRect.Width - MacTheme.N((CardPadding + CardBorderThickness) * 2), cardRect.Height - MacTheme.N((CardPadding + CardBorderThickness) * 2));
            MacTheme.DrawWrappedString(_cards[card.CardIndex].Text, textRect, textAttributes);
        }
    }

    private MasonryLayoutState ComputeLayout(nfloat availableWidth)
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

        var textWidth = Math.Max(80, columnWidth - (CardPadding + CardBorderThickness) * 2);
        var contentWidth = columnCount * columnWidth + (columnCount - 1) * Gap;
        var offsetLeft = (availableWidth - contentWidth) / 2;
        var columnHeights = Enumerable.Repeat(Gap, columnCount).ToArray();
        var positionedCards = new List<PositionedCard>(_cards.Count);

        for (var index = 0; index < _cards.Count; index++)
        {
            var targetColumn = 0;
            for (var column = 1; column < columnCount; column++)
            {
                if (columnHeights[column] < columnHeights[targetColumn])
                {
                    targetColumn = column;
                }
            }

            var metrics = PretextLayout.Layout(_cards[index].Prepared, textWidth, LineHeight);
            var totalHeight = metrics.Height + (CardPadding + CardBorderThickness) * 2;
            var x = offsetLeft + targetColumn * (columnWidth + Gap);
            var y = columnHeights[targetColumn];
            columnHeights[targetColumn] += totalHeight + Gap;
            positionedCards.Add(new PositionedCard(index, MacTheme.N(x), MacTheme.N(y), MacTheme.N(totalHeight)));
        }

        return new MasonryLayoutState(MacTheme.N(columnWidth), MacTheme.N(columnHeights.Max() + Gap), positionedCards);
    }

    private readonly record struct PositionedCard(int CardIndex, nfloat X, nfloat Y, nfloat Height);

    private sealed record MasonryLayoutState(nfloat ColumnWidth, nfloat ContentHeight, IReadOnlyList<PositionedCard> PositionedCards);
}
