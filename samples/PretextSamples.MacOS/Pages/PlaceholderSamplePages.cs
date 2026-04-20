namespace PretextSamples.MacOS;

internal abstract class PlaceholderPageView(string title, string description) : SamplePageView
{
    private readonly string _title = title;
    private readonly string _description = description;

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var width = Math.Max(720, availableSize.Width);
        var headerHeight = MacTheme.MeasureHeaderHeight((nfloat)width, "NATIVE", _title, _description);
        return new CGSize(width, headerHeight + 220);
    }

    protected override void LayoutPage(CGRect bounds)
    {
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        var header = MacTheme.DrawHeader(Bounds, "NATIVE", _title, _description);
        var card = new CGRect(MacTheme.PageMargin, header.Bottom + 24, Math.Min(720, Bounds.Width - MacTheme.PageMargin * 2), 132);
        MacTheme.FillRoundedRect(card, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString(
            "This native macOS sample page is being implemented in this host so the AppKit shell compiles while the scenario-specific rendering code lands.",
            new CGRect(card.X + 20, card.Y + 20, card.Width - 40, card.Height - 40),
            MacTheme.CreateAttributes(MacTheme.Sans(15), MacTheme.MutedBrush, 22));
    }
}
