using Pretext;

namespace PretextSamples.Samples;

public enum RichNoteTextStyleName
{
    Body,
    Link,
    Code,
}

public enum RichNoteChipTone
{
    Mention,
    Status,
    Priority,
    Time,
    Count,
}

public abstract record RichNoteInlineSpec;

public sealed record RichNoteTextSpec(string Text, RichNoteTextStyleName Style) : RichNoteInlineSpec;

public sealed record RichNoteChipSpec(string Label, RichNoteChipTone Tone) : RichNoteInlineSpec;

public sealed record PreparedRichInlineNote(
    string[] ClassNames,
    PreparedRichInline Flow);

public sealed record RichNoteFragment(
    string ClassName,
    double LeadingGap,
    string Text);

public sealed record RichNoteLine(
    IReadOnlyList<RichNoteFragment> Fragments);

public sealed record RichNoteLayout(
    double BodyWidth,
    int LineCount,
    IReadOnlyList<RichNoteLine> Lines,
    double NoteBodyHeight,
    double NoteWidth);

public static class RichNoteModel
{
    public const string BodyFont = "500 17px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    public const string LinkFont = "600 17px \"Helvetica Neue\", Helvetica, Arial, sans-serif";
    public const string CodeFont = "600 14px \"SF Mono\", ui-monospace, Menlo, Monaco, monospace";
    public const string ChipFont = "700 12px \"Helvetica Neue\", Helvetica, Arial, sans-serif";

    public const double LineHeight = 34;
    public const double LastLineBlockHeight = 24;
    public const double NoteShellChromeX = 40;
    public const double BodyMinWidth = 260;
    public const double BodyDefaultWidth = 516;
    public const double BodyMaxWidth = 760;
    public const double PageMargin = 28;
    public const double ChipChromeWidth = 22;

    public static readonly IReadOnlyList<RichNoteInlineSpec> DefaultSpecs =
    [
        new RichNoteTextSpec("Ship ", RichNoteTextStyleName.Body),
        new RichNoteChipSpec("@maya", RichNoteChipTone.Mention),
        new RichNoteTextSpec("'s ", RichNoteTextStyleName.Body),
        new RichNoteTextSpec("rich-note", RichNoteTextStyleName.Code),
        new RichNoteTextSpec(" card once ", RichNoteTextStyleName.Body),
        new RichNoteTextSpec("pre-wrap", RichNoteTextStyleName.Code),
        new RichNoteTextSpec(" lands. Status ", RichNoteTextStyleName.Body),
        new RichNoteChipSpec("blocked", RichNoteChipTone.Status),
        new RichNoteTextSpec(" by ", RichNoteTextStyleName.Body),
        new RichNoteTextSpec("vertical text", RichNoteTextStyleName.Link),
        new RichNoteTextSpec(" research, but 北京 copy and Arabic QA are both green ✅. Keep ", RichNoteTextStyleName.Body),
        new RichNoteChipSpec("جاهز", RichNoteChipTone.Status),
        new RichNoteTextSpec(" for ", RichNoteTextStyleName.Body),
        new RichNoteTextSpec("Cmd+K", RichNoteTextStyleName.Code),
        new RichNoteTextSpec(" docs; the review bundle now includes 中文 labels, عربي fallback, and one more launch pass 🚀 for ", RichNoteTextStyleName.Body),
        new RichNoteChipSpec("Fri 2:30 PM", RichNoteChipTone.Time),
        new RichNoteTextSpec(". Keep ", RichNoteTextStyleName.Body),
        new RichNoteTextSpec("layoutNextLine()", RichNoteTextStyleName.Code),
        new RichNoteTextSpec(" public, tag this ", RichNoteTextStyleName.Body),
        new RichNoteChipSpec("P1", RichNoteChipTone.Priority),
        new RichNoteTextSpec(", keep ", RichNoteTextStyleName.Body),
        new RichNoteChipSpec("3 reviewers", RichNoteChipTone.Count),
        new RichNoteTextSpec(", and route feedback to ", RichNoteTextStyleName.Body),
        new RichNoteTextSpec("design sync", RichNoteTextStyleName.Link),
        new RichNoteTextSpec(".", RichNoteTextStyleName.Body),
    ];

    public static PreparedRichInlineNote PrepareRichInlineNote(IReadOnlyList<RichNoteInlineSpec>? specs = null)
    {
        specs ??= DefaultSpecs;
        var classNames = new string[specs.Count];
        var items = new RichInlineItem[specs.Count];

        for (var index = 0; index < specs.Count; index++)
        {
            switch (specs[index])
            {
                case RichNoteChipSpec chip:
                    classNames[index] = ResolveChipClassName(chip.Tone);
                    items[index] = new RichInlineItem(
                        chip.Label,
                        ChipFont,
                        RichInlineBreakMode.Never,
                        ChipChromeWidth);
                    break;

                case RichNoteTextSpec text:
                    var style = ResolveTextStyle(text.Style);
                    classNames[index] = style.ClassName;
                    items[index] = new RichInlineItem(
                        text.Text,
                        style.Font,
                        RichInlineBreakMode.Normal,
                        style.ExtraWidth);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported rich note spec: {specs[index]?.GetType().Name ?? "<null>"}");
            }
        }

        return new PreparedRichInlineNote(classNames, PretextLayout.PrepareRichInline(items));
    }

    public static RichNoteLayout LayoutRichNote(PreparedRichInlineNote prepared, double bodyWidth)
    {
        var lines = new List<RichNoteLine>();
        PretextLayout.WalkRichInlineLineRanges(prepared.Flow, bodyWidth, range =>
        {
            var line = PretextLayout.MaterializeRichInlineLineRange(prepared.Flow, range);
            var fragments = new RichNoteFragment[line.Fragments.Length];
            for (var index = 0; index < fragments.Length; index++)
            {
                var fragment = line.Fragments[index];
                fragments[index] = new RichNoteFragment(
                    prepared.ClassNames[fragment.ItemIndex],
                    fragment.GapBefore,
                    fragment.Text);
            }

            lines.Add(new RichNoteLine(fragments));
        });

        var lineCount = lines.Count;
        return new RichNoteLayout(
            bodyWidth,
            lineCount,
            lines,
            lineCount == 0 ? LastLineBlockHeight : (lineCount - 1) * LineHeight + LastLineBlockHeight,
            bodyWidth + NoteShellChromeX);
    }

    public static (double BodyWidth, double MaxBodyWidth) ResolveRichNoteBodyWidth(double viewportWidth, double requestedWidth)
    {
        var maxBodyWidth = Math.Max(
            BodyMinWidth,
            Math.Min(BodyMaxWidth, viewportWidth - PageMargin * 2 - NoteShellChromeX));
        return (
            Math.Max(BodyMinWidth, Math.Min(maxBodyWidth, requestedWidth)),
            maxBodyWidth);
    }

    private static (string ClassName, string Font, double ExtraWidth) ResolveTextStyle(RichNoteTextStyleName style)
    {
        return style switch
        {
            RichNoteTextStyleName.Body => ("body", BodyFont, 0),
            RichNoteTextStyleName.Link => ("link", LinkFont, 0),
            RichNoteTextStyleName.Code => ("code", CodeFont, 14),
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
        };
    }

    private static string ResolveChipClassName(RichNoteChipTone tone)
    {
        return tone switch
        {
            RichNoteChipTone.Mention => "chip--mention",
            RichNoteChipTone.Status => "chip--status",
            RichNoteChipTone.Priority => "chip--priority",
            RichNoteChipTone.Time => "chip--time",
            RichNoteChipTone.Count => "chip--count",
            _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, null),
        };
    }
}
