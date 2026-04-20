using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;

namespace PretextSamples.Samples;

public sealed class RichNoteSampleView : UserControl
{
    private const double ToolbarCornerRadius = 18;
    private const double NoteShellCornerRadius = 20;

    private readonly Slider _slider = new()
    {
        Minimum = RichNoteModel.BodyMinWidth,
        Maximum = RichNoteModel.BodyMaxWidth,
        Value = RichNoteModel.BodyDefaultWidth,
    };

    private readonly TextBlock _sliderValue = new() { Foreground = SampleTheme.MutedBrush };
    private readonly Canvas _noteCanvas = new();
    private readonly Border _shell;
    private readonly PreparedRichInlineNote _preparedNote;
    private readonly FrameworkElement _pageRoot;
    private readonly UiRenderScheduler _renderScheduler;
    private double _lastBodyWidth = -1;
    private double _lastMaxBodyWidth = -1;

    public RichNoteSampleView()
    {
        _preparedNote = RichNoteModel.PrepareRichInlineNote();
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);

        var stack = SampleUi.CreatePageStack();
        var header = SampleUi.CreateHeader(
            "DEMO",
            "Rich text fragments that still wrap",
            "The sample now uses the core rich-inline helper directly. Text, links, and code spans split across lines, while chips stay atomic and still participate in the same inline flow.");
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

        var (bodyWidth, maxBodyWidth) = RichNoteModel.ResolveRichNoteBodyWidth(_pageRoot.ActualWidth, _slider.Value);
        if (Math.Abs(bodyWidth - _lastBodyWidth) < 0.5 &&
            Math.Abs(maxBodyWidth - _lastMaxBodyWidth) < 0.5)
        {
            return;
        }

        _lastBodyWidth = bodyWidth;
        _lastMaxBodyWidth = maxBodyWidth;
        _slider.Maximum = maxBodyWidth;
        _slider.Value = bodyWidth;
        _sliderValue.Text = $"{Math.Round(bodyWidth)}px";

        var layout = RichNoteModel.LayoutRichNote(_preparedNote, bodyWidth);

        _noteCanvas.Width = layout.BodyWidth;
        _noteCanvas.Height = layout.NoteBodyHeight;
        _noteCanvas.Children.Clear();

        for (var lineIndex = 0; lineIndex < layout.Lines.Count; lineIndex++)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0,
            };

            var line = layout.Lines[lineIndex];
            for (var fragmentIndex = 0; fragmentIndex < line.Fragments.Count; fragmentIndex++)
            {
                var fragment = line.Fragments[fragmentIndex];
                var element = BuildFragment(fragment);
                if (fragment.LeadingGap > 0)
                {
                    element.Margin = new Thickness(fragment.LeadingGap, 0, 0, 0);
                }

                row.Children.Add(element);
            }

            Canvas.SetTop(row, lineIndex * RichNoteModel.LineHeight);
            _noteCanvas.Children.Add(row);
        }

        _shell.Width = layout.NoteWidth;
    }

    private static FrameworkElement BuildFragment(RichNoteFragment fragment)
    {
        return fragment.ClassName switch
        {
            "chip--mention" => BuildChip(fragment.Text, SampleTheme.Brush(0x15, 0x5A, 0x88), SampleTheme.Brush(0x15, 0x5A, 0x88), SampleTheme.Brush(0xE8, 0xF1, 0xF6)),
            "chip--status" => BuildChip(fragment.Text, SampleTheme.Brush(0x35, 0x5F, 0x38), SampleTheme.Brush(0x46, 0x76, 0x4D), SampleTheme.Brush(0xEB, 0xF2, 0xEB)),
            "chip--priority" => BuildChip(fragment.Text, SampleTheme.Brush(0x8E, 0x23, 0x23), SampleTheme.Brush(0xB0, 0x2C, 0x2C), SampleTheme.Brush(0xF6, 0xE7, 0xE7)),
            "chip--time" => BuildChip(fragment.Text, SampleTheme.Brush(0x48, 0x3E, 0x83), SampleTheme.Brush(0x43, 0x39, 0x7A), SampleTheme.Brush(0xEF, 0xED, 0xF8)),
            "chip--count" => BuildChip(fragment.Text, SampleTheme.Brush(0x48, 0x3E, 0x83), SampleTheme.Brush(0x43, 0x39, 0x7A), SampleTheme.Brush(0xEF, 0xED, 0xF8)),
            "code" => new Border
            {
                Background = SampleTheme.AccentSoftBrush,
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(7, 2, 7, 3),
                Child = new TextBlock
                {
                    Text = fragment.Text,
                    Foreground = SampleTheme.InkBrush,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                },
            },
            "link" => new TextBlock
            {
                Text = fragment.Text,
                Foreground = SampleTheme.AccentBrush,
                FontFamily = new FontFamily("Helvetica Neue"),
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                TextDecorations = Windows.UI.Text.TextDecorations.Underline,
            },
            _ => new TextBlock
            {
                Text = fragment.Text,
                Foreground = SampleTheme.InkBrush,
                FontFamily = new FontFamily("Helvetica Neue"),
                FontSize = 17,
                FontWeight = FontWeights.Normal,
                TextDecorations = Windows.UI.Text.TextDecorations.None,
            },
        };
    }

    private static Border BuildChip(string text, Brush foreground, Brush border, Brush background)
    {
        return new Border
        {
            Background = background,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4, 10, 4),
            RenderTransform = new TranslateTransform { Y = -1 },
            Child = new TextBlock
            {
                Text = text,
                Foreground = foreground,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
            },
        };
    }
}
