namespace PretextSamples.MacOS;

internal sealed class AccordionPageView : SamplePageView
{
    private const string BodyFont = "16px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const double LineHeight = 26;
    private static readonly nfloat HeaderHeight = 56;
    private static readonly nfloat BodyPaddingX = 20;
    private static readonly nfloat BodyPaddingBottom = 18;

    private readonly PreparedText[] _prepared = AccordionSampleData.Items.Select(static item => PretextLayout.Prepare(item.Text, BodyFont)).ToArray();
    private readonly List<CGRect> _headerRects = [];
    private readonly List<nfloat> _bodyHeights = [];
    private int _openItemIndex;
    private nfloat _cardWidth;
    private nfloat _headerBottom;

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(760), availableSize.Width);
        var headerHeight = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Finally sane accordion", "The section heights are predicted by Pretext first, then the accordion opens to those measurements without reading the visible text tree.");
        _cardWidth = MacTheme.Min(MacTheme.N(920), contentWidth - MacTheme.PageMargin * 2);
        var textWidth = Math.Max(220, (double)(_cardWidth - BodyPaddingX * 2));
        _bodyHeights.Clear();
        foreach (var prepared in _prepared)
        {
            var metrics = PretextLayout.Layout(prepared, textWidth, LineHeight);
            _bodyHeights.Add(MacTheme.N(metrics.Height) + BodyPaddingBottom);
        }

        var totalHeight = headerHeight + 24 + HeaderHeight * AccordionSampleData.Items.Count;
        if (_openItemIndex >= 0 && _openItemIndex < _bodyHeights.Count)
        {
            totalHeight += _bodyHeights[_openItemIndex];
        }

        return new CGSize(contentWidth, totalHeight + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
        _headerBottom = MacTheme.MeasureHeaderHeight(bounds.Width, "DEMO", "Finally sane accordion", "The section heights are predicted by Pretext first, then the accordion opens to those measurements without reading the visible text tree.");
        _headerRects.Clear();
        var y = _headerBottom + 24;
        for (var index = 0; index < AccordionSampleData.Items.Count; index++)
        {
            _headerRects.Add(new CGRect(MacTheme.PageMargin, y, _cardWidth, HeaderHeight));
            y += HeaderHeight;
            if (index == _openItemIndex)
            {
                y += _bodyHeights[index];
            }
        }
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Finally sane accordion", "The section heights are predicted by Pretext first, then the accordion opens to those measurements without reading the visible text tree.");

        if (_headerRects.Count == 0)
        {
            return;
        }

        var cardHeight = _headerRects[^1].Bottom - _headerRects[0].Top + (_openItemIndex >= 0 && _openItemIndex < _bodyHeights.Count ? _bodyHeights[_openItemIndex] : 0);
        var cardRect = new CGRect(MacTheme.PageMargin, _headerRects[0].Y, _cardWidth, cardHeight);
        MacTheme.FillRoundedRect(cardRect, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);

        var titleAttributes = MacTheme.CreateAttributes(MacTheme.Sans(17, bold: true), MacTheme.InkBrush);
        var metaAttributes = MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.MutedBrush);
        var bodyAttributes = MacTheme.CreateCssAttributes(BodyFont, MacTheme.InkBrush, MacTheme.N(LineHeight));
        var indicatorAttributes = MacTheme.CreateAttributes(MacTheme.Sans(14, bold: true), MacTheme.AccentBrush);

        nfloat currentY = cardRect.Y;
        for (var index = 0; index < AccordionSampleData.Items.Count; index++)
        {
            if (index > 0)
            {
                MacTheme.FillRect(new CGRect(cardRect.X + 1, currentY, cardRect.Width - 2, 1), MacTheme.RuleBrush);
            }

            var item = AccordionSampleData.Items[index];
            var headerRect = new CGRect(cardRect.X, currentY, cardRect.Width, HeaderHeight);
            var metrics = PretextLayout.Layout(_prepared[index], Math.Max(220, (double)(cardRect.Width - BodyPaddingX * 2)), LineHeight);
            MacTheme.DrawWrappedString(item.Title, new CGRect(headerRect.X + 20, headerRect.Y + 17, headerRect.Width - 180, 22), titleAttributes);
            MacTheme.DrawWrappedString($"Measurement: {metrics.LineCount} lines · {Math.Round(metrics.Height)}px", new CGRect(headerRect.Right - 176, headerRect.Y + 19, 132, 18), metaAttributes);
            MacTheme.DrawWrappedString(index == _openItemIndex ? "▾" : "▸", new CGRect(headerRect.Right - 30, headerRect.Y + 17, 18, 18), indicatorAttributes);

            currentY += HeaderHeight;
            if (index != _openItemIndex)
            {
                continue;
            }

            var bodyRect = new CGRect(cardRect.X + BodyPaddingX, currentY, cardRect.Width - BodyPaddingX * 2, _bodyHeights[index] - BodyPaddingBottom);
            MacTheme.DrawWrappedString(item.Text, bodyRect, bodyAttributes);
            currentY += _bodyHeights[index];
        }
    }

    public override void MouseDown(NSEvent theEvent)
    {
        base.MouseDown(theEvent);
        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        for (var index = 0; index < _headerRects.Count; index++)
        {
            if (!_headerRects[index].Contains(point))
            {
                continue;
            }

            _openItemIndex = _openItemIndex == index ? -1 : index;
            InvalidatePageLayout();
            break;
        }
    }
}
