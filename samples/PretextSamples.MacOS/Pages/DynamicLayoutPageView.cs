namespace PretextSamples.MacOS;

internal sealed class DynamicLayoutPageView : SamplePageView
{
    private const string BodyFont = "20px \"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";
    private readonly PreparedTextWithSegments _preparedBody = PretextLayout.PrepareWithSegments(SampleTextData.DynamicBodyCopy, BodyFont);
    private CGRect _stageRect;
    private CGRect _openAiRect;
    private CGRect _claudeRect;
    private bool _swapBadges;
    private nfloat _headerBottom;

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(980), availableSize.Width);
        _headerBottom = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Situational awareness: the decade ahead", "A responsive editorial spread laid out in C#. Click the native badges to perturb the obstacle geometry and reflow the body copy.");
        _stageRect = new CGRect(MacTheme.PageMargin, _headerBottom + 18, contentWidth - MacTheme.PageMargin * 2, 720);
        var badgeY = _stageRect.Y + 40;
        if (_swapBadges)
        {
            _openAiRect = new CGRect(_stageRect.X + 40, badgeY, 108, 36);
            _claudeRect = new CGRect(_stageRect.Right - 148, badgeY, 108, 36);
        }
        else
        {
            _openAiRect = new CGRect(_stageRect.Right - 148, badgeY, 108, 36);
            _claudeRect = new CGRect(_stageRect.X + 40, badgeY, 108, 36);
        }

        return new CGSize(contentWidth, _stageRect.Bottom + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Situational awareness: the decade ahead", "A responsive editorial spread laid out in C#. Click the native badges to perturb the obstacle geometry and reflow the body copy.");

        MacTheme.FillRoundedRect(_stageRect, 28, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.FillRoundedRect(new CGRect(_stageRect.X + 18, _stageRect.Y + 18, _stageRect.Width - 36, _stageRect.Height - 36), 22, MacTheme.Color(0xF3, 0xEE, 0xE4));

        var titleRect = new CGRect(_stageRect.X + 40, _stageRect.Y + 24, _stageRect.Width * 0.58, 92);
        MacTheme.DrawWrappedString("SITUATIONAL AWARENESS: THE DECADE AHEAD", titleRect, MacTheme.CreateAttributes(MacTheme.Serif(34, bold: true), MacTheme.InkBrush, MacTheme.N(40)));
        MacTheme.DrawWrappedString("LEOPOLD ASCHENBRENNER", new CGRect(_stageRect.X + 40, _stageRect.Y + 126, 220, 16), MacTheme.CreateAttributes(MacTheme.Mono(11), MacTheme.MutedBrush));

        DrawBadge(_openAiRect, "OpenAI", MacTheme.Color(0x20, 0x1B, 0x18), MacTheme.Color(0xF0, 0xE4, 0xDA));
        DrawBadge(_claudeRect, "Claude", MacTheme.Color(0x95, 0x5F, 0x3B), MacTheme.Color(0xF8, 0xF2, 0xEA));

        var columns = BuildColumns();
        var obstacles = new[]
        {
            new RectObstacle(_openAiRect.X - _stageRect.X, _openAiRect.Y - _stageRect.Y, _openAiRect.Width, _openAiRect.Height),
            new RectObstacle(_claudeRect.X - _stageRect.X, _claudeRect.Y - _stageRect.Y, _claudeRect.Width, _claudeRect.Height),
        };
        var lines = ColumnFlowLayout.LayoutIntoColumns(_preparedBody, columns, obstacles, Array.Empty<CircleObstacle>(), lineHeight: 32, minSlotWidth: 120);
        var bodyAttrs = MacTheme.CreateCssAttributes(BodyFont, MacTheme.InkBrush, 32);
        foreach (var line in lines)
        {
            MacTheme.DrawWrappedString(line.Text, new CGRect(_stageRect.X + MacTheme.N(line.X), _stageRect.Y + MacTheme.N(line.Y), MacTheme.N(line.Width + 2), 32), bodyAttrs);
        }
    }

    public override void MouseDown(NSEvent theEvent)
    {
        base.MouseDown(theEvent);
        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        if (_openAiRect.Contains(point) || _claudeRect.Contains(point))
        {
            _swapBadges = !_swapBadges;
            InvalidatePageLayout();
        }
    }

    private RectObstacle[] BuildColumns()
    {
        var top = 174d;
        var leftWidth = _stageRect.Width < 860 ? _stageRect.Width - 80 : (_stageRect.Width - 112) / 2;
        if (_stageRect.Width < 860)
        {
            return
            [
                new RectObstacle(40, top, _stageRect.Width - 80, _stageRect.Height - top - 28),
            ];
        }

        return
        [
            new RectObstacle(40, top, leftWidth, _stageRect.Height - top - 28),
            new RectObstacle(56 + leftWidth, top, leftWidth, _stageRect.Height - top - 28),
        ];
    }

    private static void DrawBadge(CGRect rect, string text, NSColor foreground, NSColor background)
    {
        MacTheme.FillRoundedRect(rect, 999, background, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString(text, new CGRect(rect.X + 18, rect.Y + 9, rect.Width - 36, 18), MacTheme.CreateAttributes(MacTheme.Sans(12, bold: true), foreground, alignment: NSTextAlignment.Center));
    }
}
