namespace PretextSamples.Samples;

public sealed class EditorialEngineSampleView : UserControl
{
    private const string TitleText = "THE FUTURE OF TEXT LAYOUT IS NOT CSS";
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

    private readonly Grid _root;
    private readonly Canvas _stage = new();
    private readonly Canvas _headlineLayer = new();
    private readonly Canvas _bodyLayer = new();
    private readonly Canvas _pullquoteLayer = new();
    private readonly Canvas _orbLayer = new();
    private readonly List<TextBlock> _headlinePool = [];
    private readonly List<TextBlock> _bodyPool = [];
    private readonly List<TextBlock> _pullquoteLinePool = [];
    private readonly List<Border> _pullquoteBoxPool = [];
    private readonly List<OrbVisual> _orbPool = [];
    private readonly TextBlock _dropCapBlock;
    private readonly Border _hintPill;
    private readonly TextBlock _creditBlock;
    private readonly PreparedTextWithSegments _bodyPrepared = PretextLayout.PrepareWithSegments(SampleTextData.EditorialBodyText, BodyFont);
    private readonly PreparedTextWithSegments[] _pullquotes = SampleTextData.EditorialPullquotes.Select(text => PretextLayout.PrepareWithSegments(text, PullquoteFont)).ToArray();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(24) };
    private readonly UiRenderScheduler _renderScheduler;
    private readonly List<OrbState> _orbs;
    private readonly DragState _dragState = new();
    private long _lastTickMs;
    private double _dropCapWidth;
    private double _cachedHeadlineWidth = -1;
    private double _cachedHeadlineHeight = -1;
    private double _cachedHeadlineMaxSize = -1;
    private double _cachedHeadlineFontSize = 24;
    private List<PositionedLine> _cachedHeadlineLines = [];

    public EditorialEngineSampleView()
    {
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);
        _root = new Grid
        {
            Background = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.4),
                RadiusX = 0.8,
                RadiusY = 0.8,
                GradientStops =
                {
                    new GradientStop { Offset = 0, Color = ColorHelper.FromArgb(255, 15, 15, 20) },
                    new GradientStop { Offset = 1, Color = ColorHelper.FromArgb(255, 10, 10, 12) },
                },
            },
        };
        _root.Children.Add(_stage);

        _stage.Children.Add(_bodyLayer);
        _stage.Children.Add(_pullquoteLayer);
        _stage.Children.Add(_headlineLayer);
        _stage.Children.Add(_orbLayer);

        _dropCapBlock = SampleUi.CreateCanvasLine("T", "Iowan Old Style", BodyLineHeight * DropCapLines - 4, SampleTheme.Brush(0xC4, 0xA3, 0x5A), FontWeights.Bold);
        _dropCapBlock.Text = "T";
        _dropCapBlock.Visibility = Visibility.Visible;
        _stage.Children.Add(_dropCapBlock);

        _hintPill = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 0, 0),
            Background = SampleTheme.Brush(115, 0, 0, 0),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(18, 8, 18, 8),
            Child = new TextBlock
            {
                Text = "Drag the orbs · Click to pause · Zero UI-tree reads",
                Foreground = SampleTheme.Brush(56, 255, 255, 255),
                FontFamily = new FontFamily("Helvetica Neue"),
                FontSize = 13,
                TextWrapping = TextWrapping.NoWrap,
            },
        };

        _creditBlock = new TextBlock
        {
            Text = "Made by @somnai_dreams",
            Foreground = SampleTheme.Brush(72, 255, 255, 255),
            FontFamily = new FontFamily("Helvetica Neue"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 16, 12),
        };

        _root.Children.Add(_hintPill);
        _root.Children.Add(_creditBlock);
        Content = _root;

        _orbs =
        [
            new OrbState(0.52, 0.22, 110, 24, 16, 196, 163, 90),
            new OrbState(0.18, 0.48, 85, -19, 26, 100, 140, 255),
            new OrbState(0.74, 0.58, 95, 16, -21, 232, 100, 130),
            new OrbState(0.38, 0.72, 75, -26, -14, 80, 200, 140),
            new OrbState(0.86, 0.18, 65, -13, 19, 150, 100, 220),
        ];

        var dropCapPrepared = PretextLayout.PrepareWithSegments("T", $"700 {BodyLineHeight * DropCapLines - 4}px {HeadlineFontFamily}");
        PretextLayout.WalkLineRanges(dropCapPrepared, 9999, line => _dropCapWidth = line.Width);

        _root.PointerPressed += OnPointerPressed;
        _root.PointerMoved += OnPointerMoved;
        _root.PointerReleased += OnPointerReleased;
        _root.PointerCanceled += OnPointerReleased;
        _root.PointerCaptureLost += OnPointerCaptureLost;
        _root.PointerExited += OnPointerExited;
        _timer.Tick += OnTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => _renderScheduler.Schedule();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SeedOrbPositions();
        _lastTickMs = Environment.TickCount64;
        _renderScheduler.Schedule();
        _timer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
    }

    private void SeedOrbPositions()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        foreach (var orb in _orbs)
        {
            orb.X = orb.Fx * ActualWidth;
            orb.Y = orb.Fy * ActualHeight;
        }
    }

    private void OnTick(object? sender, object e)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var now = Environment.TickCount64;
        var dt = Math.Min((now - _lastTickMs) / 1000d, 0.05);
        _lastTickMs = now;
        AdvanceOrbs(dt);
        _renderScheduler.Schedule();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_root).Position;
        var orbIndex = HitTestOrbs(point.X, point.Y);
        if (orbIndex < 0)
        {
            return;
        }

        _dragState.Active = true;
        _dragState.OrbIndex = orbIndex;
        _dragState.StartPointerX = point.X;
        _dragState.StartPointerY = point.Y;
        _dragState.StartOrbX = _orbs[orbIndex].X;
        _dragState.StartOrbY = _orbs[orbIndex].Y;
        _dragState.Moved = false;
        _root.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragState.Active || _dragState.OrbIndex < 0)
        {
            return;
        }

        var point = e.GetCurrentPoint(_root).Position;
        var orb = _orbs[_dragState.OrbIndex];
        var dx = point.X - _dragState.StartPointerX;
        var dy = point.Y - _dragState.StartPointerY;
        orb.X = _dragState.StartOrbX + dx;
        orb.Y = _dragState.StartOrbY + dy;
        _dragState.Moved = _dragState.Moved || dx * dx + dy * dy > 16;
        _renderScheduler.Schedule();
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragState.Active || _dragState.OrbIndex < 0)
        {
            return;
        }

        var point = e.GetCurrentPoint(_root).Position;
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

        _root.ReleasePointerCaptures();
        _dragState.Reset();
        _renderScheduler.Schedule();
        e.Handled = true;
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragState.Active)
        {
            return;
        }

        if (!e.GetCurrentPoint(_root).Properties.IsLeftButtonPressed)
        {
            _root.ReleasePointerCaptures();
            _dragState.Reset();
            _renderScheduler.Schedule();
        }
    }

    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragState.Active)
        {
            return;
        }

        _dragState.Reset();
        _renderScheduler.Schedule();
    }

    private int HitTestOrbs(double x, double y)
    {
        var isNarrow = ActualWidth < NarrowBreakpoint;
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

    private void AdvanceOrbs(double dt)
    {
        var pageWidth = ActualWidth;
        var pageHeight = ActualHeight;
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

    private void Render()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var pageWidth = ActualWidth;
        var pageHeight = ActualHeight;
        var isNarrow = pageWidth < NarrowBreakpoint;
        var gutter = isNarrow ? NarrowGutter : Gutter;
        var colGap = isNarrow ? NarrowColGap : ColGap;
        var bottomGap = isNarrow ? NarrowBottomGap : BottomGap;
        var orbRadiusScale = isNarrow ? NarrowOrbScale : 1;
        var activeOrbCount = isNarrow ? Math.Min(NarrowActiveOrbs, _orbs.Count) : _orbs.Count;

        _stage.Width = pageWidth;
        _stage.Height = pageHeight;
        _hintPill.Visibility = isNarrow ? Visibility.Collapsed : Visibility.Visible;
        _creditBlock.Visibility = isNarrow ? Visibility.Collapsed : Visibility.Visible;

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

            foreach (var pullquote in pullquoteRects.Where(p => p.ColumnIndex == columnIndex))
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

        SampleUi.EnsurePool(_headlineLayer, _headlinePool, headlineLines.Count, () =>
            SampleUi.CreateCanvasLine(string.Empty, "Iowan Old Style", 64, SampleTheme.WhiteBrush, FontWeights.Bold));
        for (var index = 0; index < headlineLines.Count; index++)
        {
            var line = headlineLines[index];
            var block = _headlinePool[index];
            block.Text = line.Text;
            block.FontFamily = new FontFamily("Iowan Old Style");
            block.FontSize = headlineSize;
            block.FontWeight = FontWeights.Bold;
            block.Foreground = SampleTheme.WhiteBrush;
            Canvas.SetLeft(block, gutter);
            Canvas.SetTop(block, gutter + line.Y);
        }

        Canvas.SetLeft(_dropCapBlock, contentLeft);
        Canvas.SetTop(_dropCapBlock, bodyTop);

        SampleUi.EnsurePool(_bodyLayer, _bodyPool, allBodyLines.Count, () =>
            SampleUi.CreateCanvasLine(string.Empty, "Iowan Old Style", 18, SampleTheme.Brush(0xE8, 0xE4, 0xDC), FontWeights.Normal));
        for (var index = 0; index < allBodyLines.Count; index++)
        {
            var line = allBodyLines[index];
            var block = _bodyPool[index];
            block.Text = line.Text;
            block.FontSize = 18;
            block.FontFamily = new FontFamily("Iowan Old Style");
            block.Foreground = SampleTheme.Brush(0xE8, 0xE4, 0xDC);
            Canvas.SetLeft(block, line.X);
            Canvas.SetTop(block, line.Y);
        }

        SampleUi.EnsurePool(_pullquoteLayer, _pullquoteBoxPool, pullquoteRects.Count, CreatePullquoteBox);
        var totalPullquoteLines = pullquoteRects.Sum(pullquote => pullquote.Lines.Count);
        SampleUi.EnsurePool(_pullquoteLayer, _pullquoteLinePool, totalPullquoteLines, () =>
            SampleUi.CreateCanvasLine(string.Empty, "Iowan Old Style", 19, SampleTheme.Brush(0xB8, 0xA0, 0x70), FontWeights.Normal));

        var pullquoteLineIndex = 0;
        for (var index = 0; index < pullquoteRects.Count; index++)
        {
            var pullquote = pullquoteRects[index];
            var box = _pullquoteBoxPool[index];
            box.Width = pullquote.Rect.Width;
            box.Height = pullquote.Rect.Height;
            Canvas.SetLeft(box, pullquote.Rect.X);
            Canvas.SetTop(box, pullquote.Rect.Y);

            foreach (var line in pullquote.Lines)
            {
                var lineBlock = _pullquoteLinePool[pullquoteLineIndex++];
                lineBlock.Text = line.Text;
                lineBlock.FontFamily = new FontFamily("Iowan Old Style");
                lineBlock.FontSize = 19;
                lineBlock.FontStyle = Windows.UI.Text.FontStyle.Italic;
                lineBlock.Foreground = SampleTheme.Brush(0xB8, 0xA0, 0x70);
                Canvas.SetLeft(lineBlock, line.X);
                Canvas.SetTop(lineBlock, line.Y);
            }
        }

        EnsureOrbPool(activeOrbCount);
        for (var index = 0; index < activeOrbCount; index++)
        {
            var orb = _orbs[index];
            var visual = _orbPool[index];
            var radius = orb.Radius * orbRadiusScale;
            visual.SetColor(orb.R, orb.G, orb.B);
            visual.SetSize(radius);
            visual.Root.Opacity = orb.Paused ? 0.45 : 1;
            Canvas.SetLeft(visual.Root, orb.X - visual.OuterRadius);
            Canvas.SetTop(visual.Root, orb.Y - visual.OuterRadius);
        }
    }

    private Border CreatePullquoteBox()
    {
        return new Border
        {
            BorderBrush = SampleTheme.Brush(0x6B, 0x5A, 0x3D),
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(14, 0, 0, 0),
            Background = null,
            IsHitTestVisible = false,
        };
    }

    private void EnsureOrbPool(int count)
    {
        while (_orbPool.Count < count)
        {
            var visual = new OrbVisual();
            _orbPool.Add(visual);
            _orbLayer.Children.Add(visual.Root);
        }

        for (var index = 0; index < _orbPool.Count; index++)
        {
            _orbPool[index].Root.Visibility = index < count ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static Brush CreateOrbCoreBrush(byte r, byte g, byte b)
    {
        return new RadialGradientBrush
        {
            Center = new Point(0.35, 0.35),
            RadiusX = 0.72,
            RadiusY = 0.72,
            GradientStops =
            {
                new GradientStop { Offset = 0, Color = ColorHelper.FromArgb(89, r, g, b) },
                new GradientStop { Offset = 0.55, Color = ColorHelper.FromArgb(31, r, g, b) },
                new GradientStop { Offset = 1, Color = ColorHelper.FromArgb(0, r, g, b) },
            },
        };
    }

    private static Brush CreateOrbNearGlowBrush(byte r, byte g, byte b)
    {
        return new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            GradientStops =
            {
                new GradientStop { Offset = 0, Color = ColorHelper.FromArgb(46, r, g, b) },
                new GradientStop { Offset = 0.42, Color = ColorHelper.FromArgb(18, r, g, b) },
                new GradientStop { Offset = 1, Color = ColorHelper.FromArgb(0, r, g, b) },
            },
        };
    }

    private static Brush CreateOrbFarGlowBrush(byte r, byte g, byte b)
    {
        return new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            GradientStops =
            {
                new GradientStop { Offset = 0, Color = ColorHelper.FromArgb(18, r, g, b) },
                new GradientStop { Offset = 0.38, Color = ColorHelper.FromArgb(10, r, g, b) },
                new GradientStop { Offset = 1, Color = ColorHelper.FromArgb(0, r, g, b) },
            },
        };
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

    private sealed class OrbVisual
    {
        private readonly Ellipse _farGlow;
        private readonly Ellipse _nearGlow;
        private readonly Ellipse _core;
        private byte _r;
        private byte _g;
        private byte _b;

        public OrbVisual()
        {
            _farGlow = CreateLayerEllipse();
            _nearGlow = CreateLayerEllipse();
            _core = CreateLayerEllipse();

            Root = new Grid
            {
                IsHitTestVisible = false,
                Children =
                {
                    _farGlow,
                    _nearGlow,
                    _core,
                },
            };
        }

        public Grid Root { get; }

        public double OuterRadius { get; private set; }

        public void SetColor(byte r, byte g, byte b)
        {
            if (_r == r && _g == g && _b == b)
            {
                return;
            }

            _r = r;
            _g = g;
            _b = b;
            _farGlow.Fill = CreateOrbFarGlowBrush(r, g, b);
            _nearGlow.Fill = CreateOrbNearGlowBrush(r, g, b);
            _core.Fill = CreateOrbCoreBrush(r, g, b);
        }

        public void SetSize(double radius)
        {
            var nearGlowRadius = radius * 1.42;
            var farGlowRadius = radius * 1.92;
            OuterRadius = farGlowRadius;

            Root.Width = farGlowRadius * 2;
            Root.Height = farGlowRadius * 2;
            SetEllipseRadius(_farGlow, farGlowRadius);
            SetEllipseRadius(_nearGlow, nearGlowRadius);
            SetEllipseRadius(_core, radius);
        }

        private static Ellipse CreateLayerEllipse()
        {
            return new Ellipse
            {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        private static void SetEllipseRadius(Ellipse ellipse, double radius)
        {
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
        }
    }

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
