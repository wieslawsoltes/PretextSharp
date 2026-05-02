namespace Pretext;

public static partial class PretextLayout
{
    private static readonly Dictionary<string, ShaperState> ShaperStates = new(StringComparer.Ordinal);

    public static PretextShapedRun ShapeText(string text, string font, PretextShapeOptions? options = null)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        var state = GetShaperState(font);
        return state.ShapeText(text ?? string.Empty, options);
    }

    public static bool TryShapeText(string text, string font, out PretextShapedRun? shapedRun, PretextShapeOptions? options = null)
    {
        try
        {
            shapedRun = ShapeText(text, font, options);
            return true;
        }
        catch (InvalidOperationException)
        {
            shapedRun = null;
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            shapedRun = null;
            return false;
        }
        catch (DllNotFoundException)
        {
            shapedRun = null;
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            shapedRun = null;
            return false;
        }
    }

    private static ShaperState GetShaperState(string font)
    {
        lock (FontStateGate)
        {
            if (ShaperStates.TryGetValue(font, out var cached))
            {
                return cached;
            }

            var state = ShaperState.Create(font);
            ShaperStates[font] = state;
            return state;
        }
    }

    private sealed class ShaperState : IDisposable
    {
        private ShaperState(string font, IPretextTextShaper textShaper)
        {
            Font = font;
            TextShaper = textShaper;
        }

        public string Font { get; }

        public IPretextTextShaper TextShaper { get; }

        public static ShaperState Create(string font)
        {
            return new ShaperState(font, PretextLayout.GetTextShaperFactory().CreateShaper(font));
        }

        public PretextShapedRun ShapeText(string text, PretextShapeOptions? options)
        {
            return TextShaper.ShapeText(text, options);
        }

        public void Dispose()
        {
            TextShaper.Dispose();
        }
    }
}
