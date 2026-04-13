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
        cards.Children.Add(BuildFeatureCard("Accordion", "Predicted text heights drive section metadata without measuring the visible layout tree."));
        cards.Children.Add(BuildFeatureCard("Bubbles", "Binary-search shrinkwrap produces tighter multiline chat bubbles than width-to-widest-line sizing."));
        cards.Children.Add(BuildFeatureCard("Masonry", "Card heights come from the layout engine, so the grid can place content before the UI tree measures it."));
        cards.Children.Add(BuildFeatureCard("Rich Text", "Inline text, code spans, and atomic chips share one flow while only the text fragments split across lines."));
        cards.Children.Add(BuildFeatureCard("Markdown Chat", "A 10k-message markdown conversation uses exact-height prediction and a manually virtualized visible window instead of DOM or UI-tree measurement."));
        cards.Children.Add(BuildFeatureCard("Dynamic Layout", "A fixed-height editorial spread with obstacle-aware title routing and continuous flow."));
        cards.Children.Add(BuildFeatureCard("Editorial Engine", "Animated orbs, live text reflow, pull quotes, and multi-column flow with zero UI-tree measurements."));
        cards.Children.Add(BuildFeatureCard("Justification Comparison", "Greedy, hyphenated, and optimal paragraph breaking appear side by side so rivers and spacing variance are easy to compare."));
        cards.Children.Add(BuildFeatureCard("Variable ASCII", "A particle field is rendered twice: once with a monospace ramp and once with proportional glyph choices."));

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
