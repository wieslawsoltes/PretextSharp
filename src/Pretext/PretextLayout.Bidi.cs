using Pretext.Generated;

namespace Pretext;

internal static class BidiHelper
{
    private const sbyte L = 0;
    private const sbyte R = 1;
    private const sbyte AL = 2;
    private const sbyte WS = 3;
    private const sbyte EN = 4;
    private const sbyte AN = 5;
    private const sbyte ES = 6;
    private const sbyte ET = 7;
    private const sbyte CS = 8;
    private const sbyte ON = 9;
    private const sbyte BN = 10;
    private const sbyte B = 11;
    private const sbyte S = 12;
    private const sbyte NSM = 13;

    public static sbyte[]? ComputeSegmentLevels(string text, IReadOnlyList<int> starts)
    {
        var bidiLevels = ComputeLevels(text);
        if (bidiLevels is null)
        {
            return null;
        }

        var levels = new sbyte[starts.Count];
        for (var i = 0; i < starts.Count; i++)
        {
            levels[i] = bidiLevels[starts[i]];
        }

        return levels;
    }

    private static sbyte[]? ComputeLevels(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var types = new sbyte[text.Length];
        var sawBidi = false;

        // Keep bidi classes aligned to UTF-16 offsets because prepared segments
        // index back into the normalized string by code-unit position.
        for (var i = 0; i < text.Length;)
        {
            var first = text[i];
            var codePoint = (int)first;
            var codeUnitLength = 1;

            if (char.IsHighSurrogate(first) &&
                i + 1 < text.Length &&
                char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(first, text[i + 1]);
                codeUnitLength = 2;
            }

            var type = ClassifyCodePoint(codePoint);
            if (type is R or AL or AN)
            {
                sawBidi = true;
            }

            for (var j = 0; j < codeUnitLength; j++)
            {
                types[i + j] = type;
            }

            i += codeUnitLength;
        }

        if (!sawBidi)
        {
            return null;
        }

        // Use the first strong character to approximate paragraph direction.
        var startLevel = 0;
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            if (type == L)
            {
                startLevel = 0;
                break;
            }

            if (type is R or AL)
            {
                startLevel = 1;
                break;
            }
        }

        var levels = new sbyte[text.Length];
        for (var i = 0; i < levels.Length; i++)
        {
            levels[i] = (sbyte)startLevel;
        }

        var embedding = (sbyte)((startLevel & 1) == 1 ? R : L);
        var sor = embedding;

        // W1-W7
        var lastType = sor;
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == NSM)
            {
                types[i] = lastType;
            }
            else
            {
                lastType = types[i];
            }
        }

        lastType = sor;
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == EN)
            {
                types[i] = lastType == AL ? AN : EN;
            }
            else if (types[i] is R or L or AL)
            {
                lastType = types[i];
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == AL)
            {
                types[i] = R;
            }
        }

        for (var i = 1; i < types.Length - 1; i++)
        {
            if (types[i] == ES && types[i - 1] == EN && types[i + 1] == EN)
            {
                types[i] = EN;
            }

            if (types[i] == CS &&
                (types[i - 1] == EN || types[i - 1] == AN) &&
                types[i + 1] == types[i - 1])
            {
                types[i] = types[i - 1];
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] != EN)
            {
                continue;
            }

            for (var j = i - 1; j >= 0 && types[j] == ET; j--)
            {
                types[j] = EN;
            }

            for (var j = i + 1; j < types.Length && types[j] == ET; j++)
            {
                types[j] = EN;
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] is WS or ES or ET or CS)
            {
                types[i] = ON;
            }
        }

        lastType = sor;
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == EN)
            {
                types[i] = lastType == L ? L : EN;
            }
            else if (types[i] is R or L)
            {
                lastType = types[i];
            }
        }

        // N1-N2
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] != ON)
            {
                continue;
            }

            var end = i + 1;
            while (end < types.Length && types[end] == ON)
            {
                end++;
            }

            var before = i > 0 ? types[i - 1] : sor;
            var after = end < types.Length ? types[end] : sor;
            var beforeDir = before != L ? R : L;
            var afterDir = after != L ? R : L;
            if (beforeDir == afterDir)
            {
                for (var j = i; j < end; j++)
                {
                    types[j] = beforeDir;
                }
            }

            i = end - 1;
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == ON)
            {
                types[i] = embedding;
            }
        }

        // I1-I2
        for (var i = 0; i < types.Length; i++)
        {
            if ((levels[i] & 1) == 0)
            {
                if (types[i] == R)
                {
                    levels[i]++;
                }
                else if (types[i] is AN or EN)
                {
                    levels[i] += 2;
                }
            }
            else if (types[i] is L or AN or EN)
            {
                levels[i]++;
            }
        }

        return levels;
    }

    private static sbyte ClassifyCodePoint(int codePoint)
    {
        if (codePoint <= 0x00FF)
        {
            return PretextBidiData.Latin1BidiTypes[codePoint];
        }

        var ranges = PretextBidiData.NonLatin1BidiRanges;
        var lo = 0;
        var hi = ranges.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            var range = ranges[mid];
            if (codePoint < range.Start)
            {
                hi = mid - 1;
                continue;
            }

            if (codePoint > range.End)
            {
                lo = mid + 1;
                continue;
            }

            return range.Type;
        }

        return L;
    }
}
