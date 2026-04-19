namespace PretextSamples.MacOS;

internal sealed class OverviewPageView : SamplePageView
{
    private readonly List<CGRect> _cardRects = [];

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(680), availableSize.Width);
        var headerHeight = MacTheme.MeasureHeaderHeight(contentWidth, "PRETEXT", "Native macOS samples for manual text layout", "This host uses AppKit and the CoreText backend directly. The pages below mirror the Uno sample set while keeping rendering and layout native to macOS.");
        var cardWidth = MacTheme.Min(MacTheme.N(920), contentWidth - MacTheme.PageMargin * 2);
        nfloat bodyHeight = 0;
        var attributes = MacTheme.CreateAttributes(MacTheme.Sans(15), MacTheme.MutedBrush, MacTheme.N(22));
        foreach (var feature in SampleCatalog.OverviewFeatures)
        {
            var titleHeight = MacTheme.MeasureString(feature.Title, MacTheme.CreateAttributes(MacTheme.Sans(18, bold: true), MacTheme.InkBrush), cardWidth - MacTheme.N(32)).Height;
            var bodyTextHeight = MacTheme.MeasureString(feature.Summary, attributes, cardWidth - MacTheme.N(32)).Height;
            bodyHeight += 16 + titleHeight + 8 + bodyTextHeight + 16 + 16;
        }

        return new CGSize(contentWidth, headerHeight + 24 + bodyHeight + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
        _cardRects.Clear();
        var y = MacTheme.MeasureHeaderHeight(bounds.Width, "PRETEXT", "Native macOS samples for manual text layout", "This host uses AppKit and the CoreText backend directly. The pages below mirror the Uno sample set while keeping rendering and layout native to macOS.") + 24;
        var width = MacTheme.Min(MacTheme.N(920), bounds.Width - MacTheme.PageMargin * 2);
        var attrs = MacTheme.CreateAttributes(MacTheme.Sans(15), MacTheme.MutedBrush, MacTheme.N(22));
        foreach (var feature in SampleCatalog.OverviewFeatures)
        {
            var titleHeight = MacTheme.MeasureString(feature.Title, MacTheme.CreateAttributes(MacTheme.Sans(18, bold: true), MacTheme.InkBrush), width - MacTheme.N(32)).Height;
            var bodyHeight = MacTheme.MeasureString(feature.Summary, attrs, width - MacTheme.N(32)).Height;
            _cardRects.Add(new CGRect(MacTheme.PageMargin, y, width, 16 + titleHeight + 8 + bodyHeight + 16));
            y += _cardRects[^1].Height + 16;
        }
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "PRETEXT", "Native macOS samples for manual text layout", "This host uses AppKit and the CoreText backend directly. The pages below mirror the Uno sample set while keeping rendering and layout native to macOS.");

        var titleAttributes = MacTheme.CreateAttributes(MacTheme.Sans(18, bold: true), MacTheme.InkBrush);
        var bodyAttributes = MacTheme.CreateAttributes(MacTheme.Sans(15), MacTheme.MutedBrush, MacTheme.N(22));
        for (var index = 0; index < _cardRects.Count; index++)
        {
            var card = _cardRects[index];
            var feature = SampleCatalog.OverviewFeatures[index];
            MacTheme.FillRoundedRect(card, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);
            MacTheme.DrawWrappedString(feature.Title, new CGRect(card.X + 16, card.Y + 16, card.Width - 32, 22), titleAttributes);
            MacTheme.DrawWrappedString(feature.Summary, new CGRect(card.X + 16, card.Y + 44, card.Width - 32, card.Height - 60), bodyAttributes);
        }
    }
}
