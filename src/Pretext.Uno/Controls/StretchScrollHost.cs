using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Pretext.Uno.Controls;

public sealed class StretchScrollHost : Grid
{
    private readonly Border _contentHost;
    private readonly ScrollViewer _scrollViewer;

    public StretchScrollHost(UIElement content)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _contentHost = new Border
        {
            Padding = new Thickness(28),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = content,
        };

        _scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _contentHost,
        };

        Children.Add(_scrollViewer);
        SizeChanged += OnSizeChanged;
    }

    public ScrollViewer ScrollViewer => _scrollViewer;

    public FrameworkElement ScrollContent => _contentHost;

    public Brush? ContentBackground
    {
        get => _contentHost.Background;
        set => _contentHost.Background = value;
    }

    public Thickness ContentPadding
    {
        get => _contentHost.Padding;
        set => _contentHost.Padding = value;
    }

    public bool TryGetLocalViewportBounds(FrameworkElement target, double overscan, out double top, out double bottom)
    {
        top = 0;
        bottom = 0;
        if (target.ActualHeight <= 0)
        {
            return false;
        }

        var viewportHeight = _scrollViewer.ActualHeight > 0 ? _scrollViewer.ActualHeight : ActualHeight;
        if (viewportHeight <= 0)
        {
            return false;
        }

        try
        {
            var origin = target.TransformToVisual(_contentHost).TransformPoint(new Point(0, 0));
            top = Math.Max(0, _scrollViewer.VerticalOffset - origin.Y - overscan);
            bottom = _scrollViewer.VerticalOffset + viewportHeight - origin.Y + overscan;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _contentHost.Width = Math.Max(0, e.NewSize.Width);
    }
}
