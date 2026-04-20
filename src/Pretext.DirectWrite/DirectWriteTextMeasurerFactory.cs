using System.Globalization;
using System.Runtime.InteropServices;

namespace Pretext.DirectWrite;

public sealed class DirectWriteTextMeasurerFactory : IPretextTextMeasurerFactory
{
    public string Name => "DirectWrite";

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public int Priority => 100;

    public IPretextTextMeasurer Create(string font)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("DirectWrite is only available on Windows.");
        }

        return new DirectWriteTextMeasurer(DirectWriteRuntime.Instance, FontSpec.FromDescriptor(PretextFontParser.Parse(font)));
    }

    private sealed class DirectWriteTextMeasurer : IPretextTextMeasurer
    {
        private readonly DirectWriteRuntime _runtime;
        private readonly nint _textFormat;

        public DirectWriteTextMeasurer(DirectWriteRuntime runtime, FontSpec fontSpec)
        {
            _runtime = runtime;
            _textFormat = _runtime.CreateTextFormat(fontSpec);
            if (_textFormat == 0)
            {
                throw new InvalidOperationException("Failed to create a DirectWrite text format.");
            }
        }

        public double MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var textLayout = _runtime.CreateTextLayout(text, _textFormat);
            if (textLayout == 0)
            {
                return 0;
            }

            try
            {
                DWriteTextMetrics metrics;
                int hr = DirectWriteInterop.GetMetrics(textLayout, out metrics);
                if (hr < 0)
                {
                    return 0;
                }

                return metrics.WidthIncludingTrailingWhitespace;
            }
            finally
            {
                ComInterop.Release(textLayout);
            }
        }

        public void Dispose()
        {
            ComInterop.Release(_textFormat);
        }
    }

    private sealed class DirectWriteRuntime
    {
        private readonly object _gate = new();
        private nint _factory;
        private bool _initialized;

        public static DirectWriteRuntime Instance { get; } = new();

        private DirectWriteRuntime()
        {
        }

        public nint CreateTextFormat(FontSpec fontSpec)
        {
            EnsureInitialized();
            return DirectWriteInterop.CreateTextFormat(_factory, fontSpec);
        }

        public nint CreateTextLayout(string text, nint textFormat)
        {
            EnsureInitialized();
            return DirectWriteInterop.CreateTextLayout(_factory, text, textFormat);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (_gate)
            {
                if (_initialized)
                {
                    return;
                }

                int hr = DirectWriteInterop.DWriteCreateFactory(DWriteFactoryType.Shared, DirectWriteInterop.IID_IDWriteFactory, out _factory);
                if (hr < 0 || _factory == 0)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "DWriteCreateFactory failed: 0x{0:X8}", hr));
                }

                _initialized = true;
            }
        }
    }

    private readonly struct FontSpec
    {
        public FontSpec(float size, string family, DWriteFontWeight weight, DWriteFontStyle style, string locale)
        {
            Size = size;
            Family = family;
            Weight = weight;
            Style = style;
            Locale = locale;
        }

        public float Size { get; }

        public string Family { get; }

        public DWriteFontWeight Weight { get; }

        public DWriteFontStyle Style { get; }

        public string Locale { get; }

        public static FontSpec FromDescriptor(PretextFontDescriptor descriptor)
        {
            var family = PretextFontParser.MapGenericFamily(
                descriptor.PrimaryFamily,
                sansSerifFallback: "Segoe UI",
                serifFallback: "Times New Roman",
                monospaceFallback: "Consolas");
            var weight = descriptor.Weight >= 700 ? DWriteFontWeight.Bold : descriptor.Weight >= 500 ? DWriteFontWeight.Medium : DWriteFontWeight.Normal;
            var style = descriptor.Italic ? DWriteFontStyle.Italic : DWriteFontStyle.Normal;
            var locale = string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.Name) ? "en-US" : CultureInfo.CurrentCulture.Name;
            return new FontSpec((float)descriptor.Size, family, weight, style, locale);
        }
    }

    private static class ComInterop
    {
        public static uint Release(nint pointer)
        {
            if (pointer == 0)
            {
                return 0;
            }

            unsafe
            {
                var vtable = *(nint**)pointer;
                var release = (delegate* unmanaged[Stdcall]<nint, uint>)vtable[2];
                return release(pointer);
            }
        }
    }

    private static class DirectWriteInterop
    {
        public static readonly Guid IID_IDWriteFactory = new("B859EE5A-D838-4B5B-A2E8-1ADC7D93DB48");

        [DllImport("dwrite.dll")]
        public static extern int DWriteCreateFactory(DWriteFactoryType factoryType, in Guid iid, out nint factory);

        public static unsafe nint CreateTextFormat(nint factoryHandle, FontSpec fontSpec)
        {
            if (factoryHandle == 0)
            {
                return 0;
            }

            nint textFormat = 0;
            fixed (char* family = fontSpec.Family)
            fixed (char* locale = fontSpec.Locale)
            {
                var factory = (IDWriteFactory*)factoryHandle;
                var create = (delegate* unmanaged[Stdcall]<IDWriteFactory*, char*, nint, DWriteFontWeight, DWriteFontStyle, DWriteFontStretch, float, char*, nint*, int>)factory->LpVtbl[15];
                int hr = create(factory, family, 0, fontSpec.Weight, fontSpec.Style, DWriteFontStretch.Normal, fontSpec.Size, locale, &textFormat);
                if (hr < 0 || textFormat == 0)
                {
                    return 0;
                }
            }

            return textFormat;
        }

        public static unsafe nint CreateTextLayout(nint factoryHandle, string text, nint textFormat)
        {
            if (factoryHandle == 0 || textFormat == 0 || string.IsNullOrEmpty(text))
            {
                return 0;
            }

            nint textLayout = 0;
            fixed (char* chars = text)
            {
                var factory = (IDWriteFactory*)factoryHandle;
                var create = (delegate* unmanaged[Stdcall]<IDWriteFactory*, char*, uint, nint, float, float, nint*, int>)factory->LpVtbl[18];
                int hr = create(factory, chars, (uint)text.Length, textFormat, float.MaxValue, float.MaxValue, &textLayout);
                if (hr < 0 || textLayout == 0)
                {
                    return 0;
                }
            }

            return textLayout;
        }

        public static unsafe int GetMetrics(nint textLayout, out DWriteTextMetrics metrics)
        {
            metrics = default;
            if (textLayout == 0)
            {
                return -1;
            }

            var vtable = *(nint**)textLayout;
            var getMetrics = (delegate* unmanaged[Stdcall]<nint, DWriteTextMetrics*, int>)vtable[60];
            fixed (DWriteTextMetrics* value = &metrics)
            {
                return getMetrics(textLayout, value);
            }
        }
    }

    private enum DWriteFactoryType : uint
    {
        Shared = 0,
        Isolated = 1,
    }

    private enum DWriteFontWeight : uint
    {
        Normal = 400,
        Medium = 500,
        Bold = 700,
    }

    private enum DWriteFontStyle : uint
    {
        Normal = 0,
        Oblique = 1,
        Italic = 2,
    }

    private enum DWriteFontStretch : uint
    {
        Normal = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct IDWriteFactory
    {
        public void** LpVtbl;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct DWriteTextMetrics
    {
        public DWriteTextMetrics(
            float left,
            float top,
            float width,
            float widthIncludingTrailingWhitespace,
            float height,
            float layoutWidth,
            float layoutHeight,
            uint maxBidiReorderingDepth,
            uint lineCount)
        {
            Left = left;
            Top = top;
            Width = width;
            WidthIncludingTrailingWhitespace = widthIncludingTrailingWhitespace;
            Height = height;
            LayoutWidth = layoutWidth;
            LayoutHeight = layoutHeight;
            MaxBidiReorderingDepth = maxBidiReorderingDepth;
            LineCount = lineCount;
        }

        public float Left { get; }

        public float Top { get; }

        public float Width { get; }

        public float WidthIncludingTrailingWhitespace { get; }

        public float Height { get; }

        public float LayoutWidth { get; }

        public float LayoutHeight { get; }

        public uint MaxBidiReorderingDepth { get; }

        public uint LineCount { get; }
    }
}
