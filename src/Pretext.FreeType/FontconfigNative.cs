using System.Runtime.InteropServices;

namespace Pretext.FreeType;

internal static class FontconfigNative
{
    private const string FontconfigLibrary = "libfontconfig.so.1";

    public const string FC_FAMILY = "family";
    public const string FC_FILE = "file";
    public const string FC_WEIGHT = "weight";
    public const string FC_SLANT = "slant";
    public const string FC_CHARSET = "charset";

    public const int FC_WEIGHT_THIN = 0;
    public const int FC_WEIGHT_LIGHT = 50;
    public const int FC_WEIGHT_REGULAR = 80;
    public const int FC_WEIGHT_MEDIUM = 100;
    public const int FC_WEIGHT_SEMIBOLD = 180;
    public const int FC_WEIGHT_BOLD = 200;
    public const int FC_WEIGHT_BLACK = 210;

    public const int FC_SLANT_ROMAN = 0;
    public const int FC_SLANT_ITALIC = 100;

    public const int FcMatchPattern = 0;
    public const int FcResultMatch = 0;

    [DllImport(FontconfigLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool FcInit();

    [DllImport(FontconfigLibrary)]
    public static extern IntPtr FcPatternCreate();

    [DllImport(FontconfigLibrary)]
    public static extern void FcPatternDestroy(IntPtr pattern);

    [DllImport(FontconfigLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool FcPatternAddString(IntPtr pattern, string key, string value);

    [DllImport(FontconfigLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool FcPatternAddInteger(IntPtr pattern, string key, int value);

    [DllImport(FontconfigLibrary)]
    public static extern int FcPatternGetString(IntPtr pattern, string key, int index, out IntPtr value);

    [DllImport(FontconfigLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool FcConfigSubstitute(IntPtr config, IntPtr pattern, int kind);

    [DllImport(FontconfigLibrary)]
    public static extern void FcDefaultSubstitute(IntPtr pattern);

    [DllImport(FontconfigLibrary)]
    public static extern IntPtr FcFontMatch(IntPtr config, IntPtr pattern, out int result);

    [DllImport(FontconfigLibrary)]
    public static extern IntPtr FcCharSetCreate();

    [DllImport(FontconfigLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool FcCharSetAddChar(IntPtr charSet, uint codePoint);

    [DllImport(FontconfigLibrary)]
    public static extern void FcCharSetDestroy(IntPtr charSet);

    [DllImport(FontconfigLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool FcPatternAddCharSet(IntPtr pattern, string key, IntPtr charSet);

    [DllImport(FontconfigLibrary)]
    public static extern IntPtr FcFontSort(IntPtr config, IntPtr pattern, [MarshalAs(UnmanagedType.I1)] bool trim, out IntPtr charSets, out int result);

    [DllImport(FontconfigLibrary)]
    public static extern void FcFontSetDestroy(IntPtr fontSet);
}
