namespace PretextSamples.MacOS;

internal sealed class EditorialEnginePageView : SamplePageView
{
    private const string BodyFont = "18px \"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";
    private const string PullquoteFont = "italic 19px \"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";

    private readonly PreparedTextWithSegments _preparedBody = PretextLayout.PrepareWithSegments(SampleTextData.EditorialBodyText, BodyFont);
    private readonly PreparedTextWithSegments[] _preparedPullquotes = SampleTextData.EditorialPullquotes.Select(text => PretextLayout.PrepareWithSegments(text, PullquoteFont)).ToArray();
    private readonly List<OrbState> _orbs =
    [
        new(0.52, 0.22, 86, 0.18, 0.12, MacTheme.Color(0xC4, 0xA3, 0x5A, 0xA0)),
        new(0.18, 0.48, 74, -0.16, 0.14, MacTheme.Color(0x64, 0x8C, 0xFF, 0x90)),
        new(0.74, 0.58, 82, 0.14, -0.16, MacTheme.Color(0xE8, 0x64, 0x82, 0x88)),
    ];

    private readonly NSTimer _timer;
    private CGRect _stageRect;
    private nfloat _headerBottom;
    private int _dragOrbIndex = -1;

    public EditorialEnginePageView()
    {
        _timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(24), _ =>
        {
            AdvanceOrbs();
            NeedsDisplay = true;
        });
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(1040), availableSize.Width);
        _headerBottom = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Editorial engine", "Animated orbs, pull quotes, and multi-column flow re-layout in real time with no live text-tree measurements.");
        _stageRect = new CGRect(MacTheme.PageMargin, _headerBottom + 18, contentWidth - MacTheme.PageMargin * 2, 780);
        return new CGSize(contentWidth, _stageRect.Bottom + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Editorial engine", "Animated orbs, pull quotes, and multi-column flow re-layout in real time with no live text-tree measurements.");

        MacTheme.FillRoundedRect(_stageRect, 28, MacTheme.Color(0x10, 0x10, 0x14), MacTheme.Color(0x20, 0x20, 0x24));
        MacTheme.DrawWrappedString("THE FUTURE OF TEXT LAYOUT IS NOT CSS", new CGRect(_stageRect.X + 36, _stageRect.Y + 24, _stageRect.Width - 72, 48), MacTheme.CreateAttributes(MacTheme.Serif(30, bold: true), MacTheme.WhiteBrush, 36));

        var pullquoteRects = new[]
        {
            new RectObstacle(_stageRect.Width * 0.06, 200, _stageRect.Width * 0.32, 112),
            new RectObstacle(_stageRect.Width * 0.62, 360, _stageRect.Width * 0.28, 100),
        };
        var columnWidth = (_stageRect.Width - 120) / 2;
        var columns = new[]
        {
            new RectObstacle(36, 120, columnWidth, _stageRect.Height - 156),
            new RectObstacle(36 + columnWidth + 48, 120, columnWidth, _stageRect.Height - 156),
        };
        var circles = _orbs.Select(orb => new CircleObstacle(orb.X * _stageRect.Width, orb.Y * _stageRect.Height, orb.Radius)).ToArray();
        var lines = ColumnFlowLayout.LayoutIntoColumns(_preparedBody, columns, pullquoteRects, circles, 30, 96);
        var bodyAttrs = MacTheme.CreateCssAttributes(BodyFont, MacTheme.WhiteBrush, 30);
        foreach (var line in lines)
        {
            MacTheme.DrawWrappedString(line.Text, new CGRect(_stageRect.X + MacTheme.N(line.X), _stageRect.Y + MacTheme.N(line.Y), MacTheme.N(line.Width + 2), 30), bodyAttrs);
        }

        for (var index = 0; index < pullquoteRects.Length; index++)
        {
            var rect = pullquoteRects[index];
            var screenRect = new CGRect(_stageRect.X + MacTheme.N(rect.X), _stageRect.Y + MacTheme.N(rect.Y), MacTheme.N(rect.Width), MacTheme.N(rect.Height));
            MacTheme.FillRoundedRect(screenRect, 18, MacTheme.Color(0x18, 0x18, 0x1E), MacTheme.Color(0x40, 0x40, 0x48));
            var quoteLines = PretextLayout.LayoutWithLines(_preparedPullquotes[index % _preparedPullquotes.Length], rect.Width - 28, 27);
            for (var lineIndex = 0; lineIndex < quoteLines.Lines.Count; lineIndex++)
            {
                var line = quoteLines.Lines[lineIndex];
                MacTheme.DrawWrappedString(line.Text, new CGRect(screenRect.X + 14, screenRect.Y + 14 + lineIndex * 27, screenRect.Width - 28, 24), MacTheme.CreateCssAttributes(PullquoteFont, MacTheme.Color(0xF5, 0xE7, 0xC8), 27));
            }
        }

        foreach (var orb in _orbs)
        {
            var rect = new CGRect(_stageRect.X + MacTheme.N(orb.X * _stageRect.Width - orb.Radius), _stageRect.Y + MacTheme.N(orb.Y * _stageRect.Height - orb.Radius), MacTheme.N(orb.Radius * 2), MacTheme.N(orb.Radius * 2));
            MacTheme.FillRoundedRect(rect, 999, orb.Color);
        }
    }

    public override void MouseDown(NSEvent theEvent)
    {
        base.MouseDown(theEvent);
        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        for (var index = 0; index < _orbs.Count; index++)
        {
            var orb = _orbs[index];
            var cx = _stageRect.X + MacTheme.N(orb.X * _stageRect.Width);
            var cy = _stageRect.Y + MacTheme.N(orb.Y * _stageRect.Height);
            if (Math.Sqrt(Math.Pow(point.X - cx, 2) + Math.Pow(point.Y - cy, 2)) <= orb.Radius)
            {
                _dragOrbIndex = index;
                break;
            }
        }
    }

    public override void MouseDragged(NSEvent theEvent)
    {
        base.MouseDragged(theEvent);
        if (_dragOrbIndex < 0)
        {
            return;
        }

        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        var orb = _orbs[_dragOrbIndex];
        _orbs[_dragOrbIndex] = orb with
        {
            X = Math.Clamp((point.X - _stageRect.X) / _stageRect.Width, 0.08, 0.92),
            Y = Math.Clamp((point.Y - _stageRect.Y) / _stageRect.Height, 0.12, 0.88),
        };
        NeedsDisplay = true;
    }

    public override void MouseUp(NSEvent theEvent)
    {
        base.MouseUp(theEvent);
        _dragOrbIndex = -1;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Invalidate();
        }

        base.Dispose(disposing);
    }

    private void AdvanceOrbs()
    {
        for (var index = 0; index < _orbs.Count; index++)
        {
            if (index == _dragOrbIndex)
            {
                continue;
            }

            var orb = _orbs[index];
            var x = orb.X + orb.Vx;
            var y = orb.Y + orb.Vy;
            var vx = orb.Vx;
            var vy = orb.Vy;
            if (x < 0.08 || x > 0.92)
            {
                vx = -vx;
                x = Math.Clamp(x, 0.08, 0.92);
            }

            if (y < 0.16 || y > 0.88)
            {
                vy = -vy;
                y = Math.Clamp(y, 0.16, 0.88);
            }

            _orbs[index] = orb with { X = x, Y = y, Vx = vx, Vy = vy };
        }
    }

    private readonly record struct OrbState(double X, double Y, double Radius, double Vx, double Vy, NSColor Color);
}
