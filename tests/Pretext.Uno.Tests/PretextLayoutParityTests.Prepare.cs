using Pretext;
using Xunit;

namespace Pretext.Tests;

public sealed partial class PretextLayoutParityTests
{
    [Fact(DisplayName = "whitespace-only input stays empty")]
    public void Prepare_WhitespaceOnlyInputStaysEmpty()
    {
        var prepared = PretextLayout.Prepare("  \t\n  ", Font);
        Assert.Equal(new LayoutResult(0, 0), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "collapses ordinary whitespace runs and trims the edges")]
    public void Prepare_CollapsesOrdinaryWhitespaceRunsAndTrimsEdges()
    {
        var prepared = PretextLayout.PrepareWithSegments("  Hello\t \n  World  ", Font);
        Assert.Equal(new[] { "Hello", " ", "World" }, prepared.Segments);
    }

    [Fact(DisplayName = "pre-wrap mode keeps ordinary spaces instead of collapsing them")]
    public void Prepare_PreWrapKeepsOrdinarySpaces()
    {
        var prepared = PretextLayout.PrepareWithSegments("  Hello   World  ", Font, PreWrap);
        Assert.Equal(new[] { "  ", "Hello", "   ", "World", "  " }, prepared.Segments);
        Assert.Equal(
            new[]
            {
                SegmentBreakKind.PreservedSpace,
                SegmentBreakKind.Text,
                SegmentBreakKind.PreservedSpace,
                SegmentBreakKind.Text,
                SegmentBreakKind.PreservedSpace,
            },
            prepared.Kinds);
    }

    [Fact(DisplayName = "pre-wrap mode keeps hard breaks as explicit segments")]
    public void Prepare_PreWrapKeepsHardBreaksAsExplicitSegments()
    {
        var prepared = PretextLayout.PrepareWithSegments("Hello\nWorld", Font, PreWrap);
        Assert.Equal(new[] { "Hello", "\n", "World" }, prepared.Segments);
        Assert.Equal(
            new[] { SegmentBreakKind.Text, SegmentBreakKind.HardBreak, SegmentBreakKind.Text },
            prepared.Kinds);
    }

    [Fact(DisplayName = "pre-wrap mode normalizes CRLF into a single hard break")]
    public void Prepare_PreWrapNormalizesCrLf()
    {
        var prepared = PretextLayout.PrepareWithSegments("Hello\r\nWorld", Font, PreWrap);
        Assert.Equal(new[] { "Hello", "\n", "World" }, prepared.Segments);
        Assert.Equal(
            new[] { SegmentBreakKind.Text, SegmentBreakKind.HardBreak, SegmentBreakKind.Text },
            prepared.Kinds);
    }

    [Fact(DisplayName = "pre-wrap mode keeps tabs as explicit segments")]
    public void Prepare_PreWrapKeepsTabsAsExplicitSegments()
    {
        var prepared = PretextLayout.PrepareWithSegments("Hello\tWorld", Font, PreWrap);
        Assert.Equal(new[] { "Hello", "\t", "World" }, prepared.Segments);
        Assert.Equal(
            new[] { SegmentBreakKind.Text, SegmentBreakKind.Tab, SegmentBreakKind.Text },
            prepared.Kinds);
    }

    [Fact(DisplayName = "keeps non-breaking spaces as glue instead of collapsing them away")]
    public void Prepare_KeepsNbspAsGlue()
    {
        var prepared = PretextLayout.PrepareWithSegments("Hello\u00A0world", Font);
        Assert.Equal(new[] { "Hello\u00A0world" }, prepared.Segments);
        Assert.Equal(new[] { SegmentBreakKind.Text }, prepared.Kinds);
    }

    [Fact(DisplayName = "keeps standalone non-breaking spaces as visible glue content")]
    public void Prepare_KeepsStandaloneNbspVisible()
    {
        var prepared = PretextLayout.PrepareWithSegments("\u00A0", Font);
        Assert.Equal(new[] { "\u00A0" }, prepared.Segments);
        Assert.Equal(new LayoutResult(1, LineHeight), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "pre-wrap mode keeps whitespace-only input visible")]
    public void Prepare_PreWrapKeepsWhitespaceOnlyInputVisible()
    {
        var prepared = PretextLayout.Prepare("   ", Font, PreWrap);
        Assert.Equal(new LayoutResult(1, LineHeight), PretextLayout.Layout(prepared, 200, LineHeight));
    }

    [Fact(DisplayName = "keeps narrow no-break spaces as glue content")]
    public void Prepare_KeepsNnbspAsGlue()
    {
        var prepared = PretextLayout.PrepareWithSegments("10\u202F000", Font);
        Assert.Equal(new[] { "10\u202F000" }, prepared.Segments);
        Assert.Equal(new[] { SegmentBreakKind.Text }, prepared.Kinds);
    }

    [Fact(DisplayName = "keeps word joiners as glue content")]
    public void Prepare_KeepsWordJoinersAsGlue()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo\u2060bar", Font);
        Assert.Equal(new[] { "foo\u2060bar" }, prepared.Segments);
        Assert.Equal(new[] { SegmentBreakKind.Text }, prepared.Kinds);
    }

    [Fact(DisplayName = "treats zero-width spaces as explicit break opportunities")]
    public void Prepare_TreatsZeroWidthSpacesAsExplicitBreaks()
    {
        var prepared = PretextLayout.PrepareWithSegments("alpha\u200Bbeta", Font);
        Assert.Equal(new[] { "alpha", "\u200B", "beta" }, prepared.Segments);
        Assert.Equal(
            new[] { SegmentBreakKind.Text, SegmentBreakKind.ZeroWidthBreak, SegmentBreakKind.Text },
            prepared.Kinds);

        var alphaWidth = prepared.Widths[0];
        Assert.Equal(2, PretextLayout.Layout(prepared, alphaWidth + 0.1, LineHeight).LineCount);
    }

    [Fact(DisplayName = "treats soft hyphens as discretionary break points")]
    public void Prepare_TreatsSoftHyphensAsDiscretionaryBreaks()
    {
        var prepared = PretextLayout.PrepareWithSegments("trans\u00ADatlantic", Font);
        Assert.Equal(new[] { "trans", "\u00AD", "atlantic" }, prepared.Segments);
        Assert.Equal(
            new[] { SegmentBreakKind.Text, SegmentBreakKind.SoftHyphen, SegmentBreakKind.Text },
            prepared.Kinds);

        var wide = PretextLayout.LayoutWithLines(prepared, 200, LineHeight);
        Assert.Equal(1, wide.LineCount);
        Assert.Equal(new[] { "transatlantic" }, wide.Lines.Select(static line => line.Text).ToArray());

        var prefixed = PretextLayout.PrepareWithSegments("foo trans\u00ADatlantic", Font);
        var softBreakWidth = Math.Max(
            prefixed.Widths[0] + prefixed.Widths[1] + prefixed.Widths[2] + prefixed.DiscretionaryHyphenWidth,
            prefixed.Widths[4]) + 0.1;
        var narrow = PretextLayout.LayoutWithLines(prefixed, softBreakWidth, LineHeight);
        Assert.Equal(2, narrow.LineCount);
        Assert.Equal(new[] { "foo trans-", "atlantic" }, narrow.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(narrow.LineCount, PretextLayout.Layout(prefixed, softBreakWidth, LineHeight).LineCount);

        var continuedSoftBreakWidth =
            prefixed.Widths[0] +
            prefixed.Widths[1] +
            prefixed.Widths[2] +
            RequireBreakableWidths(prefixed.BreakableWidths[4])[0] +
            prefixed.DiscretionaryHyphenWidth +
            0.1;
        var continued = PretextLayout.LayoutWithLines(prefixed, continuedSoftBreakWidth, LineHeight);
        Assert.Equal(new[] { "foo trans-a", "tlantic" }, continued.Lines.Select(static line => line.Text).ToArray());
        Assert.Equal(continued.LineCount, PretextLayout.Layout(prefixed, continuedSoftBreakWidth, LineHeight).LineCount);
    }

    [Fact(DisplayName = "keeps closing punctuation attached to the preceding word")]
    public void Prepare_KeepsClosingPunctuationAttachedToPrecedingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("hello.", Font);
        Assert.Equal(new[] { "hello." }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps arabic punctuation attached to the preceding word")]
    public void Prepare_KeepsArabicPunctuationAttachedToPrecedingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("مرحبا، عالم؟", Font);
        Assert.Equal(new[] { "مرحبا،", " ", "عالم؟" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps arabic punctuation-plus-mark clusters attached to the preceding word")]
    public void Prepare_KeepsArabicPunctuationPlusMarkClustersAttached()
    {
        var prepared = PretextLayout.PrepareWithSegments("وحوارى بكشء،ٍ من قولهم", Font);
        Assert.Equal(new[] { "وحوارى", " ", "بكشء،ٍ", " ", "من", " ", "قولهم" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps arabic no-space punctuation clusters together")]
    public void Prepare_KeepsArabicNoSpacePunctuationClustersTogether()
    {
        var prepared = PretextLayout.PrepareWithSegments("فيقول:وعليك السلام", Font);
        Assert.Equal(new[] { "فيقول:وعليك", " ", "السلام" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps arabic comma-followed text together without a space")]
    public void Prepare_KeepsArabicCommaFollowedTextTogether()
    {
        var prepared = PretextLayout.PrepareWithSegments("همزةٌ،ما كان", Font);
        Assert.Equal(new[] { "همزةٌ،ما", " ", "كان" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps leading arabic combining marks with the following word")]
    public void Prepare_KeepsLeadingArabicCombiningMarksWithFollowingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("كل ِّواحدةٍ", Font);
        Assert.Equal(new[] { "كل", " ", "ِّواحدةٍ" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps devanagari danda punctuation attached to the preceding word")]
    public void Prepare_KeepsDevanagariDandaAttachedToPrecedingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("नमस्ते। दुनिया॥", Font);
        Assert.Equal(new[] { "नमस्ते।", " ", "दुनिया॥" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps myanmar punctuation attached to the preceding word")]
    public void Prepare_KeepsMyanmarPunctuationAttachedToPrecedingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("ဖြစ်သည်။ နောက်တစ်ခု၊ ကိုက်ချီ၍ ယုံကြည်မိကြ၏။", Font);
        Assert.Equal(new[] { "ဖြစ်သည်။", " ", "နောက်တစ်ခု၊", " ", "ကိုက်", "ချီ၍", " " }, prepared.Segments.Take(7));
        Assert.Equal("ကြ၏။", prepared.Segments[prepared.Segments.Count - 1]);
    }

    [Fact(DisplayName = "keeps myanmar possessive marker attached to the following word")]
    public void Prepare_KeepsMyanmarPossessiveMarkerAttachedToFollowingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("ကျွန်ုပ်၏လက်မဖြင့်", Font);
        Assert.Equal(new[] { "ကျွန်ုပ်၏လက်မ", "ဖြင့်" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps opening quotes attached to the following word")]
    public void Prepare_KeepsOpeningQuotesAttachedToFollowingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("“Whenever", Font);
        Assert.Equal(new[] { "“Whenever" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps apostrophe-led elisions attached to the following word")]
    public void Prepare_KeepsApostropheLedElisionsAttachedToFollowingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("“Take ’em downstairs", Font);
        Assert.Equal(new[] { "“Take", " ", "’em", " ", "downstairs" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps stacked opening quotes attached to the following word")]
    public void Prepare_KeepsStackedOpeningQuotesAttachedToFollowingWord()
    {
        var prepared = PretextLayout.PrepareWithSegments("invented, “‘George B. Wilson", Font);
        Assert.Equal(new[] { "invented,", " ", "“‘George", " ", "B.", " ", "Wilson" }, prepared.Segments);
    }

    [Fact(DisplayName = "treats ascii quotes as opening and closing glue by context")]
    public void Prepare_TreatsAsciiQuotesAsOpeningAndClosingGlueByContext()
    {
        var prepared = PretextLayout.PrepareWithSegments("said \"hello\" there", Font);
        Assert.Equal(new[] { "said", " ", "\"hello\"", " ", "there" }, prepared.Segments);
    }

    [Fact(DisplayName = "treats escaped ascii quote clusters as opening and closing glue by context")]
    public void Prepare_TreatsEscapedAsciiQuoteClustersAsGlueByContext()
    {
        var text = "say \\\"hello\\\" there";
        var prepared = PretextLayout.PrepareWithSegments(text, Font);
        Assert.Equal(new[] { "say", " ", "\\\"hello\\\"", " ", "there" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps URL-like runs together as one breakable segment")]
    public void Prepare_KeepsUrlLikeRunsTogether()
    {
        var prepared = PretextLayout.PrepareWithSegments("see https://example.com/reports/q3?lang=ar&mode=full now", Font);
        Assert.Equal(
            new[]
            {
                "see",
                " ",
                "https://example.com/reports/q3?",
                "lang=ar&mode=full",
                " ",
                "now",
            },
            prepared.Segments);
    }

    [Fact(DisplayName = "keeps no-space ascii punctuation chains together as one breakable segment")]
    public void Prepare_KeepsNoSpaceAsciiPunctuationChainsTogether()
    {
        var prepared = PretextLayout.PrepareWithSegments("foo;bar foo:bar foo,bar as;lkdfjals;k", Font);
        Assert.Equal(
            new[]
            {
                "foo;bar",
                " ",
                "foo:bar",
                " ",
                "foo,bar",
                " ",
                "as;lkdfjals;k",
            },
            prepared.Segments);
    }

    [Fact(DisplayName = "keeps numeric time ranges together")]
    public void Prepare_KeepsNumericTimeRangesTogether()
    {
        var prepared = PretextLayout.PrepareWithSegments("window 7:00-9:00 only", Font);
        Assert.Equal(new[] { "window", " ", "7:00-", "9:00", " ", "only" }, prepared.Segments);
    }

    [Fact(DisplayName = "splits hyphenated numeric identifiers at preferred boundaries")]
    public void Prepare_SplitsHyphenatedNumericIdentifiersAtPreferredBoundaries()
    {
        var prepared = PretextLayout.PrepareWithSegments("SSN 420-69-8008 filed", Font);
        Assert.Equal(new[] { "SSN", " ", "420-", "69-", "8008", " ", "filed" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps unicode-digit numeric expressions together")]
    public void Prepare_KeepsUnicodeDigitNumericExpressionsTogether()
    {
        var prepared = PretextLayout.PrepareWithSegments("यह २४×७ सपोर्ट है", Font);
        Assert.Equal(new[] { "यह", " ", "२४×७", " ", "सपोर्ट", " ", "है" }, prepared.Segments);
    }

    [Fact(DisplayName = "does not attach opening punctuation to following whitespace")]
    public void Prepare_DoesNotAttachOpeningPunctuationToFollowingWhitespace()
    {
        var prepared = PretextLayout.PrepareWithSegments("“ hello", Font);
        Assert.Equal(new[] { "“", " ", "hello" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps japanese iteration marks attached to the preceding kana")]
    public void Prepare_KeepsJapaneseIterationMarksAttachedToPrecedingKana()
    {
        var prepared = PretextLayout.PrepareWithSegments("棄てゝ行く", Font);
        Assert.Equal(new[] { "棄", "てゝ", "行", "く" }, prepared.Segments);
    }

    [Fact(DisplayName = "carries trailing cjk opening punctuation forward across segment boundaries")]
    public void Prepare_CarriesTrailingCjkOpeningPunctuationForwardAcrossSegmentBoundaries()
    {
        var prepared = PretextLayout.PrepareWithSegments("作者はさつき、「下人", Font);
        Assert.Equal(new[] { "作", "者", "は", "さ", "つ", "き、", "「下", "人" }, prepared.Segments);
    }

    [Fact(DisplayName = "keeps em dashes breakable")]
    public void Prepare_KeepsEmDashesBreakable()
    {
        var prepared = PretextLayout.PrepareWithSegments("universe—so", Font);
        Assert.Equal(new[] { "universe", "—", "so" }, prepared.Segments);
    }

    [Fact(DisplayName = "coalesces repeated punctuation runs into a single segment")]
    public void Prepare_CoalescesRepeatedPunctuationRunsIntoSingleSegment()
    {
        var prepared = PretextLayout.PrepareWithSegments("=== heading ===", Font);
        Assert.Equal(new[] { "===", " ", "heading", " ", "===" }, prepared.Segments);
    }

    [Fact(DisplayName = "applies CJK and Hangul punctuation attachment rules")]
    public void Prepare_AppliesCjkAndHangulPunctuationAttachmentRules()
    {
        Assert.Equal(new[] { "中", "文，", "测", "试。" }, PretextLayout.PrepareWithSegments("中文，测试。", Font).Segments);
        Assert.Equal("다.", PretextLayout.PrepareWithSegments("테스트입니다.", Font).Segments[^1]);
    }

    [Fact(DisplayName = "treats astral CJK ideographs as CJK break units")]
    public void Prepare_TreatsAstralCjkIdeographsAsBreakUnits()
    {
        Assert.Equal(new[] { "𠀀", "𠀁" }, PretextLayout.PrepareWithSegments("𠀀𠀁", Font).Segments);
        Assert.Equal(new[] { "𠀀。" }, PretextLayout.PrepareWithSegments("𠀀。", Font).Segments);
    }

    [Fact(DisplayName = "prepare and prepareWithSegments agree on layout behavior")]
    public void Prepare_PrepareAndPrepareWithSegmentsAgreeOnLayoutBehavior()
    {
        var plain = PretextLayout.Prepare("Alpha beta gamma", Font);
        var rich = PretextLayout.PrepareWithSegments("Alpha beta gamma", Font);

        foreach (var width in new[] { 40d, 80d, 200d })
        {
            Assert.Equal(PretextLayout.Layout(plain, width, LineHeight), PretextLayout.Layout(rich, width, LineHeight));
        }
    }

    [Fact(DisplayName = "locale can be reset without disturbing later prepares")]
    public void Prepare_LocaleCanBeResetWithoutDisturbingLaterPrepares()
    {
        PretextLayout.SetLocale("th");
        var thai = PretextLayout.Prepare("ภาษาไทยภาษาไทย", Font);
        Assert.True(PretextLayout.Layout(thai, 80, LineHeight).LineCount > 0);

        PretextLayout.SetLocale();
        var latin = PretextLayout.Prepare("hello world", Font);
        Assert.Equal(new LayoutResult(1, LineHeight), PretextLayout.Layout(latin, 200, LineHeight));
    }

    [Fact(DisplayName = "thai locale prepareWithSegments preserves text and uses locale-aware word segmentation when available")]
    public void Prepare_ThaiLocalePreservesTextAndUsesLocaleAwareWordSegmentationWhenAvailable()
    {
        const string text = "ภาษาไทยภาษาไทย";

        PretextLayout.SetLocale("th");
        var prepared = PretextLayout.PrepareWithSegments(text, Font);

        Assert.NotEmpty(prepared.Segments);
        Assert.All(prepared.Segments, static segment => Assert.False(string.IsNullOrEmpty(segment)));
        Assert.Equal(text, string.Concat(prepared.Segments));

        if (prepared.Segments.Count > 1)
        {
            Assert.Contains(prepared.Segments, segment => segment.Length < text.Length);
        }
        else
        {
            Assert.Equal(text, prepared.Segments[0]);
        }
    }
}
