using System.Runtime.InteropServices;

namespace Pretext.CoreText;

public sealed class CoreTextTextMeasurerFactory : IPretextTextMeasurerFactory
{
    public string Name => "CoreText";

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public int Priority => 100;

    public IPretextTextMeasurer Create(string font)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("CoreText is only available on macOS.");
        }

        return new CoreTextTextMeasurer(FontSpec.FromDescriptor(PretextFontParser.Parse(font)));
    }

    private sealed class CoreTextTextMeasurer : IPretextTextMeasurer
    {
        private IntPtr _font;

        public CoreTextTextMeasurer(FontSpec fontSpec)
        {
            _font = CoreTextRuntime.CreateFont(fontSpec);
            if (_font == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create a CoreText font.");
            }
        }

        public double MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            return CoreTextRuntime.MeasureText(_font, text);
        }

        public void Dispose()
        {
            if (_font == IntPtr.Zero)
            {
                return;
            }

            CoreTextRuntime.Release(_font);
            _font = IntPtr.Zero;
        }
    }

    private readonly struct FontSpec
    {
        public FontSpec(double size, string family, int weight, bool italic)
        {
            Size = size;
            Family = family;
            Weight = weight;
            Italic = italic;
        }

        public double Size { get; }

        public string Family { get; }

        public int Weight { get; }

        public bool Italic { get; }

        public static FontSpec FromDescriptor(PretextFontDescriptor descriptor)
        {
            var family = PretextFontParser.MapGenericFamily(
                descriptor.PrimaryFamily,
                sansSerifFallback: "Helvetica Neue",
                serifFallback: "Times",
                monospaceFallback: "Menlo");
            return new FontSpec(Math.Max(1, descriptor.Size), family, descriptor.Weight, descriptor.Italic);
        }
    }

    private static class CoreTextRuntime
    {
        private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        private const string CoreTextLibrary = "/System/Library/Frameworks/CoreText.framework/CoreText";
        private const string LibSystemLibrary = "/usr/lib/libSystem.B.dylib";
        private const int RTLD_NOW = 2;
        private const int CFNumberFloat64Type = 13;

        private static readonly object s_gate = new();
        private static bool s_initializationAttempted;
        private static IntPtr s_ctFontAttributeName;
        private static IntPtr s_ctFontTraitsAttribute;
        private static IntPtr s_ctFontWeightTrait;
        private static IntPtr s_ctFontSlantTrait;
        private static IntPtr s_cfTypeDictionaryKeyCallBacks;
        private static IntPtr s_cfTypeDictionaryValueCallBacks;

        public static IntPtr CreateFont(FontSpec spec)
        {
            EnsureInitialized();

            var familyName = CreateString(spec.Family);
            if (familyName == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var baseFont = IntPtr.Zero;
            try
            {
                baseFont = CTFontCreateWithName(familyName, spec.Size, IntPtr.Zero);
                if (baseFont == IntPtr.Zero && !string.Equals(spec.Family, "Helvetica Neue", StringComparison.Ordinal))
                {
                    Release(baseFont);
                    baseFont = CreateFallbackFont(spec.Size);
                }

                if (baseFont == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                if (spec.Weight == 400 && !spec.Italic)
                {
                    return baseFont;
                }

                var styled = TryCreateStyledFont(baseFont, spec);
                if (styled == IntPtr.Zero)
                {
                    return baseFont;
                }

                Release(baseFont);
                return styled;
            }
            finally
            {
                Release(familyName);
            }
        }

        public static double MeasureText(IntPtr font, string text)
        {
            EnsureInitialized();

            if (font == IntPtr.Zero || string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var lineWidth = MeasureWithCoreTextLine(font, text);
            if (lineWidth > 0)
            {
                return lineWidth;
            }

            return MeasureWithGlyphAdvances(font, text);
        }

        public static void Release(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                CFRelease(handle);
            }
        }

        private static IntPtr CreateFallbackFont(double size)
        {
            var fallbackName = CreateString("Helvetica Neue");
            if (fallbackName == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                return CTFontCreateWithName(fallbackName, size, IntPtr.Zero);
            }
            finally
            {
                Release(fallbackName);
            }
        }

        private static double MeasureWithCoreTextLine(IntPtr font, string text)
        {
            if (s_ctFontAttributeName == IntPtr.Zero ||
                s_cfTypeDictionaryKeyCallBacks == IntPtr.Zero ||
                s_cfTypeDictionaryValueCallBacks == IntPtr.Zero)
            {
                return 0;
            }

            var cfText = CreateString(text);
            if (cfText == IntPtr.Zero)
            {
                return 0;
            }

            IntPtr attributes = IntPtr.Zero;
            IntPtr attributedString = IntPtr.Zero;
            IntPtr line = IntPtr.Zero;

            try
            {
                attributes = CFDictionaryCreate(
                    IntPtr.Zero,
                    new[] { s_ctFontAttributeName },
                    new[] { font },
                    (nint)1,
                    s_cfTypeDictionaryKeyCallBacks,
                    s_cfTypeDictionaryValueCallBacks);
                if (attributes == IntPtr.Zero)
                {
                    return 0;
                }

                attributedString = CFAttributedStringCreate(IntPtr.Zero, cfText, attributes);
                if (attributedString == IntPtr.Zero)
                {
                    return 0;
                }

                line = CTLineCreateWithAttributedString(attributedString);
                if (line == IntPtr.Zero)
                {
                    return 0;
                }

                return CTLineGetTypographicBounds(line, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                Release(line);
                Release(attributedString);
                Release(attributes);
                Release(cfText);
            }
        }

        private static double MeasureWithGlyphAdvances(IntPtr font, string text)
        {
            var characters = text.ToCharArray();
            if (characters.Length == 0)
            {
                return 0;
            }

            var glyphs = new ushort[characters.Length];
            if (!CTFontGetGlyphsForCharacters(font, characters, glyphs, (nint)glyphs.Length))
            {
                return 0;
            }

            var advances = new CGSize[glyphs.Length];
            CTFontGetAdvancesForGlyphs(font, 0, glyphs, advances, (nint)glyphs.Length);

            double width = 0;
            for (var index = 0; index < advances.Length; index++)
            {
                width += advances[index].Width;
            }

            return width;
        }

        private static IntPtr TryCreateStyledFont(IntPtr baseFont, FontSpec spec)
        {
            if (baseFont == IntPtr.Zero ||
                s_ctFontTraitsAttribute == IntPtr.Zero ||
                s_ctFontWeightTrait == IntPtr.Zero ||
                s_cfTypeDictionaryKeyCallBacks == IntPtr.Zero ||
                s_cfTypeDictionaryValueCallBacks == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var weightValue = MapWeight(spec.Weight);
            var weightNumber = CFNumberCreate(IntPtr.Zero, CFNumberFloat64Type, ref weightValue);
            if (weightNumber == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            IntPtr slantNumber = IntPtr.Zero;
            IntPtr traitsDictionary = IntPtr.Zero;
            IntPtr attributesDictionary = IntPtr.Zero;
            IntPtr descriptor = IntPtr.Zero;

            try
            {
                IntPtr[] traitKeys;
                IntPtr[] traitValues;

                if (spec.Italic && s_ctFontSlantTrait != IntPtr.Zero)
                {
                    var slantValue = 1d;
                    slantNumber = CFNumberCreate(IntPtr.Zero, CFNumberFloat64Type, ref slantValue);
                    if (slantNumber == IntPtr.Zero)
                    {
                        return IntPtr.Zero;
                    }

                    traitKeys = new[] { s_ctFontWeightTrait, s_ctFontSlantTrait };
                    traitValues = new[] { weightNumber, slantNumber };
                }
                else
                {
                    traitKeys = new[] { s_ctFontWeightTrait };
                    traitValues = new[] { weightNumber };
                }

                traitsDictionary = CFDictionaryCreate(
                    IntPtr.Zero,
                    traitKeys,
                    traitValues,
                    (nint)traitKeys.Length,
                    s_cfTypeDictionaryKeyCallBacks,
                    s_cfTypeDictionaryValueCallBacks);
                if (traitsDictionary == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                attributesDictionary = CFDictionaryCreate(
                    IntPtr.Zero,
                    new[] { s_ctFontTraitsAttribute },
                    new[] { traitsDictionary },
                    (nint)1,
                    s_cfTypeDictionaryKeyCallBacks,
                    s_cfTypeDictionaryValueCallBacks);
                if (attributesDictionary == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                descriptor = CTFontDescriptorCreateWithAttributes(attributesDictionary);
                if (descriptor == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                return CTFontCreateCopyWithAttributes(baseFont, spec.Size, IntPtr.Zero, descriptor);
            }
            finally
            {
                Release(descriptor);
                Release(attributesDictionary);
                Release(traitsDictionary);
                Release(slantNumber);
                Release(weightNumber);
            }
        }

        private static double MapWeight(int weight)
        {
            if (weight <= 100)
            {
                return -0.8;
            }

            if (weight <= 200)
            {
                return -0.6;
            }

            if (weight <= 300)
            {
                return -0.4;
            }

            if (weight <= 400)
            {
                return 0.0;
            }

            if (weight <= 500)
            {
                return 0.23;
            }

            if (weight <= 600)
            {
                return 0.3;
            }

            if (weight <= 700)
            {
                return 0.4;
            }

            if (weight <= 800)
            {
                return 0.56;
            }

            return 0.62;
        }

        private static IntPtr CreateString(string value)
        {
            return string.IsNullOrEmpty(value)
                ? IntPtr.Zero
                : CFStringCreateWithCharacters(IntPtr.Zero, value, (nint)value.Length);
        }

        private static void EnsureInitialized()
        {
            if (s_initializationAttempted)
            {
                return;
            }

            lock (s_gate)
            {
                if (s_initializationAttempted)
                {
                    return;
                }

                s_initializationAttempted = true;
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return;
                }

                var coreText = dlopen(CoreTextLibrary, RTLD_NOW);
                var coreFoundation = dlopen(CoreFoundationLibrary, RTLD_NOW);
                if (coreText == IntPtr.Zero || coreFoundation == IntPtr.Zero)
                {
                    return;
                }

                s_ctFontAttributeName = ReadObjectSymbol(coreText, "kCTFontAttributeName");
                s_ctFontTraitsAttribute = ReadObjectSymbol(coreText, "kCTFontTraitsAttribute");
                s_ctFontWeightTrait = ReadObjectSymbol(coreText, "kCTFontWeightTrait");
                s_ctFontSlantTrait = ReadObjectSymbol(coreText, "kCTFontSlantTrait");
                s_cfTypeDictionaryKeyCallBacks = dlsym(coreFoundation, "kCFTypeDictionaryKeyCallBacks");
                s_cfTypeDictionaryValueCallBacks = dlsym(coreFoundation, "kCFTypeDictionaryValueCallBacks");
            }
        }

        private static IntPtr ReadObjectSymbol(IntPtr libraryHandle, string symbolName)
        {
            var symbol = dlsym(libraryHandle, symbolName);
            return symbol == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(symbol);
        }

        [DllImport(CoreFoundationLibrary)]
        private static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator, string chars, nint numChars);

        [DllImport(CoreFoundationLibrary)]
        private static extern IntPtr CFNumberCreate(IntPtr allocator, int type, ref double value);

        [DllImport(CoreFoundationLibrary)]
        private static extern IntPtr CFDictionaryCreate(
            IntPtr allocator,
            IntPtr[] keys,
            IntPtr[] values,
            nint numValues,
            IntPtr keyCallbacks,
            IntPtr valueCallbacks);

        [DllImport(CoreFoundationLibrary)]
        private static extern IntPtr CFAttributedStringCreate(IntPtr allocator, IntPtr value, IntPtr attributes);

        [DllImport(CoreFoundationLibrary)]
        private static extern void CFRelease(IntPtr handle);

        [DllImport(CoreTextLibrary)]
        private static extern IntPtr CTFontCreateWithName(IntPtr name, double size, IntPtr matrix);

        [DllImport(CoreTextLibrary)]
        private static extern IntPtr CTFontCreateCopyWithAttributes(IntPtr font, double size, IntPtr matrix, IntPtr attributes);

        [DllImport(CoreTextLibrary)]
        private static extern IntPtr CTFontDescriptorCreateWithAttributes(IntPtr attributes);

        [DllImport(CoreTextLibrary)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool CTFontGetGlyphsForCharacters(IntPtr font, char[] characters, ushort[] glyphs, nint count);

        [DllImport(CoreTextLibrary)]
        private static extern double CTFontGetAdvancesForGlyphs(IntPtr font, int orientation, ushort[] glyphs, CGSize[] advances, nint count);

        [DllImport(CoreTextLibrary)]
        private static extern IntPtr CTLineCreateWithAttributedString(IntPtr attributedString);

        [DllImport(CoreTextLibrary)]
        private static extern double CTLineGetTypographicBounds(IntPtr line, IntPtr ascent, IntPtr descent, IntPtr leading);

        [DllImport(LibSystemLibrary)]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport(LibSystemLibrary)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CGSize
    {
        public CGSize(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width { get; }

        public double Height { get; }
    }
}
