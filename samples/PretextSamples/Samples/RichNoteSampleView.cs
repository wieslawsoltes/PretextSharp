namespace PretextSamples.Samples;

public sealed class RichNoteSampleView : UserControl
{
    private const string BodyFont = "500 17px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const string LinkFont = "600 17px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const string CodeFont = "600 14px \"SF Mono\", ui-monospace, Menlo, Monaco, monospace";
    private const string ChipFont = "700 12px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    private const double ToolbarCornerRadius = 18;
    private const double NoteShellCornerRadius = 20;
    private const double LineHeight = 34;
    private const double LastLineBlockHeight = 24;
    private const double NoteChrome = 40;
    private const double BodyMinWidth = 260;
    private const double BodyMaxWidth = 760;
    private const double UnboundedWidth = 100_000;
    private static readonly LayoutCursor LineStartCursor = new(0, 0);
    private static readonly double InlineBoundaryGap = MeasureCollapsedSpaceWidth(BodyFont);

    private readonly Slider _slider = new() { Minimum = BodyMinWidth, Maximum = BodyMaxWidth, Value = 516 };
    private readonly TextBlock _sliderValue = new() { Foreground = SampleTheme.MutedBrush };
    private readonly Canvas _noteCanvas = new();
    private readonly Border _shell;
    private readonly List<InlineItem> _items;
    private readonly FrameworkElement _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;
    private double _lastWidth = -1;

    public RichNoteSampleView()
    {
        _items = BuildInlineItems();
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);

        var stack = SampleUi.CreatePageStack();
        var header = SampleUi.CreateHeader(
            "DEMO",
            "Rich text fragments that still wrap",
            "Text, links, and code spans keep wrapping naturally while atomic chips stay whole and reserve their own inline chrome.");
        header.MaxWidth = 720;
        header.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(header);

        _slider.ValueChanged += (_, _) => _renderScheduler.Schedule();
        var controls = new Grid { ColumnSpacing = 12 };
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        controls.Children.Add(new TextBlock
        {
            Text = "Text width",
            Foreground = SampleTheme.MutedBrush,
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(_slider, 1);
        controls.Children.Add(_slider);
        Grid.SetColumn(_sliderValue, 2);
        controls.Children.Add(_sliderValue);
        var controlsCard = SampleUi.CreateCard(controls, 16);
        controlsCard.MaxWidth = 720;
        controlsCard.Width = 720;
        controlsCard.CornerRadius = new CornerRadius(ToolbarCornerRadius);
        controlsCard.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(controlsCard);

        _shell = new Border
        {
            Background = SampleTheme.PanelBrush,
            BorderBrush = SampleTheme.RuleBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(NoteShellCornerRadius),
            Padding = new Thickness(20),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = _noteCanvas,
        };

        var preview = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        preview.Children.Add(_shell);
        stack.Children.Add(preview);
        _pageRoot = SampleUi.CreatePageRoot(stack);
        Content = _pageRoot;
        Loaded += (_, _) => _renderScheduler.Schedule();
        SizeChanged += (_, _) => _renderScheduler.Schedule();
    }

    private void Render()
    {
        if (_pageRoot.ActualWidth <= 0)
        {
            return;
        }

        var maxWidth = Math.Max(BodyMinWidth, Math.Min(BodyMaxWidth, _pageRoot.ActualWidth - NoteChrome - 40));
        var width = Math.Max(BodyMinWidth, Math.Min(maxWidth, _slider.Value));
        if (Math.Abs(width - _lastWidth) < 0.5 && Math.Abs(_slider.Maximum - maxWidth) < 0.5)
        {
            return;
        }

        _lastWidth = width;
        _slider.Maximum = maxWidth;
        _sliderValue.Text = $"{Math.Round(width)}px";

        var lines = LayoutInlineItems(width);
        var noteBodyHeight = lines.Count == 0
            ? LastLineBlockHeight
            : (lines.Count - 1) * LineHeight + LastLineBlockHeight;

        _noteCanvas.Width = width;
        _noteCanvas.Height = noteBodyHeight;
        _noteCanvas.Children.Clear();

        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0,
            };

            foreach (var fragment in lines[lineIndex].Fragments)
            {
                var element = BuildFragment(fragment);
                if (fragment.LeadingGap > 0)
                {
                    element.Margin = new Thickness(fragment.LeadingGap, 0, 0, 0);
                }

                row.Children.Add(element);
            }

            Canvas.SetTop(row, lineIndex * LineHeight);
            _noteCanvas.Children.Add(row);
        }

        _shell.Width = width + NoteChrome;
    }

    private static FrameworkElement BuildFragment(InlineFragment fragment)
    {
        if (fragment is ChipFragment chip)
        {
            return new Border
            {
                Background = chip.Background,
                BorderBrush = chip.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(10, 4, 10, 4),
                RenderTransform = new TranslateTransform { Y = -1 },
                Child = new TextBlock
                {
                    Text = chip.Text,
                    Foreground = chip.Foreground,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                },
            };
        }

        var text = (TextFragment)fragment;
        if (text.Kind == TextKind.Code)
        {
            return new Border
            {
                Background = SampleTheme.AccentSoftBrush,
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(7, 2, 7, 3),
                Child = new TextBlock
                {
                    Text = text.Text,
                    Foreground = SampleTheme.InkBrush,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                },
            };
        }

        return new TextBlock
        {
            Text = text.Text,
            Foreground = text.Kind == TextKind.Link ? SampleTheme.AccentBrush : SampleTheme.InkBrush,
            FontFamily = new FontFamily("Helvetica Neue"),
            FontSize = 17,
            FontWeight = text.Kind == TextKind.Link ? FontWeights.SemiBold : FontWeights.Normal,
            TextDecorations = text.Kind == TextKind.Link
                ? Windows.UI.Text.TextDecorations.Underline
                : Windows.UI.Text.TextDecorations.None,
        };
    }

    private static List<InlineItem> BuildInlineItems()
    {
        var specs = new InlineSpec[]
        {
            new TextInlineSpec("Ship ", BodyFont, TextKind.Body),
            new ChipInlineSpec("@maya", SampleTheme.Brush(0x15, 0x5A, 0x88), SampleTheme.Brush(0x15, 0x5A, 0x88), SampleTheme.Brush(0xE8, 0xF1, 0xF6)),
            new TextInlineSpec("'s ", BodyFont, TextKind.Body),
            new TextInlineSpec("rich-note", CodeFont, TextKind.Code, 14),
            new TextInlineSpec(" card once ", BodyFont, TextKind.Body),
            new TextInlineSpec("pre-wrap", CodeFont, TextKind.Code, 14),
            new TextInlineSpec(" lands. Status ", BodyFont, TextKind.Body),
            new ChipInlineSpec("blocked", SampleTheme.Brush(0x91, 0x62, 0x07), SampleTheme.Brush(0xC4, 0x81, 0x14), SampleTheme.Brush(0xF8, 0xEF, 0xDE)),
            new TextInlineSpec(" by ", BodyFont, TextKind.Body),
            new TextInlineSpec("vertical text", LinkFont, TextKind.Link),
            new TextInlineSpec(" research, but 北京 copy and Arabic QA are both green ✅. Keep ", BodyFont, TextKind.Body),
            new ChipInlineSpec("جاهز", SampleTheme.Brush(0x35, 0x5F, 0x38), SampleTheme.Brush(0x46, 0x76, 0x4D), SampleTheme.Brush(0xEB, 0xF2, 0xEB)),
            new TextInlineSpec(" for ", BodyFont, TextKind.Body),
            new TextInlineSpec("Cmd+K", CodeFont, TextKind.Code, 14),
            new TextInlineSpec(" docs; the review bundle now includes 中文 labels, عربي fallback, and one more launch pass 🚀 for ", BodyFont, TextKind.Body),
            new ChipInlineSpec("Fri 2:30 PM", SampleTheme.Brush(0x48, 0x3E, 0x83), SampleTheme.Brush(0x43, 0x39, 0x7A), SampleTheme.Brush(0xEF, 0xED, 0xF8)),
            new TextInlineSpec(". Keep ", BodyFont, TextKind.Body),
            new TextInlineSpec("layoutNextLine()", CodeFont, TextKind.Code, 14),
            new TextInlineSpec(" public, tag this ", BodyFont, TextKind.Body),
            new ChipInlineSpec("P1", SampleTheme.Brush(0x8E, 0x23, 0x23), SampleTheme.Brush(0xB0, 0x2C, 0x2C), SampleTheme.Brush(0xF6, 0xE7, 0xE7)),
            new TextInlineSpec(", keep ", BodyFont, TextKind.Body),
            new ChipInlineSpec("3 reviewers", SampleTheme.Brush(0x48, 0x3E, 0x83), SampleTheme.Brush(0x43, 0x39, 0x7A), SampleTheme.Brush(0xEF, 0xED, 0xF8)),
            new TextInlineSpec(", and route feedback to ", BodyFont, TextKind.Body),
            new TextInlineSpec("design sync", LinkFont, TextKind.Link),
            new TextInlineSpec(".", BodyFont, TextKind.Body),
        };

        var items = new List<InlineItem>(specs.Length);
        var pendingGap = 0d;

        foreach (var spec in specs)
        {
            switch (spec)
            {
                case ChipInlineSpec chip:
                    items.Add(new ChipInlineItem(
                        chip.Label,
                        Math.Ceiling(MeasureSingleLineWidth(PretextLayout.PrepareWithSegments(chip.Label, ChipFont))) + 22,
                        pendingGap,
                        chip.Foreground,
                        chip.Border,
                        chip.Background));
                    pendingGap = 0;
                    break;

                case TextInlineSpec text:
                    var carryGap = pendingGap;
                    var hasLeadingWhitespace = !string.IsNullOrEmpty(text.Text) && char.IsWhiteSpace(text.Text[0]);
                    var hasTrailingWhitespace = !string.IsNullOrEmpty(text.Text) && char.IsWhiteSpace(text.Text[^1]);
                    var trimmedText = text.Text.Trim();
                    pendingGap = hasTrailingWhitespace ? InlineBoundaryGap : 0;
                    if (trimmedText.Length == 0)
                    {
                        break;
                    }

                    var prepared = PretextLayout.PrepareWithSegments(trimmedText, text.Font);
                    var wholeLine = PretextLayout.LayoutNextLine(prepared, LineStartCursor, UnboundedWidth);
                    if (wholeLine is null)
                    {
                        break;
                    }

                    items.Add(new TextInlineItem(
                        text.Kind,
                        text.ChromeWidth,
                        carryGap > 0 || hasLeadingWhitespace ? InlineBoundaryGap : 0,
                        wholeLine.End,
                        wholeLine.Text,
                        wholeLine.Width,
                        prepared));
                    break;
            }
        }

        return items;
    }

    private static double MeasureSingleLineWidth(PreparedTextWithSegments prepared)
    {
        var maxWidth = 0d;
        PretextLayout.WalkLineRanges(prepared, UnboundedWidth, line =>
        {
            if (line.Width > maxWidth)
            {
                maxWidth = line.Width;
            }
        });
        return maxWidth;
    }

    private static double MeasureCollapsedSpaceWidth(string font)
    {
        var joinedWidth = MeasureSingleLineWidth(PretextLayout.PrepareWithSegments("A A", font));
        var compactWidth = MeasureSingleLineWidth(PretextLayout.PrepareWithSegments("AA", font));
        return Math.Max(0, joinedWidth - compactWidth);
    }

    private List<RichLine> LayoutInlineItems(double maxWidth)
    {
        var lines = new List<RichLine>();
        var safeWidth = Math.Max(1, maxWidth);
        var itemIndex = 0;
        LayoutCursor? textCursor = null;

        while (itemIndex < _items.Count)
        {
            var fragments = new List<InlineFragment>();
            var lineWidth = 0d;
            var remainingWidth = safeWidth;

            while (itemIndex < _items.Count)
            {
                switch (_items[itemIndex])
                {
                    case ChipInlineItem chip:
                    {
                        var leadingGap = fragments.Count == 0 ? 0 : chip.LeadingGap;
                        if (fragments.Count > 0 && leadingGap + chip.Width > remainingWidth)
                        {
                            goto FinishLine;
                        }

                        fragments.Add(new ChipFragment(leadingGap, chip.Text, chip.Foreground, chip.Border, chip.Background));
                        lineWidth += leadingGap + chip.Width;
                        remainingWidth = Math.Max(0, safeWidth - lineWidth);
                        itemIndex++;
                        textCursor = null;
                        continue;
                    }

                    case TextInlineItem text:
                    {
                        if (textCursor is not null && CursorsMatch(textCursor.Value, text.EndCursor))
                        {
                            itemIndex++;
                            textCursor = null;
                            continue;
                        }

                        var leadingGap = fragments.Count == 0 ? 0 : text.LeadingGap;
                        var reservedWidth = leadingGap + text.ChromeWidth;
                        if (fragments.Count > 0 && reservedWidth >= remainingWidth)
                        {
                            goto FinishLine;
                        }

                        if (textCursor is null)
                        {
                            var fullWidth = leadingGap + text.FullWidth + text.ChromeWidth;
                            if (fullWidth <= remainingWidth)
                            {
                                fragments.Add(new TextFragment(text.Kind, leadingGap, text.FullText));
                                lineWidth += fullWidth;
                                remainingWidth = Math.Max(0, safeWidth - lineWidth);
                                itemIndex++;
                                continue;
                            }
                        }

                        var startCursor = textCursor ?? LineStartCursor;
                        var line = PretextLayout.LayoutNextLine(text.Prepared, startCursor, Math.Max(1, remainingWidth - reservedWidth));
                        if (line is null || CursorsMatch(startCursor, line.End))
                        {
                            itemIndex++;
                            textCursor = null;
                            continue;
                        }

                        fragments.Add(new TextFragment(text.Kind, leadingGap, line.Text));
                        lineWidth += leadingGap + line.Width + text.ChromeWidth;
                        remainingWidth = Math.Max(0, safeWidth - lineWidth);

                        if (CursorsMatch(line.End, text.EndCursor))
                        {
                            itemIndex++;
                            textCursor = null;
                            continue;
                        }

                        textCursor = line.End;
                        goto FinishLine;
                    }
                }
            }

        FinishLine:
            if (fragments.Count == 0)
            {
                break;
            }

            lines.Add(new RichLine(fragments));
        }

        return lines;
    }

    private static bool CursorsMatch(LayoutCursor a, LayoutCursor b)
    {
        return a.SegmentIndex == b.SegmentIndex && a.GraphemeIndex == b.GraphemeIndex;
    }

    private abstract record InlineSpec;

    private sealed record TextInlineSpec(string Text, string Font, TextKind Kind, double ChromeWidth = 0) : InlineSpec;

    private sealed record ChipInlineSpec(string Label, Brush Foreground, Brush Border, Brush Background) : InlineSpec;

    private abstract record InlineItem;

    private sealed record TextInlineItem(
        TextKind Kind,
        double ChromeWidth,
        double LeadingGap,
        LayoutCursor EndCursor,
        string FullText,
        double FullWidth,
        PreparedTextWithSegments Prepared) : InlineItem;

    private sealed record ChipInlineItem(
        string Text,
        double Width,
        double LeadingGap,
        Brush Foreground,
        Brush Border,
        Brush Background) : InlineItem;

    private abstract record InlineFragment(double LeadingGap);

    private sealed record TextFragment(TextKind Kind, double LeadingGap, string Text) : InlineFragment(LeadingGap);

    private sealed record ChipFragment(double LeadingGap, string Text, Brush Foreground, Brush Border, Brush Background) : InlineFragment(LeadingGap);

    private sealed record RichLine(IReadOnlyList<InlineFragment> Fragments);

    private enum TextKind
    {
        Body,
        Link,
        Code,
    }
}
