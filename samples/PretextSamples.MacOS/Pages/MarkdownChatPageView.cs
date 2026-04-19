namespace PretextSamples.MacOS;

internal sealed class MarkdownChatPageView : SamplePageView
{
    private const string Eyebrow = "DEMO";
    private const string Title = "Virtualized markdown chat";
    private const string DescriptionText = "A 10k-message chat surface measured ahead of time so only the visible message window is materialized into native AppKit drawing.";
    private static readonly nfloat ControlsCardHeight = 90;

    private readonly IReadOnlyList<PreparedChatTemplate> _templates = MarkdownChatModel.CreatePreparedChatTemplates();
    private readonly NSSlider _widthSlider = MacTheme.CreateSlider(MarkdownChatModel.MinChatWidth, MarkdownChatModel.MaxChatWidth, MarkdownChatModel.DefaultChatWidth);
    private readonly NSButton _maskButton = new()
    {
        BezelStyle = NSBezelStyle.Rounded,
        Font = MacTheme.Mono(12, bold: true),
        Bordered = true,
    };
    private readonly NSScrollView _viewport = new()
    {
        HasVerticalScroller = true,
        AutohidesScrollers = true,
        BorderType = NSBorderType.NoBorder,
        DrawsBackground = false,
    };
    private readonly MarkdownChatCanvasView _canvas = new();
    private readonly MarkdownChatHostView _host;
    private readonly MaskOverlayView _topMask = new();
    private readonly MaskOverlayView _bottomMask = new();

    private ConversationFrame? _frame;
    private CGRect _headerRect;
    private CGRect _controlsRect;
    private CGRect _sliderFrame;
    private CGRect _shellRect;
    private CGRect _statsRect;
    private CGRect _widthValueRect;
    private nfloat _occlusionHeight;
    private bool _visualizationEnabled;
    private bool _isWideHeroLayout;
    private double? _pendingScrollOffset;

    public MarkdownChatPageView()
    {
        _host = new MarkdownChatHostView(_canvas);

        AddSubview(_viewport);
        AddSubview(_topMask);
        AddSubview(_bottomMask);
        AddSubview(_widthSlider);
        AddSubview(_maskButton);

        _viewport.DocumentView = _host;
        _viewport.ContentView.PostsBoundsChangedNotifications = true;
        NSNotificationCenter.DefaultCenter.AddObserver(NSView.BoundsChangedNotification, _ => _canvas.NeedsDisplay = true, _viewport.ContentView);

        _widthSlider.Activated += (_, _) => InvalidatePageLayout();
        _maskButton.Activated += (_, _) =>
        {
            _visualizationEnabled = !_visualizationEnabled;
            UpdateMaskState();
            _canvas.NeedsDisplay = true;
        };

        UpdateMaskState();
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(960), availableSize.Width);
        _isWideHeroLayout = contentWidth >= 1120;

        var headerWidth = MacTheme.Min(MacTheme.N(720), contentWidth - MacTheme.PageMargin * 2);
        var eyebrowHeight = MacTheme.MeasureString(Eyebrow, MacTheme.CreateAttributes(MacTheme.Mono(11), MacTheme.AccentBrush), headerWidth).Height;
        var titleHeight = MacTheme.MeasureString(Title, MacTheme.CreateAttributes(MacTheme.Serif(24, bold: true), MacTheme.InkBrush, 30), headerWidth).Height;
        var descriptionHeight = MacTheme.MeasureString(DescriptionText, MacTheme.CreateAttributes(MacTheme.Sans(13), MacTheme.MutedBrush, 19), MacTheme.Min(headerWidth, MacTheme.N(620))).Height;
        var headerHeight = eyebrowHeight + 4 + titleHeight + 4 + descriptionHeight;

        var controlsWidth = MacTheme.Min(MacTheme.N(420), contentWidth - MacTheme.PageMargin * 2);
        var heroTop = MacTheme.PageMargin;
        if (_isWideHeroLayout)
        {
            _headerRect = new CGRect(MacTheme.PageMargin, heroTop, headerWidth, headerHeight);
            _controlsRect = new CGRect(contentWidth - MacTheme.PageMargin - controlsWidth, heroTop + 8, controlsWidth, ControlsCardHeight);
        }
        else
        {
            _headerRect = new CGRect(MacTheme.PageMargin, heroTop, headerWidth, headerHeight);
            _controlsRect = new CGRect(MacTheme.PageMargin, _headerRect.Bottom + 12, controlsWidth, ControlsCardHeight);
        }

        var heroBottom = Math.Max(_headerRect.Bottom, _controlsRect.Bottom);
        var shellWidth = MacTheme.Min(MacTheme.N(1180), contentWidth - MacTheme.PageMargin * 2);
        var shellHeight = Math.Clamp(availableSize.Height - heroBottom - 120, 520, 860);
        _shellRect = new CGRect((contentWidth - shellWidth) / 2, heroBottom + 12, shellWidth, shellHeight);
        _occlusionHeight = MacTheme.N(MarkdownChatModel.GetOcclusionBannerHeight(shellHeight));

        _sliderFrame = new CGRect(_controlsRect.X + 16, _controlsRect.Y + 32, _controlsRect.Width - 112, 24);
        _widthValueRect = new CGRect(_controlsRect.Right - 78, _controlsRect.Y + 31, 60, 16);
        _statsRect = new CGRect(_controlsRect.X + 16, _controlsRect.Y + 60, _controlsRect.Width - 32, 16);

        var requestedWidth = Math.Round(_widthSlider.DoubleValue);
        var maxChatWidth = MarkdownChatModel.GetMaxChatWidth(_shellRect.Width);
        var chatWidth = Math.Max(MarkdownChatModel.MinChatWidth, Math.Min(maxChatWidth, requestedWidth));
        _widthSlider.MaxValue = maxChatWidth;
        _widthSlider.DoubleValue = chatWidth;

        var previousFrame = _frame;
        var canReuse = previousFrame is not null &&
                       Math.Abs(previousFrame.ChatWidth - chatWidth) < 0.5 &&
                       Math.Abs(previousFrame.OcclusionBannerHeight - _occlusionHeight) < 0.5;

        if (!canReuse)
        {
            if (previousFrame is not null)
            {
                var oldScrollableHeight = Math.Max(0, previousFrame.TotalHeight - _shellRect.Height);
                if (oldScrollableHeight > 0)
                {
                    var ratio = _viewport.ContentView.Bounds.Y / oldScrollableHeight;
                    _frame = MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, _occlusionHeight);
                    var newScrollableHeight = Math.Max(0, _frame.TotalHeight - _shellRect.Height);
                    _pendingScrollOffset = ratio * newScrollableHeight;
                }
                else
                {
                    _frame = MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, _occlusionHeight);
                }
            }
            else
            {
                _frame = MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, _occlusionHeight);
            }
        }

        _frame ??= MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, _occlusionHeight);
        UpdateMaskState();
        return new CGSize(contentWidth, _shellRect.Bottom + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
        if (_frame is null)
        {
            return;
        }

        _widthSlider.Frame = _sliderFrame;
        _viewport.Frame = _shellRect;
        _host.UpdateLayout(_frame, _shellRect.Width, _occlusionHeight, _visualizationEnabled);

        var maskLeft = _shellRect.X + (_shellRect.Width - MacTheme.N(_frame.ChatWidth)) / 2;
        _topMask.Frame = new CGRect(maskLeft, _shellRect.Y, MacTheme.N(_frame.ChatWidth), _occlusionHeight);
        _bottomMask.Frame = new CGRect(maskLeft, _shellRect.Bottom - _occlusionHeight, MacTheme.N(_frame.ChatWidth), _occlusionHeight);

        var buttonWidth = MacTheme.MeasureString(_maskButton.Title, MacTheme.CreateAttributes(MacTheme.Mono(12, bold: true), MacTheme.InkBrush), 1000).Width + 28;
        var buttonSize = new CGSize(Math.Min(_topMask.Frame.Width - 24, Math.Max(180, buttonWidth)), 28);
        _maskButton.Frame = new CGRect(
            _topMask.Frame.X + (_topMask.Frame.Width - buttonSize.Width) / 2,
            _topMask.Frame.Y + (_topMask.Frame.Height - buttonSize.Height) / 2,
            buttonSize.Width,
            buttonSize.Height);

        if (_pendingScrollOffset is double offset)
        {
            _pendingScrollOffset = null;
            _viewport.ContentView.ScrollToPoint(new CGPoint(0, offset));
            _viewport.ReflectScrolledClipView(_viewport.ContentView);
        }
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);

        DrawHeader();
        DrawControlsCard();
        MacTheme.FillRoundedRect(_shellRect, 18, MacTheme.ChatBackgroundBrush);
    }

    private void DrawHeader()
    {
        var eyebrowRect = new CGRect(_headerRect.X, _headerRect.Y, _headerRect.Width, 14);
        MacTheme.DrawWrappedString(Eyebrow, eyebrowRect, MacTheme.CreateAttributes(MacTheme.Mono(11), MacTheme.AccentBrush));

        var titleY = eyebrowRect.Bottom + 4;
        var titleRect = new CGRect(_headerRect.X, titleY, _headerRect.Width, 60);
        MacTheme.DrawWrappedString(Title, titleRect, MacTheme.CreateAttributes(MacTheme.Serif(24, bold: true), MacTheme.InkBrush, 30));

        var descriptionY = titleY + MacTheme.MeasureString(Title, MacTheme.CreateAttributes(MacTheme.Serif(24, bold: true), MacTheme.InkBrush, 30), _headerRect.Width).Height + 4;
        var descriptionRect = new CGRect(_headerRect.X, descriptionY, MacTheme.Min(_headerRect.Width, MacTheme.N(620)), _headerRect.Height);
        MacTheme.DrawWrappedString(DescriptionText, descriptionRect, MacTheme.CreateAttributes(MacTheme.Sans(13), MacTheme.MutedBrush, 19));
    }

    private void DrawControlsCard()
    {
        MacTheme.FillRoundedRect(_controlsRect, 14, MacTheme.Color(0x3A, 0x40, 0x48), MacTheme.Color(0x56, 0x5C, 0x66));
        MacTheme.DrawWrappedString("Chat width", new CGRect(_controlsRect.X + 16, _controlsRect.Y + 12, 90, 14), MacTheme.CreateAttributes(MacTheme.Mono(11), MacTheme.MutedBrush));

        if (_frame is not null)
        {
            MacTheme.DrawWrappedString(MacTheme.FormatPixels(_frame.ChatWidth), _widthValueRect, MacTheme.CreateAttributes(MacTheme.Sans(13, bold: true), MacTheme.Color(0xCF, 0xD7, 0xE1), alignment: NSTextAlignment.Right));
            MacTheme.DrawWrappedString($"10k messages · visible window only · canvas height {Math.Round(_frame.TotalHeight):N0}px", _statsRect, MacTheme.CreateAttributes(MacTheme.Sans(12), MacTheme.Color(0xCF, 0xD7, 0xE1)));
        }
    }

    private void UpdateMaskState()
    {
        var alpha = _visualizationEnabled ? (byte)0x9C : (byte)0xF2;
        _topMask.FillColor = MacTheme.Color(0x33, 0x37, 0x40, alpha);
        _bottomMask.FillColor = MacTheme.Color(0x33, 0x37, 0x40, alpha);
        _maskButton.Title = _visualizationEnabled ? "Hide virtualization mask" : "Show virtualization mask";
    }

    private sealed class MarkdownChatHostView(MarkdownChatCanvasView canvas) : NSView
    {
        private readonly MarkdownChatCanvasView _canvas = canvas;

        public override bool IsFlipped => true;

        public void UpdateLayout(ConversationFrame frame, nfloat viewportWidth, nfloat occlusionHeight, bool showMask)
        {
            SetFrameSize(new CGSize(viewportWidth, MacTheme.N(frame.TotalHeight)));
            _canvas.UpdateFrame(frame, occlusionHeight, showMask);
            if (_canvas.Superview is null)
            {
                AddSubview(_canvas);
            }

            _canvas.Frame = new CGRect((viewportWidth - MacTheme.N(frame.ChatWidth)) / 2, 0, MacTheme.N(frame.ChatWidth), MacTheme.N(frame.TotalHeight));
        }
    }

    private sealed class MaskOverlayView : NSView
    {
        public NSColor FillColor { get; set; } = MacTheme.Color(0x33, 0x37, 0x40, 0xF2);

        public override bool IsFlipped => true;

        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);
            MacTheme.FillRect(Bounds, FillColor);
        }
    }
}

internal sealed class MarkdownChatCanvasView : NSView
{
    private ConversationFrame? _frame;
    private nfloat _occlusionHeight;
    private bool _showMask;

    public override bool IsFlipped => true;

    public void UpdateFrame(ConversationFrame frame, nfloat occlusionHeight, bool showMask)
    {
        _frame = frame;
        _occlusionHeight = occlusionHeight;
        _showMask = showMask;
        SetFrameSize(new CGSize(MacTheme.N(frame.ChatWidth), MacTheme.N(frame.TotalHeight)));
        NeedsDisplay = true;
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        if (_frame is null)
        {
            return;
        }

        var scrollTop = EnclosingScrollView?.ContentView.Bounds.Y ?? 0;
        var viewportHeight = EnclosingScrollView?.ContentView.Bounds.Height ?? Bounds.Height;
        var (start, end) = MarkdownChatModel.FindVisibleRange(_frame, scrollTop, viewportHeight, _occlusionHeight, _occlusionHeight);

        for (var index = start; index < end; index++)
        {
            DrawMessage(_frame.Messages[index]);
        }
    }

    private void DrawMessage(ChatMessageInstance message)
    {
        var bubbleX = message.Frame.Role == ChatRole.User
            ? Bounds.Width - MacTheme.N(MarkdownChatModel.MessageSidePadding) - MacTheme.N(message.Frame.FrameWidth)
            : MacTheme.N(MarkdownChatModel.MessageSidePadding);
        var bubbleY = MacTheme.N(message.Top);
        var bubbleRect = new CGRect(bubbleX, bubbleY, MacTheme.N(message.Frame.FrameWidth), MacTheme.N(message.Frame.BubbleHeight));

        if (message.Frame.Role == ChatRole.User)
        {
            MacTheme.FillRoundedRect(bubbleRect, 16, MacTheme.Color(0x39, 0x40, 0x48), MacTheme.Color(0xFF, 0xFF, 0xFF, 0x22));
        }

        var contentInsetX = MacTheme.N(message.Frame.ContentInsetX);
        foreach (var block in MarkdownChatModel.MaterializeTemplateBlocks(message))
        {
            DrawBlock(bubbleRect, contentInsetX, block);
        }
    }

    private void DrawBlock(CGRect bubbleRect, nfloat contentInsetX, BlockLayout block)
    {
        foreach (var railLeft in block.QuoteRailLefts)
        {
            MacTheme.FillRoundedRect(
                new CGRect(bubbleRect.X + contentInsetX + MacTheme.N(railLeft), bubbleRect.Y + MacTheme.N(block.Top), 3, MacTheme.N(block.Height)),
                999,
                MacTheme.Color(0x9E, 0xA6, 0xB2, 0x2F));
        }

        if (block.MarkerText is not null && block.MarkerLeft is double markerLeft)
        {
            MacTheme.DrawWrappedString(
                block.MarkerText,
                new CGRect(bubbleRect.X + contentInsetX + MacTheme.N(markerLeft), bubbleRect.Y + MacTheme.N(block.KindMarkerTop()), 36, 14),
                MacTheme.CreateAttributes(MacTheme.Mono(11), MacTheme.Color(0x9E, 0xA6, 0xB2), alignment: NSTextAlignment.Right));
        }

        switch (block)
        {
            case InlineBlockLayout inlineBlock:
                DrawInlineBlock(bubbleRect, contentInsetX, inlineBlock);
                break;

            case CodeBlockLayout codeBlock:
                DrawCodeBlock(bubbleRect, contentInsetX, codeBlock);
                break;

            case RuleBlockLayout ruleBlock:
                MacTheme.FillRect(
                    new CGRect(
                        bubbleRect.X + contentInsetX + MacTheme.N(ruleBlock.ContentLeft),
                        bubbleRect.Y + MacTheme.N(ruleBlock.Top + Math.Floor(ruleBlock.Height / 2)),
                        MacTheme.N(ruleBlock.Width),
                        1),
                    MacTheme.Color(0x45, 0x4B, 0x55));
                break;
        }
    }

    private static void DrawInlineBlock(CGRect bubbleRect, nfloat contentInsetX, InlineBlockLayout block)
    {
        for (var lineIndex = 0; lineIndex < block.Lines.Count; lineIndex++)
        {
            var line = block.Lines[lineIndex];
            nfloat x = bubbleRect.X + contentInsetX + MacTheme.N(block.ContentLeft);
            var y = bubbleRect.Y + MacTheme.N(block.Top + lineIndex * block.LineHeight);
            foreach (var fragment in line.Fragments)
            {
                x += MacTheme.N(fragment.LeadingGap);
                DrawInlineFragment(fragment, x, y);
                x += MeasureInlineFragmentWidth(fragment);
            }
        }
    }

    private static void DrawCodeBlock(CGRect bubbleRect, nfloat contentInsetX, CodeBlockLayout block)
    {
        var codeRect = new CGRect(
            bubbleRect.X + contentInsetX + MacTheme.N(block.ContentLeft),
            bubbleRect.Y + MacTheme.N(block.Top),
            MacTheme.N(block.Width),
            MacTheme.N(block.Height));
        MacTheme.FillRoundedRect(codeRect, 10, MacTheme.Color(0x31, 0x38, 0x40), MacTheme.Color(0xFF, 0xFF, 0xFF, 0x12));
        var attrs = MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.Color(0xD5, 0xD9, 0xE1), MacTheme.N(MarkdownChatModel.CodeLineHeight));
        for (var lineIndex = 0; lineIndex < block.Lines.Count; lineIndex++)
        {
            var line = block.Lines[lineIndex];
            MacTheme.DrawWrappedString(
                line.Text,
                new CGRect(
                    codeRect.X + MacTheme.N(MarkdownChatModel.CodeBlockPaddingX),
                    codeRect.Y + MacTheme.N(MarkdownChatModel.CodeBlockPaddingY + lineIndex * MarkdownChatModel.CodeLineHeight),
                    codeRect.Width - MacTheme.N(MarkdownChatModel.CodeBlockPaddingX * 2),
                    MacTheme.N(MarkdownChatModel.CodeLineHeight)),
                attrs);
        }
    }

    private static void DrawInlineFragment(InlineFragmentLayout fragment, nfloat x, nfloat y)
    {
        if (fragment.ClassName.Contains("frag--code", StringComparison.Ordinal))
        {
            var attrs = MacTheme.CreateAttributes(MacTheme.Mono(12, bold: true), MacTheme.Color(0xD5, 0xD9, 0xE1));
            var size = MacTheme.MeasureString(fragment.Text, attrs, 1000);
            var rect = new CGRect(x, y + 1, size.Width + 12, 18);
            MacTheme.FillRoundedRect(rect, 8, MacTheme.Color(0x31, 0x38, 0x40));
            MacTheme.DrawWrappedString(fragment.Text, new CGRect(rect.X + 6, rect.Y + 2, rect.Width - 12, 14), attrs);
            return;
        }

        if (fragment.ClassName.Contains("frag--chip", StringComparison.Ordinal))
        {
            var attrs = MacTheme.CreateAttributes(MacTheme.Sans(11, bold: true), MacTheme.Color(0xB7, 0xC0, 0xCF));
            var size = MacTheme.MeasureString(fragment.Text, attrs, 1000);
            var rect = new CGRect(x, y + 1, size.Width + 14, 18);
            MacTheme.FillRoundedRect(rect, 999, MacTheme.Color(0x31, 0x38, 0x40));
            MacTheme.DrawWrappedString(fragment.Text, new CGRect(rect.X + 7, rect.Y + 2, rect.Width - 14, 14), attrs);
            return;
        }

        var attributes = fragment.ClassName.Contains("frag--heading-1", StringComparison.Ordinal)
            ? MacTheme.CreateAttributes(MacTheme.Serif(20, bold: true), MacTheme.Color(0xD5, 0xD9, 0xE1), MacTheme.N(28))
            : fragment.ClassName.Contains("frag--heading-2", StringComparison.Ordinal)
                ? MacTheme.CreateAttributes(MacTheme.Serif(17, bold: true), MacTheme.Color(0xD5, 0xD9, 0xE1), MacTheme.N(25))
                : MacTheme.CreateAttributes(MacTheme.Sans(14, bold: fragment.ClassName.Contains("is-strong", StringComparison.Ordinal)), fragment.ClassName.Contains("is-link", StringComparison.Ordinal) ? MacTheme.Color(0xB7, 0xC0, 0xCF) : MacTheme.Color(0xD5, 0xD9, 0xE1), MacTheme.N(22), underline: fragment.ClassName.Contains("is-link", StringComparison.Ordinal));
        MacTheme.DrawWrappedString(fragment.Text, new CGRect(x, y, 1000, 24), attributes);

        if (fragment.ClassName.Contains("is-del", StringComparison.Ordinal))
        {
            var width = MeasureInlineFragmentWidth(fragment);
            MacTheme.FillRect(new CGRect(x, y + 10, width, 1), MacTheme.Color(0xD5, 0xD9, 0xE1));
        }
    }

    private static nfloat MeasureInlineFragmentWidth(InlineFragmentLayout fragment)
    {
        if (fragment.ClassName.Contains("frag--code", StringComparison.Ordinal))
        {
            return MacTheme.MeasureString(fragment.Text, MacTheme.CreateAttributes(MacTheme.Mono(12, bold: true), MacTheme.Color(0xD5, 0xD9, 0xE1)), 1000).Width + 12;
        }

        if (fragment.ClassName.Contains("frag--chip", StringComparison.Ordinal))
        {
            return MacTheme.MeasureString(fragment.Text, MacTheme.CreateAttributes(MacTheme.Sans(11, bold: true), MacTheme.Color(0xB7, 0xC0, 0xCF)), 1000).Width + 14;
        }

        var attrs = fragment.ClassName.Contains("frag--heading-1", StringComparison.Ordinal)
            ? MacTheme.CreateAttributes(MacTheme.Serif(20, bold: true), MacTheme.Color(0xD5, 0xD9, 0xE1))
            : fragment.ClassName.Contains("frag--heading-2", StringComparison.Ordinal)
                ? MacTheme.CreateAttributes(MacTheme.Serif(17, bold: true), MacTheme.Color(0xD5, 0xD9, 0xE1))
                : MacTheme.CreateAttributes(MacTheme.Sans(14, bold: fragment.ClassName.Contains("is-strong", StringComparison.Ordinal)), MacTheme.Color(0xD5, 0xD9, 0xE1));
        return MacTheme.MeasureString(fragment.Text, attrs, 1000).Width;
    }
}

internal static class MarkdownChatLayoutExtensions
{
    public static double KindMarkerTop(this BlockLayout block)
    {
        return block switch
        {
            InlineBlockLayout inline => inline.Top + Math.Max(0, Math.Round((inline.LineHeight - 12) / 2)),
            CodeBlockLayout code => code.Top + MarkdownChatModel.CodeBlockPaddingY,
            RuleBlockLayout rule => rule.Top,
            _ => block.Top,
        };
    }
}
