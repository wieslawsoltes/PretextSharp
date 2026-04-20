using System.Runtime.InteropServices;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownBlock = Markdig.Syntax.Block;
using MarkdownInline = Markdig.Syntax.Inlines.Inline;

namespace PretextSamples.Samples;

public enum ChatRole
{
    Assistant,
    User,
}

public enum InlineVariant
{
    Body,
    Heading1,
    Heading2,
}

public sealed record MarkState(
    bool Bold,
    bool Italic,
    bool Strike,
    string? Href);

public sealed record ParseContext(
    int ListDepth,
    int QuoteDepth);

public sealed record InlinePiece(
    RichInlineBreakMode BreakMode,
    string ClassName,
    double ExtraWidth,
    string Font,
    string? Href,
    string Text);

public abstract record PreparedBlock(
    double ContentLeft,
    double MarginTop,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts);

public sealed record PreparedInlineBlock(
    double ContentLeft,
    double MarginTop,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    string[] ClassNames,
    PreparedRichInline Flow,
    string?[] Hrefs,
    double LineHeight)
    : PreparedBlock(ContentLeft, MarginTop, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts);

public sealed record PreparedCodeBlock(
    double ContentLeft,
    double MarginTop,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double LineHeight,
    PreparedTextWithSegments Prepared)
    : PreparedBlock(ContentLeft, MarginTop, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts);

public sealed record PreparedRuleBlock(
    double ContentLeft,
    double MarginTop,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Height)
    : PreparedBlock(ContentLeft, MarginTop, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts);

public sealed record PreparedChatTemplate(
    ChatRole Role,
    IReadOnlyList<PreparedBlock> Blocks);

public abstract record BlockFrame(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top);

public sealed record InlineBlockFrame(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top,
    double LineHeight,
    double UsedWidth)
    : BlockFrame(ContentLeft, Height, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts, Top);

public sealed record CodeBlockFrame(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top,
    double LineHeight,
    double Width)
    : BlockFrame(ContentLeft, Height, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts, Top);

public sealed record RuleBlockFrame(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top,
    double Width)
    : BlockFrame(ContentLeft, Height, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts, Top);

public sealed record InlineFragmentLayout(
    string ClassName,
    string? Href,
    double LeadingGap,
    string Text);

public abstract record BlockLayout(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top);

public sealed record InlineBlockLayout(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top,
    double LineHeight,
    double UsedWidth,
    IReadOnlyList<(IReadOnlyList<InlineFragmentLayout> Fragments, double Width)> Lines)
    : BlockLayout(ContentLeft, Height, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts, Top);

public sealed record CodeBlockLayout(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top,
    double UsedWidth,
    double Width,
    IReadOnlyList<LayoutLine> Lines)
    : BlockLayout(ContentLeft, Height, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts, Top);

public sealed record RuleBlockLayout(
    double ContentLeft,
    double Height,
    string? MarkerClassName,
    double? MarkerLeft,
    string? MarkerText,
    double[] QuoteRailLefts,
    double Top,
    double Width)
    : BlockLayout(ContentLeft, Height, MarkerClassName, MarkerLeft, MarkerText, QuoteRailLefts, Top);

public sealed record TemplateFrame(
    IReadOnlyList<BlockFrame> Blocks,
    double BubbleHeight,
    double ContentInsetX,
    double FrameWidth,
    double LayoutContentWidth,
    ChatRole Role,
    double TotalHeight);

public sealed record ChatMessageInstance(
    double Bottom,
    PreparedChatTemplate Prepared,
    TemplateFrame Frame,
    double Top);

public sealed record ConversationFrame(
    double ChatWidth,
    IReadOnlyList<ChatMessageInstance> Messages,
    double OcclusionBannerHeight,
    double TotalHeight);

public static class MarkdownChatModel
{
    public const double MinChatWidth = 360;
    public const double DefaultChatWidth = 640;
    public const double MaxChatWidth = 860;
    public const int TotalMessageCount = 10_000;
    public const double ChatViewportHeight = 560;
    public const double OcclusionBannerHeight = 61;
    public const double MessageSidePadding = 22;
    public const double CodeLineHeight = 18;
    public const double CodeBlockPaddingX = 12;
    public const double CodeBlockPaddingY = 8;

    private const double CompactOcclusionBannerHeight = 43;
    private const double CompactOcclusionViewportHeight = 460;
    private const double PageMargin = 28;
    private const double ChatTopPaddingOffset = 14;
    private const double ChatBottomPaddingOffset = 10;
    private const double MessageGap = 12;
    private const double BubbleMaxRatio = 0.78;
    private const double BubblePaddingX = 16;
    private const double BubblePaddingY = 10;
    private const double BodyLineHeight = 22;
    private const double HeadingOneLineHeight = 28;
    private const double HeadingTwoLineHeight = 25;
    private const double HardBreakGap = 4;
    private const double BlockGap = 12;
    private const double RichBlockGap = 2;
    private const double ListItemGap = 4;
    private const double ListNestingIndent = 18;
    private const double BlockquoteIndent = 18;
    private const double ListMarkerGap = 10;
    private const double RuleHeight = 18;
    private const double RailOffset = 5;
    private const string SansFamily = "\"Helvetica Neue\", Arial, sans-serif";
    private const string SerifFamily = "Georgia, \"Times New Roman\", serif";
    private const string MonoFamily = "\"SF Mono\", ui-monospace, Menlo, Monaco, monospace";
    private const string InlineCodeFont = $"600 12px {MonoFamily}";
    private const double InlineCodeExtraWidth = 12;
    private const string ImageFont = $"700 11px {SansFamily}";
    private const double ImageExtraWidth = 14;
    private const string MarkerFont = $"600 11px {MonoFamily}";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly MarkState EmptyMarkState = new(false, false, false, null);
    private static readonly Dictionary<string, double> MarkerWidthCache = new(StringComparer.Ordinal);

    public static IReadOnlyList<PreparedChatTemplate> CreatePreparedChatTemplates(
        IReadOnlyList<MarkdownChatSeed>? specs = null)
    {
        specs ??= MarkdownChatData.BaseMessageSpecs;
        var templates = new PreparedChatTemplate[specs.Count];
        for (var index = 0; index < templates.Length; index++)
        {
            var spec = specs[index];
            templates[index] = new PreparedChatTemplate(
                spec.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? ChatRole.Assistant : ChatRole.User,
                ParseMarkdownBlocks(spec.Markdown));
        }

        return templates;
    }

    public static double GetMaxChatWidth(double viewportWidth)
        => Math.Max(240, Math.Min(MaxChatWidth, viewportWidth - PageMargin * 2));

    public static double GetOcclusionBannerHeight(double viewportHeight)
        => viewportHeight <= CompactOcclusionViewportHeight
            ? CompactOcclusionBannerHeight
            : OcclusionBannerHeight;

    public static ConversationFrame BuildConversationFrame(
        IReadOnlyList<PreparedChatTemplate> templates,
        double chatWidth,
        double occlusionBannerHeight = OcclusionBannerHeight)
    {
        var laneWidth = Math.Max(120, chatWidth - MessageSidePadding * 2);
        var userFrameWidth = Math.Min(laneWidth, Math.Max(240, Math.Floor(chatWidth * BubbleMaxRatio)));
        var assistantFrameWidth = laneWidth;
        var messages = new ChatMessageInstance[TotalMessageCount];
        var chatTopPadding = occlusionBannerHeight + ChatTopPaddingOffset;
        var chatBottomPadding = occlusionBannerHeight + ChatBottomPaddingOffset;

        var y = chatTopPadding;
        for (var ordinal = 0; ordinal < TotalMessageCount; ordinal++)
        {
            var template = templates[ordinal % templates.Count];
            var contentInsetX = template.Role == ChatRole.Assistant ? 0 : BubblePaddingX;
            var frameWidth = template.Role == ChatRole.Assistant ? assistantFrameWidth : userFrameWidth;
            var contentWidth = Math.Max(120, frameWidth - contentInsetX * 2);
            var messageFrame = LayoutTemplateFrame(template, frameWidth, contentWidth, contentInsetX);
            var top = y;
            var bottom = top + messageFrame.TotalHeight;

            messages[ordinal] = new ChatMessageInstance(bottom, template, messageFrame, top);
            y = bottom + MessageGap;
        }

        var totalHeight = messages.Length == 0
            ? chatTopPadding + chatBottomPadding
            : y - MessageGap + chatBottomPadding;

        return new ConversationFrame(chatWidth, messages, occlusionBannerHeight, totalHeight);
    }

    public static (int Start, int End) FindVisibleRange(
        ConversationFrame frame,
        double scrollTop,
        double viewportHeight,
        double topOcclusionHeight,
        double bottomOcclusionHeight)
    {
        if (frame.Messages.Count == 0)
        {
            return (0, 0);
        }

        var minY = Math.Max(0, scrollTop + topOcclusionHeight);
        var maxY = Math.Max(minY, scrollTop + viewportHeight - bottomOcclusionHeight);

        var start = LowerBound(frame.Messages, minY, static (message, value) => message.Bottom > value);
        var end = LowerBound(frame.Messages, maxY, static (message, value) => message.Top >= value, start);
        return (start, end);
    }

    public static IReadOnlyList<BlockLayout> MaterializeTemplateBlocks(ChatMessageInstance message)
    {
        var blocks = new BlockLayout[message.Prepared.Blocks.Count];
        for (var index = 0; index < blocks.Length; index++)
        {
            blocks[index] = MaterializeBlockLayout(
                message.Prepared.Blocks[index],
                message.Frame.Blocks[index],
                message.Frame.LayoutContentWidth);
        }

        return blocks;
    }

    private static IReadOnlyList<PreparedBlock> ParseMarkdownBlocks(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        return ParseBlockCollection(document, new ParseContext(0, 0));
    }

    private static List<PreparedBlock> ParseBlockCollection(ContainerBlock blocks, ParseContext context)
    {
        var result = new List<PreparedBlock>();

        foreach (var block in blocks)
        {
            switch (block)
            {
                case ParagraphBlock paragraph:
                    AppendBlockGroup(result, BuildInlineBlocks(paragraph.Inline, InlineVariant.Body, context), BlockGap);
                    break;

                case HeadingBlock heading:
                    AppendBlockGroup(result, BuildInlineBlocks(heading.Inline, HeadingVariant(heading.Level), context), BlockGap + 4);
                    break;

                case FencedCodeBlock fenced:
                    AppendBlockGroup(result, [BuildCodeBlock(GetBlockText(fenced), context)], RichBlockGap);
                    break;

                case CodeBlock code:
                    AppendBlockGroup(result, [BuildCodeBlock(GetBlockText(code), context)], RichBlockGap);
                    break;

                case ListBlock list:
                    AppendBlockGroup(result, BuildListBlocks(list, context), BlockGap);
                    break;

                case QuoteBlock quote:
                    AppendBlockGroup(result, ParseBlockCollection(quote, new ParseContext(context.ListDepth, context.QuoteDepth + 1)), RichBlockGap);
                    break;

                case ThematicBreakBlock:
                    AppendBlockGroup(result, [BuildRuleBlock(context)], BlockGap + 2);
                    break;

                case HtmlBlock html:
                    AppendBlockGroup(result, [BuildCodeBlock(GetBlockText(html), context)], RichBlockGap);
                    break;

                default:
                    break;
            }
        }

        return result;
    }

    private static List<PreparedBlock> BuildListBlocks(ListBlock list, ParseContext context)
    {
        var result = new List<PreparedBlock>();
        var itemContext = new ParseContext(context.ListDepth + 1, context.QuoteDepth);
        var itemIndex = 0;

        foreach (var block in list)
        {
            if (block is not ListItemBlock item)
            {
                continue;
            }

            var itemBlocks = ParseBlockCollection(item, itemContext);
            if (itemBlocks.Count == 0)
            {
                itemBlocks = BuildPlainTextBlocks(GetBlockText(item), InlineVariant.Body, itemContext);
            }

            DecorateListItemBlocks(itemBlocks, ResolveListMarkerText(list, itemIndex), ResolveListMarkerClassName(list));
            AppendBlockGroup(result, itemBlocks, ListItemGap);
            itemIndex++;
        }

        return result;
    }

    private static void DecorateListItemBlocks(
        List<PreparedBlock> blocks,
        string markerText,
        string markerClassName)
    {
        if (blocks.Count == 0)
        {
            return;
        }

        var markerArea = MeasureMarkerWidth(markerText) + ListMarkerGap;
        for (var index = 0; index < blocks.Count; index++)
        {
            blocks[index] = ShiftBlock(blocks[index], markerArea);
        }

        blocks[0] = blocks[0] switch
        {
            PreparedInlineBlock inline => inline with
            {
                MarkerClassName = markerClassName,
                MarkerLeft = inline.ContentLeft - markerArea,
                MarkerText = markerText,
            },
            PreparedCodeBlock code => code with
            {
                MarkerClassName = markerClassName,
                MarkerLeft = code.ContentLeft - markerArea,
                MarkerText = markerText,
            },
            PreparedRuleBlock rule => rule with
            {
                MarkerClassName = markerClassName,
                MarkerLeft = rule.ContentLeft - markerArea,
                MarkerText = markerText,
            },
            _ => blocks[0],
        };
    }

    private static List<PreparedBlock> BuildPlainTextBlocks(string text, InlineVariant variant, ParseContext context)
    {
        var piece = CreateTextPiece(text, EmptyMarkState, variant);
        return piece is null
            ? []
            : BuildPreparedInlineBlocks([[piece]], variant, context);
    }

    private static List<PreparedBlock> BuildInlineBlocks(ContainerInline? container, InlineVariant variant, ParseContext context)
    {
        var lines = CollectInlinePieceLines(container, variant);
        return BuildPreparedInlineBlocks(lines, variant, context);
    }

    private static List<PreparedBlock> BuildPreparedInlineBlocks(
        List<List<InlinePiece>> lines,
        InlineVariant variant,
        ParseContext context)
    {
        var blocks = new List<PreparedBlock>();
        foreach (var line in lines)
        {
            var block = BuildPreparedInlineBlock(line, variant, context);
            if (block is null)
            {
                continue;
            }

            blocks.Add(blocks.Count == 0 ? block : block with { MarginTop = HardBreakGap });
        }

        return blocks;
    }

    private static PreparedInlineBlock? BuildPreparedInlineBlock(
        IReadOnlyList<InlinePiece> pieces,
        InlineVariant variant,
        ParseContext context)
    {
        if (pieces.Count == 0)
        {
            return null;
        }

        var blockBase = CreateBlockBase(context);
        var classNames = new string[pieces.Count];
        var richItems = new RichInlineItem[pieces.Count];
        var hrefs = new string?[pieces.Count];
        for (var index = 0; index < pieces.Count; index++)
        {
            var piece = pieces[index];
            classNames[index] = piece.ClassName;
            richItems[index] = new RichInlineItem(piece.Text, piece.Font, piece.BreakMode, piece.ExtraWidth);
            hrefs[index] = piece.Href;
        }

        return new PreparedInlineBlock(
            blockBase.ContentLeft,
            blockBase.MarginTop,
            blockBase.MarkerClassName,
            blockBase.MarkerLeft,
            blockBase.MarkerText,
            blockBase.QuoteRailLefts,
            classNames,
            PretextLayout.PrepareRichInline(richItems),
            hrefs,
            LineHeightForVariant(variant));
    }

    private static PreparedCodeBlock BuildCodeBlock(string text, ParseContext context)
    {
        var blockBase = CreateBlockBase(context);
        return new PreparedCodeBlock(
            blockBase.ContentLeft,
            blockBase.MarginTop,
            blockBase.MarkerClassName,
            blockBase.MarkerLeft,
            blockBase.MarkerText,
            blockBase.QuoteRailLefts,
            CodeLineHeight,
            PretextLayout.PrepareWithSegments(
                StripSingleTrailingNewline(text),
                $"500 12px {MonoFamily}",
                new PrepareOptions(WhiteSpaceMode.PreWrap)));
    }

    private static PreparedRuleBlock BuildRuleBlock(ParseContext context)
    {
        var blockBase = CreateBlockBase(context);
        return new PreparedRuleBlock(
            blockBase.ContentLeft,
            blockBase.MarginTop,
            blockBase.MarkerClassName,
            blockBase.MarkerLeft,
            blockBase.MarkerText,
            blockBase.QuoteRailLefts,
            RuleHeight);
    }

    private static PreparedBlock CreateBlockBase(ParseContext context)
    {
        var listIndent = Math.Max(0, context.ListDepth - 1) * ListNestingIndent;
        var contentLeft = listIndent + context.QuoteDepth * BlockquoteIndent;
        var quoteRailLefts = new double[context.QuoteDepth];
        for (var depth = 0; depth < context.QuoteDepth; depth++)
        {
            quoteRailLefts[depth] = listIndent + depth * BlockquoteIndent + RailOffset;
        }

        return new PreparedRuleBlock(contentLeft, 0, null, null, null, quoteRailLefts, 0);
    }

    private static List<List<InlinePiece>> CollectInlinePieceLines(ContainerInline? container, InlineVariant variant)
    {
        var lines = new List<List<InlinePiece>> { new() };

        List<InlinePiece> CurrentLine() => lines[^1];
        void PushLineBreak() => lines.Add(new List<InlinePiece>());
        void PushPiece(InlinePiece? piece)
        {
            if (piece is null)
            {
                return;
            }

            var line = CurrentLine();
            var previous = line.Count > 0 ? line[^1] : null;
            if (previous is not null && CanMergeInlinePieces(previous, piece))
            {
                line[^1] = previous with { Text = previous.Text + piece.Text };
                return;
            }

            line.Add(piece);
        }

        void Walk(ContainerInline? parent, MarkState marks)
        {
            for (MarkdownInline? inline = parent?.FirstChild; inline is not null; inline = inline.NextSibling)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        PushPiece(CreateTextPiece(literal.Content.ToString(), marks, variant));
                        break;

                    case CodeInline code:
                        PushPiece(CreateCodePiece(code.Content));
                        break;

                    case LineBreakInline:
                        PushLineBreak();
                        break;

                    case HtmlInline html:
                        PushPiece(CreateTextPiece(html.Tag, marks, variant));
                        break;

                    case EmphasisInline emphasis:
                    {
                        var nextMarks = emphasis.DelimiterChar switch
                        {
                            '~' => marks with { Strike = true },
                            _ => marks with
                            {
                                Bold = marks.Bold || emphasis.DelimiterCount >= 2,
                                Italic = marks.Italic || (emphasis.DelimiterCount % 2 == 1),
                            },
                        };
                        Walk(emphasis, nextMarks);
                        break;
                    }

                    case LinkInline link when link.IsImage:
                        PushPiece(CreateImagePiece(InlineToPlainText(link) is { Length: > 0 } alt ? alt : link.Url ?? "image"));
                        break;

                    case LinkInline link:
                        Walk(link, marks with { Href = link.Url });
                        break;

                    case ContainerInline nested:
                        Walk(nested, marks);
                        break;

                    default:
                        PushPiece(CreateTextPiece(inline.ToString() ?? string.Empty, marks, variant));
                        break;
                }
            }
        }

        Walk(container, EmptyMarkState);
        while (lines.Count > 0 && lines[^1].Count == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    private static InlinePiece? CreateTextPiece(string text, MarkState marks, InlineVariant variant)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return new InlinePiece(
            RichInlineBreakMode.Normal,
            ResolveTextClassName(variant, marks),
            0,
            ResolveTextFont(variant, marks),
            marks.Href,
            text);
    }

    private static InlinePiece? CreateCodePiece(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return new InlinePiece(
            RichInlineBreakMode.Normal,
            "frag frag--code",
            InlineCodeExtraWidth,
            InlineCodeFont,
            null,
            text);
    }

    private static InlinePiece CreateImagePiece(string text)
    {
        return new InlinePiece(
            RichInlineBreakMode.Never,
            "frag frag--chip",
            ImageExtraWidth,
            ImageFont,
            null,
            string.IsNullOrWhiteSpace(text) ? "image" : text);
    }

    private static bool CanMergeInlinePieces(InlinePiece left, InlinePiece right)
    {
        return left.BreakMode == right.BreakMode &&
               left.ClassName == right.ClassName &&
               left.ExtraWidth.Equals(right.ExtraWidth) &&
               left.Font == right.Font &&
               left.Href == right.Href;
    }

    private static string ResolveTextFont(InlineVariant variant, MarkState marks)
    {
        var italicPrefix = marks.Italic ? "italic " : string.Empty;
        return variant switch
        {
            InlineVariant.Heading1 => $"{italicPrefix}{(marks.Bold ? 800 : 700)} 20px {SerifFamily}",
            InlineVariant.Heading2 => $"{italicPrefix}{(marks.Bold ? 800 : 700)} 17px {SerifFamily}",
            _ => $"{italicPrefix}{(marks.Bold ? 700 : marks.Href is null ? 400 : 500)} 14px {SansFamily}",
        };
    }

    private static string ResolveTextClassName(InlineVariant variant, MarkState marks)
    {
        var className = variant switch
        {
            InlineVariant.Heading1 => "frag frag--heading-1",
            InlineVariant.Heading2 => "frag frag--heading-2",
            _ => "frag frag--body",
        };

        if (marks.Href is not null)
        {
            className += " is-link";
        }

        if (marks.Bold)
        {
            className += " is-strong";
        }

        if (marks.Italic)
        {
            className += " is-em";
        }

        if (marks.Strike)
        {
            className += " is-del";
        }

        return className;
    }

    private static InlineVariant HeadingVariant(int level)
        => level <= 1 ? InlineVariant.Heading1 : level == 2 ? InlineVariant.Heading2 : InlineVariant.Body;

    private static double LineHeightForVariant(InlineVariant variant)
        => variant switch
        {
            InlineVariant.Heading1 => HeadingOneLineHeight,
            InlineVariant.Heading2 => HeadingTwoLineHeight,
            _ => BodyLineHeight,
        };

    private static void AppendBlockGroup(
        List<PreparedBlock> target,
        IReadOnlyList<PreparedBlock> group,
        double firstMargin)
    {
        if (group.Count == 0)
        {
            return;
        }

        for (var index = 0; index < group.Count; index++)
        {
            var block = group[index];
            var marginTop = index == 0 ? (target.Count == 0 ? 0 : firstMargin) : block.MarginTop;
            target.Add(block switch
            {
                PreparedInlineBlock inline => inline with { MarginTop = marginTop },
                PreparedCodeBlock code => code with { MarginTop = marginTop },
                PreparedRuleBlock rule => rule with { MarginTop = marginTop },
                _ => block,
            });
        }
    }

    private static PreparedBlock ShiftBlock(PreparedBlock block, double delta)
    {
        return block switch
        {
            PreparedInlineBlock inline => inline with { ContentLeft = inline.ContentLeft + delta },
            PreparedCodeBlock code => code with { ContentLeft = code.ContentLeft + delta },
            PreparedRuleBlock rule => rule with { ContentLeft = rule.ContentLeft + delta },
            _ => block,
        };
    }

    private static string ResolveListMarkerText(ListBlock list, int index)
    {
        if (!list.IsOrdered)
        {
            return "\u2022";
        }

        return $"{(int.TryParse(list.OrderedStart, out var start) ? start : 1) + index}.";
    }

    private static string ResolveListMarkerClassName(ListBlock list)
        => list.IsOrdered ? "block-marker block-marker--ordered" : "block-marker block-marker--bullet";

    private static double MeasureMarkerWidth(string text)
    {
        ref var cached = ref CollectionsMarshal.GetValueRefOrAddDefault(MarkerWidthCache, text, out var exists);
        if (exists)
        {
            return cached;
        }

        cached = PretextLayout.MeasureNaturalWidth(PretextLayout.PrepareWithSegments(text, MarkerFont));
        return cached;
    }

    private static string InlineToPlainText(ContainerInline? inline)
    {
        var builder = new StringBuilder();

        void Walk(MarkdownInline? current)
        {
            for (var node = current; node is not null; node = node.NextSibling)
            {
                switch (node)
                {
                    case LiteralInline literal:
                        builder.Append(literal.Content.ToString());
                        break;
                    case CodeInline code:
                        builder.Append(code.Content);
                        break;
                    case LineBreakInline:
                        builder.Append('\n');
                        break;
                    case HtmlInline html:
                        builder.Append(html.Tag);
                        break;
                    case ContainerInline nested:
                        Walk(nested.FirstChild);
                        break;
                }
            }
        }

        Walk(inline?.FirstChild);
        return builder.ToString();
    }

    private static string GetBlockText(MarkdownBlock block)
    {
        if (block is LeafBlock leaf)
        {
            return leaf.Lines.ToString() ?? string.Empty;
        }

        if (block is not ContainerBlock container)
        {
            return block.ToString() ?? string.Empty;
        }

        var builder = new StringBuilder();
        AppendContainerBlockText(builder, container);
        return builder.ToString();
    }

    private static string StripSingleTrailingNewline(string text)
        => text.EndsWith('\n') ? text[..^1] : text;

    private static TemplateFrame LayoutTemplateFrame(
        PreparedChatTemplate template,
        double maxFrameWidth,
        double maxContentWidth,
        double contentInsetX)
    {
        var blocks = new List<BlockFrame>(template.Blocks.Count);
        var y = BubblePaddingY;
        var usedContentWidth = 0d;

        foreach (var block in template.Blocks)
        {
            y += block.MarginTop;
            var frame = LayoutBlockFrame(block, maxContentWidth, y);
            blocks.Add(frame);
            y += frame.Height;
            usedContentWidth = Math.Max(usedContentWidth, GetUsedBlockWidth(frame));
        }

        var bubbleHeight = y + BubblePaddingY;
        var frameWidth = template.Role == ChatRole.Assistant
            ? maxFrameWidth
            : Math.Min(maxFrameWidth, contentInsetX * 2 + Math.Max(1, usedContentWidth));

        return new TemplateFrame(
            blocks,
            bubbleHeight,
            contentInsetX,
            frameWidth,
            maxContentWidth,
            template.Role,
            bubbleHeight);
    }

    private static BlockFrame LayoutBlockFrame(PreparedBlock block, double contentWidth, double top)
    {
        switch (block)
        {
            case PreparedInlineBlock inline:
            {
                var lineWidth = Math.Max(1, contentWidth - inline.ContentLeft);
                var stats = PretextLayout.MeasureRichInlineStats(inline.Flow, lineWidth);
                return new InlineBlockFrame(
                    inline.ContentLeft,
                    stats.LineCount * inline.LineHeight,
                    inline.MarkerClassName,
                    inline.MarkerLeft,
                    inline.MarkerText,
                    inline.QuoteRailLefts,
                    top,
                    inline.LineHeight,
                    stats.MaxLineWidth);
            }

            case PreparedCodeBlock code:
            {
                var boxWidth = Math.Max(1, contentWidth - code.ContentLeft);
                var innerWidth = Math.Max(1, boxWidth - CodeBlockPaddingX * 2);
                var stats = PretextLayout.MeasureLineStats(code.Prepared, innerWidth);
                return new CodeBlockFrame(
                    code.ContentLeft,
                    stats.LineCount * code.LineHeight + CodeBlockPaddingY * 2,
                    code.MarkerClassName,
                    code.MarkerLeft,
                    code.MarkerText,
                    code.QuoteRailLefts,
                    top,
                    code.LineHeight,
                    stats.MaxLineWidth + CodeBlockPaddingX * 2);
            }

            case PreparedRuleBlock rule:
                return new RuleBlockFrame(
                    rule.ContentLeft,
                    rule.Height,
                    rule.MarkerClassName,
                    rule.MarkerLeft,
                    rule.MarkerText,
                    rule.QuoteRailLefts,
                    top,
                    Math.Max(1, contentWidth - rule.ContentLeft));

            default:
                throw new InvalidOperationException($"Unsupported block type: {block.GetType().Name}");
        }
    }

    private static double GetUsedBlockWidth(BlockFrame frame)
    {
        return frame switch
        {
            InlineBlockFrame inline => inline.ContentLeft + inline.UsedWidth,
            CodeBlockFrame code => code.ContentLeft + code.Width,
            RuleBlockFrame rule => rule.ContentLeft + rule.Width,
            _ => 0,
        };
    }

    private static BlockLayout MaterializeBlockLayout(PreparedBlock block, BlockFrame frame, double contentWidth)
    {
        switch (frame)
        {
            case InlineBlockFrame inlineFrame when block is PreparedInlineBlock inlineBlock:
            {
                var lineWidth = Math.Max(1, contentWidth - inlineFrame.ContentLeft);
                var estimatedLineCount = Math.Max(1, (int)Math.Ceiling(inlineFrame.Height / Math.Max(1, inlineFrame.LineHeight)));
                var lines = new List<(IReadOnlyList<InlineFragmentLayout> Fragments, double Width)>(estimatedLineCount);
                PretextLayout.WalkRichInlineLineRanges(inlineBlock.Flow, lineWidth, range =>
                {
                    var line = PretextLayout.MaterializeRichInlineLineRange(inlineBlock.Flow, range);
                    var fragments = new InlineFragmentLayout[line.Fragments.Length];
                    for (var index = 0; index < fragments.Length; index++)
                    {
                        var fragment = line.Fragments[index];
                        fragments[index] = new InlineFragmentLayout(
                            inlineBlock.ClassNames[fragment.ItemIndex],
                            inlineBlock.Hrefs[fragment.ItemIndex],
                            fragment.GapBefore,
                            fragment.Text);
                    }

                    lines.Add((fragments, line.Width));
                });

                return new InlineBlockLayout(
                    inlineFrame.ContentLeft,
                    inlineFrame.Height,
                    inlineFrame.MarkerClassName,
                    inlineFrame.MarkerLeft,
                    inlineFrame.MarkerText,
                    inlineFrame.QuoteRailLefts,
                    inlineFrame.Top,
                    inlineFrame.LineHeight,
                    inlineFrame.UsedWidth,
                    lines);
            }

            case CodeBlockFrame codeFrame when block is PreparedCodeBlock codeBlock:
            {
                var boxWidth = Math.Max(1, contentWidth - codeFrame.ContentLeft);
                var innerWidth = Math.Max(1, boxWidth - CodeBlockPaddingX * 2);
                var layout = PretextLayout.LayoutWithLines(codeBlock.Prepared, innerWidth, codeFrame.LineHeight);
                return new CodeBlockLayout(
                    codeFrame.ContentLeft,
                    codeFrame.Height,
                    codeFrame.MarkerClassName,
                    codeFrame.MarkerLeft,
                    codeFrame.MarkerText,
                    codeFrame.QuoteRailLefts,
                    codeFrame.Top,
                    codeFrame.Width,
                    codeFrame.Width,
                    layout.Lines);
            }

            case RuleBlockFrame ruleFrame when block is PreparedRuleBlock:
                return new RuleBlockLayout(
                    ruleFrame.ContentLeft,
                    ruleFrame.Height,
                    ruleFrame.MarkerClassName,
                    ruleFrame.MarkerLeft,
                    ruleFrame.MarkerText,
                    ruleFrame.QuoteRailLefts,
                    ruleFrame.Top,
                    ruleFrame.Width);

            default:
                throw new InvalidOperationException("Block/frame mismatch.");
        }
    }

    private static int LowerBound(
        IReadOnlyList<ChatMessageInstance> messages,
        double value,
        Func<ChatMessageInstance, double, bool> predicate,
        int start = 0)
    {
        var low = start;
        var high = messages.Count;
        while (low < high)
        {
            var mid = (low + high) >> 1;
            if (predicate(messages[mid], value))
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }

    private static void AppendContainerBlockText(StringBuilder builder, ContainerBlock container)
    {
        var needsSeparator = false;
        foreach (var child in container)
        {
            if (needsSeparator)
            {
                builder.Append('\n');
            }

            if (child is LeafBlock leaf)
            {
                builder.Append(leaf.Lines.ToString());
            }
            else if (child is ContainerBlock nested)
            {
                AppendContainerBlockText(builder, nested);
            }
            else
            {
                builder.Append(child.ToString());
            }

            needsSeparator = true;
        }
    }
}
