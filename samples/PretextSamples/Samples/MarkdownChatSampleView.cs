using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace PretextSamples.Samples;

public sealed class MarkdownChatSampleView : UserControl
{
    private readonly IReadOnlyList<PreparedChatTemplate> _templates = MarkdownChatModel.CreatePreparedChatTemplates();
    private readonly Grid _shell;
    private readonly ScrollViewer _viewport;
    private readonly Canvas _canvas;
    private readonly Border _topOcclusion;
    private readonly Border _bottomOcclusion;
    private readonly Button _toggleButton;
    private readonly TextBlock _statsText;
    private readonly Slider _widthSlider;
    private readonly TextBlock _widthValue;
    private readonly FrameworkElement _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;

    private ConversationFrame? _frame;
    private bool _visualizationEnabled;
    private double? _pendingScrollOffset;

    public MarkdownChatSampleView()
    {
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);

        _toggleButton = new Button
        {
            Content = "Show virtualization mask",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(14, 10, 14, 10),
            Background = new SolidColorBrush(ColorHelper.FromArgb(235, 57, 64, 72)),
            Foreground = SampleTheme.WhiteBrush,
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(25, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(999),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
        };
        _toggleButton.Click += (_, _) =>
        {
            _visualizationEnabled = !_visualizationEnabled;
            _renderScheduler.Schedule();
        };

        _statsText = SampleUi.CreateBodyText(string.Empty, 13);
        _statsText.Foreground = SampleTheme.Brush(0xCF, 0xD7, 0xE1);

        _widthSlider = new Slider
        {
            Minimum = MarkdownChatModel.MinChatWidth,
            Maximum = MarkdownChatModel.MaxChatWidth,
            Value = MarkdownChatModel.DefaultChatWidth,
            StepFrequency = 1,
            SmallChange = 1,
            LargeChange = 16,
            Width = 220,
        };
        _widthValue = new TextBlock
        {
            Foreground = SampleTheme.Brush(0xCF, 0xD7, 0xE1),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _widthSlider.ValueChanged += (_, _) => _renderScheduler.Schedule();

        _canvas = new Canvas();
        _viewport = new ScrollViewer
        {
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollMode = ScrollMode.Enabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _canvas,
        };
        _viewport.ViewChanged += (_, _) => _renderScheduler.Schedule();

        _topOcclusion = new Border
        {
            VerticalAlignment = VerticalAlignment.Top,
            Background = SampleTheme.Brush(0xF2, 0x33, 0x37, 0x40),
            Child = new Grid
            {
                Children =
                {
                    _toggleButton,
                },
            },
            IsHitTestVisible = true,
        };

        _bottomOcclusion = new Border
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = SampleTheme.Brush(0xF2, 0x33, 0x37, 0x40),
            IsHitTestVisible = false,
        };

        _shell = new Grid
        {
            Background = SampleTheme.Brush(0x33, 0x37, 0x40),
            Height = MarkdownChatModel.ChatViewportHeight,
            MaxWidth = MarkdownChatModel.MaxChatWidth + 80,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _shell.Children.Add(_viewport);
        _shell.Children.Add(_topOcclusion);
        _shell.Children.Add(_bottomOcclusion);

        var controls = new StackPanel
        {
            Spacing = 10,
        };
        controls.Children.Add(new TextBlock
        {
            Text = "Chat width",
            Foreground = SampleTheme.MutedBrush,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
        });

        var widthRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
        };
        widthRow.Children.Add(_widthSlider);
        widthRow.Children.Add(_widthValue);
        controls.Children.Add(widthRow);
        controls.Children.Add(_statsText);

        var controlsCard = SampleUi.CreateCard(controls, 16);
        controlsCard.MaxWidth = 760;
        controlsCard.Background = SampleTheme.Brush(0x3A, 0x40, 0x48);
        controlsCard.BorderBrush = SampleTheme.Brush(0x56, 0x5C, 0x66);

        var stack = SampleUi.CreatePageStack();
        var header = SampleUi.CreateHeader(
            "DEMO",
            "Virtualized markdown chat",
            "A 10k-message chat surface built from exact Pretext height prediction. Markdown blocks are parsed once, measured into reusable templates, and only the visible message window is materialized into Uno elements.");
        header.MaxWidth = 760;
        header.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(header);
        stack.Children.Add(controlsCard);
        stack.Children.Add(_shell);

        _pageRoot = SampleUi.CreatePageRoot(stack);
        Content = _pageRoot;
        Loaded += (_, _) => _renderScheduler.Schedule();
        SizeChanged += (_, _) => _renderScheduler.Schedule();
    }

    private void Render()
    {
        if (_pageRoot.ActualWidth <= 0 || _shell.ActualHeight <= 0)
        {
            return;
        }

        var viewportWidth = _shell.ActualWidth;
        var viewportHeight = _shell.ActualHeight;
        var requestedChatWidth = Math.Round(_widthSlider.Value);
        var maxChatWidth = MarkdownChatModel.GetMaxChatWidth(viewportWidth);
        var chatWidth = Math.Max(MarkdownChatModel.MinChatWidth, Math.Min(maxChatWidth, requestedChatWidth));
        var occlusionHeight = MarkdownChatModel.GetOcclusionBannerHeight(viewportHeight);

        var previousFrame = _frame;
        var canReuseFrame = previousFrame is not null &&
                            Math.Abs(previousFrame.ChatWidth - chatWidth) < 0.5 &&
                            Math.Abs(previousFrame.OcclusionBannerHeight - occlusionHeight) < 0.5;

        if (!canReuseFrame)
        {
            if (previousFrame is not null)
            {
                var oldScrollableHeight = Math.Max(0, previousFrame.TotalHeight - viewportHeight);
                if (oldScrollableHeight > 0)
                {
                    var ratio = _viewport.VerticalOffset / oldScrollableHeight;
                    var newFrame = MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, occlusionHeight);
                    var newScrollableHeight = Math.Max(0, newFrame.TotalHeight - viewportHeight);
                    _pendingScrollOffset = ratio * newScrollableHeight;
                    _frame = newFrame;
                }
                else
                {
                    _frame = MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, occlusionHeight);
                }
            }
            else
            {
                _frame = MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, occlusionHeight);
            }
        }

        var frame = _frame ??= MarkdownChatModel.BuildConversationFrame(_templates, chatWidth, occlusionHeight);
        var topMask = occlusionHeight;
        var bottomMask = occlusionHeight;

        _widthSlider.Maximum = maxChatWidth;
        _widthSlider.Value = chatWidth;
        _widthValue.Text = $"{Math.Round(chatWidth)}px";
        _statsText.Text = $"10k messages · visible window only · canvas height {Math.Round(frame.TotalHeight):N0}px";

        _canvas.Width = frame.ChatWidth;
        _canvas.Height = frame.TotalHeight;
        _topOcclusion.Height = topMask;
        _bottomOcclusion.Height = bottomMask;
        _toggleButton.Content = _visualizationEnabled ? "Hide virtualization mask" : "Show virtualization mask";

        var occlusionBrush = _visualizationEnabled
            ? SampleTheme.Brush(0x9C, 0x33, 0x37, 0x40)
            : SampleTheme.Brush(0xF2, 0x33, 0x37, 0x40);
        _topOcclusion.Background = occlusionBrush;
        _bottomOcclusion.Background = occlusionBrush;

        if (_pendingScrollOffset is double offset)
        {
            _pendingScrollOffset = null;
            _viewport.ChangeView(null, offset, null, true);
        }

        var (start, end) = MarkdownChatModel.FindVisibleRange(frame, _viewport.VerticalOffset, viewportHeight, topMask, bottomMask);
        RenderVisibleRows(frame, start, end);
    }

    private void RenderVisibleRows(ConversationFrame frame, int start, int end)
    {
        _canvas.Children.Clear();

        for (var index = start; index < end; index++)
        {
            var message = frame.Messages[index];
            var row = BuildMessageRow(frame.ChatWidth, message);
            Canvas.SetTop(row, message.Top);
            _canvas.Children.Add(row);
        }
    }

    private static FrameworkElement BuildMessageRow(double chatWidth, ChatMessageInstance message)
    {
        var row = new Grid
        {
            Width = chatWidth,
            Height = message.Frame.TotalHeight,
            Padding = new Thickness(MarkdownChatModel.MessageSidePadding, 0, MarkdownChatModel.MessageSidePadding, 0),
        };

        var bubbleCanvas = new Canvas
        {
            Width = message.Frame.FrameWidth,
            Height = message.Frame.BubbleHeight,
        };

        var bubble = new Border
        {
            Width = message.Frame.FrameWidth,
            Height = message.Frame.BubbleHeight,
            HorizontalAlignment = message.Frame.Role == ChatRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = message.Frame.Role == ChatRole.User
                ? SampleTheme.Brush(0x39, 0x40, 0x48)
                : new SolidColorBrush(Colors.Transparent),
            BorderBrush = message.Frame.Role == ChatRole.User
                ? SampleTheme.Brush(0x22, 0xFF, 0xFF, 0xFF)
                : new SolidColorBrush(Colors.Transparent),
            BorderThickness = message.Frame.Role == ChatRole.User ? new Thickness(1) : new Thickness(0),
            CornerRadius = new CornerRadius(message.Frame.Role == ChatRole.User ? 16 : 0),
            Child = bubbleCanvas,
        };

        row.Children.Add(bubble);
        foreach (var block in MarkdownChatModel.MaterializeTemplateBlocks(message))
        {
            AddBlockVisuals(bubbleCanvas, message.Frame.ContentInsetX, message.Frame.Role, block);
        }

        return row;
    }

    private static void AddBlockVisuals(Canvas bubbleCanvas, double contentInsetX, ChatRole role, BlockLayout block)
    {
        foreach (var railLeft in block.QuoteRailLefts)
        {
            var rail = new Border
            {
                Width = 3,
                Height = block.Height,
                Background = SampleTheme.Brush(0x2F, 0x9E, 0xA6, 0xB2),
                CornerRadius = new CornerRadius(999),
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(rail, contentInsetX + railLeft);
            Canvas.SetTop(rail, block.Top);
            bubbleCanvas.Children.Add(rail);
        }

        if (block.MarkerText is not null && block.MarkerLeft is double markerLeft)
        {
            var marker = new TextBlock
            {
                Text = block.MarkerText,
                Foreground = SampleTheme.Brush(0x9E, 0xA6, 0xB2),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
            };
            Canvas.SetLeft(marker, contentInsetX + markerLeft);
            Canvas.SetTop(marker, block.KindMarkerTop());
            bubbleCanvas.Children.Add(marker);
        }

        switch (block)
        {
            case InlineBlockLayout inlineBlock:
                for (var lineIndex = 0; lineIndex < inlineBlock.Lines.Count; lineIndex++)
                {
                    var row = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 0,
                    };
                    var line = inlineBlock.Lines[lineIndex];
                    foreach (var fragment in line.Fragments)
                    {
                        var element = BuildInlineFragment(fragment);
                        if (fragment.LeadingGap > 0)
                        {
                            element.Margin = new Thickness(fragment.LeadingGap, 0, 0, 0);
                        }

                        row.Children.Add(element);
                    }

                    Canvas.SetLeft(row, contentInsetX + inlineBlock.ContentLeft);
                    Canvas.SetTop(row, inlineBlock.Top + lineIndex * inlineBlock.LineHeight);
                    bubbleCanvas.Children.Add(row);
                }
                break;

            case CodeBlockLayout codeBlock:
            {
                var codeBox = new Border
                {
                    Width = codeBlock.Width,
                    Height = codeBlock.Height,
                    Background = role == ChatRole.User
                        ? SampleTheme.Brush(0x31, 0x38, 0x40)
                        : SampleTheme.Brush(0x31, 0x38, 0x40),
                    BorderBrush = SampleTheme.Brush(0x12, 0xFF, 0xFF, 0xFF),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                };
                var codeCanvas = new Canvas
                {
                    Width = codeBlock.Width,
                    Height = codeBlock.Height,
                };
                codeBox.Child = codeCanvas;

                for (var lineIndex = 0; lineIndex < codeBlock.Lines.Count; lineIndex++)
                {
                    var line = codeBlock.Lines[lineIndex];
                    var text = new TextBlock
                    {
                        Text = line.Text,
                        Foreground = SampleTheme.Brush(0xD5, 0xD9, 0xE1),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12,
                        LineHeight = MarkdownChatModel.CodeLineHeight,
                        TextWrapping = TextWrapping.NoWrap,
                    };
                    Canvas.SetLeft(text, MarkdownChatModel.CodeBlockPaddingX);
                    Canvas.SetTop(text, MarkdownChatModel.CodeBlockPaddingY + lineIndex * MarkdownChatModel.CodeLineHeight);
                    codeCanvas.Children.Add(text);
                }

                Canvas.SetLeft(codeBox, contentInsetX + codeBlock.ContentLeft);
                Canvas.SetTop(codeBox, codeBlock.Top);
                bubbleCanvas.Children.Add(codeBox);
                break;
            }

            case RuleBlockLayout ruleBlock:
            {
                var rule = new Rectangle
                {
                    Width = ruleBlock.Width,
                    Height = 1,
                    Fill = SampleTheme.Brush(0x45, 0x4B, 0x55),
                    IsHitTestVisible = false,
                };
                Canvas.SetLeft(rule, contentInsetX + ruleBlock.ContentLeft);
                Canvas.SetTop(rule, ruleBlock.Top + Math.Floor(ruleBlock.Height / 2));
                bubbleCanvas.Children.Add(rule);
                break;
            }
        }
    }

    private static FrameworkElement BuildInlineFragment(InlineFragmentLayout fragment)
    {
        if (fragment.ClassName.Contains("frag--code", StringComparison.Ordinal))
        {
            return new Border
            {
                Background = SampleTheme.Brush(0x31, 0x38, 0x40),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 3),
                Child = new TextBlock
                {
                    Text = fragment.Text,
                    Foreground = SampleTheme.Brush(0xD5, 0xD9, 0xE1),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                },
            };
        }

        if (fragment.ClassName.Contains("frag--chip", StringComparison.Ordinal))
        {
            return new Border
            {
                Background = SampleTheme.Brush(0x31, 0x38, 0x40),
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(7, 0, 7, 0),
                MinHeight = 18,
                Child = new TextBlock
                {
                    Text = fragment.Text,
                    Foreground = SampleTheme.Brush(0xB7, 0xC0, 0xCF),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                },
            };
        }

        var text = new TextBlock
        {
            Text = fragment.Text,
            Foreground = fragment.ClassName.Contains("is-link", StringComparison.Ordinal)
                ? SampleTheme.Brush(0xB7, 0xC0, 0xCF)
                : SampleTheme.Brush(0xD5, 0xD9, 0xE1),
            FontStyle = fragment.ClassName.Contains("is-em", StringComparison.Ordinal) ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal,
            TextWrapping = TextWrapping.NoWrap,
        };

        if (fragment.ClassName.Contains("frag--heading-1", StringComparison.Ordinal))
        {
            text.FontFamily = new FontFamily("Georgia");
            text.FontSize = 20;
            text.FontWeight = fragment.ClassName.Contains("is-strong", StringComparison.Ordinal) ? FontWeights.ExtraBold : FontWeights.Bold;
        }
        else if (fragment.ClassName.Contains("frag--heading-2", StringComparison.Ordinal))
        {
            text.FontFamily = new FontFamily("Georgia");
            text.FontSize = 17;
            text.FontWeight = fragment.ClassName.Contains("is-strong", StringComparison.Ordinal) ? FontWeights.ExtraBold : FontWeights.Bold;
        }
        else
        {
            text.FontFamily = new FontFamily("Helvetica Neue");
            text.FontSize = 14;
            text.FontWeight = fragment.ClassName.Contains("is-strong", StringComparison.Ordinal) ? FontWeights.Bold : FontWeights.Normal;
        }

        var decorations = Windows.UI.Text.TextDecorations.None;
        if (fragment.ClassName.Contains("is-link", StringComparison.Ordinal))
        {
            decorations |= Windows.UI.Text.TextDecorations.Underline;
        }

        if (fragment.ClassName.Contains("is-del", StringComparison.Ordinal))
        {
            decorations |= Windows.UI.Text.TextDecorations.Strikethrough;
        }

        text.TextDecorations = decorations;
        return text;
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
