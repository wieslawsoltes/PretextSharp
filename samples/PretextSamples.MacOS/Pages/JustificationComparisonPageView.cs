namespace PretextSamples.MacOS;

internal sealed class JustificationComparisonPageView : SamplePageView
{
    private const string Eyebrow = "DEMO";
    private const string Title = "Justification Algorithms Compared";
    private const string DescriptionText = "Side-by-side comparison of greedy justification, greedy hyphenation, and optimal Knuth-Plass breaking. The sample visualizes rivers and spacing variance so the typography tradeoffs are obvious.";

    private readonly JustificationResources _resources = JustificationComparisonModel.CreateResources();
    private readonly NSSlider _widthSlider = MacTheme.CreateSlider(200, 600, 364);
    private readonly NSButton _indicatorToggle = MacTheme.CreateCheckBox("Toggle red visualizers", true);

    private CGRect _controlsRect;
    private CGRect _sliderFrame;
    private CGRect _toggleFrame;
    private JustificationPageState? _state;
    private nfloat _headerBottom;

    public JustificationComparisonPageView()
    {
        AddSubview(_widthSlider);
        AddSubview(_indicatorToggle);
        _widthSlider.Activated += (_, _) => InvalidatePageLayout();
        _indicatorToggle.Activated += (_, _) => NeedsDisplay = true;
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var viewportWidth = availableSize.Width;
        _headerBottom = MacTheme.MeasureHeaderHeight(viewportWidth, Eyebrow, Title, DescriptionText);
        _controlsRect = new CGRect(MacTheme.PageMargin, _headerBottom + 18, 320, 74);
        _sliderFrame = new CGRect(_controlsRect.X + 16, _controlsRect.Y + 26, 180, 24);
        _toggleFrame = new CGRect(_controlsRect.X + 16, _controlsRect.Y + 50, 220, 20);

        var columnWidth = Math.Max(220, Math.Round(_widthSlider.DoubleValue));
        var contentWidth = MacTheme.Max(viewportWidth, MacTheme.N(columnWidth * 3 + JustificationComparisonModel.ColumnGap * 2 + (double)MacTheme.PageMargin * 2));
        _state = BuildState(contentWidth, columnWidth, _indicatorToggle.State == NSCellStateValue.On);
        return new CGSize(contentWidth, _state.TotalHeight);
    }

    protected override void LayoutPage(CGRect bounds)
    {
        _widthSlider.Frame = _sliderFrame;
        _indicatorToggle.Frame = _toggleFrame;
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, Eyebrow, Title, DescriptionText);

        if (_state is null)
        {
            return;
        }

        MacTheme.FillRoundedRect(_controlsRect, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString("Column width", new CGRect(_controlsRect.X + 16, _controlsRect.Y + 10, 100, 14), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.MutedBrush));
        MacTheme.DrawWrappedString(MacTheme.FormatPixels(_state.ColumnWidth), new CGRect(_controlsRect.Right - 72, _controlsRect.Y + 10, 56, 14), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.AccentBrush));

        DrawColumn(_state.GreedyRect, "CSS / Greedy", "Browser-style line-by-line justification", _state.Greedy.Paragraphs, _state.Greedy.Metrics, _state.ShowIndicators);
        DrawColumn(_state.HyphenRect, "Pretext (Hyphenation)", "Greedy with syllable-level hyphenation", _state.Hyphen.Paragraphs, _state.Hyphen.Metrics, _state.ShowIndicators);
        DrawColumn(_state.OptimalRect, "Pretext (Knuth-Plass)", "Global line-breaking with syllable hyphenation", _state.Optimal.Paragraphs, _state.Optimal.Metrics, _state.ShowIndicators);
    }

    private JustificationPageState BuildState(nfloat contentWidth, double columnWidth, bool showIndicators)
    {
        var innerWidth = Math.Max(120, columnWidth - JustificationComparisonModel.Pad * 2);
        var greedy = JustificationComparisonModel.BuildGreedyFrame(_resources.BasePreparedParagraphs, innerWidth, _resources.NaturalSpaceWidth, _resources.HyphenWidth);
        var hyphen = JustificationComparisonModel.BuildGreedyFrame(_resources.HyphenatedPreparedParagraphs, innerWidth, _resources.NaturalSpaceWidth, _resources.HyphenWidth);
        var optimal = JustificationComparisonModel.BuildOptimalFrame(_resources.HyphenatedPreparedParagraphs, innerWidth, _resources.NaturalSpaceWidth, _resources.HyphenWidth);

        var top = _controlsRect.Bottom + 18;
        var height = MacTheme.Max(ColumnHeight(greedy.Paragraphs), MacTheme.Max(ColumnHeight(hyphen.Paragraphs), ColumnHeight(optimal.Paragraphs))) + 116;
        var greedyRect = new CGRect(MacTheme.PageMargin, top, columnWidth, height);
        var hyphenRect = new CGRect(greedyRect.Right + JustificationComparisonModel.ColumnGap, top, columnWidth, height);
        var optimalRect = new CGRect(hyphenRect.Right + JustificationComparisonModel.ColumnGap, top, columnWidth, height);

        return new JustificationPageState(greedyRect, hyphenRect, optimalRect, greedy, hyphen, optimal, showIndicators, columnWidth, top + height + MacTheme.PageMargin);
    }

    private static nfloat ColumnHeight(IReadOnlyList<IReadOnlyList<JustifiedLine>> paragraphs)
    {
        var lines = paragraphs.Sum(paragraph => paragraph.Count);
        var paragraphGaps = Math.Max(0, paragraphs.Count - 1) * JustificationComparisonModel.ParagraphGap;
        return MacTheme.N(lines * JustificationComparisonModel.BodyLineHeight + paragraphGaps + JustificationComparisonModel.Pad * 2);
    }

    private void DrawColumn(
        CGRect rect,
        string title,
        string subtitle,
        IReadOnlyList<IReadOnlyList<JustifiedLine>> paragraphs,
        QualityMetrics metrics,
        bool showIndicators)
    {
        MacTheme.DrawWrappedString(title, new CGRect(rect.X, rect.Y, rect.Width, 16), MacTheme.CreateAttributes(MacTheme.Mono(11), MacTheme.AccentBrush));
        MacTheme.DrawWrappedString(subtitle, new CGRect(rect.X, rect.Y + 18, rect.Width, 28), MacTheme.CreateAttributes(MacTheme.Sans(11), MacTheme.MutedBrush, 16));

        var textRect = new CGRect(rect.X, rect.Y + 52, rect.Width, ColumnHeight(paragraphs));
        MacTheme.FillRoundedRect(textRect, 4, MacTheme.WhiteBrush, MacTheme.Color(0xE8, 0xE0, 0xD4));
        DrawJustifiedParagraphs(textRect, paragraphs, showIndicators);

        var metricsRect = new CGRect(rect.X, textRect.Bottom + 8, rect.Width, 56);
        MacTheme.FillRoundedRect(metricsRect, 4, MacTheme.Color(0xF5, 0xF2, 0xED));
        DrawMetric(metricsRect, 0, "Lines", metrics.LineCount.ToString());
        DrawMetric(metricsRect, 1, "Avg deviation", $"{metrics.AvgDeviation * 100:0.0}%");
        DrawMetric(metricsRect, 2, "Max deviation", $"{metrics.MaxDeviation * 100:0.0}%");
        DrawMetric(metricsRect, 3, "River spaces", metrics.RiverCount.ToString());
    }

    private void DrawJustifiedParagraphs(CGRect rect, IReadOnlyList<IReadOnlyList<JustifiedLine>> paragraphs, bool showIndicators)
    {
        var textAttrs = MacTheme.CreateAttributes(MacTheme.Serif((nfloat)JustificationComparisonModel.BodyFontSize), MacTheme.InkBrush, MacTheme.N(JustificationComparisonModel.BodyLineHeight));
        var y = rect.Y + MacTheme.N(JustificationComparisonModel.Pad);

        foreach (var paragraph in paragraphs)
        {
            foreach (var line in paragraph)
            {
                var shouldJustify = !line.IsLast && line.LineWidth >= line.MaxWidth * 0.6;
                if (!shouldJustify)
                {
                    var ragged = string.Concat(line.Segments.Select(segment => segment.Text));
                    MacTheme.DrawWrappedString(ragged, new CGRect(rect.X + MacTheme.N(JustificationComparisonModel.Pad), y, rect.Width - MacTheme.N(JustificationComparisonModel.Pad * 2), MacTheme.N(JustificationComparisonModel.BodyLineHeight)), textAttrs);
                    y += MacTheme.N(JustificationComparisonModel.BodyLineHeight);
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

                var naturalSpace = _resources.NaturalSpaceWidth;
                var justifiedSpace = spaceCount > 0 ? Math.Max((line.MaxWidth - wordWidth) / spaceCount, naturalSpace * 0.75) : naturalSpace;
                var x = rect.X + MacTheme.N(JustificationComparisonModel.Pad);
                foreach (var segment in line.Segments)
                {
                    if (segment.IsSpace)
                    {
                        if (showIndicators && justifiedSpace > naturalSpace * 1.5)
                        {
                            MacTheme.FillRect(new CGRect(x + 1, y + 2, MacTheme.N(Math.Max(0, justifiedSpace - 2)), MacTheme.N(JustificationComparisonModel.BodyLineHeight - 4)), MacTheme.Color(220, 140, 140, 100));
                        }

                        x += MacTheme.N(justifiedSpace);
                    }
                    else
                    {
                        MacTheme.DrawWrappedString(segment.Text, new CGRect(x, y, 1000, MacTheme.N(JustificationComparisonModel.BodyLineHeight)), textAttrs);
                        x += MacTheme.N(segment.Width);
                    }
                }

                y += MacTheme.N(JustificationComparisonModel.BodyLineHeight);
            }

            y += MacTheme.N(JustificationComparisonModel.ParagraphGap);
        }
    }

    private static void DrawMetric(CGRect rect, int index, string label, string value)
    {
        var rowY = rect.Y + 8 + index % 2 * 22;
        var columnX = rect.X + 10 + (index / 2) * (rect.Width / 2);
        MacTheme.DrawWrappedString(label, new CGRect(columnX, rowY, rect.Width / 2 - 16, 14), MacTheme.CreateAttributes(MacTheme.Sans(11), MacTheme.MutedBrush));
        MacTheme.DrawWrappedString(value, new CGRect(columnX + rect.Width / 2 - 70, rowY, 60, 14), MacTheme.CreateAttributes(MacTheme.Sans(11, bold: true), MacTheme.AccentBrush, alignment: NSTextAlignment.Right));
    }

    private sealed record JustificationPageState(
        CGRect GreedyRect,
        CGRect HyphenRect,
        CGRect OptimalRect,
        (IReadOnlyList<IReadOnlyList<JustifiedLine>> Paragraphs, QualityMetrics Metrics) Greedy,
        (IReadOnlyList<IReadOnlyList<JustifiedLine>> Paragraphs, QualityMetrics Metrics) Hyphen,
        (IReadOnlyList<IReadOnlyList<JustifiedLine>> Paragraphs, QualityMetrics Metrics) Optimal,
        bool ShowIndicators,
        double ColumnWidth,
        nfloat TotalHeight);
}
