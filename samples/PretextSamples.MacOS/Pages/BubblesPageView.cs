namespace PretextSamples.MacOS;

internal sealed class BubblesPageView : SamplePageView
{
    private const string Font = "15px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const double LineHeight = 20;
    private static readonly nfloat PaddingX = 12;
    private static readonly nfloat PaddingY = 8;
    private const double BubbleMaxRatio = 0.8;
    private static readonly nfloat PanelGap = 16;

    private readonly (bool Sent, string Text, PreparedTextWithSegments Prepared)[] _messages =
        BubblesSampleData.Messages
            .Select(static message => (message.Sent, message.Text, PretextLayout.PrepareWithSegments(message.Text, Font)))
            .ToArray();

    private readonly NSSlider _slider = MacTheme.CreateSlider(220, 760, 340);
    private BubblePageState? _state;
    private nfloat _headerBottom;

    public BubblesPageView()
    {
        AddSubview(_slider);
        _slider.Activated += (_, _) => InvalidatePageLayout();
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(960), availableSize.Width);
        _headerBottom = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Shrinkwrap showdown", "The left column sizes bubbles like fit-content. The right column binary-searches the tightest width that preserves the same line count.");
        _state = BuildState(contentWidth);
        return new CGSize(contentWidth, _state.TotalHeight);
    }

    protected override void LayoutPage(CGRect bounds)
    {
        if (_state is null)
        {
            return;
        }

        _slider.Frame = _state.SliderFrame;
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Shrinkwrap showdown", "The left column sizes bubbles like fit-content. The right column binary-searches the tightest width that preserves the same line count.");

        if (_state is null)
        {
            return;
        }

        DrawControls();
        DrawBubbleColumn(_state.CssColumn, "CSS fit-content", "Sizes to the widest wrapped line, which leaves dead space behind shorter lines.", _state.CssWasteText);
        DrawBubbleColumn(_state.TightColumn, "Pretext shrinkwrap", "Walks line ranges and reuses the line count to find the smallest width with the same wraps.", "0");

        MacTheme.FillRoundedRect(_state.WhyCardRect, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString("Why can't CSS do this?", new CGRect(_state.WhyCardRect.X + 18, _state.WhyCardRect.Y + 16, _state.WhyCardRect.Width - 36, 24), MacTheme.CreateAttributes(MacTheme.Serif(20, bold: true), MacTheme.InkBrush));
        MacTheme.DrawWrappedString(
            "CSS can give you fit-content, which is the width of the widest wrapped line after layout. It cannot search for the narrowest width that preserves the same line count. Pretext can, because it measures the prepared text at multiple candidate widths and compares the resulting wraps without reading the visible tree.",
            new CGRect(_state.WhyCardRect.X + 18, _state.WhyCardRect.Y + 48, _state.WhyCardRect.Width - 36, _state.WhyCardRect.Height - 60),
            MacTheme.CreateAttributes(MacTheme.Sans(15), MacTheme.MutedBrush, MacTheme.N(22)));
    }

    private void DrawControls()
    {
        if (_state is null)
        {
            return;
        }

        MacTheme.FillRoundedRect(_state.ControlsRect, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString("Container width", new CGRect(_state.ControlsRect.X + 16, _state.ControlsRect.Y + 18, 140, 18), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.MutedBrush));
        MacTheme.DrawWrappedString(_state.WidthText, new CGRect(_state.ControlsRect.Right - 80, _state.ControlsRect.Y + 18, 64, 18), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.AccentBrush));
    }

    private void DrawBubbleColumn(BubbleColumnState column, string title, string description, string wasteText)
    {
        MacTheme.FillRoundedRect(column.CardRect, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString(title, new CGRect(column.CardRect.X + 18, column.CardRect.Y + 16, column.CardRect.Width - 36, 22), MacTheme.CreateAttributes(MacTheme.Sans(18, bold: true), MacTheme.InkBrush));
        MacTheme.DrawWrappedString(description, new CGRect(column.CardRect.X + 18, column.CardRect.Y + 44, column.CardRect.Width - 36, 42), MacTheme.CreateAttributes(MacTheme.Sans(15), MacTheme.MutedBrush, MacTheme.N(22)));

        var metricRect = new CGRect(column.CardRect.X + 18, column.CardRect.Y + 94, column.CardRect.Width - 36, 34);
        MacTheme.FillRoundedRect(metricRect, 10, MacTheme.AccentSoftBrush);
        MacTheme.DrawWrappedString("Wasted pixels:", new CGRect(metricRect.X + 10, metricRect.Y + 8, 110, 18), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.InkBrush));
        MacTheme.DrawWrappedString(wasteText, new CGRect(metricRect.Right - 78, metricRect.Y + 8, 68, 18), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.InkBrush, alignment: NSTextAlignment.Right));

        MacTheme.FillRoundedRect(column.ChatRect, 14, MacTheme.ChatBackgroundBrush);
        var messageAttributes = MacTheme.CreateCssAttributes(Font, MacTheme.WhiteBrush, MacTheme.N(LineHeight));
        foreach (var bubble in column.Bubbles)
        {
            MacTheme.FillRoundedRect(bubble.BubbleRect, 16, bubble.Sent ? MacTheme.SentBubbleBrush : MacTheme.ReceiveBubbleBrush);
            MacTheme.DrawWrappedString(bubble.Text, bubble.TextRect, messageAttributes);
        }
    }

    private BubblePageState BuildState(nfloat availableWidth)
    {
        var controlsTop = _headerBottom + 18;
        var controlsRect = new CGRect(MacTheme.PageMargin, controlsTop, Math.Min(MacTheme.N(720), availableWidth - MacTheme.PageMargin * 2), 62);
        var sliderFrame = new CGRect(controlsRect.X + 150, controlsRect.Y + 14, controlsRect.Width - 230, 24);

        var maxChatWidth = Math.Max(220, (double)(availableWidth - MacTheme.PageMargin * 2 - 32));
        var requestedWidth = _slider.DoubleValue;
        var chatWidth = Math.Min(requestedWidth, maxChatWidth);
        _slider.MaxValue = maxChatWidth;
        _slider.DoubleValue = chatWidth;

        var comparisonTop = controlsRect.Bottom + 18;
        var cardWidth = (availableWidth - MacTheme.PageMargin * 2 - PanelGap) / 2;
        var bubbleMaxWidth = Math.Floor(chatWidth * BubbleMaxRatio);
        var contentMaxWidth = Math.Max(1, bubbleMaxWidth - PaddingX * 2);

        var cssColumn = BuildBubbleColumnState(new CGRect(MacTheme.PageMargin, comparisonTop, cardWidth, 0), contentMaxWidth, bubbleMaxWidth, tight: false);
        var tightColumn = BuildBubbleColumnState(new CGRect(MacTheme.PageMargin + cardWidth + PanelGap, comparisonTop, cardWidth, 0), contentMaxWidth, bubbleMaxWidth, tight: true);
        var whyCardTop = Math.Max(cssColumn.CardRect.Bottom, tightColumn.CardRect.Bottom) + 18;
        var whyRect = new CGRect(MacTheme.PageMargin, whyCardTop, Math.Min(MacTheme.N(920), availableWidth - MacTheme.PageMargin * 2), 140);
        return new BubblePageState(
            controlsRect,
            sliderFrame,
            cssColumn,
            tightColumn,
            whyRect,
            MacTheme.FormatPixels(chatWidth),
            Math.Round(cssColumn.TotalWaste).ToString("N0"),
            whyRect.Bottom + MacTheme.PageMargin);
    }

    private BubbleColumnState BuildBubbleColumnState(CGRect cardRect, double contentMaxWidth, double bubbleMaxWidth, bool tight)
    {
        var chatRect = new CGRect(cardRect.X + 18, cardRect.Y + 140, cardRect.Width - 36, 0);
        var bubbles = new List<BubbleVisual>(_messages.Length);
        var y = chatRect.Y + 16;
        var totalWaste = 0d;

        foreach (var message in _messages)
        {
            var cssMetrics = PreparedTextMetrics.CollectWrapMetrics(message.Prepared, contentMaxWidth, LineHeight);
            var selectedMetrics = tight ? PreparedTextMetrics.FindTightWrapMetrics(message.Prepared, contentMaxWidth, LineHeight) : cssMetrics;
            var cssWidth = Math.Ceiling(cssMetrics.MaxLineWidth) + PaddingX * 2;
            var desiredWidth = Math.Ceiling(selectedMetrics.MaxLineWidth) + PaddingX * 2;
            totalWaste += Math.Max(0, cssWidth - desiredWidth) * (cssMetrics.Height + PaddingY * 2);

            var bubbleWidth = MacTheme.N(Math.Min(desiredWidth, bubbleMaxWidth));
            var bubbleHeight = MacTheme.N(selectedMetrics.Height) + PaddingY * 2;
            var bubbleX = message.Sent
                ? chatRect.Right - 16 - bubbleWidth
                : chatRect.X + 16;
            var bubbleRect = new CGRect(bubbleX, y, bubbleWidth, bubbleHeight);
            var textRect = new CGRect(bubbleRect.X + PaddingX, bubbleRect.Y + PaddingY - 2, bubbleRect.Width - PaddingX * 2, bubbleRect.Height - PaddingY * 2);
            bubbles.Add(new BubbleVisual(message.Text, bubbleRect, textRect, message.Sent));
            y += bubbleHeight + 8;
        }

        chatRect.Height = y - chatRect.Y + 8;
        cardRect.Height = chatRect.Bottom - cardRect.Y + 18;
        return new BubbleColumnState(cardRect, chatRect, bubbles, totalWaste);
    }

    private sealed record BubbleVisual(string Text, CGRect BubbleRect, CGRect TextRect, bool Sent);

    private sealed record BubbleColumnState(CGRect CardRect, CGRect ChatRect, IReadOnlyList<BubbleVisual> Bubbles, double TotalWaste);

    private sealed record BubblePageState(
        CGRect ControlsRect,
        CGRect SliderFrame,
        BubbleColumnState CssColumn,
        BubbleColumnState TightColumn,
        CGRect WhyCardRect,
        string WidthText,
        string CssWasteText,
        nfloat TotalHeight);
}
