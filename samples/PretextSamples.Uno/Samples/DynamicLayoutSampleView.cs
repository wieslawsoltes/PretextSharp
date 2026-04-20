namespace PretextSamples.Samples;

public sealed class DynamicLayoutSampleView : UserControl
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

    private readonly Grid _root;
    private readonly Canvas _stage = new();
    private readonly Canvas _headlineLayer = new();
    private readonly Canvas _bodyLayer = new();
    private readonly Canvas _overlayLayer = new();
    private readonly List<TextBlock> _headlinePool = [];
    private readonly List<TextBlock> _bodyPool = [];
    private readonly TextBlock _creditBlock;
    private readonly Border _hintPill;
    private readonly Border _openAiLogoHost;
    private readonly Border _claudeLogoHost;
    private readonly UiRenderScheduler _renderScheduler;
    private readonly DispatcherTimer _spinTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly PreparedTextWithSegments _bodyPrepared = PretextLayout.PrepareWithSegments(SampleTextData.DynamicBodyCopy, BodyFont);
    private readonly Dictionary<string, PreparedTextWithSegments> _preparedCache = new(StringComparer.Ordinal);
    private readonly LogoAnimationState _openAiLogo = new();
    private readonly LogoAnimationState _claudeLogo = new();

    public DynamicLayoutSampleView()
    {
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);
        _root = CreateRoot();
        _root.Children.Add(BuildAtmosphereLeft());
        _root.Children.Add(BuildAtmosphereRight());
        _root.Children.Add(_stage);

        _stage.Children.Add(_bodyLayer);
        _stage.Children.Add(_headlineLayer);
        _stage.Children.Add(_overlayLayer);

        _creditBlock = new TextBlock
        {
            Text = CreditText,
            Foreground = SampleTheme.Brush(148, 17, 16, 13),
            FontFamily = new FontFamily("Helvetica Neue"),
            FontSize = 12,
            CharacterSpacing = 140,
        };

        _hintPill = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 0, 0),
            Background = SampleTheme.Brush(240, 17, 16, 13),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(16, 10, 16, 11),
            Child = new TextBlock
            {
                Text = "Everything laid out in C#. Resize horizontally and vertically, then click the logos.",
                Foreground = SampleTheme.Brush(245, 246, 240, 230),
                FontFamily = new FontFamily("Helvetica Neue"),
                FontSize = 12,
                TextWrapping = TextWrapping.NoWrap,
            },
        };

        _openAiLogoHost = CreateLogoHost("ms-appx:///Assets/openai_symbol.png", "OpenAI", () => StartLogoSpin(_openAiLogo, 1));
        _claudeLogoHost = CreateLogoHost("ms-appx:///Assets/claude_symbol.png", "Claude", () => StartLogoSpin(_claudeLogo, -1));

        _overlayLayer.Children.Add(_creditBlock);
        _overlayLayer.Children.Add(_openAiLogoHost);
        _overlayLayer.Children.Add(_claudeLogoHost);
        _root.Children.Add(_hintPill);

        Content = _root;

        Loaded += (_, _) => _renderScheduler.Schedule();
        SizeChanged += (_, _) => _renderScheduler.Schedule();
        Unloaded += (_, _) => _spinTimer.Stop();
        _spinTimer.Tick += (_, _) =>
        {
            if (!UpdateSpinState())
            {
                _spinTimer.Stop();
            }

            _renderScheduler.Schedule();
        };
    }

    private void Render()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var pageWidth = ActualWidth;
        var pageHeight = ActualHeight;
        var layout = BuildLayout(pageWidth, pageHeight, BodyLineHeight);
        var evaluation = EvaluateLayout(layout);

        _stage.Width = pageWidth;
        _stage.Height = pageHeight;

        _hintPill.Visibility = layout.IsNarrow ? Visibility.Collapsed : Visibility.Visible;

        SampleUi.EnsurePool(_headlineLayer, _headlinePool, evaluation.HeadlineLines.Count, () =>
            SampleUi.CreateCanvasLine(string.Empty, "Iowan Old Style", 48, SampleTheme.InkBrush, FontWeights.Bold));

        for (var index = 0; index < evaluation.HeadlineLines.Count; index++)
        {
            var line = evaluation.HeadlineLines[index];
            var block = _headlinePool[index];
            block.Text = line.Text;
            block.FontSize = layout.HeadlineFontSize;
            block.FontFamily = new FontFamily("Iowan Old Style");
            block.FontWeight = FontWeights.Bold;
            Canvas.SetLeft(block, line.X);
            Canvas.SetTop(block, line.Y);
        }

        var bodyLines = evaluation.LeftLines.Count + evaluation.RightLines.Count;
        SampleUi.EnsurePool(_bodyLayer, _bodyPool, bodyLines, () =>
            SampleUi.CreateCanvasLine(string.Empty, "Iowan Old Style", 20, SampleTheme.InkBrush, FontWeights.Normal));

        var bodyIndex = 0;
        foreach (var line in evaluation.LeftLines)
        {
            var block = _bodyPool[bodyIndex++];
            block.Text = line.Text;
            block.FontSize = 20;
            block.FontFamily = new FontFamily("Iowan Old Style");
            Canvas.SetLeft(block, line.X);
            Canvas.SetTop(block, line.Y);
        }

        foreach (var line in evaluation.RightLines)
        {
            var block = _bodyPool[bodyIndex++];
            block.Text = line.Text;
            block.FontSize = 20;
            block.FontFamily = new FontFamily("Iowan Old Style");
            Canvas.SetLeft(block, line.X);
            Canvas.SetTop(block, line.Y);
        }

        Canvas.SetLeft(_creditBlock, evaluation.CreditLeft);
        Canvas.SetTop(_creditBlock, evaluation.CreditTop);

        ApplyLogoLayout(_openAiLogoHost, layout.OpenAiRect, _openAiLogo.Angle);
        ApplyLogoLayout(_claudeLogoHost, layout.ClaudeRect, _claudeLogo.Angle);
    }

    private Grid CreateRoot()
    {
        return new Grid
        {
            Background = SampleTheme.PageBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    private static Rectangle BuildAtmosphereLeft()
    {
        return new Rectangle
        {
            IsHitTestVisible = false,
            Fill = new RadialGradientBrush
            {
                Center = new Point(0.16, 0.82),
                RadiusX = 0.62,
                RadiusY = 0.54,
                GradientStops =
                {
                    new GradientStop { Offset = 0, Color = ColorHelper.FromArgb(41, 45, 88, 128) },
                    new GradientStop { Offset = 0.69, Color = ColorHelper.FromArgb(0, 45, 88, 128) },
                },
            },
        };
    }

    private static Rectangle BuildAtmosphereRight()
    {
        return new Rectangle
        {
            IsHitTestVisible = false,
            Fill = new RadialGradientBrush
            {
                Center = new Point(0.86, 0.16),
                RadiusX = 0.58,
                RadiusY = 0.48,
                GradientStops =
                {
                    new GradientStop { Offset = 0, Color = ColorHelper.FromArgb(46, 217, 119, 87) },
                    new GradientStop { Offset = 0.7, Color = ColorHelper.FromArgb(0, 217, 119, 87) },
                },
            },
        };
    }

    private Border CreateLogoHost(string assetUri, string label, Action onClick)
    {
        var host = new Border
        {
            Background = new SolidColorBrush(Colors.Transparent),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransformOrigin = new Point(0.5, 0.5),
            Child = new Image
            {
                Source = new BitmapImage(new Uri(assetUri)),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            },
        };

        ToolTipService.SetToolTip(host, label);
        host.Tapped += (_, _) => onClick();
        return host;
    }

    private void StartLogoSpin(LogoAnimationState logo, int direction)
    {
        var delta = direction * Math.PI;
        var now = Environment.TickCount64;
        logo.Spin = new SpinState(logo.Angle, logo.Angle + delta, now, 900);
        _spinTimer.Start();
        _renderScheduler.Schedule();
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
            var width = slot.Width;
            var line = PretextLayout.LayoutNextLine(prepared, cursor, width);
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

    private static void ApplyLogoLayout(Border logoHost, RectObstacle rect, double angle)
    {
        logoHost.Width = rect.Width;
        logoHost.Height = rect.Height;
        logoHost.RenderTransform = new RotateTransform { Angle = angle * 180 / Math.PI };
        Canvas.SetLeft(logoHost, rect.X);
        Canvas.SetTop(logoHost, rect.Y);
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
}
