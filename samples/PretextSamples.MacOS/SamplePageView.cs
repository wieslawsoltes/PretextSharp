namespace PretextSamples.MacOS;

internal abstract class SamplePageView : NSView
{
    protected CGSize AvailableSize { get; private set; }

    public override bool IsFlipped => true;

    public void UpdateAvailableSize(CGSize availableSize)
    {
        AvailableSize = new CGSize(Math.Max(320, availableSize.Width), Math.Max(320, availableSize.Height));
        var measured = MeasurePage(AvailableSize);
        SetFrameSize(measured);
        LayoutPage(Bounds);
        NeedsDisplay = true;
    }

    protected void InvalidatePageLayout()
    {
        if (AvailableSize.Width > 0)
        {
            UpdateAvailableSize(AvailableSize);
        }
    }

    public override void Layout()
    {
        base.Layout();
        LayoutPage(Bounds);
    }

    protected abstract CGSize MeasurePage(CGSize availableSize);

    protected abstract void LayoutPage(CGRect bounds);
}
