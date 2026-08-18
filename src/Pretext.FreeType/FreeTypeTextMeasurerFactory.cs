using System.Runtime.InteropServices;

namespace Pretext.FreeType;

public sealed class FreeTypeTextMeasurerFactory : IPretextTextMeasurerFactory, IPretextTextShaperFactory
{
    public string Name => "FreeType";

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public int Priority => 100;

    public IPretextTextMeasurer Create(string font)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("FreeType is only available on Linux.");
        }

        return new FreeTypeTextMeasurer(FontSpec.FromDescriptor(PretextFontParser.Parse(font)));
    }

    public IPretextTextShaper CreateShaper(string font)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("FreeType is only available on Linux.");
        }

        return new FreeTypeTextMeasurer(FontSpec.FromDescriptor(PretextFontParser.Parse(font)));
    }

    private sealed class FreeTypeTextMeasurer : IPretextTextMeasurer, IPretextTextShaper
    {
        private readonly FontSpec _fontSpec;
        private readonly FreeTypeFace _primaryFace;
        private readonly Dictionary<uint, FreeTypeFace?> _fallbackFaces = new();
        private readonly Dictionary<string, FreeTypeFace> _openedFaces = new(StringComparer.Ordinal);

        public FreeTypeTextMeasurer(FontSpec fontSpec)
        {
            _fontSpec = fontSpec;

            var fontPath = LinuxFontResolver.ResolvePrimaryFontPath(fontSpec.Family, fontSpec.Weight, fontSpec.Italic);
            if (string.IsNullOrWhiteSpace(fontPath))
            {
                throw new InvalidOperationException($"Unable to resolve Linux font '{fontSpec.Family}'.");
            }

            _primaryFace = OpenFace(fontPath!);
        }

        public double MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            if (HarfBuzzRuntime.TryMeasureText(_primaryFace, text, out var shapedWidth))
            {
                return shapedWidth;
            }

            return MeasureFallback(text);
        }

        public PretextShapedRun ShapeText(string text, PretextShapeOptions? options = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new PretextShapedRun(
                    PretextGlyphRunKind.Shaped,
                    Array.Empty<PretextShapedGlyph>(),
                    new[] { new PretextShapedFontRun(0, _primaryFace.Path, 0, 0) },
                    0,
                    0);
            }

            if (HarfBuzzRuntime.TryShapeText(_primaryFace, text, options, out var shapedRun) &&
                shapedRun is not null &&
                !shapedRun.HasMissingGlyphs)
            {
                return shapedRun;
            }

            return ShapeFallback(text);
        }

        public void Dispose()
        {
            foreach (var face in _openedFaces.Values)
            {
                face.Dispose();
            }

            _openedFaces.Clear();
            _fallbackFaces.Clear();
        }

        private double MeasureFallback(string text)
        {
            double width = 0;
            FreeTypeFace? previousFace = null;
            uint previousGlyph = 0;

            foreach (var codePoint in EnumerateCodePoints(text))
            {
                var face = ResolveFace(codePoint) ?? _primaryFace;
                var glyph = face.GetGlyphIndex(codePoint);
                if (glyph == 0)
                {
                    previousFace = null;
                    previousGlyph = 0;
                    continue;
                }

                if (previousFace == face && previousGlyph != 0)
                {
                    width += face.GetKerning(previousGlyph, glyph);
                }

                width += face.GetAdvance(glyph);
                previousFace = face;
                previousGlyph = glyph;
            }

            return width;
        }

        private FreeTypeFace? ResolveFace(uint codePoint)
        {
            if (_primaryFace.GetGlyphIndex(codePoint) != 0)
            {
                return _primaryFace;
            }

            if (_fallbackFaces.TryGetValue(codePoint, out var cached))
            {
                return cached;
            }

            var fallbackPath = LinuxFontResolver.ResolveFallbackFontPath(codePoint, _fontSpec.Weight, _fontSpec.Italic);
            if (string.IsNullOrWhiteSpace(fallbackPath))
            {
                _fallbackFaces[codePoint] = null;
                return null;
            }

            var face = OpenFace(fallbackPath!);
            if (face.GetGlyphIndex(codePoint) == 0)
            {
                _fallbackFaces[codePoint] = null;
                return null;
            }

            _fallbackFaces[codePoint] = face;
            return face;
        }

        private PretextShapedRun ShapeFallback(string text)
        {
            var glyphs = new List<PretextShapedGlyph>();
            var fontRuns = new List<PretextShapedFontRun>();
            double penX = 0;
            FreeTypeFace? previousFace = null;
            uint previousGlyph = 0;
            FreeTypeFace? activeRunFace = null;
            var activeRunFirstGlyphIndex = 0;
            var activeRunGlyphCount = 0;
            var activeRunIndex = -1;

            void FlushActiveRun()
            {
                if (activeRunFace is null || activeRunGlyphCount == 0)
                {
                    return;
                }

                fontRuns.Add(new PretextShapedFontRun(
                    activeRunIndex,
                    activeRunFace.Path,
                    activeRunFirstGlyphIndex,
                    activeRunGlyphCount));
            }

            foreach (var codePoint in EnumerateCodePointsWithClusters(text))
            {
                var face = ResolveFace(codePoint.Value) ?? _primaryFace;
                var glyph = face.GetGlyphIndex(codePoint.Value);

                if (!ReferenceEquals(face, activeRunFace))
                {
                    FlushActiveRun();
                    activeRunFace = face;
                    activeRunFirstGlyphIndex = glyphs.Count;
                    activeRunGlyphCount = 0;
                    activeRunIndex = fontRuns.Count;
                }

                if (previousFace == face && previousGlyph != 0 && glyph != 0)
                {
                    penX += face.GetKerning(previousGlyph, glyph);
                }

                var advance = glyph == 0 ? 0 : face.GetAdvance(glyph);
                glyphs.Add(new PretextShapedGlyph(
                    glyph,
                    codePoint.Cluster,
                    penX,
                    0,
                    advance,
                    0,
                    0,
                    0,
                    activeRunIndex));
                activeRunGlyphCount++;
                penX += advance;
                previousFace = face;
                previousGlyph = glyph;
            }

            FlushActiveRun();
            return new PretextShapedRun(PretextGlyphRunKind.Mapped, glyphs, fontRuns, penX, 0);
        }

        private FreeTypeFace OpenFace(string path)
        {
            if (_openedFaces.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var face = FreeTypeFace.Create(path, _fontSpec.PixelSize);
            _openedFaces[path] = face;
            return face;
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

        public uint PixelSize => (uint)Math.Max(1, Math.Round(Size));

        public static FontSpec FromDescriptor(PretextFontDescriptor descriptor)
        {
            var family = PretextFontParser.MapGenericFamily(
                descriptor.PrimaryFamily,
                sansSerifFallback: "DejaVu Sans",
                serifFallback: "DejaVu Serif",
                monospaceFallback: "DejaVu Sans Mono");
            return new FontSpec(Math.Max(1, descriptor.Size), family, descriptor.Weight, descriptor.Italic);
        }
    }

    private sealed class FreeTypeFace : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<uint, uint> _glyphIndexCache = new();
        private readonly Dictionary<uint, double> _advanceCache = new();
        private IntPtr _harfBuzzFont;

        private FreeTypeFace(string path, IntPtr face)
        {
            Path = path;
            Face = face;
        }

        public string Path { get; }

        public IntPtr Face { get; private set; }

        public static FreeTypeFace Create(string path, uint pixelSize)
        {
            var library = FreeTypeLibrary.Instance.Handle;
            var error = FreeTypeNative.FT_New_Face(library, path, 0, out var face);
            if (error != 0 || face == IntPtr.Zero)
            {
                throw new InvalidOperationException($"FT_New_Face failed with code {error} for '{path}'.");
            }

            error = FreeTypeNative.FT_Set_Pixel_Sizes(face, 0, pixelSize);
            if (error != 0)
            {
                FreeTypeNative.FT_Done_Face(face);
                throw new InvalidOperationException($"FT_Set_Pixel_Sizes failed with code {error} for '{path}'.");
            }

            return new FreeTypeFace(path, face);
        }

        public uint GetGlyphIndex(uint codePoint)
        {
            lock (_gate)
            {
                if (_glyphIndexCache.TryGetValue(codePoint, out var cached))
                {
                    return cached;
                }

                var glyphIndex = FreeTypeNative.FT_Get_Char_Index(Face, codePoint);
                _glyphIndexCache[codePoint] = glyphIndex;
                return glyphIndex;
            }
        }

        public double GetAdvance(uint glyphIndex)
        {
            lock (_gate)
            {
                if (_advanceCache.TryGetValue(glyphIndex, out var cached))
                {
                    return cached;
                }

                var error = FreeTypeNative.FT_Get_Advance(Face, glyphIndex, FreeTypeNative.FT_LOAD_DEFAULT, out var advanceFixed);
                if (error != 0)
                {
                    return 0;
                }

                var advance = advanceFixed.ToInt64() / 65536.0;
                _advanceCache[glyphIndex] = advance;
                return advance;
            }
        }

        public double GetKerning(uint leftGlyph, uint rightGlyph)
        {
            if (leftGlyph == 0 || rightGlyph == 0)
            {
                return 0;
            }

            lock (_gate)
            {
                var error = FreeTypeNative.FT_Get_Kerning(Face, leftGlyph, rightGlyph, FreeTypeNative.FT_KERNING_DEFAULT, out var kerning);
                return error == 0 ? kerning.X.ToInt64() / 64.0 : 0;
            }
        }

        public bool TryGetOrCreateHarfBuzzFont(out IntPtr font)
        {
            lock (_gate)
            {
                if (_harfBuzzFont != IntPtr.Zero)
                {
                    font = _harfBuzzFont;
                    return true;
                }

                if (!HarfBuzzRuntime.IsAvailable)
                {
                    font = IntPtr.Zero;
                    return false;
                }

                try
                {
                    _harfBuzzFont = HarfBuzzNative.hb_ft_font_create(Face, IntPtr.Zero);
                    font = _harfBuzzFont;
                    return font != IntPtr.Zero;
                }
                catch (DllNotFoundException)
                {
                    font = IntPtr.Zero;
                    return false;
                }
                catch (EntryPointNotFoundException)
                {
                    font = IntPtr.Zero;
                    return false;
                }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_harfBuzzFont != IntPtr.Zero)
                {
                    try
                    {
                        HarfBuzzNative.hb_font_destroy(_harfBuzzFont);
                    }
                    catch
                    {
                        // Ignore shutdown failures from optional HarfBuzz support.
                    }

                    _harfBuzzFont = IntPtr.Zero;
                }

                if (Face != IntPtr.Zero)
                {
                    FreeTypeNative.FT_Done_Face(Face);
                    Face = IntPtr.Zero;
                }
            }
        }
    }

    private sealed class FreeTypeLibrary : IDisposable
    {
        private static readonly Lazy<FreeTypeLibrary> s_instance = new(static () => new FreeTypeLibrary());

        private FreeTypeLibrary()
        {
            var error = FreeTypeNative.FT_Init_FreeType(out var handle);
            if (error != 0 || handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"FT_Init_FreeType failed with code {error}.");
            }

            Handle = handle;
        }

        public static FreeTypeLibrary Instance => s_instance.Value;

        public IntPtr Handle { get; }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                FreeTypeNative.FT_Done_FreeType(Handle);
            }
        }
    }

    private static unsafe class HarfBuzzRuntime
    {
        public static bool IsAvailable
        {
            get
            {
                if (s_probeState == 0)
                {
                    ProbeAvailability();
                }

                return s_probeState > 0;
            }
        }

        private static int s_probeState;

        public static bool TryMeasureText(FreeTypeFace face, string text, out double width)
        {
            width = 0;
            if (!IsAvailable || string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (!face.TryGetOrCreateHarfBuzzFont(out var hbFont) || hbFont == IntPtr.Zero)
            {
                return false;
            }

            IntPtr buffer = IntPtr.Zero;
            try
            {
                buffer = HarfBuzzNative.hb_buffer_create();
                if (buffer == IntPtr.Zero)
                {
                    return false;
                }

                fixed (char* chars = text)
                {
                    HarfBuzzNative.hb_buffer_reset(buffer);
                    HarfBuzzNative.hb_buffer_add_utf16(buffer, (ushort*)chars, text.Length, 0, text.Length);
                    HarfBuzzNative.hb_buffer_guess_segment_properties(buffer);
                    HarfBuzzNative.hb_shape(hbFont, buffer, IntPtr.Zero, 0);
                }

                var glyphInfos = HarfBuzzNative.hb_buffer_get_glyph_infos(buffer, out var glyphCount);
                var glyphPositions = HarfBuzzNative.hb_buffer_get_glyph_positions(buffer, out var positionCount);
                if (glyphInfos == null || glyphPositions == null || glyphCount == 0 || glyphCount != positionCount)
                {
                    return false;
                }

                for (uint index = 0; index < glyphCount; index++)
                {
                    if (glyphInfos[index].Codepoint == 0)
                    {
                        width = 0;
                        return false;
                    }

                    width += glyphPositions[index].XAdvance / 64.0;
                }

                return true;
            }
            catch (DllNotFoundException)
            {
                s_probeState = -1;
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                s_probeState = -1;
                return false;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    HarfBuzzNative.hb_buffer_destroy(buffer);
                }
            }
        }

        public static bool TryShapeText(
            FreeTypeFace face,
            string text,
            PretextShapeOptions? options,
            out PretextShapedRun? shapedRun)
        {
            shapedRun = null;
            if (!IsAvailable || string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (!face.TryGetOrCreateHarfBuzzFont(out var hbFont) || hbFont == IntPtr.Zero)
            {
                return false;
            }

            IntPtr buffer = IntPtr.Zero;
            try
            {
                buffer = HarfBuzzNative.hb_buffer_create();
                if (buffer == IntPtr.Zero)
                {
                    return false;
                }

                fixed (char* chars = text)
                {
                    HarfBuzzNative.hb_buffer_reset(buffer);
                    HarfBuzzNative.hb_buffer_add_utf16(buffer, (ushort*)chars, text.Length, 0, text.Length);
                    ApplyDirection(buffer, options);
                    HarfBuzzNative.hb_buffer_guess_segment_properties(buffer);
                    HarfBuzzNative.hb_shape(hbFont, buffer, IntPtr.Zero, 0);
                }

                var glyphInfos = HarfBuzzNative.hb_buffer_get_glyph_infos(buffer, out var glyphCount);
                var glyphPositions = HarfBuzzNative.hb_buffer_get_glyph_positions(buffer, out var positionCount);
                if (glyphInfos == null || glyphPositions == null || glyphCount == 0 || glyphCount != positionCount)
                {
                    return false;
                }

                var glyphs = new PretextShapedGlyph[glyphCount];
                double penX = 0;
                double penY = 0;
                for (uint index = 0; index < glyphCount; index++)
                {
                    var position = glyphPositions[index];
                    var xAdvance = position.XAdvance / 64.0;
                    var yAdvance = position.YAdvance / 64.0;
                    var xOffset = position.XOffset / 64.0;
                    var yOffset = position.YOffset / 64.0;
                    glyphs[index] = new PretextShapedGlyph(
                        glyphInfos[index].Codepoint,
                        checked((int)glyphInfos[index].Cluster),
                        penX + xOffset,
                        penY + yOffset,
                        xAdvance,
                        yAdvance,
                        xOffset,
                        yOffset,
                        0);
                    penX += xAdvance;
                    penY += yAdvance;
                }

                shapedRun = new PretextShapedRun(
                    PretextGlyphRunKind.Shaped,
                    glyphs,
                    new[] { new PretextShapedFontRun(0, face.Path, 0, glyphs.Length) },
                    penX,
                    penY);
                return true;
            }
            catch (DllNotFoundException)
            {
                s_probeState = -1;
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                s_probeState = -1;
                return false;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    HarfBuzzNative.hb_buffer_destroy(buffer);
                }
            }
        }

        private static void ApplyDirection(IntPtr buffer, PretextShapeOptions? options)
        {
            if (options is null || options.Direction == PretextTextDirection.Auto)
            {
                return;
            }

            HarfBuzzNative.hb_buffer_set_direction(
                buffer,
                options.Direction == PretextTextDirection.RightToLeft
                    ? hb_direction_t.HB_DIRECTION_RTL
                    : hb_direction_t.HB_DIRECTION_LTR);
        }

        private static void ProbeAvailability()
        {
            try
            {
                var buffer = HarfBuzzNative.hb_buffer_create();
                if (buffer != IntPtr.Zero)
                {
                    HarfBuzzNative.hb_buffer_destroy(buffer);
                }

                s_probeState = 1;
            }
            catch (DllNotFoundException)
            {
                s_probeState = -1;
            }
            catch (EntryPointNotFoundException)
            {
                s_probeState = -1;
            }
        }
    }

    private static IEnumerable<uint> EnumerateCodePoints(string text)
    {
        foreach (var codePoint in EnumerateCodePointsWithClusters(text))
        {
            yield return codePoint.Value;
        }
    }

    private static IEnumerable<CodePointWithCluster> EnumerateCodePointsWithClusters(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (char.IsHighSurrogate(current) &&
                index + 1 < text.Length &&
                char.IsLowSurrogate(text[index + 1]))
            {
                yield return new CodePointWithCluster((uint)char.ConvertToUtf32(current, text[index + 1]), index);
                index++;
                continue;
            }

            yield return new CodePointWithCluster(current, index);
        }
    }

    private readonly struct CodePointWithCluster
    {
        public CodePointWithCluster(uint value, int cluster)
        {
            Value = value;
            Cluster = cluster;
        }

        public uint Value { get; }

        public int Cluster { get; }
    }
}
