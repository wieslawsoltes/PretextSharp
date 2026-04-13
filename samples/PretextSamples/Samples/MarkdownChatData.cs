namespace PretextSamples.Samples;

internal sealed record MarkdownChatSeed(
    string Role,
    string Markdown);

internal static class MarkdownChatData
{
    private static MarkdownChatSeed Message(string role, params string[] lines)
        => new(role, string.Join('\n', lines));

    public static readonly IReadOnlyList<MarkdownChatSeed> BaseMessageSpecs =
    [
        Message(
            "user",
            "Can we treat the rich-text inline flow helper (`rich-inline`) as a real primitive, or is it only good for one tiny demo?",
            "",
            "I mostly care about:",
            "- exact bubble heights",
            "- virtualization without DOM reads",
            "- markdown-ish inline styling"),
        Message(
            "assistant",
            "Short answer: **yes, inside a bounded corridor**.",
            "",
            "It already handles rich-text inline flow, `code`, and links like [Pretext](https://github.com/chenglou/pretext), while keeping pills and badges atomic. The real pressure starts once a chat bubble stops being one paragraph."),
        Message(
            "user",
            "Right. My side is usually short, but your side has the weird stuff: Beijing 北京, Arabic مرحبا, emoji 👩‍🚀, and long URLs like https://example.com/reports/q3?lang=ar&mode=full"),
        Message(
            "assistant",
            "### What a chat renderer actually needs",
            "",
            "1. Parse markdown somewhere else.",
            "2. Normalize it into blocks and inline runs.",
            "3. Use the rich-text inline flow helper (`rich-inline`) for paragraph-ish content.",
            "4. Use the `pre-wrap` path for fenced code."),
        Message(
            "user",
            "Then let’s stress it with **real markdown**: ***nested emphasis***, ~~deletions~~, `inline code`, [links](https://openai.com/), and a couple messages that are obviously richer on the AI side than on mine."),
        Message(
            "assistant",
            "> If we know the exact height in advance, then virtualization is no longer guesswork.",
            ">",
            "> It becomes geometry.",
            "",
            "That is the whole reason to keep the primitive low-level and composable."),
        Message(
            "user",
            "Okay, but the design matters too. The left side should feel lighter and more editorial, while my side can stay bubble-y."),
        Message(
            "assistant",
            "```ts",
            "const frame = buildConversationFrame(templates, width)",
            "const visible = findVisibleRange(frame, scrollTop, viewportHeight, topMask, bottomMask)",
            "renderMessages(frame, visible.start, visible.end)",
            "```"),
        Message(
            "user",
            "I also want code fences, quotes, and lists to show up often enough that the 10k-thread run actually teaches us something."),
        Message(
            "assistant",
            "That part is important.",
            "",
            "- paragraph layout is one leaf",
            "- code fences are another leaf",
            "- the chat message is the block-level container above both",
            "",
            "The assistant side is the real stress test because it keeps hitting headings, bullets, quotes, code fences, and occasional long explanations."),
        Message(
            "user",
            "Try a checklist too. A product chat is full of little status updates."),
        Message(
            "assistant",
            "Current polish pass:",
            "",
            "- lighter body copy is in",
            "- the assistant lane is bubble-less",
            "- exact height prediction is wired up",
            "- mobile screenshot smoke tests still remain"),
        Message(
            "user",
            "Can we keep top-level bullets flush? I do not want them shoved way in from the left like an old email client."),
        Message(
            "assistant",
            "Yes. The top-level list should read almost like paragraph rhythm with markers, not like a nested document outline.",
            "",
            "Nested lists can still step in when they actually nest."),
        Message(
            "user",
            "I want a structured status block too. It does not need table syntax if we are not really rendering tables here."),
        Message(
            "assistant",
            "```yaml",
            "paragraph_leaf: rich-text-inline-flow",
            "code_leaf: pre-wrap",
            "quote_wrapper: block shell",
            "virtualization: exact-height-first",
            "```"),
        Message(
            "user",
            "What about images or chips? Even if they are fake, I want to know the primitive can hold an atomic thing."),
        Message(
            "assistant",
            "It can. Something like ![diagram](https://example.com/mock-wireframe.png) behaves more like an inline chip than a splittable word, which is exactly the right stress case."),
        Message(
            "user",
            "Throw in a messy status message too: deploys, timestamps, a ticket number, and maybe one escaped quote like \\\"ship it\\\"."),
        Message(
            "assistant",
            "Status snapshot:",
            "",
            "- deploy window 7:00-9:00",
            "- owner `RICH-431`",
            "- locale mix `24×7` and `२४×७`",
            "- comment: \\\"ship it\\\" after the Safari check"),
        Message(
            "user",
            "I still think the width negotiation matters more than the parser. If widths are wrong, everything feels fake."),
        Message(
            "assistant",
            "Agreed. The parser is just an upstream producer.",
            "",
            "The hard contract is: once width and fonts are known, the layout layer should answer height exactly enough that virtualization never has to ask the DOM for help."),
        Message(
            "user",
            "Give me one answer that feels more structured, almost like a mini design review."),
        Message(
            "assistant",
            "## Design review",
            "",
            "The strongest signal so far is that **assistant messages want a different presentation contract from user messages**. The human side reads well as compact bubbles. The assistant side reads better as content on a surface with room to breathe.",
            "",
            "That split also maps nicely to the measurement model because user messages are usually short and AI responses are much more likely to hit rich block transitions."),
        Message(
            "user",
            "And one answer that feels operational, like we are handing this to another engineer."),
        Message(
            "assistant",
            "```json",
            "{",
            "  \"parser\": \"marked\",",
            "  \"paragraphLeaf\": \"rich-text-inline-flow\",",
            "  \"codeLeaf\": \"pre-wrap\",",
            "  \"virtualization\": \"exact-height-first\",",
            "  \"openRisk\": \"long-scroll-device-smoke\"",
            "}",
            "```"),
        Message(
            "user",
            "We should probably include a short answer too, otherwise every assistant message looks essay-shaped."),
        Message(
            "assistant",
            "Yep. Short answers are a useful canary because they should not inherit giant block spacing or overbuilt chrome."),
        Message(
            "user",
            "Try a message with HTML-ish content too. Not because we love it, just because users paste odd things."),
        Message(
            "assistant",
            "<aside>Escaped HTML still needs a stable fallback path.</aside>",
            "",
            "If it comes through as a block token, we can still route it through a plain-text or code-like presentation instead of pretending we have a full browser renderer."),
        Message(
            "user",
            "I also want more than one quote style in the loop. Otherwise we are tuning for a single canned example."),
        Message(
            "assistant",
            "> The nice version of exact measurement is smooth scrolling.",
            ">",
            "> The brutal version is that every wrong guess accumulates.",
            "",
            "> In a 10k thread, accumulation is the enemy."),
        Message(
            "user",
            "Can you make one answer about search or citations? AI chats have that vibe a lot now."),
        Message(
            "assistant",
            "Sure. A citation-heavy answer is mostly the same inline problem with a little more link density:",
            "",
            "- [spec note](https://example.com/spec)",
            "- [issue thread](https://example.com/issues/431)",
            "- [benchmark doc](https://example.com/benchmarks)"),
        Message(
            "user",
            "Maybe a message that mixes scripts more aggressively: English, 日本語, العربية, and a little emoji."),
        Message(
            "assistant",
            "Mixed-script sample: English for the framing, 日本語 for compact line breaks, العربية for punctuation clusters, and emoji like 🧪📐 to keep the grapheme path honest."),
        Message(
            "user",
            "I want at least one answer that uses nested bullets so we can see whether the second level still reads okay."),
        Message(
            "assistant",
            "Things to watch:",
            "",
            "- top-level bullets should feel close to body text",
            "- nested bullets can indent, but only when they truly nest",
            "  - this second level should not explode the width math",
            "  - the marker still needs a clean left gutter",
            "- quotes and code fences should tuck closer to the lead-in paragraph"),
        Message(
            "user",
            "Can we include a very plain human message too? Real threads have those."),
        Message(
            "assistant",
            "Absolutely."),
        Message(
            "user",
            "cool. also make sure the scroll still feels stable when widths change"),
        Message(
            "assistant",
            "That is one of the better parts of the demo right now: width changes rebuild the frame, preserve relative scroll intent, and remount only the visible window."),
        Message(
            "user",
            "One last thing: mention the hypothesis space. I still care a lot about not painting ourselves into a corner."),
        Message(
            "assistant",
            "The good version of this alpha API is not “we solved rich text.” It is “we found a low-level paragraph leaf that keeps the hypothesis space open for a richer block model above it.”"),
    ];
}
