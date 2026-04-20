namespace PretextSamples.MacOS;

internal sealed class DynamicLayoutPageView : SamplePageView
{
    private const string TitleText = "SITUATIONAL AWARENESS: THE DECADE AHEAD";
    private const string TitleFontFamily = "\"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";
    private const string BodyFont = "20px \"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";
    private const string CreditText = "LEOPOLD ASCHENBRENNER";
    private const string CreditFont = "12px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const int CreditLineHeight = 16;
    private const int BodyLineHeight = 32;
    private const int NarrowBreakpoint = 760;
    private const int NarrowColumnMaxWidth = 430;
    private const string HintText = "Everything laid out in C#. Resize horizontally and vertically, then click the logos.";

    private static readonly NSColor AtmosphereLeftInnerBrush = MacTheme.Color(45, 88, 128, 41);
    private static readonly NSColor AtmosphereLeftOuterBrush = MacTheme.Color(45, 88, 128, 0);
    private static readonly NSColor AtmosphereRightInnerBrush = MacTheme.Color(217, 119, 87, 46);
    private static readonly NSColor AtmosphereRightOuterBrush = MacTheme.Color(217, 119, 87, 0);
    private static readonly NSColor HintBackgroundBrush = MacTheme.Color(17, 16, 13, 240);
    private static readonly NSColor HintForegroundBrush = MacTheme.Color(245, 246, 240, 230);
    private static readonly NSColor CreditBrush = MacTheme.Color(17, 16, 13, 148);

    private readonly PreparedTextWithSegments _bodyPrepared = PretextLayout.PrepareWithSegments(SampleTextData.DynamicBodyCopy, BodyFont);
    private readonly Dictionary<string, PreparedTextWithSegments> _preparedCache = new(StringComparer.Ordinal);
    private readonly LogoView _openAiLogoHost;
    private readonly LogoView _claudeLogoHost;
    private readonly LogoAnimationState _openAiLogo = new();
    private readonly LogoAnimationState _claudeLogo = new();
    private readonly NSTimer _spinTimer;

    private PageLayout? _layout;
    private LayoutEvaluation? _evaluation;

    public DynamicLayoutPageView()
    {
        _openAiLogoHost = CreateLogoHost("openai_symbol", "png", "OpenAI", () => StartLogoSpin(_openAiLogo, 1));
        _claudeLogoHost = CreateLogoHost("claude_symbol", "png", "Claude", () => StartLogoSpin(_claudeLogo, -1));
        AddSubview(_openAiLogoHost);
        AddSubview(_claudeLogoHost);

        _spinTimer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(16), _ =>
        {
            if (!UpdateSpinState())
            {
                return;
            }

            ApplyLogoTransforms();
            NeedsDisplay = true;
        });
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var pageWidth = Math.Max(680, (double)availableSize.Width);
        var pageHeight = Math.Max(720, (double)availableSize.Height);
        _layout = BuildLayout(pageWidth, pageHeight, BodyLineHeight);
        _evaluation = EvaluateLayout(_layout);
        ApplyLogoTransforms();
        return new CGSize(MacTheme.N(pageWidth), MacTheme.N(_evaluation.ContentHeight));
    }

    protected override void LayoutPage(CGRect bounds)
    {
        ApplyLogoTransforms();
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);

        if (_layout is null || _evaluation is null)
        {
            return;
        }

        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        DrawAtmosphere(Bounds);
        DrawBody();
        DrawHeadline();
        DrawCredit();
        if (!_layout.IsNarrow)
        {
            DrawHintPill();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spinTimer.Invalidate();
        }

        base.Dispose(disposing);
    }

    private void DrawAtmosphere(CGRect bounds)
    {
        DrawRadialAtmosphere(bounds, new CGPoint(bounds.Width * 0.16, bounds.Height * 0.82), Math.Max(bounds.Width * 0.62, bounds.Height * 0.54), AtmosphereLeftInnerBrush, AtmosphereLeftOuterBrush);
        DrawRadialAtmosphere(bounds, new CGPoint(bounds.Width * 0.86, bounds.Height * 0.16), Math.Max(bounds.Width * 0.58, bounds.Height * 0.48), AtmosphereRightInnerBrush, AtmosphereRightOuterBrush);
    }

    private static void DrawRadialAtmosphere(CGRect bounds, CGPoint center, double radius, NSColor inner, NSColor outer)
    {
        using var gradient = new NSGradient(new[] { inner, outer }, new nfloat[] { 0, 1 });
        gradient.DrawFromCenterRadius(center, 0, center, MacTheme.N(radius), NSGradientDrawingOptions.AfterEndingLocation);
    }

    private void DrawHeadline()
    {
        if (_layout is null || _evaluation is null)
        {
            return;
        }

        var attributes = MacTheme.CreateCssAttributes(_layout.HeadlineFont, MacTheme.InkBrush, MacTheme.N(_layout.HeadlineLineHeight));
        foreach (var line in _evaluation.HeadlineLines)
        {
            MacTheme.DrawWrappedString(
                line.Text,
                new CGRect(MacTheme.N(line.X), MacTheme.N(line.Y), MacTheme.N(Math.Max(24, line.Width + 4)), MacTheme.N(_layout.HeadlineLineHeight)),
                attributes);
        }
    }

    private void DrawBody()
    {
        if (_evaluation is null)
        {
            return;
        }

        var bodyAttributes = MacTheme.CreateCssAttributes(BodyFont, MacTheme.InkBrush, MacTheme.N(BodyLineHeight));
        foreach (var line in _evaluation.LeftLines)
        {
            MacTheme.DrawWrappedString(
                line.Text,
                new CGRect(MacTheme.N(line.X), MacTheme.N(line.Y), MacTheme.N(Math.Max(24, line.Width + 4)), MacTheme.N(BodyLineHeight)),
                bodyAttributes);
        }

        foreach (var line in _evaluation.RightLines)
        {
            MacTheme.DrawWrappedString(
                line.Text,
                new CGRect(MacTheme.N(line.X), MacTheme.N(line.Y), MacTheme.N(Math.Max(24, line.Width + 4)), MacTheme.N(BodyLineHeight)),
                bodyAttributes);
        }
    }

    private void DrawCredit()
    {
        if (_evaluation is null)
        {
            return;
        }

        var attributes = MacTheme.CreateCssAttributes(CreditFont, CreditBrush, CreditLineHeight);
        var size = MacTheme.MeasureString(CreditText, attributes, 1000);
        MacTheme.DrawWrappedString(
            CreditText,
            new CGRect(MacTheme.N(_evaluation.CreditLeft), MacTheme.N(_evaluation.CreditTop), size.Width + 2, size.Height),
            attributes);
    }

    private void DrawHintPill()
    {
        var attributes = MacTheme.CreateAttributes(MacTheme.Sans(12), HintForegroundBrush);
        var size = MacTheme.MeasureString(HintText, attributes, MacTheme.N(Math.Max(320, Bounds.Width - 120)));
        var width = size.Width + 32;
        var height = Math.Max(34, size.Height + 18);
        var rect = new CGRect((Bounds.Width - width) / 2, 16, width, height);
        MacTheme.FillRoundedRect(rect, 20, HintBackgroundBrush);
        MacTheme.DrawWrappedString(HintText, new CGRect(rect.X + 16, rect.Y + (rect.Height - size.Height) / 2, rect.Width - 32, size.Height), attributes);
    }

    private void ApplyLogoTransforms()
    {
        if (_layout is null)
        {
            return;
        }

        _openAiLogoHost.Frame = ToRect(_layout.OpenAiRect);
        _openAiLogoHost.Angle = _openAiLogo.Angle;
        _openAiLogoHost.Hidden = false;
        _claudeLogoHost.Frame = ToRect(_layout.ClaudeRect);
        _claudeLogoHost.Angle = _claudeLogo.Angle;
        _claudeLogoHost.Hidden = false;
    }

    private static CGRect ToRect(RectObstacle rect)
    {
        return new CGRect(MacTheme.N(rect.X), MacTheme.N(rect.Y), MacTheme.N(rect.Width), MacTheme.N(rect.Height));
    }

    private LogoView CreateLogoHost(string resourceName, string extension, string label, Action onClick)
    {
        var path = NSBundle.MainBundle.PathForResource(resourceName, extension);
        var image = path is null ? null : new NSImage(path);
        return new LogoView(image, label, onClick);
    }

    private void StartLogoSpin(LogoAnimationState logo, int direction)
    {
        var delta = direction * Math.PI;
        var now = Environment.TickCount64;
        logo.Spin = new SpinState(logo.Angle, logo.Angle + delta, now, 900);
        NeedsDisplay = true;
    }

    private bool UpdateSpinState()
    {
        var now = Environment.TickCount64;
        var openAiAnimating = UpdateSpin(_openAiLogo, now);
        var claudeAnimating = UpdateSpin(_claudeLogo, now);
        return openAiAnimating || claudeAnimating;
    }

    private static bool UpdateSpin(LogoAnimationState logo, long now)
    {
        if (logo.Spin is null)
        {
            return false;
        }

        var spin = logo.Spin;
        var progress = Math.Clamp((now - spin.StartMs) / spin.DurationMs, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        logo.Angle = spin.From + (spin.To - spin.From) * eased;
        if (progress >= 1)
        {
            logo.Angle = spin.To;
            logo.Spin = null;
            return false;
        }

        return true;
    }

    private PreparedTextWithSegments GetPrepared(string text, string font)
    {
        var key = $"{font}::{text}";
        if (_preparedCache.TryGetValue(key, out var prepared))
        {
            return prepared;
        }

        prepared = PretextLayout.PrepareWithSegments(text, font);
        _preparedCache[key] = prepared;
        return prepared;
    }

    private bool HeadlineBreaksInsideWord(PreparedTextWithSegments prepared, double maxWidth)
    {
        var breaksInsideWord = false;
        PretextLayout.WalkLineRanges(prepared, maxWidth, line =>
        {
            if (line.End.GraphemeIndex != 0)
            {
                breaksInsideWord = true;
            }
        });
        return breaksInsideWord;
    }

    private double GetPreparedSingleLineWidth(PreparedTextWithSegments prepared)
    {
        var width = 0d;
        PretextLayout.WalkLineRanges(prepared, 100_000, line => width = line.Width);
        return width;
    }

    private double FitHeadlineFontSize(double headlineWidth, double pageWidth)
    {
        var low = Math.Ceiling(Math.Max(22, pageWidth * 0.026));
        var high = Math.Floor(Math.Min(94.4, Math.Max(55.2, pageWidth * 0.055)));
        var best = low;

        while (low <= high)
        {
            var size = Math.Floor((low + high) / 2);
            var font = $"700 {size}px {TitleFontFamily}";
            var headlinePrepared = GetPrepared(TitleText, font);
            if (!HeadlineBreaksInsideWord(headlinePrepared, headlineWidth))
            {
                best = size;
                low = size + 1;
            }
            else
            {
                high = size - 1;
            }
        }

        return best;
    }

    private PageLayout BuildLayout(double pageWidth, double pageHeight, double lineHeight)
    {
        var isNarrow = pageWidth < NarrowBreakpoint;
        if (isNarrow)
        {
            var gutter = Math.Round(Math.Max(18, Math.Min(28, pageWidth * 0.06)));
            var columnWidth = Math.Round(Math.Min(pageWidth - gutter * 2, NarrowColumnMaxWidth));
            var headlineWidth = pageWidth - gutter * 2;
            var headlineFontSize = Math.Min(48, FitHeadlineFontSize(headlineWidth, pageWidth));
            var headlineLineHeight = Math.Round(headlineFontSize * 0.92);
            var headlineFont = $"700 {headlineFontSize}px {TitleFontFamily}";
            var creditGap = Math.Round(Math.Max(12, lineHeight * 0.5));
            var copyGap = Math.Round(Math.Max(18, lineHeight * 0.7));
            var claudeSize = Math.Round(Math.Min(92, Math.Min(pageWidth * 0.23, pageHeight * 0.11)));
            var openAiSize = Math.Round(Math.Min(138, pageWidth * 0.34));

            return new PageLayout(
                true,
                gutter,
                pageWidth,
                pageHeight,
                0,
                columnWidth,
                new RectObstacle(gutter, 28, headlineWidth, Math.Max(320, pageHeight - 28 - gutter)),
                headlineFont,
                headlineFontSize,
                headlineLineHeight,
                creditGap,
                copyGap,
                new RectObstacle(gutter - Math.Round(openAiSize * 0.22), pageHeight - gutter - openAiSize + Math.Round(openAiSize * 0.08), openAiSize, openAiSize),
                new RectObstacle(pageWidth - gutter - Math.Round(claudeSize * 0.88), 4, claudeSize, claudeSize));
        }

        var wideGutter = Math.Round(Math.Max(52, pageWidth * 0.048));
        var centerGap = Math.Round(Math.Max(28, pageWidth * 0.025));
        var wideColumnWidth = Math.Round((pageWidth - wideGutter * 2 - centerGap) / 2);
        var headlineTop = Math.Round(Math.Max(42, Math.Max(pageWidth * 0.04, 72)));
        var wideHeadlineWidth = Math.Round(Math.Min(pageWidth - wideGutter * 2, Math.Max(wideColumnWidth, pageWidth * 0.5)));
        var wideHeadlineFontSize = FitHeadlineFontSize(wideHeadlineWidth, pageWidth);
        var wideHeadlineLineHeight = Math.Round(wideHeadlineFontSize * 0.92);
        var wideHeadlineFont = $"700 {wideHeadlineFontSize}px {TitleFontFamily}";
        var wideCreditGap = Math.Round(Math.Max(14, lineHeight * 0.6));
        var wideCopyGap = Math.Round(Math.Max(20, lineHeight * 0.9));
        var openAiShrinkT = Math.Max(0, Math.Min(1, (960 - pageWidth) / 260));
        var openAiSizeWide = Math.Round(Math.Min(400 - openAiShrinkT * 56, pageHeight * 0.43));
        var claudeSizeWide = Math.Round(Math.Max(276, Math.Min(500, Math.Min(pageWidth * 0.355, pageHeight * 0.45))));

        return new PageLayout(
            false,
            wideGutter,
            pageWidth,
            pageHeight,
            centerGap,
            wideColumnWidth,
            new RectObstacle(wideGutter, headlineTop, wideHeadlineWidth, pageHeight - headlineTop - wideGutter),
            wideHeadlineFont,
            wideHeadlineFontSize,
            wideHeadlineLineHeight,
            wideCreditGap,
            wideCopyGap,
            new RectObstacle(wideGutter - Math.Round(openAiSizeWide * 0.3), pageHeight - wideGutter - openAiSizeWide + Math.Round(openAiSizeWide * 0.2), openAiSizeWide, openAiSizeWide),
            new RectObstacle(pageWidth - Math.Round(claudeSizeWide * 0.69), -Math.Round(claudeSizeWide * 0.22), claudeSizeWide, claudeSizeWide));
    }

    private LayoutEvaluation EvaluateLayout(PageLayout layout)
    {
        var headlinePrepared = GetPrepared(TitleText, layout.HeadlineFont);
        var openAiObstacle = new EllipseBandObstacle(
            layout.OpenAiRect.X + layout.OpenAiRect.Width / 2,
            layout.OpenAiRect.Y + layout.OpenAiRect.Height / 2,
            layout.OpenAiRect.Width / 2,
            layout.OpenAiRect.Height / 2,
            Math.Round(BodyLineHeight * 0.82),
            Math.Round(BodyLineHeight * 0.26));
        var claudeObstacle = new RectBandObstacle(
            [layout.ClaudeRect],
            Math.Round(BodyLineHeight * 0.28),
            Math.Round(BodyLineHeight * 0.12));

        var headlineResult = LayoutColumn(
            headlinePrepared,
            new LayoutCursor(0, 0),
            layout.HeadlineRegion,
            layout.HeadlineLineHeight,
            [openAiObstacle],
            preferRightOnTie: false);
        var headlineLines = headlineResult.Lines;
        var headlineRects = headlineLines
            .Select(line => new RectObstacle(line.X, line.Y, Math.Ceiling(line.Width), layout.HeadlineLineHeight))
            .ToArray();
        var headlineBottom = headlineLines.Count == 0
            ? layout.HeadlineRegion.Y
            : headlineLines.Max(line => line.Y + layout.HeadlineLineHeight);
        var creditTop = headlineBottom + layout.CreditGap;
        var creditRegion = new RectObstacle(layout.Gutter + 4, creditTop, layout.HeadlineRegion.Width, CreditLineHeight);
        var copyTop = creditTop + CreditLineHeight + layout.CopyGap;
        var titleObstacle = new RectBandObstacle(headlineRects, Math.Round(BodyLineHeight * 0.95), Math.Round(BodyLineHeight * 0.3));
        var creditBlocked = GetObstacleIntervals(openAiObstacle, creditRegion.Y, creditRegion.Bottom);
        var claudeCreditBlocked = GetObstacleIntervals(claudeObstacle, creditRegion.Y, creditRegion.Bottom);
        var creditSlots = ObstacleLayoutHelper.CarveTextLineSlots(
            new Interval(creditRegion.X, creditRegion.Right),
            layout.IsNarrow ? creditBlocked.Concat(claudeCreditBlocked) : creditBlocked);
        var creditWidth = GetPreparedSingleLineWidth(GetPrepared(CreditText, CreditFont));
        var creditLeft = creditRegion.X;
        foreach (var slot in creditSlots)
        {
            if (slot.Width >= creditWidth)
            {
                creditLeft = Math.Round(slot.Left);
                break;
            }
        }

        if (layout.IsNarrow)
        {
            var bodyRegion = new RectObstacle(
                Math.Round((layout.PageWidth - layout.ColumnWidth) / 2),
                copyTop,
                layout.ColumnWidth,
                Math.Max(0, layout.PageHeight - copyTop - layout.Gutter));
            var bodyResult = LayoutColumn(_bodyPrepared, new LayoutCursor(0, 0), bodyRegion, BodyLineHeight, [claudeObstacle, openAiObstacle], false);
            return new LayoutEvaluation(headlineLines, creditLeft, creditTop, bodyResult.Lines, [], layout.PageHeight);
        }

        var leftRegion = new RectObstacle(layout.Gutter, copyTop, layout.ColumnWidth, layout.PageHeight - copyTop - layout.Gutter);
        var rightRegion = new RectObstacle(layout.Gutter + layout.ColumnWidth + layout.CenterGap, layout.HeadlineRegion.Y, layout.ColumnWidth, layout.PageHeight - layout.HeadlineRegion.Y - layout.Gutter);
        var leftResult = LayoutColumn(_bodyPrepared, new LayoutCursor(0, 0), leftRegion, BodyLineHeight, [openAiObstacle], false);
        var rightResult = LayoutColumn(_bodyPrepared, leftResult.Cursor, rightRegion, BodyLineHeight, [titleObstacle, claudeObstacle, openAiObstacle], true);
        return new LayoutEvaluation(headlineLines, creditLeft, creditTop, leftResult.Lines, rightResult.Lines, layout.PageHeight);
    }

    private static (List<PositionedLine> Lines, LayoutCursor Cursor) LayoutColumn(
        PreparedTextWithSegments prepared,
        LayoutCursor startCursor,
        RectObstacle region,
        double lineHeight,
        IReadOnlyList<BandObstacle> obstacles,
        bool preferRightOnTie)
    {
        var cursor = startCursor;
        var lineTop = region.Y;
        var lines = new List<PositionedLine>();
        while (lineTop + lineHeight <= region.Bottom)
        {
            var blocked = new List<Interval>();
            foreach (var obstacle in obstacles)
            {
                blocked.AddRange(GetObstacleIntervals(obstacle, lineTop, lineTop + lineHeight));
            }

            var slots = ObstacleLayoutHelper.CarveTextLineSlots(new Interval(region.X, region.Right), blocked);
            if (slots.Count == 0)
            {
                lineTop += lineHeight;
                continue;
            }

            var slot = ObstacleLayoutHelper.PickSlot(slots, preferRightOnTie);
            var line = PretextLayout.LayoutNextLine(prepared, cursor, slot.Width);
            if (line is null)
            {
                break;
            }

            lines.Add(new PositionedLine(line.Text, Math.Round(slot.Left), Math.Round(lineTop), line.Width));
            cursor = line.End;
            lineTop += lineHeight;
        }

        return (lines, cursor);
    }

    private static List<Interval> GetObstacleIntervals(BandObstacle obstacle, double bandTop, double bandBottom)
    {
        return obstacle switch
        {
            EllipseBandObstacle ellipse => ObstacleLayoutHelper.EllipseIntervalForBand(
                ellipse.CenterX,
                ellipse.CenterY,
                ellipse.RadiusX,
                ellipse.RadiusY,
                bandTop,
                bandBottom,
                ellipse.HorizontalPadding,
                ellipse.VerticalPadding) is { } interval
                ? [interval]
                : [],
            RectBandObstacle rects => ObstacleLayoutHelper.GetRectIntervalsForBand(
                rects.Rects,
                bandTop,
                bandBottom,
                rects.HorizontalPadding,
                rects.VerticalPadding),
            _ => [],
        };
    }

    private sealed record PageLayout(
        bool IsNarrow,
        double Gutter,
        double PageWidth,
        double PageHeight,
        double CenterGap,
        double ColumnWidth,
        RectObstacle HeadlineRegion,
        string HeadlineFont,
        double HeadlineFontSize,
        double HeadlineLineHeight,
        double CreditGap,
        double CopyGap,
        RectObstacle OpenAiRect,
        RectObstacle ClaudeRect);

    private sealed record LayoutEvaluation(
        IReadOnlyList<PositionedLine> HeadlineLines,
        double CreditLeft,
        double CreditTop,
        IReadOnlyList<PositionedLine> LeftLines,
        IReadOnlyList<PositionedLine> RightLines,
        double ContentHeight);

    private abstract record BandObstacle(double HorizontalPadding, double VerticalPadding);

    private sealed record RectBandObstacle(IReadOnlyList<RectObstacle> Rects, double HorizontalPadding, double VerticalPadding)
        : BandObstacle(HorizontalPadding, VerticalPadding);

    private sealed record EllipseBandObstacle(double CenterX, double CenterY, double RadiusX, double RadiusY, double HorizontalPadding, double VerticalPadding)
        : BandObstacle(HorizontalPadding, VerticalPadding);

    private sealed class LogoAnimationState
    {
        public double Angle { get; set; }

        public SpinState? Spin { get; set; }
    }

    private sealed record SpinState(double From, double To, long StartMs, double DurationMs);

    private sealed class LogoView(NSImage? image, string label, Action onClick) : NSView
    {
        private readonly NSImage? _image = image;
        private readonly string _label = label;
        private readonly Action _onClick = onClick;

        public double Angle { get; set; }

        public override bool IsFlipped => true;

        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);
            if (_image is null)
            {
                return;
            }

            NSGraphicsContext.GlobalSaveGraphicsState();
            var transform = new NSAffineTransform();
            transform.Translate(MidX(Bounds), MidY(Bounds));
            transform.RotateByRadians(MacTheme.N(Angle));
            transform.Translate(-MidX(Bounds), -MidY(Bounds));
            transform.Concat();
            _image.Draw(Bounds);
            NSGraphicsContext.GlobalRestoreGraphicsState();
        }

        public override void MouseDown(NSEvent theEvent)
        {
            base.MouseDown(theEvent);
            _onClick();
        }

        private static nfloat MidX(CGRect rect) => rect.X + rect.Width / 2;

        private static nfloat MidY(CGRect rect) => rect.Y + rect.Height / 2;
    }
}
