using System.Runtime.InteropServices;

namespace Pretext.FreeType;

internal static class FreeTypeNative
{
    private const string FreeTypeLibrary = "libfreetype.so.6";

    public const int FT_LOAD_DEFAULT = 0x0;
    public const int FT_KERNING_DEFAULT = 0;

    [DllImport(FreeTypeLibrary)]
    public static extern int FT_Init_FreeType(out IntPtr library);

    [DllImport(FreeTypeLibrary)]
    public static extern int FT_Done_FreeType(IntPtr library);

    [DllImport(FreeTypeLibrary)]
    public static extern int FT_New_Face(IntPtr library, string filePath, int faceIndex, out IntPtr face);

    [DllImport(FreeTypeLibrary)]
    public static extern int FT_Done_Face(IntPtr face);

    [DllImport(FreeTypeLibrary)]
    public static extern int FT_Set_Pixel_Sizes(IntPtr face, uint pixelWidth, uint pixelHeight);

    [DllImport(FreeTypeLibrary)]
    public static extern uint FT_Get_Char_Index(IntPtr face, uint charCode);

    [DllImport(FreeTypeLibrary)]
    public static extern int FT_Get_Advance(IntPtr face, uint glyphIndex, int loadFlags, out IntPtr advance);

    [DllImport(FreeTypeLibrary)]
    public static extern int FT_Get_Kerning(IntPtr face, uint leftGlyph, uint rightGlyph, uint kernMode, out FT_Vector kerning);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct FT_Vector
{
    public FT_Vector(IntPtr x, IntPtr y)
    {
        X = x;
        Y = y;
    }

    public IntPtr X { get; }

    public IntPtr Y { get; }
}
