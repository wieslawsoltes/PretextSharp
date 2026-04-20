namespace PretextSamples.Samples;

public sealed class OverviewSampleView : UserControl
{
    public OverviewSampleView()
    {
        var stack = SampleUi.CreatePageStack();
        stack.Children.Add(SampleUi.CreateHeader(
            "PRETEXT",
            "Uno samples for manual text layout",
            "This port keeps the library-style API shape from the original project and recreates the demo surface in native Uno views. The pages below focus on predicted line counts, shrinkwrap widths, manual line routing, and custom editorial geometry."));

        var cards = new StackPanel { Spacing = 16 };
        foreach (var feature in SampleCatalog.OverviewFeatures)
        {
            cards.Children.Add(BuildFeatureCard(feature.Title, feature.Summary));
        }

        stack.Children.Add(SampleUi.CreateCard(cards));
        Content = SampleUi.CreatePageRoot(stack);
    }

    private static Border BuildFeatureCard(string title, string body)
    {
        var cardStack = new StackPanel { Spacing = 8 };
        cardStack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = SampleTheme.InkBrush,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
        });
        cardStack.Children.Add(SampleUi.CreateBodyText(body));
        return SampleUi.CreateCard(cardStack, 16);
    }
}
