namespace Pretext;

internal static class BidiHelper
{
    private static readonly sbyte[] BaseTypes =
    {
        10,10,10,10,10,10,10,10,10,11,10,11,12,
        10,10,10,10,10,10,10,10,10,10,10,10,10,
        10,10,10,10,10,11,12,9,9,7,7,7,9,
        9,9,9,9,9,8,9,8,9,4,4,4,
        4,4,4,4,4,4,4,9,9,9,9,9,
        9,9,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,0,0,9,9,
        9,9,9,9,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,9,9,9,9,10,10,10,10,10,10,10,10,
        10,10,10,10,10,10,10,10,10,10,10,10,
        10,10,10,10,10,10,10,10,10,10,10,10,
        10,8,9,7,7,7,7,9,9,9,9,0,9,
        9,9,9,9,7,7,4,4,9,0,9,9,9,
        4,0,9,9,9,9,9,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,9,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,9,0,0,0,0,0,0,0,0
    };

    private static readonly sbyte[] ArabicTypes =
    {
        2,2,2,2,2,2,2,2,2,2,2,2,
        8,2,9,9,13,13,13,13,13,13,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,13,13,13,13,13,13,13,
        13,13,13,13,13,13,13,2,2,2,2,
        2,2,2,5,5,5,5,5,5,5,5,5,
        5,7,5,5,2,2,2,13,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,2,
        2,13,13,13,13,13,13,13,13,13,13,
        13,13,13,13,13,13,13,13,13,9,13,
        13,13,13,2,2,2,2,2,2,2,2,2,
        2,2,2,2,2,2,2,2,2
    };

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
        var bidiCount = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var type = Classify(text[i]);
            if (type is 1 or 2 or 5)
            {
                bidiCount++;
            }

            types[i] = type;
        }

        if (bidiCount == 0)
        {
            return null;
        }

        var startLevel = (text.Length / (double)bidiCount) < 0.3 ? 0 : 1;
        var levels = Enumerable.Repeat((sbyte)startLevel, text.Length).ToArray();
        var embedding = (sbyte)(startLevel % 2 == 1 ? 1 : 0);
        var sor = embedding == 1 ? (sbyte)1 : (sbyte)0;

        var lastType = sor;
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == 13)
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
            if (types[i] == 4)
            {
                types[i] = lastType == 2 ? (sbyte)5 : (sbyte)4;
            }
            else if (types[i] is 0 or 1 or 2)
            {
                lastType = types[i];
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == 2)
            {
                types[i] = 1;
            }
        }

        for (var i = 1; i < types.Length - 1; i++)
        {
            if (types[i] == 6 && types[i - 1] == 4 && types[i + 1] == 4)
            {
                types[i] = 4;
            }

            if (types[i] == 8 && (types[i - 1] == 4 || types[i - 1] == 5) && types[i + 1] == types[i - 1])
            {
                types[i] = types[i - 1];
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] != 4)
            {
                continue;
            }

            for (var j = i - 1; j >= 0 && types[j] == 7; j--)
            {
                types[j] = 4;
            }

            for (var j = i + 1; j < types.Length && types[j] == 7; j++)
            {
                types[j] = 4;
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] is 12 or 6 or 7 or 8)
            {
                types[i] = 9;
            }
        }

        lastType = sor;
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == 4)
            {
                types[i] = lastType == 0 ? (sbyte)0 : (sbyte)4;
            }
            else if (types[i] is 1 or 0)
            {
                lastType = types[i];
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] != 9)
            {
                continue;
            }

            var end = i + 1;
            while (end < types.Length && types[end] == 9)
            {
                end++;
            }

            var before = i > 0 ? types[i - 1] : sor;
            var after = end < types.Length ? types[end] : sor;
            var beforeDir = before != 0 ? (sbyte)1 : (sbyte)0;
            var afterDir = after != 0 ? (sbyte)1 : (sbyte)0;
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
            if (types[i] == 9)
            {
                types[i] = embedding == 1 ? (sbyte)1 : (sbyte)0;
            }
        }

        for (var i = 0; i < types.Length; i++)
        {
            if ((levels[i] & 1) == 0)
            {
                if (types[i] == 1)
                {
                    levels[i]++;
                }
                else if (types[i] is 5 or 4)
                {
                    levels[i] += 2;
                }
            }
            else if (types[i] is 0 or 5 or 4)
            {
                levels[i]++;
            }
        }

        return levels;
    }

    private static sbyte Classify(char ch)
    {
        var code = (int)ch;
        if (code <= 0x00FF)
        {
            return BaseTypes[code];
        }

        if (code is >= 0x0590 and <= 0x05F4)
        {
            return 1;
        }

        if (code is >= 0x0600 and <= 0x06FF)
        {
            return ArabicTypes[code & 0xFF];
        }

        if (code is >= 0x0700 and <= 0x08AC)
        {
            return 2;
        }

        return 0;
    }
}
