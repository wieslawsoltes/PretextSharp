namespace PretextSamples.MacOS;

internal sealed class MarkdownChatPageView : SamplePageView
{
    private readonly IReadOnlyList<PreparedChatTemplate> _templates = MarkdownChatModel.CreatePreparedChatTemplates();
    private readonly NSSlider _widthSlider = MacTheme.CreateSlider(MarkdownChatModel.MinChatWidth, MarkdownChatModel.MaxChatWidth, MarkdownChatModel.DefaultChatWidth);
    private readonly NSButton _maskToggle = MacTheme.CreateCheckBox("Show virtualization mask", false);
    private readonly NSScrollView _viewport = new()
    {
        HasVerticalScroller = true,
        AutohidesScrollers = true,
        BorderType = NSBorderType.NoBorder,
        DrawsBackground = false,
    };
    private readonly MarkdownChatCanvasView _canvas = new();
    private readonly MaskOverlayView _topMask = new();
    private readonly MaskOverlayView _bottomMask = new();

    private ConversationFrame? _frame;
    private CGRect _controlsRect;
    private CGRect _sliderFrame;
    private CGRect _toggleFrame;
    private CGRect _shellRect;
    private nfloat _headerBottom;
    private nfloat _occlusionHeight;

    public MarkdownChatPageView()
    {
        AddSubview(_viewport);
        AddSubview(_topMask);
        AddSubview(_bottomMask);
        AddSubview(_widthSlider);
        AddSubview(_maskToggle);
        _viewport.DocumentView = _canvas;
        _viewport.ContentView.PostsBoundsChangedNotifications = true;
        NSNotificationCenter.DefaultCenter.AddObserver(NSView.BoundsChangedNotification, _ => _canvas.NeedsDisplay = true, _viewport.ContentView);

        _widthSlider.Activated += (_, _) => InvalidatePageLayout();
        _maskToggle.Activated += (_, _) =>
        {
            UpdateMaskState();
            _canvas.NeedsDisplay = true;
        };
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(960), availableSize.Width);
        _headerBottom = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Virtualized markdown chat", "A 10k-message chat surface measured ahead of time so only the visible message window is materialized into native AppKit drawing.");

        _controlsRect = new CGRect(MacTheme.PageMargin, _headerBottom + 18, MacTheme.Min(MacTheme.N(460), contentWidth - MacTheme.PageMargin * 2), 78);
        _sliderFrame = new CGRect(_controlsRect.X + 16, _controlsRect.Y + 26, _controlsRect.Width - 120, 24);
        _toggleFrame = new CGRect(_controlsRect.X + 16, _controlsRect.Y + 52, 200, 18);

        var shellHeight = MacTheme.Max(MacTheme.N(560), availableSize.Height - _headerBottom - 180);
        shellHeight = MacTheme.Min(shellHeight, MacTheme.N(860));
        _shellRect = new CGRect(MacTheme.PageMargin, _controlsRect.Bottom + 18, contentWidth - MacTheme.PageMargin * 2, shellHeight);
        _occlusionHeight = MacTheme.N(MarkdownChatModel.GetOcclusionBannerHeight(shellHeight));

        var requestedWidth = Math.Round(_widthSlider.DoubleValue);
        var maxChatWidth = MarkdownChatModel.GetMaxChatWidth(_shellRect.Width);
        var chatWidth = Math.Max(MarkdownChatModel.MinChatWidth, Math.Min(maxChatWidth, requestedWidth));
        _widthSlider.MaxValue = maxChatWidth;
        _widthSlider.DoubleValue = chatWidth;

        var previousFrame = _frame;
        if (previousFrame is null ||
            Math.Abs(previousFrame.ChatWidth - chatWidth) > 0.5 ||
            Math.Abs(previousFrame.OcclusionBannerHeight - _occlusionHeight) > 0.5)
        {
            var scrollRatio = 0d;
            if (previousFrame is not null)
            {
                var oldScrollableHeight = Math.Max(0, previousFrame.TotalHeight - _shellRect.Height);
                if (oldScrollableHeight > 0)
                {
                    scrollRatio = _viewport.ContentView.Bounds.Y / oldScrollableHeight;
                }
            }

            _frame = MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, _occlusionHeight);
            var newScrollableHeight = Math.Max(0, _frame.TotalHeight - _shellRect.Height);
            _viewport.ContentView.ScrollToPoint(new CGPoint(0, scrollRatio * newScrollableHeight));
        }

        _canvas.UpdateFrame(_frame!, _occlusionHeight, _maskToggle.State == NSCellStateValue.On);
        UpdateMaskState();
        return new CGSize(contentWidth, _shellRect.Bottom + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
        _widthSlider.Frame = _sliderFrame;
        _maskToggle.Frame = _toggleFrame;
        _viewport.Frame = _shellRect;
        _topMask.Frame = new CGRect(_shellRect.X, _shellRect.Y, _shellRect.Width, _occlusionHeight);
        _bottomMask.Frame = new CGRect(_shellRect.X, _shellRect.Bottom - _occlusionHeight, _shellRect.Width, _occlusionHeight);
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Virtualized markdown chat", "A 10k-message chat surface measured ahead of time so only the visible message window is materialized into native AppKit drawing.");

        MacTheme.FillRoundedRect(_controlsRect, MacTheme.CardRadius, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString("Chat width", new CGRect(_controlsRect.X + 16, _controlsRect.Y + 8, 90, 14), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.MutedBrush));
        if (_frame is not null)
        {
            MacTheme.DrawWrappedString(MacTheme.FormatPixels(_frame.ChatWidth), new CGRect(_controlsRect.Right - 76, _controlsRect.Y + 8, 60, 14), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.AccentBrush));
            MacTheme.DrawWrappedString($"10k messages · visible window only · canvas height {Math.Round(_frame.TotalHeight):N0}px", new CGRect(_controlsRect.X + 16, _controlsRect.Y + 30, _controlsRect.Width - 32, 16), MacTheme.CreateAttributes(MacTheme.Sans(12), MacTheme.MutedBrush));
        }

        MacTheme.FillRoundedRect(_shellRect, 18, MacTheme.ChatBackgroundBrush);
    }

    private void UpdateMaskState()
    {
        var active = _maskToggle.State == NSCellStateValue.On;
        var alpha = active ? (byte)0x9C : (byte)0xF2;
        _topMask.FillColor = MacTheme.Color(0x33, 0x37, 0x40, alpha);
        _bottomMask.FillColor = MacTheme.Color(0x33, 0x37, 0x40, alpha);
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
