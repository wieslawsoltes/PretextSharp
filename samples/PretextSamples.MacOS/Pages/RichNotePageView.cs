namespace PretextSamples.MacOS;

internal sealed class RichNotePageView : SamplePageView
{
    private readonly NSSlider _slider = MacTheme.CreateSlider(RichNoteModel.BodyMinWidth, RichNoteModel.BodyMaxWidth, RichNoteModel.BodyDefaultWidth);
    private readonly PreparedRichInlineNote _preparedNote = RichNoteModel.PrepareRichInlineNote();
    private RichNoteLayout? _layout;
    private CGRect _controlsRect;
    private CGRect _sliderFrame;
    private CGRect _shellRect;
    private nfloat _headerBottom;

    public RichNotePageView()
    {
        AddSubview(_slider);
        _slider.Activated += (_, _) => InvalidatePageLayout();
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(920), availableSize.Width);
        _headerBottom = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Rich text fragments that still wrap", "The sample uses the core rich-inline helper directly. Text, links, and code spans split across lines, while chips stay atomic and still participate in the same inline flow.");
        var controlsWidth = MacTheme.Min(MacTheme.N(720), contentWidth - MacTheme.PageMargin * 2);
        _controlsRect = new CGRect((contentWidth - controlsWidth) / 2, _headerBottom + 18, controlsWidth, 62);
        _sliderFrame = new CGRect(_controlsRect.X + 110, _controlsRect.Y + 14, _controlsRect.Width - 190, 24);

        var (bodyWidth, maxBodyWidth) = RichNoteModel.ResolveRichNoteBodyWidth(contentWidth, _slider.DoubleValue);
        _slider.MaxValue = maxBodyWidth;
        _slider.DoubleValue = bodyWidth;
        _layout = RichNoteModel.LayoutRichNote(_preparedNote, bodyWidth);
        _shellRect = new CGRect((contentWidth - MacTheme.N(_layout.NoteWidth)) / 2, _controlsRect.Bottom + 22, MacTheme.N(_layout.NoteWidth), MacTheme.N(_layout.NoteBodyHeight) + 40);
        return new CGSize(contentWidth, _shellRect.Bottom + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
        _slider.Frame = _sliderFrame;
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Rich text fragments that still wrap", "The sample uses the core rich-inline helper directly. Text, links, and code spans split across lines, while chips stay atomic and still participate in the same inline flow.");

        if (_layout is null)
        {
            return;
        }

        MacTheme.FillRoundedRect(_controlsRect, 18, MacTheme.PanelBrush, MacTheme.RuleBrush);
        MacTheme.DrawWrappedString("Text width", new CGRect(_controlsRect.X + 16, _controlsRect.Y + 18, 80, 18), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.MutedBrush));
        MacTheme.DrawWrappedString(MacTheme.FormatPixels(_layout.BodyWidth), new CGRect(_controlsRect.Right - 76, _controlsRect.Y + 18, 60, 18), MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.AccentBrush));

        MacTheme.FillRoundedRect(_shellRect, 20, MacTheme.PanelBrush, MacTheme.RuleBrush);
        var originX = _shellRect.X + 20;
        var originY = _shellRect.Y + 20;
        for (var lineIndex = 0; lineIndex < _layout.Lines.Count; lineIndex++)
        {
            var line = _layout.Lines[lineIndex];
            nfloat x = originX;
            var y = originY + lineIndex * MacTheme.N(RichNoteModel.LineHeight);
            foreach (var fragment in line.Fragments)
            {
                x += MacTheme.N(fragment.LeadingGap);
                DrawFragment(fragment, x, y);
                x += MeasureFragmentWidth(fragment);
            }
        }
    }

    private void DrawFragment(RichNoteFragment fragment, nfloat x, nfloat y)
    {
        switch (fragment.ClassName)
        {
            case "chip--mention":
                DrawChip(fragment.Text, x, y, MacTheme.Color(0x15, 0x5A, 0x88), MacTheme.Color(0x15, 0x5A, 0x88), MacTheme.Color(0xE8, 0xF1, 0xF6));
                break;
            case "chip--status":
                DrawChip(fragment.Text, x, y, MacTheme.Color(0x35, 0x5F, 0x38), MacTheme.Color(0x46, 0x76, 0x4D), MacTheme.Color(0xEB, 0xF2, 0xEB));
                break;
            case "chip--priority":
                DrawChip(fragment.Text, x, y, MacTheme.Color(0x8E, 0x23, 0x23), MacTheme.Color(0xB0, 0x2C, 0x2C), MacTheme.Color(0xF6, 0xE7, 0xE7));
                break;
            case "chip--time":
            case "chip--count":
                DrawChip(fragment.Text, x, y, MacTheme.Color(0x48, 0x3E, 0x83), MacTheme.Color(0x43, 0x39, 0x7A), MacTheme.Color(0xEF, 0xED, 0xF8));
                break;
            case "code":
                {
                    var attrs = MacTheme.CreateAttributes(MacTheme.Mono(14, bold: true), MacTheme.InkBrush);
                    var size = MacTheme.MeasureString(fragment.Text, attrs, 1000);
                    var rect = new CGRect(x, y + 2, size.Width + 14, 24);
                    MacTheme.FillRoundedRect(rect, 9, MacTheme.AccentSoftBrush);
                    MacTheme.DrawWrappedString(fragment.Text, new CGRect(rect.X + 7, rect.Y + 4, rect.Width - 14, 16), attrs);
                    break;
                }
            case "link":
                {
                    var attrs = MacTheme.CreateAttributes(MacTheme.Sans(17, bold: true), MacTheme.AccentBrush, underline: true);
                    MacTheme.DrawWrappedString(fragment.Text, new CGRect(x, y, 1000, 20), attrs);
                    break;
                }
            default:
                {
                    var attrs = MacTheme.CreateAttributes(MacTheme.Sans(17), MacTheme.InkBrush);
                    MacTheme.DrawWrappedString(fragment.Text, new CGRect(x, y, 1000, 20), attrs);
                    break;
                }
        }
    }

    private nfloat MeasureFragmentWidth(RichNoteFragment fragment)
    {
        return fragment.ClassName switch
        {
            "chip--mention" or "chip--status" or "chip--priority" or "chip--time" or "chip--count"
                => MacTheme.MeasureString(fragment.Text, MacTheme.CreateAttributes(MacTheme.Sans(12, bold: true), MacTheme.InkBrush), 1000).Width + 22,
            "code"
                => MacTheme.MeasureString(fragment.Text, MacTheme.CreateAttributes(MacTheme.Mono(14, bold: true), MacTheme.InkBrush), 1000).Width + 14,
            "link"
                => MacTheme.MeasureString(fragment.Text, MacTheme.CreateAttributes(MacTheme.Sans(17, bold: true), MacTheme.AccentBrush, underline: true), 1000).Width,
            _
                => MacTheme.MeasureString(fragment.Text, MacTheme.CreateAttributes(MacTheme.Sans(17), MacTheme.InkBrush), 1000).Width,
        };
    }

    private static void DrawChip(string text, nfloat x, nfloat y, NSColor foreground, NSColor border, NSColor background)
    {
        var attrs = MacTheme.CreateAttributes(MacTheme.Sans(12, bold: true), foreground);
        var size = MacTheme.MeasureString(text, attrs, 1000);
        var rect = new CGRect(x, y + 1, size.Width + 20, 22);
        MacTheme.FillRoundedRect(rect, 999, background, border);
        MacTheme.DrawWrappedString(text, new CGRect(rect.X + 10, rect.Y + 4, rect.Width - 20, 14), attrs);
    }
}
