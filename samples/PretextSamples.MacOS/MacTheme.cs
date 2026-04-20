using System.Globalization;

namespace PretextSamples.MacOS;

internal static class MacTheme
{
    public static readonly nfloat PageMargin = 28;
    public static readonly nfloat CardRadius = 20;
    public static readonly nfloat SectionGap = 18;

    public static readonly NSColor PageBrush = Color(0xF5, 0xF2, 0xEC);
    public static readonly NSColor PanelBrush = Color(0xFF, 0xFD, 0xF9);
    public static readonly NSColor InkBrush = Color(0x20, 0x1B, 0x18);
    public static readonly NSColor MutedBrush = Color(0x6D, 0x64, 0x5D);
    public static readonly NSColor RuleBrush = Color(0xD8, 0xCE, 0xC3);
    public static readonly NSColor AccentBrush = Color(0x95, 0x5F, 0x3B);
    public static readonly NSColor AccentSoftBrush = Color(0xF0, 0xE4, 0xDA);
    public static readonly NSColor ChatBackgroundBrush = Color(0x1C, 0x1C, 0x1E);
    public static readonly NSColor SentBubbleBrush = Color(0x0B, 0x84, 0xFE);
    public static readonly NSColor ReceiveBubbleBrush = Color(0x2C, 0x2C, 0x2E);
    public static readonly NSColor WhiteBrush = Color(0xFF, 0xFF, 0xFF);
    public static readonly NSColor SidebarBrush = Color(0xEF, 0xE8, 0xDE);

    public static nfloat N(double value) => (nfloat)value;
    public static nfloat Max(nfloat a, nfloat b) => a >= b ? a : b;
    public static nfloat Min(nfloat a, nfloat b) => a <= b ? a : b;

    public static NSColor Color(byte r, byte g, byte b, byte a = 255)
        => NSColor.FromSrgb(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);

    public static NSTextField CreateLabel(string text, NSColor color, NSFont font, NSTextAlignment alignment = NSTextAlignment.Left)
    {
        return new NSTextField
        {
            StringValue = text,
            Editable = false,
            Selectable = false,
            Bezeled = false,
            DrawsBackground = false,
            Bordered = false,
            TextColor = color,
            Font = font,
            Alignment = alignment,
            LineBreakMode = NSLineBreakMode.ByWordWrapping,
            UsesSingleLineMode = false,
        };
    }

    public static NSButton CreateButton(string title)
    {
        return new NSButton
        {
            Title = title,
            BezelStyle = NSBezelStyle.Rounded,
            Font = Sans(12, bold: true),
        };
    }

    public static NSButton CreateCheckBox(string title, bool isChecked)
    {
        var button = new NSButton
        {
            Title = title,
            Font = Sans(13),
        };
        button.SetButtonType(NSButtonType.Switch);
        button.State = isChecked ? NSCellStateValue.On : NSCellStateValue.Off;
        return button;
    }

    public static NSSlider CreateSlider(double min, double max, double value)
    {
        return new NSSlider
        {
            MinValue = min,
            MaxValue = max,
            DoubleValue = value,
            Continuous = true,
        };
    }

    public static NSFont Sans(nfloat size, bool bold = false)
        => ResolveNamedFont(bold ? "Helvetica Neue Bold" : "Helvetica Neue", size, RequiredFont(bold ? NSFont.BoldSystemFontOfSize(size) : NSFont.SystemFontOfSize(size), size));

    public static NSFont Serif(nfloat size, bool bold = false, bool italic = false)
    {
        var name = italic && bold ? "Georgia-BoldItalic" :
            italic ? "Georgia-Italic" :
            bold ? "Georgia-Bold" :
            "Georgia";
        return ResolveNamedFont(name, size, RequiredFont(bold ? NSFont.BoldSystemFontOfSize(size) : NSFont.SystemFontOfSize(size), size));
    }

    public static NSFont Mono(nfloat size, bool bold = false)
    {
        var name = bold ? "SFMono-Semibold" : "SFMono-Regular";
        return ResolveNamedFont(name, size, RequiredFont(NSFont.MonospacedSystemFont(size, bold ? NSFontWeight.Semibold : NSFontWeight.Regular), size));
    }

    public static NSFont ResolveCssFont(string font)
    {
        var descriptor = PretextFontParser.Parse(font);
        var family = PretextFontParser.MapGenericFamily(
            descriptor.PrimaryFamily,
            sansSerifFallback: "Helvetica Neue",
            serifFallback: "Georgia",
            monospaceFallback: "SFMono-Regular");

        if (string.Equals(family, "Georgia", StringComparison.OrdinalIgnoreCase))
        {
            return Serif((nfloat)descriptor.Size, descriptor.Weight >= 600, descriptor.Italic);
        }

        if (string.Equals(family, "SF Mono", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(family, "SFMono-Regular", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(family, "Menlo", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(family, "Monaco", StringComparison.OrdinalIgnoreCase))
        {
            return Mono((nfloat)descriptor.Size, descriptor.Weight >= 600);
        }

        return ResolveNamedFont(
            family,
            (nfloat)descriptor.Size,
            RequiredFont(
                descriptor.Weight >= 600
                    ? NSFont.BoldSystemFontOfSize((nfloat)descriptor.Size)
                    : NSFont.SystemFontOfSize((nfloat)descriptor.Size),
                (nfloat)descriptor.Size));
    }

    public static NSStringAttributes CreateAttributes(
        NSFont font,
        NSColor color,
        nfloat? lineHeight = null,
        NSTextAlignment alignment = NSTextAlignment.Left,
        bool underline = false)
    {
        var paragraph = new NSMutableParagraphStyle
        {
            Alignment = alignment,
            LineBreakMode = NSLineBreakMode.ByWordWrapping,
        };
        if (lineHeight is { } fixedLineHeight)
        {
            paragraph.MinimumLineHeight = fixedLineHeight;
            paragraph.MaximumLineHeight = fixedLineHeight;
        }

        var attributes = new NSStringAttributes
        {
            Font = font,
            ForegroundColor = color,
            ParagraphStyle = paragraph,
        };
        if (underline)
        {
            attributes.UnderlineStyle = (int)NSUnderlineStyle.Single;
        }

        return attributes;
    }

    public static NSStringAttributes CreateCssAttributes(
        string font,
        NSColor color,
        nfloat? lineHeight = null,
        NSTextAlignment alignment = NSTextAlignment.Left,
        bool underline = false)
        => CreateAttributes(ResolveCssFont(font), color, lineHeight, alignment, underline);

    public static CGSize MeasureString(string text, NSStringAttributes attributes, nfloat maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return CGSize.Empty;
        }

        var box = new NSString(text).GetBoundingRect(
            new CGSize(maxWidth, nfloat.MaxValue),
            NSStringDrawingOptions.UsesLineFragmentOrigin | NSStringDrawingOptions.UsesFontLeading,
            attributes,
            null);
        return new CGSize((nfloat)Math.Ceiling(box.Width), (nfloat)Math.Ceiling(box.Height));
    }

    public static void DrawWrappedString(string text, CGRect rect, NSStringAttributes attributes)
    {
        if (!string.IsNullOrEmpty(text))
        {
            new NSString(text).DrawInRect(rect, attributes);
        }
    }

    public static void FillRoundedRect(CGRect rect, nfloat radius, NSColor fill, NSColor? stroke = null, double strokeWidth = 1)
    {
        var path = NSBezierPath.FromRoundedRect(rect, radius, radius);
        fill.SetFill();
        path.Fill();

        if (stroke is not null)
        {
            stroke.SetStroke();
            path.LineWidth = (nfloat)strokeWidth;
            path.Stroke();
        }
    }

    public static void FillRect(CGRect rect, NSColor fill)
    {
        fill.SetFill();
        NSBezierPath.FillRect(rect);
    }

    public static CGRect DrawHeader(CGRect bounds, string eyebrow, string title, string description)
    {
        var x = bounds.Left + PageMargin;
        var y = bounds.Top + PageMargin;
        var width = Max(N(320), bounds.Width - PageMargin * 2);

        var eyebrowAttributes = CreateAttributes(Mono(12), AccentBrush);
        var eyebrowSize = MeasureString(eyebrow, eyebrowAttributes, width);
        DrawWrappedString(eyebrow, new CGRect(x, y, width, eyebrowSize.Height), eyebrowAttributes);
        y += eyebrowSize.Height + 8;

        var titleAttributes = CreateAttributes(Serif(32, bold: true), InkBrush, N(38));
        var titleWidth = Min(width, N(920));
        var titleSize = MeasureString(title, titleAttributes, titleWidth);
        DrawWrappedString(title, new CGRect(x, y, titleWidth, titleSize.Height), titleAttributes);
        y += titleSize.Height + 8;

        var bodyAttributes = CreateAttributes(Sans(15), MutedBrush, N(22));
        var bodyWidth = Min(width, N(720));
        var bodySize = MeasureString(description, bodyAttributes, bodyWidth);
        DrawWrappedString(description, new CGRect(x, y, bodyWidth, bodySize.Height), bodyAttributes);
        y += bodySize.Height;

        return new CGRect(x, bounds.Top + PageMargin, width, y - bounds.Top - PageMargin);
    }

    public static nfloat MeasureHeaderHeight(nfloat width, string eyebrow, string title, string description)
    {
        var contentWidth = Max(N(320), width - PageMargin * 2);
        var eyebrowHeight = MeasureString(eyebrow, CreateAttributes(Mono(12), AccentBrush), contentWidth).Height;
        var titleHeight = MeasureString(title, CreateAttributes(Serif(32, bold: true), InkBrush, N(38)), Min(contentWidth, N(920))).Height;
        var descriptionHeight = MeasureString(description, CreateAttributes(Sans(15), MutedBrush, N(22)), Min(contentWidth, N(720))).Height;
        return PageMargin + eyebrowHeight + 8 + titleHeight + 8 + descriptionHeight;
    }

    public static string FormatPixels(double value) => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(value):N0}px");

    private static NSFont ResolveNamedFont(string name, nfloat size, NSFont fallback)
        => NSFont.FromFontName(name, size) ?? fallback;

    private static NSFont RequiredFont(NSFont? font, nfloat size)
        => font ?? NSFont.SystemFontOfSize(size) ?? throw new InvalidOperationException("macOS returned a null system font.");
}
