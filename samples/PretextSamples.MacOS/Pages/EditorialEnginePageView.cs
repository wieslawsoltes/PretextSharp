namespace PretextSamples.MacOS;

internal sealed class EditorialEnginePageView : SamplePageView
{
    private const string TitleText = "THE FUTURE OF TEXT LAYOUT IS NOT CSS";
    private const string HintText = "Drag the orbs · Click to pause · Zero UI-tree reads";
    private const string CreditText = "Made by @somnai_dreams";
    private const string HeadlineFontFamily = "\"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";
    private const string BodyFont = "18px \"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";
    private const string PullquoteFont = "italic 19px \"Iowan Old Style\", \"Palatino Linotype\", \"Book Antiqua\", Palatino, serif";
    private const double BodyLineHeight = 30;
    private const int Gutter = 48;
    private const int ColGap = 40;
    private const int BottomGap = 20;
    private const int DropCapLines = 3;
    private const int MinSlotWidth = 50;
    private const int NarrowBreakpoint = 760;
    private const int NarrowGutter = 20;
    private const int NarrowColGap = 20;
    private const int NarrowBottomGap = 16;
    private const double NarrowOrbScale = 0.58;
    private const int NarrowActiveOrbs = 3;
    private const int PullquoteLineHeight = 27;

    private static readonly NSColor PageInnerBrush = MacTheme.Color(0x0F, 0x0F, 0x14);
    private static readonly NSColor PageOuterBrush = MacTheme.Color(0x0A, 0x0A, 0x0C);
    private static readonly NSColor HeadlineBrush = MacTheme.WhiteBrush;
    private static readonly NSColor BodyBrush = MacTheme.Color(0xE8, 0xE4, 0xDC);
    private static readonly NSColor PullquoteBrush = MacTheme.Color(0xB8, 0xA0, 0x70);
    private static readonly NSColor PullquoteRuleBrush = MacTheme.Color(0x6B, 0x5A, 0x3D);
    private static readonly NSColor DropCapBrush = MacTheme.Color(0xC4, 0xA3, 0x5A);
    private static readonly NSColor HintBackgroundBrush = MacTheme.Color(0x00, 0x00, 0x00, 0x73);
    private static readonly NSColor HintTextBrush = MacTheme.Color(0xFF, 0xFF, 0xFF, 0x38);
    private static readonly NSColor CreditBrush = MacTheme.Color(0xFF, 0xFF, 0xFF, 0x48);

    private readonly PreparedTextWithSegments _bodyPrepared = PretextLayout.PrepareWithSegments(SampleTextData.EditorialBodyText, BodyFont);
    private readonly PreparedTextWithSegments[] _pullquotes = SampleTextData.EditorialPullquotes.Select(text => PretextLayout.PrepareWithSegments(text, PullquoteFont)).ToArray();
    private readonly List<OrbState> _orbs =
    [
        new(0.52, 0.22, 110, 24, 16, 196, 163, 90),
        new(0.18, 0.48, 85, -19, 26, 100, 140, 255),
        new(0.74, 0.58, 95, 16, -21, 232, 100, 130),
        new(0.38, 0.72, 75, -26, -14, 80, 200, 140),
        new(0.86, 0.18, 65, -13, 19, 150, 100, 220),
    ];

    private readonly NSTimer _timer;
    private readonly DragState _dragState = new();
    private CGSize _pageSize;
    private CGSize _lastOrbSeedSize;
    private long _lastTickMs;
    private double _dropCapWidth;
    private double _cachedHeadlineWidth = -1;
    private double _cachedHeadlineHeight = -1;
    private double _cachedHeadlineMaxSize = -1;
    private double _cachedHeadlineFontSize = 24;
    private List<PositionedLine> _cachedHeadlineLines = [];

    public EditorialEnginePageView()
    {
        var dropCapPrepared = PretextLayout.PrepareWithSegments("T", $"700 {BodyLineHeight * DropCapLines - 4}px {HeadlineFontFamily}");
        PretextLayout.WalkLineRanges(dropCapPrepared, 9999, line => _dropCapWidth = line.Width);

        _lastTickMs = Environment.TickCount64;
        _timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(24), _ => OnTick());
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var size = new CGSize(
            MacTheme.Max(MacTheme.N(720), availableSize.Width),
            MacTheme.Max(MacTheme.N(760), availableSize.Height));
        _pageSize = size;
        RescaleOrbsIfNeeded(size);
        return size;
    }

    protected override void LayoutPage(CGRect bounds)
    {
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        RescaleOrbsIfNeeded(bounds.Size);

        var pageWidth = (double)bounds.Width;
        var pageHeight = (double)bounds.Height;
        var isNarrow = pageWidth < NarrowBreakpoint;
        var gutter = isNarrow ? NarrowGutter : Gutter;
        var colGap = isNarrow ? NarrowColGap : ColGap;
        var bottomGap = isNarrow ? NarrowBottomGap : BottomGap;
        var orbRadiusScale = isNarrow ? NarrowOrbScale : 1;
        var activeOrbCount = isNarrow ? Math.Min(NarrowActiveOrbs, _orbs.Count) : _orbs.Count;

        DrawBackground(bounds);

        var headlineWidth = Math.Min(pageWidth - gutter * 2 - (isNarrow ? 12 : 0), 1000);
        var maxHeadlineHeight = Math.Floor(pageHeight * (isNarrow ? 0.2 : 0.24));
        var headlineFit = FitHeadline(headlineWidth, maxHeadlineHeight, isNarrow ? 38 : 92);
        var headlineSize = headlineFit.FontSize;
        var headlineLines = headlineFit.Lines;
        var headlineLineHeight = Math.Round(headlineSize * 0.93);
        var bodyTop = gutter + headlineLines.Count * headlineLineHeight + (isNarrow ? 14 : 20);
        var bodyHeight = pageHeight - bodyTop - bottomGap;
        var columnCount = pageWidth > 1000 ? 3 : pageWidth > 640 ? 2 : 1;
        var maxContentWidth = Math.Min(pageWidth, 1500);
        var columnWidth = Math.Floor((maxContentWidth - gutter * 2 - colGap * (columnCount - 1)) / columnCount);
        var contentLeft = Math.Round((pageWidth - (columnCount * columnWidth + (columnCount - 1) * colGap)) / 2);
        var dropCapRect = new RectObstacle(contentLeft - 2, bodyTop - 2, Math.Ceiling(_dropCapWidth) + 10, DropCapLines * BodyLineHeight + 2);

        var pullquotePlacements = new[]
        {
            new PullquotePlacement(0, 0.48, 0.52, false),
            new PullquotePlacement(1, 0.32, 0.5, true),
        };

        var pullquoteRects = new List<PullquoteRect>();
        for (var index = 0; index < _pullquotes.Length; index++)
        {
            if (isNarrow)
            {
                break;
            }

            var placement = pullquotePlacements[index];
            if (placement.ColumnIndex >= columnCount)
            {
                continue;
            }

            var pullquoteWidth = Math.Round(columnWidth * placement.WidthFraction);
            var pullquoteLines = PretextLayout.LayoutWithLines(_pullquotes[index], pullquoteWidth - 20, PullquoteLineHeight).Lines;
            var pullquoteHeight = pullquoteLines.Count * PullquoteLineHeight + 16;
            var columnX = contentLeft + placement.ColumnIndex * (columnWidth + colGap);
            var pullquoteX = placement.AlignLeft ? columnX : columnX + columnWidth - pullquoteWidth;
            var pullquoteY = Math.Round(bodyTop + bodyHeight * placement.YFraction);
            var positionedLines = pullquoteLines
                .Select((line, lineIndex) => new PositionedLine(line.Text, pullquoteX + 20, pullquoteY + 8 + lineIndex * PullquoteLineHeight, line.Width))
                .ToList();

            pullquoteRects.Add(new PullquoteRect(new RectObstacle(pullquoteX, pullquoteY, pullquoteWidth, pullquoteHeight), positionedLines, placement.ColumnIndex));
        }

        var circleObstacles = new List<EditorialCircleObstacle>();
        for (var index = 0; index < activeOrbCount; index++)
        {
            var orb = _orbs[index];
            circleObstacles.Add(new EditorialCircleObstacle(orb.X, orb.Y, orb.Radius * orbRadiusScale, isNarrow ? 10 : 14, isNarrow ? 2 : 4));
        }

        var allBodyLines = new List<PositionedLine>();
        var cursor = new LayoutCursor(0, 1);
        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            var columnX = contentLeft + columnIndex * (columnWidth + colGap);
            var rects = new List<RectObstacle>();
            if (columnIndex == 0)
            {
                rects.Add(dropCapRect);
            }

            foreach (var pullquote in pullquoteRects.Where(pullquote => pullquote.ColumnIndex == columnIndex))
            {
                rects.Add(pullquote.Rect);
            }

            var result = LayoutEditorialColumn(
                _bodyPrepared,
                cursor,
                columnX,
                bodyTop,
                columnWidth,
                bodyHeight,
                BodyLineHeight,
                circleObstacles,
                rects,
                isNarrow);
            allBodyLines.AddRange(result.Lines);
            cursor = result.Cursor;
        }

        var bodyAttributes = MacTheme.CreateCssAttributes(BodyFont, BodyBrush, MacTheme.N(BodyLineHeight));
        foreach (var line in allBodyLines)
        {
            MacTheme.DrawWrappedString(
                line.Text,
                new CGRect(MacTheme.N(line.X), MacTheme.N(line.Y), MacTheme.N(Math.Max(12, line.Width + 4)), MacTheme.N(BodyLineHeight)),
                bodyAttributes);
        }

        var pullquoteAttributes = MacTheme.CreateCssAttributes(PullquoteFont, PullquoteBrush, MacTheme.N(PullquoteLineHeight));
        foreach (var pullquote in pullquoteRects)
        {
            MacTheme.FillRect(
                new CGRect(MacTheme.N(pullquote.Rect.X), MacTheme.N(pullquote.Rect.Y), 3, MacTheme.N(pullquote.Rect.Height)),
                PullquoteRuleBrush);

            foreach (var line in pullquote.Lines)
            {
                MacTheme.DrawWrappedString(
                    line.Text,
                    new CGRect(MacTheme.N(line.X), MacTheme.N(line.Y), MacTheme.N(Math.Max(12, line.Width + 4)), MacTheme.N(PullquoteLineHeight)),
                    pullquoteAttributes);
            }
        }

        var headlineAttributes = MacTheme.CreateCssAttributes($"700 {headlineSize}px {HeadlineFontFamily}", HeadlineBrush, MacTheme.N(headlineLineHeight));
        foreach (var line in headlineLines)
        {
            MacTheme.DrawWrappedString(
                line.Text,
                new CGRect(MacTheme.N(gutter), MacTheme.N(gutter + line.Y), MacTheme.N(Math.Max(24, line.Width + 6)), MacTheme.N(headlineLineHeight)),
                headlineAttributes);
        }

        for (var index = 0; index < activeOrbCount; index++)
        {
            DrawOrb(_orbs[index], orbRadiusScale);
        }

        var dropCapAttributes = MacTheme.CreateCssAttributes($"700 {BodyLineHeight * DropCapLines - 4}px {HeadlineFontFamily}", DropCapBrush);
        MacTheme.DrawWrappedString(
            "T",
            new CGRect(MacTheme.N(contentLeft), MacTheme.N(bodyTop - 4), MacTheme.N(_dropCapWidth + 12), MacTheme.N(DropCapLines * BodyLineHeight)),
            dropCapAttributes);

        if (!isNarrow)
        {
            DrawHintPill(pageWidth);
            DrawCredit(pageWidth, pageHeight);
        }
    }

    public override void MouseDown(NSEvent theEvent)
    {
        base.MouseDown(theEvent);

        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        var orbIndex = HitTestOrbs(point.X, point.Y);
        if (orbIndex < 0)
        {
            return;
        }

        var orb = _orbs[orbIndex];
        _dragState.Active = true;
        _dragState.OrbIndex = orbIndex;
        _dragState.StartPointerX = point.X;
        _dragState.StartPointerY = point.Y;
        _dragState.StartOrbX = orb.X;
        _dragState.StartOrbY = orb.Y;
        _dragState.Moved = false;
    }

    public override void MouseDragged(NSEvent theEvent)
    {
        base.MouseDragged(theEvent);

        if (!_dragState.Active || _dragState.OrbIndex < 0)
        {
            return;
        }

        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        var orb = _orbs[_dragState.OrbIndex];
        var dx = point.X - _dragState.StartPointerX;
        var dy = point.Y - _dragState.StartPointerY;
        orb.X = _dragState.StartOrbX + dx;
        orb.Y = _dragState.StartOrbY + dy;
        _dragState.Moved = _dragState.Moved || dx * dx + dy * dy > 16;
        NeedsDisplay = true;
    }

    public override void MouseUp(NSEvent theEvent)
    {
        base.MouseUp(theEvent);

        if (!_dragState.Active || _dragState.OrbIndex < 0)
        {
            return;
        }

        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        var orb = _orbs[_dragState.OrbIndex];
        var dx = point.X - _dragState.StartPointerX;
        var dy = point.Y - _dragState.StartPointerY;
        if (!_dragState.Moved && dx * dx + dy * dy < 16)
        {
            orb.Paused = !orb.Paused;
        }
        else
        {
            orb.X = _dragState.StartOrbX + dx;
            orb.Y = _dragState.StartOrbY + dy;
        }

        _dragState.Reset();
        NeedsDisplay = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Invalidate();
        }

        base.Dispose(disposing);
    }

    private void OnTick()
    {
        if (_pageSize.Width <= 0 || _pageSize.Height <= 0)
        {
            return;
        }

        var now = Environment.TickCount64;
        var dt = Math.Min((now - _lastTickMs) / 1000d, 0.05);
        _lastTickMs = now;
        AdvanceOrbs(dt, _pageSize.Width, _pageSize.Height);
        NeedsDisplay = true;
    }

    private void RescaleOrbsIfNeeded(CGSize newSize)
    {
        var width = (double)newSize.Width;
        var height = (double)newSize.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (_lastOrbSeedSize.Width <= 0 || _lastOrbSeedSize.Height <= 0)
        {
            foreach (var orb in _orbs)
            {
                orb.X = orb.Fx * width;
                orb.Y = orb.Fy * height;
            }
        }
        else if (Math.Abs(width - _lastOrbSeedSize.Width) > 0.5 || Math.Abs(height - _lastOrbSeedSize.Height) > 0.5)
        {
            var scaleX = width / _lastOrbSeedSize.Width;
            var scaleY = height / _lastOrbSeedSize.Height;
            foreach (var orb in _orbs)
            {
                orb.X *= scaleX;
                orb.Y *= scaleY;
            }
        }

        _lastOrbSeedSize = newSize;
        ClampOrbPositions(width, height);
    }

    private void ClampOrbPositions(double pageWidth, double pageHeight)
    {
        var isNarrow = pageWidth < NarrowBreakpoint;
        var radiusScale = isNarrow ? NarrowOrbScale : 1;
        var gutter = isNarrow ? NarrowGutter : Gutter;
        var bottomGap = isNarrow ? NarrowBottomGap : BottomGap;

        foreach (var orb in _orbs)
        {
            var radius = orb.Radius * radiusScale;
            orb.X = Math.Clamp(orb.X, radius, pageWidth - radius);
            orb.Y = Math.Clamp(orb.Y, radius + gutter * 0.5, pageHeight - bottomGap - radius);
        }
    }

    private void AdvanceOrbs(double dt, double pageWidth, double pageHeight)
    {
        if (pageWidth <= 0 || pageHeight <= 0)
        {
            return;
        }

        var isNarrow = pageWidth < NarrowBreakpoint;
        var gutter = isNarrow ? NarrowGutter : Gutter;
        var bottomGap = isNarrow ? NarrowBottomGap : BottomGap;
        var radiusScale = isNarrow ? NarrowOrbScale : 1;
        var activeCount = isNarrow ? Math.Min(NarrowActiveOrbs, _orbs.Count) : _orbs.Count;
        var draggedIndex = _dragState.Active ? _dragState.OrbIndex : -1;

        for (var index = 0; index < activeCount; index++)
        {
            var orb = _orbs[index];
            var radius = orb.Radius * radiusScale;
            if (orb.Paused || index == draggedIndex)
            {
                continue;
            }

            orb.X += orb.Vx * dt;
            orb.Y += orb.Vy * dt;

            if (orb.X - radius < 0)
            {
                orb.X = radius;
                orb.Vx = Math.Abs(orb.Vx);
            }

            if (orb.X + radius > pageWidth)
            {
                orb.X = pageWidth - radius;
                orb.Vx = -Math.Abs(orb.Vx);
            }

            if (orb.Y - radius < gutter * 0.5)
            {
                orb.Y = radius + gutter * 0.5;
                orb.Vy = Math.Abs(orb.Vy);
            }

            if (orb.Y + radius > pageHeight - bottomGap)
            {
                orb.Y = pageHeight - bottomGap - radius;
                orb.Vy = -Math.Abs(orb.Vy);
            }
        }

        for (var index = 0; index < activeCount; index++)
        {
            var a = _orbs[index];
            var aRadius = a.Radius * radiusScale;
            for (var otherIndex = index + 1; otherIndex < activeCount; otherIndex++)
            {
                var b = _orbs[otherIndex];
                var bRadius = b.Radius * radiusScale;
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                var minDist = aRadius + bRadius + (isNarrow ? 12 : 20);
                if (dist >= minDist || dist <= 0.1)
                {
                    continue;
                }

                var force = (minDist - dist) * 0.8;
                var nx = dx / dist;
                var ny = dy / dist;
                if (!a.Paused && index != draggedIndex)
                {
                    a.Vx -= nx * force * dt;
                    a.Vy -= ny * force * dt;
                }

                if (!b.Paused && otherIndex != draggedIndex)
                {
                    b.Vx += nx * force * dt;
                    b.Vy += ny * force * dt;
                }
            }
        }
    }

    private int HitTestOrbs(double x, double y)
    {
        var isNarrow = Bounds.Width < NarrowBreakpoint;
        var activeCount = isNarrow ? Math.Min(NarrowActiveOrbs, _orbs.Count) : _orbs.Count;
        var radiusScale = isNarrow ? NarrowOrbScale : 1;
        for (var index = activeCount - 1; index >= 0; index--)
        {
            var orb = _orbs[index];
            var radius = orb.Radius * radiusScale;
            var dx = x - orb.X;
            var dy = y - orb.Y;
            if (dx * dx + dy * dy <= radius * radius)
            {
                return index;
            }
        }

        return -1;
    }

    private void DrawBackground(CGRect bounds)
    {
        using var gradient = new NSGradient(
            new[] { PageInnerBrush, PageOuterBrush },
            new nfloat[] { 0, 1 });
        var center = new CGPoint(bounds.Width * 0.5, bounds.Height * 0.4);
        var radius = MacTheme.N(Math.Max(bounds.Width, bounds.Height) * 0.8);
        gradient.DrawFromCenterRadius(center, 0, center, radius, NSGradientDrawingOptions.None);
    }

    private void DrawHintPill(double pageWidth)
    {
        var attributes = MacTheme.CreateAttributes(MacTheme.Sans(13), HintTextBrush);
        var size = MacTheme.MeasureString(HintText, attributes, MacTheme.N(pageWidth));
        var rect = new CGRect(
            MacTheme.N((pageWidth - size.Width - 36) / 2),
            16,
            size.Width + 36,
            Math.Max(34, size.Height + 16));
        MacTheme.FillRoundedRect(rect, 999, HintBackgroundBrush);
        MacTheme.DrawWrappedString(
            HintText,
            new CGRect(rect.X + 18, rect.Y + (rect.Height - size.Height) / 2, rect.Width - 36, size.Height),
            attributes);
    }

    private void DrawCredit(double pageWidth, double pageHeight)
    {
        var attributes = MacTheme.CreateAttributes(MacTheme.Sans(11), CreditBrush);
        var size = MacTheme.MeasureString(CreditText, attributes, 240);
        MacTheme.DrawWrappedString(
            CreditText,
            new CGRect(MacTheme.N(pageWidth - size.Width - 16), MacTheme.N(pageHeight - size.Height - 12), size.Width + 2, size.Height),
            attributes);
    }

    private void DrawOrb(OrbState orb, double radiusScale)
    {
        var coreRadius = orb.Radius * radiusScale;
        var nearGlowRadius = coreRadius * 1.42;
        var farGlowRadius = coreRadius * 1.92;
        var opacity = orb.Paused ? 0.45 : 1.0;
        var center = new CGPoint(MacTheme.N(orb.X), MacTheme.N(orb.Y));

        DrawRadialGlow(
            center,
            farGlowRadius,
            orb.Color(18 * opacity),
            orb.Color(10 * opacity),
            orb.Color(0),
            0.38);
        DrawRadialGlow(
            center,
            nearGlowRadius,
            orb.Color(46 * opacity),
            orb.Color(18 * opacity),
            orb.Color(0),
            0.42);
        DrawRadialGlow(
            center,
            coreRadius,
            orb.Color(89 * opacity),
            orb.Color(31 * opacity),
            orb.Color(0),
            0.55);
    }

    private static void DrawRadialGlow(CGPoint center, double radius, NSColor inner, NSColor middle, NSColor outer, double middleStop)
    {
        var rect = new CGRect(center.X - MacTheme.N(radius), center.Y - MacTheme.N(radius), MacTheme.N(radius * 2), MacTheme.N(radius * 2));
        using var path = NSBezierPath.FromOvalInRect(rect);
        NSGraphicsContext.GlobalSaveGraphicsState();
        path.AddClip();
        using var gradient = new NSGradient(
            new[] { inner, middle, outer },
            new nfloat[] { 0, MacTheme.N(middleStop), 1 });
        gradient.DrawFromCenterRadius(center, 0, center, MacTheme.N(radius), NSGradientDrawingOptions.None);
        NSGraphicsContext.GlobalRestoreGraphicsState();
    }

    private HeadlineFit FitHeadline(double maxWidth, double maxHeight, double maxSize)
    {
        if (Math.Abs(maxWidth - _cachedHeadlineWidth) < 0.5 &&
            Math.Abs(maxHeight - _cachedHeadlineHeight) < 0.5 &&
            Math.Abs(maxSize - _cachedHeadlineMaxSize) < 0.5)
        {
            return new HeadlineFit(_cachedHeadlineFontSize, _cachedHeadlineLines);
        }

        _cachedHeadlineWidth = maxWidth;
        _cachedHeadlineHeight = maxHeight;
        _cachedHeadlineMaxSize = maxSize;
        var low = 20d;
        var high = maxSize;
        var best = low;
        var bestLines = new List<PositionedLine>();

        while (low <= high)
        {
            var size = Math.Floor((low + high) / 2);
            var font = $"700 {size}px {HeadlineFontFamily}";
            var lineHeight = Math.Round(size * 0.93);
            var prepared = PretextLayout.PrepareWithSegments(TitleText, font);
            var breaksWord = false;
            var lineCount = 0;

            PretextLayout.WalkLineRanges(prepared, maxWidth, line =>
            {
                lineCount++;
                if (line.End.GraphemeIndex != 0)
                {
                    breaksWord = true;
                }
            });

            var totalHeight = lineCount * lineHeight;
            if (!breaksWord && totalHeight <= maxHeight)
            {
                best = size;
                bestLines = PretextLayout.LayoutWithLines(prepared, maxWidth, lineHeight).Lines
                    .Select((line, index) => new PositionedLine(line.Text, 0, index * lineHeight, line.Width))
                    .ToList();
                low = size + 1;
            }
            else
            {
                high = size - 1;
            }
        }

        _cachedHeadlineFontSize = best;
        _cachedHeadlineLines = bestLines;
        return new HeadlineFit(best, bestLines);
    }

    private static (List<PositionedLine> Lines, LayoutCursor Cursor) LayoutEditorialColumn(
        PreparedTextWithSegments prepared,
        LayoutCursor startCursor,
        double regionX,
        double regionY,
        double regionWidth,
        double regionHeight,
        double lineHeight,
        IReadOnlyList<EditorialCircleObstacle> circleObstacles,
        IReadOnlyList<RectObstacle> rectObstacles,
        bool singleSlotOnly)
    {
        var cursor = startCursor;
        var lineTop = regionY;
        var lines = new List<PositionedLine>();
        var textExhausted = false;

        while (lineTop + lineHeight <= regionY + regionHeight && !textExhausted)
        {
            var blocked = new List<Interval>();
            foreach (var obstacle in circleObstacles)
            {
                var interval = ObstacleLayoutHelper.CircleIntervalForBand(
                    obstacle.CenterX,
                    obstacle.CenterY,
                    obstacle.Radius,
                    lineTop,
                    lineTop + lineHeight,
                    obstacle.HorizontalPadding,
                    obstacle.VerticalPadding);
                if (interval is not null)
                {
                    blocked.Add(interval.Value);
                }
            }

            blocked.AddRange(ObstacleLayoutHelper.GetRectIntervalsForBand(rectObstacles, lineTop, lineTop + lineHeight));
            var slots = ObstacleLayoutHelper.CarveTextLineSlots(new Interval(regionX, regionX + regionWidth), blocked, MinSlotWidth);
            if (slots.Count == 0)
            {
                lineTop += lineHeight;
                continue;
            }

            var orderedSlots = singleSlotOnly
                ? new[] { ObstacleLayoutHelper.PickSlot(slots, false) }
                : slots.OrderBy(slot => slot.Left).ToArray();

            foreach (var slot in orderedSlots)
            {
                var line = PretextLayout.LayoutNextLine(prepared, cursor, slot.Width);
                if (line is null)
                {
                    textExhausted = true;
                    break;
                }

                lines.Add(new PositionedLine(line.Text, Math.Round(slot.Left), Math.Round(lineTop), line.Width));
                cursor = line.End;
            }

            lineTop += lineHeight;
        }

        return (lines, cursor);
    }

    private sealed record EditorialCircleObstacle(double CenterX, double CenterY, double Radius, double HorizontalPadding, double VerticalPadding);

    private sealed record PullquotePlacement(int ColumnIndex, double YFraction, double WidthFraction, bool AlignLeft);

    private sealed record PullquoteRect(RectObstacle Rect, IReadOnlyList<PositionedLine> Lines, int ColumnIndex);

    private sealed record HeadlineFit(double FontSize, IReadOnlyList<PositionedLine> Lines);

    private sealed class OrbState(double fx, double fy, double radius, double vx, double vy, byte r, byte g, byte b)
    {
        public double Fx { get; } = fx;

        public double Fy { get; } = fy;

        public double Radius { get; } = radius;

        public double X { get; set; }

        public double Y { get; set; }

        public double Vx { get; set; } = vx;

        public double Vy { get; set; } = vy;

        public byte R { get; } = r;

        public byte G { get; } = g;

        public byte B { get; } = b;

        public bool Paused { get; set; }

        public NSColor Color(double alpha)
        {
            var clamped = (byte)Math.Clamp(Math.Round(alpha), 0, 255);
            return MacTheme.Color(R, G, B, clamped);
        }
    }

    private sealed class DragState
    {
        public bool Active { get; set; }

        public int OrbIndex { get; set; } = -1;

        public double StartPointerX { get; set; }

        public double StartPointerY { get; set; }

        public double StartOrbX { get; set; }

        public double StartOrbY { get; set; }

        public bool Moved { get; set; }

        public void Reset()
        {
            Active = false;
            OrbIndex = -1;
            StartPointerX = 0;
            StartPointerY = 0;
            StartOrbX = 0;
            StartOrbY = 0;
            Moved = false;
        }
    }
}
