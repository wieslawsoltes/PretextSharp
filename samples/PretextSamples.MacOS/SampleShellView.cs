namespace PretextSamples.MacOS;

internal sealed class SampleShellView : NSView
{
    private static readonly nfloat SidebarWidth = 220;
    private static readonly nfloat SidebarButtonHeight = 34;
    private static readonly nfloat SidebarGap = 8;

    private readonly NSView _sidebar = new();
    private readonly NSScrollView _contentScroll = new()
    {
        HasVerticalScroller = true,
        HasHorizontalScroller = true,
        AutohidesScrollers = true,
        BorderType = NSBorderType.NoBorder,
        DrawsBackground = false,
    };

    private readonly Dictionary<string, Func<SamplePageView>> _factories;
    private readonly Dictionary<string, NSButton> _buttons = new(StringComparer.Ordinal);
    private SamplePageView? _currentPage;

    public SampleShellView()
    {
        WantsLayer = true;

        _factories = new Dictionary<string, Func<SamplePageView>>(StringComparer.Ordinal)
        {
            ["overview"] = static () => new OverviewPageView(),
            ["accordion"] = static () => new AccordionPageView(),
            ["bubbles"] = static () => new BubblesPageView(),
            ["masonry"] = static () => new MasonryPageView(),
            ["rich"] = static () => new RichNotePageView(),
            ["markdown-chat"] = static () => new MarkdownChatPageView(),
            ["dynamic"] = static () => new DynamicLayoutPageView(),
            ["editorial"] = static () => new EditorialEnginePageView(),
            ["justification"] = static () => new JustificationComparisonPageView(),
            ["ascii"] = static () => new VariableAsciiPageView(),
        };

        AddSubview(_sidebar);
        AddSubview(_contentScroll);

        BuildSidebar();
        ShowSample("overview");
    }

    public override bool IsFlipped => true;

    public override void Layout()
    {
        base.Layout();

        _sidebar.Frame = new CGRect(0, 0, SidebarWidth, Bounds.Height);
        _contentScroll.Frame = new CGRect(SidebarWidth + 1, 0, Math.Max(320, Bounds.Width - SidebarWidth - 1), Bounds.Height);

        nfloat y = 88;
        foreach (var sample in SampleCatalog.Samples)
        {
            var button = _buttons[sample.Tag];
            button.Frame = new CGRect(16, y, SidebarWidth - 32, SidebarButtonHeight);
            y += SidebarButtonHeight + SidebarGap;
        }

        if (_currentPage is not null)
        {
            _currentPage.UpdateAvailableSize(_contentScroll.ContentSize);
        }
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);

        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.FillRect(new CGRect(0, 0, SidebarWidth, Bounds.Height), MacTheme.SidebarBrush);
        MacTheme.FillRect(new CGRect(SidebarWidth, 0, 1, Bounds.Height), MacTheme.RuleBrush);

        var titleAttributes = MacTheme.CreateAttributes(MacTheme.Serif(24, bold: true), MacTheme.InkBrush);
        MacTheme.DrawWrappedString("Pretext", new CGRect(16, 24, SidebarWidth - 32, 30), titleAttributes);

        var subtitleAttributes = MacTheme.CreateAttributes(MacTheme.Sans(12), MacTheme.MutedBrush, 17);
        MacTheme.DrawWrappedString("Native macOS samples powered by CoreText", new CGRect(16, 52, SidebarWidth - 32, 34), subtitleAttributes);
    }

    private void BuildSidebar()
    {
        foreach (var sample in SampleCatalog.Samples)
        {
            var tag = sample.Tag;
            var button = new NSButton
            {
                Title = sample.Title,
                BezelStyle = NSBezelStyle.TexturedRounded,
                Font = MacTheme.Sans(13, bold: true),
            };
            button.SetButtonType(NSButtonType.PushOnPushOff);
            button.Activated += (_, _) => ShowSample(tag);
            _buttons[tag] = button;
            _sidebar.AddSubview(button);
        }
    }

    private void ShowSample(string tag)
    {
        if (!_factories.TryGetValue(tag, out var factory))
        {
            return;
        }

        _currentPage?.RemoveFromSuperview();
        _currentPage?.Dispose();
        _currentPage = factory();
        _contentScroll.DocumentView = _currentPage;
        _currentPage.UpdateAvailableSize(_contentScroll.ContentSize);

        foreach (var pair in _buttons)
        {
            pair.Value.State = pair.Key == tag ? NSCellStateValue.On : NSCellStateValue.Off;
        }
    }
}
