using System.Globalization;
using System.Text.RegularExpressions;

namespace Pretext;

public readonly struct PretextFontDescriptor
{
    public PretextFontDescriptor(double size, string primaryFamily, int weight, bool italic)
    {
        Size = size;
        PrimaryFamily = string.IsNullOrWhiteSpace(primaryFamily) ? "Arial" : primaryFamily;
        Weight = weight;
        Italic = italic;
    }

    public double Size { get; }

    public string PrimaryFamily { get; }

    public int Weight { get; }

    public bool Italic { get; }
}

public static class PretextFontParser
{
    private static readonly Regex s_fontSizeRegex = new(@"(\d+(?:\.\d+)?)\s*px", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static PretextFontDescriptor Parse(string font)
    {
        if (string.IsNullOrWhiteSpace(font))
        {
            return new PretextFontDescriptor(16, "Arial", 400, italic: false);
        }

        var match = s_fontSizeRegex.Match(font);
        if (!match.Success)
        {
            return new PretextFontDescriptor(16, "Arial", 400, italic: false);
        }

        var size = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var beforeSize = font.Substring(0, match.Index);
        var afterSize = font.Substring(match.Index + match.Length).Trim();

        if (afterSize.Length > 0 && afterSize[0] == '/')
        {
            var nextSpace = afterSize.IndexOf(' ');
            afterSize = nextSpace >= 0 ? afterSize.Substring(nextSpace + 1).Trim() : string.Empty;
        }

        var italic = false;
        var weight = 400;
        var tokens = beforeSize.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (string.Equals(token, "italic", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "oblique", StringComparison.OrdinalIgnoreCase))
            {
                italic = true;
                continue;
            }

            if (string.Equals(token, "bold", StringComparison.OrdinalIgnoreCase))
            {
                weight = 700;
                continue;
            }

            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWeight))
            {
                weight = parsedWeight;
                break;
            }
        }

        return new PretextFontDescriptor(size, ExtractPrimaryFamily(afterSize), weight, italic);
    }

    public static string MapGenericFamily(string primaryFamily, string sansSerifFallback, string serifFallback, string monospaceFallback)
    {
        if (string.IsNullOrWhiteSpace(primaryFamily))
        {
            return sansSerifFallback;
        }

        if (string.Equals(primaryFamily, "sans-serif", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(primaryFamily, "system-ui", StringComparison.OrdinalIgnoreCase))
        {
            return sansSerifFallback;
        }

        if (string.Equals(primaryFamily, "serif", StringComparison.OrdinalIgnoreCase))
        {
            return serifFallback;
        }

        if (string.Equals(primaryFamily, "monospace", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(primaryFamily, "ui-monospace", StringComparison.OrdinalIgnoreCase))
        {
            return monospaceFallback;
        }

        return primaryFamily;
    }

    private static string ExtractPrimaryFamily(string familyList)
    {
        if (string.IsNullOrWhiteSpace(familyList))
        {
            return "Arial";
        }

        var commaIndex = familyList.IndexOf(',');
        var primary = commaIndex >= 0 ? familyList.Substring(0, commaIndex) : familyList;
        primary = primary.Trim();

        if (primary.Length >= 2 &&
            ((primary[0] == '"' && primary[primary.Length - 1] == '"') ||
             (primary[0] == '\'' && primary[primary.Length - 1] == '\'')))
        {
            primary = primary.Substring(1, primary.Length - 2).Trim();
        }

        return string.IsNullOrWhiteSpace(primary) ? "Arial" : primary;
    }
}
