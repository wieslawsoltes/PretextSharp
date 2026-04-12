using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace PretextSamples.Samples;

internal static class SampleTheme
{
    public static readonly SolidColorBrush PageBrush = Brush(0xF5, 0xF2, 0xEC);
    public static readonly SolidColorBrush PanelBrush = Brush(0xFF, 0xFD, 0xF9);
    public static readonly SolidColorBrush InkBrush = Brush(0x20, 0x1B, 0x18);
    public static readonly SolidColorBrush MutedBrush = Brush(0x6D, 0x64, 0x5D);
    public static readonly SolidColorBrush RuleBrush = Brush(0xD8, 0xCE, 0xC3);
    public static readonly SolidColorBrush AccentBrush = Brush(0x95, 0x5F, 0x3B);
    public static readonly SolidColorBrush AccentSoftBrush = Brush(0xF0, 0xE4, 0xDA);
    public static readonly SolidColorBrush ChatBackgroundBrush = Brush(0x1C, 0x1C, 0x1E);
    public static readonly SolidColorBrush SentBubbleBrush = Brush(0x0B, 0x84, 0xFE);
    public static readonly SolidColorBrush ReceiveBubbleBrush = Brush(0x2C, 0x2C, 0x2E);
    public static readonly SolidColorBrush WhiteBrush = Brush(0xFF, 0xFF, 0xFF);

    public static SolidColorBrush Brush(byte r, byte g, byte b) => new(ColorHelper.FromArgb(255, r, g, b));

    public static SolidColorBrush Brush(byte a, byte r, byte g, byte b) => new(ColorHelper.FromArgb(a, r, g, b));
}

internal static class SampleUi
{
    public static FrameworkElement CreatePageRoot(UIElement content)
    {
        var host = new StretchScrollHost(content)
        {
            Background = SampleTheme.PageBrush,
            ContentBackground = SampleTheme.PageBrush,
        };
        return host;
    }

    public static StackPanel CreatePageStack()
    {
        return new StackPanel
        {
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
    }

    public static StackPanel CreateHeader(string eyebrow, string title, string description)
    {
        var stack = new StackPanel
        {
            Spacing = 8,
            MaxWidth = 920,
        };

        stack.Children.Add(new TextBlock
        {
            Text = eyebrow,
            Foreground = SampleTheme.AccentBrush,
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            CharacterSpacing = 120,
            TextWrapping = TextWrapping.NoWrap,
        });

        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = SampleTheme.InkBrush,
            FontSize = 32,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Georgia"),
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        stack.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = SampleTheme.MutedBrush,
            FontSize = 15,
            TextWrapping = TextWrapping.WrapWholeWords,
            MaxWidth = 720,
        });

        return stack;
    }

    public static Border CreateCard(UIElement content, double padding = 18)
    {
        return new Border
        {
            Background = SampleTheme.PanelBrush,
            BorderBrush = SampleTheme.RuleBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(padding),
            Child = content,
        };
    }

    public static TextBlock CreateBodyText(string text, double fontSize = 15)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = SampleTheme.MutedBrush,
            FontSize = fontSize,
            TextWrapping = TextWrapping.WrapWholeWords,
        };
    }

    public static TextBlock CreateCanvasLine(string text, string fontFamily, double fontSize, Brush brush, Windows.UI.Text.FontWeight? weight = null)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = brush,
            FontFamily = new FontFamily(fontFamily),
            FontSize = fontSize,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.NoWrap,
        };
    }

    public static void EnsurePool<T>(Panel panel, List<T> pool, int count, Func<T> factory) where T : UIElement
    {
        while (pool.Count < count)
        {
            var element = factory();
            pool.Add(element);
            panel.Children.Add(element);
        }

        for (var index = 0; index < pool.Count; index++)
        {
            pool[index].Visibility = index < count ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
