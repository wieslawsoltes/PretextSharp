using System.Runtime.InteropServices;

namespace Pretext.FreeType;

internal static unsafe class HarfBuzzNative
{
    private const string HarfBuzzLibrary = "libharfbuzz.so.0";

    [DllImport(HarfBuzzLibrary)]
    public static extern IntPtr hb_ft_font_create(IntPtr freeTypeFace, IntPtr destroy);

    [DllImport(HarfBuzzLibrary)]
    public static extern void hb_font_destroy(IntPtr font);

    [DllImport(HarfBuzzLibrary)]
    public static extern IntPtr hb_buffer_create();

    [DllImport(HarfBuzzLibrary)]
    public static extern void hb_buffer_destroy(IntPtr buffer);

    [DllImport(HarfBuzzLibrary)]
    public static extern void hb_buffer_reset(IntPtr buffer);

    [DllImport(HarfBuzzLibrary)]
    public static extern void hb_buffer_add_utf16(IntPtr buffer, ushort* text, int textLength, uint itemOffset, int itemLength);

    [DllImport(HarfBuzzLibrary)]
    public static extern void hb_buffer_guess_segment_properties(IntPtr buffer);

    [DllImport(HarfBuzzLibrary)]
    public static extern void hb_shape(IntPtr font, IntPtr buffer, IntPtr features, uint featureCount);

    [DllImport(HarfBuzzLibrary)]
    public static extern hb_glyph_info_t* hb_buffer_get_glyph_infos(IntPtr buffer, out uint length);

    [DllImport(HarfBuzzLibrary)]
    public static extern hb_glyph_position_t* hb_buffer_get_glyph_positions(IntPtr buffer, out uint length);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct hb_glyph_info_t
{
    public hb_glyph_info_t(uint codepoint, uint mask, uint cluster, uint var1, uint var2)
    {
        Codepoint = codepoint;
        Mask = mask;
        Cluster = cluster;
        Var1 = var1;
        Var2 = var2;
    }

    public uint Codepoint { get; }

    public uint Mask { get; }

    public uint Cluster { get; }

    public uint Var1 { get; }

    public uint Var2 { get; }
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct hb_glyph_position_t
{
    public hb_glyph_position_t(int xAdvance, int yAdvance, int xOffset, int yOffset, uint var)
    {
        XAdvance = xAdvance;
        YAdvance = yAdvance;
        XOffset = xOffset;
        YOffset = yOffset;
        Var = var;
    }

    public int XAdvance { get; }

    public int YAdvance { get; }

    public int XOffset { get; }

    public int YOffset { get; }

    public uint Var { get; }
}
