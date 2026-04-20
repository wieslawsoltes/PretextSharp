namespace PretextSamples.Samples;

public sealed record SampleDescriptor(string Tag, string Title, string Summary);

public sealed record OverviewFeature(string Title, string Summary);

public static class SampleCatalog
{
    public static readonly IReadOnlyList<SampleDescriptor> Samples =
    [
        new("overview", "Overview", "Manual text layout scenarios built on top of Pretext."),
        new("accordion", "Accordion", "Predicted text heights drive section expansion without measuring the visible text tree."),
        new("bubbles", "Bubbles", "Binary-search shrinkwrap finds tighter multiline chat bubbles than widest-line sizing."),
        new("masonry", "Masonry", "Card heights are computed ahead of time so the grid places content before the UI tree measures."),
        new("rich", "Rich Text", "Text, links, code spans, and atomic chips share the same inline flow."),
        new("markdown-chat", "Markdown Chat", "A 10k-message conversation uses exact-height prediction and virtualization."),
        new("dynamic", "Dynamic Layout", "Obstacle-aware editorial layout responds to live geometry and window changes."),
        new("editorial", "Editorial Engine", "Animated obstacles, pull quotes, and column flow re-layout in real time."),
        new("justification", "Justification Comparison", "Greedy, hyphenated, and optimal line breaking appear side by side."),
        new("ascii", "Variable ASCII", "A particle field is rendered through monospace and proportional ASCII projections."),
    ];

    public static readonly IReadOnlyList<OverviewFeature> OverviewFeatures =
    [
        new("Accordion", "Predicted text heights drive section metadata without measuring the visible layout tree."),
        new("Bubbles", "Binary-search shrinkwrap produces tighter multiline chat bubbles than width-to-widest-line sizing."),
        new("Masonry", "Card heights come from the layout engine, so the grid can place content before the UI tree measures it."),
        new("Rich Text", "Inline text, code spans, and atomic chips share one flow while only the text fragments split across lines."),
        new("Markdown Chat", "A 10k-message markdown conversation uses exact-height prediction and a manually virtualized visible window."),
        new("Dynamic Layout", "A fixed-height editorial spread with obstacle-aware title routing and continuous flow."),
        new("Editorial Engine", "Animated orbs, live text reflow, pull quotes, and multi-column flow with zero UI-tree measurements."),
        new("Justification Comparison", "Greedy, hyphenated, and optimal paragraph breaking appear side by side so rivers and spacing variance are easy to compare."),
        new("Variable ASCII", "A particle field is rendered twice: once with a monospace ramp and once with proportional glyph choices."),
    ];
}
