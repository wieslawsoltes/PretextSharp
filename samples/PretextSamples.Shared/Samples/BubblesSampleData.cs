namespace PretextSamples.Samples;

public sealed record BubbleMessage(bool Sent, string Text);

public static class BubblesSampleData
{
    public static readonly IReadOnlyList<BubbleMessage> Messages =
    [
        new(false, "Yo did you see the new Pretext library?"),
        new(true, "yeah! It measures text without the DOM. Pure JavaScript arithmetic"),
        new(false, "That shrinkwrap demo is wild it finds the exact minimum width for multiline text. CSS can't do that."),
        new(true, "성능 최적화가 정말 많이 되었더라고요 🎉"),
        new(false, "Oh wow it handles CJK and emoji too??"),
        new(true, "كل شيء! Mixed bidi, grapheme clusters, whatever you want. Try resizing"),
        new(true, "the best part: zero layout reflow. You could shrinkwrap 10,000 bubbles and the browser wouldn't even blink"),
    ];
}
