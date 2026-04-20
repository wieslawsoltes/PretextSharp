using System.Runtime.InteropServices;
using System.Text;

namespace Pretext.FreeType;

internal static class LinuxFontResolver
{
    public static string? ResolvePrimaryFontPath(string family, int weight, bool italic)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            family = "DejaVu Sans";
        }

        if (LooksLikeFontPath(family) && File.Exists(family))
        {
            return family;
        }

        var fontconfigPath = ResolveWithFontconfig(family, weight, italic);
        if (!string.IsNullOrEmpty(fontconfigPath))
        {
            return fontconfigPath;
        }

        return ProbeCommonFontDirectories(family, weight, italic);
    }

    public static string? ResolveFallbackFontPath(uint codepoint, int weight, bool italic)
    {
        IntPtr charSet = IntPtr.Zero;
        IntPtr pattern = IntPtr.Zero;
        IntPtr fontSet = IntPtr.Zero;

        try
        {
            if (!FontconfigNative.FcInit())
            {
                return null;
            }

            charSet = FontconfigNative.FcCharSetCreate();
            if (charSet == IntPtr.Zero)
            {
                return null;
            }

            if (!FontconfigNative.FcCharSetAddChar(charSet, codepoint))
            {
                return null;
            }

            pattern = FontconfigNative.FcPatternCreate();
            if (pattern == IntPtr.Zero)
            {
                return null;
            }

            if (!FontconfigNative.FcPatternAddCharSet(pattern, FontconfigNative.FC_CHARSET, charSet))
            {
                return null;
            }

            FontconfigNative.FcPatternAddInteger(pattern, FontconfigNative.FC_WEIGHT, MapWeight(weight));
            FontconfigNative.FcPatternAddInteger(pattern, FontconfigNative.FC_SLANT, italic ? FontconfigNative.FC_SLANT_ITALIC : FontconfigNative.FC_SLANT_ROMAN);
            FontconfigNative.FcConfigSubstitute(IntPtr.Zero, pattern, FontconfigNative.FcMatchPattern);
            FontconfigNative.FcDefaultSubstitute(pattern);

            fontSet = FontconfigNative.FcFontSort(IntPtr.Zero, pattern, trim: true, out _, out var result);
            if (fontSet == IntPtr.Zero || result != FontconfigNative.FcResultMatch)
            {
                return null;
            }

            var set = Marshal.PtrToStructure<FcFontSet>(fontSet);
            if (set.Fonts == IntPtr.Zero || set.Count <= 0)
            {
                return null;
            }

            for (var index = 0; index < set.Count; index++)
            {
                var fontPattern = Marshal.ReadIntPtr(set.Fonts, index * IntPtr.Size);
                if (fontPattern == IntPtr.Zero)
                {
                    continue;
                }

                if (TryGetPatternString(fontPattern, FontconfigNative.FC_FILE, out var filePath) &&
                    !string.IsNullOrWhiteSpace(filePath) &&
                    File.Exists(filePath))
                {
                    return filePath;
                }
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            if (fontSet != IntPtr.Zero)
            {
                FontconfigNative.FcFontSetDestroy(fontSet);
            }

            if (pattern != IntPtr.Zero)
            {
                FontconfigNative.FcPatternDestroy(pattern);
            }

            if (charSet != IntPtr.Zero)
            {
                FontconfigNative.FcCharSetDestroy(charSet);
            }
        }

        return null;
    }

    private static string? ResolveWithFontconfig(string family, int weight, bool italic)
    {
        IntPtr pattern = IntPtr.Zero;
        IntPtr match = IntPtr.Zero;

        try
        {
            if (!FontconfigNative.FcInit())
            {
                return null;
            }

            pattern = FontconfigNative.FcPatternCreate();
            if (pattern == IntPtr.Zero)
            {
                return null;
            }

            if (!FontconfigNative.FcPatternAddString(pattern, FontconfigNative.FC_FAMILY, family))
            {
                return null;
            }

            FontconfigNative.FcPatternAddInteger(pattern, FontconfigNative.FC_WEIGHT, MapWeight(weight));
            FontconfigNative.FcPatternAddInteger(pattern, FontconfigNative.FC_SLANT, italic ? FontconfigNative.FC_SLANT_ITALIC : FontconfigNative.FC_SLANT_ROMAN);
            FontconfigNative.FcConfigSubstitute(IntPtr.Zero, pattern, FontconfigNative.FcMatchPattern);
            FontconfigNative.FcDefaultSubstitute(pattern);

            match = FontconfigNative.FcFontMatch(IntPtr.Zero, pattern, out var result);
            if (match == IntPtr.Zero || result != FontconfigNative.FcResultMatch)
            {
                return null;
            }

            return TryGetPatternString(match, FontconfigNative.FC_FILE, out var filePath) &&
                !string.IsNullOrWhiteSpace(filePath) &&
                File.Exists(filePath)
                ? filePath
                : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (match != IntPtr.Zero)
            {
                FontconfigNative.FcPatternDestroy(match);
            }

            if (pattern != IntPtr.Zero)
            {
                FontconfigNative.FcPatternDestroy(pattern);
            }
        }
    }

    private static string? ProbeCommonFontDirectories(string family, int weight, bool italic)
    {
        var normalizedFamily = family.Replace(" ", string.Empty);
        var bold = weight >= 600;

        string[] candidateNames =
        [
            bold && italic ? normalizedFamily + "-BoldItalic" : string.Empty,
            bold ? normalizedFamily + "-Bold" : string.Empty,
            italic ? normalizedFamily + "-Italic" : string.Empty,
            normalizedFamily + ".ttf",
            normalizedFamily + ".otf",
            "DejaVuSans.ttf"
        ];

        string[] directories =
        [
            "/usr/share/fonts",
            "/usr/local/share/fonts",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fonts"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/fonts")
        ];

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(file);
                    for (var index = 0; index < candidateNames.Length; index++)
                    {
                        var candidate = candidateNames[index];
                        if (!string.IsNullOrEmpty(candidate) &&
                            string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            return file;
                        }
                    }
                }
            }
            catch
            {
                // Ignore unreadable directories and continue probing.
            }
        }

        return null;
    }

    private static bool TryGetPatternString(IntPtr pattern, string key, out string? value)
    {
        value = null;
        var result = FontconfigNative.FcPatternGetString(pattern, key, 0, out var pointer);
        if (result != FontconfigNative.FcResultMatch || pointer == IntPtr.Zero)
        {
            return false;
        }

        value = PtrToUtf8String(pointer);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string? PtrToUtf8String(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
        {
            return null;
        }

        var bytes = new List<byte>();
        var offset = 0;
        while (true)
        {
            var value = Marshal.ReadByte(pointer, offset++);
            if (value == 0)
            {
                break;
            }

            bytes.Add(value);
        }

        return bytes.Count == 0 ? string.Empty : Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static int MapWeight(int weight)
    {
        if (weight <= 100)
        {
            return FontconfigNative.FC_WEIGHT_THIN;
        }

        if (weight <= 300)
        {
            return FontconfigNative.FC_WEIGHT_LIGHT;
        }

        if (weight <= 450)
        {
            return FontconfigNative.FC_WEIGHT_REGULAR;
        }

        if (weight <= 550)
        {
            return FontconfigNative.FC_WEIGHT_MEDIUM;
        }

        if (weight <= 650)
        {
            return FontconfigNative.FC_WEIGHT_SEMIBOLD;
        }

        if (weight <= 800)
        {
            return FontconfigNative.FC_WEIGHT_BOLD;
        }

        return FontconfigNative.FC_WEIGHT_BLACK;
    }

    private static bool LooksLikeFontPath(string value)
    {
        return value.Contains('/') || value.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FcFontSet
    {
        public FcFontSet(int count, int storage, IntPtr fonts)
        {
            Count = count;
            Storage = storage;
            Fonts = fonts;
        }

        public int Count { get; }

        public int Storage { get; }

        public IntPtr Fonts { get; }
    }
}
