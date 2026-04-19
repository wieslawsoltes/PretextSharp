using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PretextSamples.MacOS;

internal sealed class VariableAsciiPageView : SamplePageView
{
    private const int Columns = 50;
    private const int Rows = 28;
    private const int AsciiFontSize = 14;
    private const int LineHeight = 16;
    private const int TargetRowWidth = 440;
    private const int PanelWidth = 468;
    private const string PropFamilyCss = "Georgia, Palatino, \"Times New Roman\", serif";
    private const string PropFamilyDisplay = "Georgia";
    private const int FieldOversample = 2;
    private const int FieldCols = Columns * FieldOversample;
    private const int FieldRows = Rows * FieldOversample;
    private const int CanvasWidth = 220;
    private const int CanvasHeight = 224;
    private const double FieldScaleX = (double)FieldCols / CanvasWidth;
    private const double FieldScaleY = (double)FieldRows / CanvasHeight;
    private const double TargetCellWidth = (double)TargetRowWidth / Columns;
    private const int ParticleCount = 120;
    private const int SpriteRadius = 14;
    private const int AttractorRadius = 12;
    private const int LargeAttractorRadius = 30;
    private const double AttractorForceNear = 0.22;
    private const double AttractorForceFar = 0.05;
    private const double FieldDecay = 0.82;
    private const string Charset = " .,:;!+-=*#@%&abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string MonoRamp = " .`-_:,;^=+/|)\\\\!?0oOQ#%@";
    private static readonly int[] Weights = [300, 500, 800];
    private static readonly bool[] ItalicOptions = [false, true];
    private static readonly NSColor[] PropAlphaBrushes = CreatePropAlphaBrushes();

    private readonly NSTimer _timer;
    private readonly Stopwatch _clock = new();
    private readonly List<Particle> _particles = [];
    private readonly float[] _brightnessField = new float[FieldCols * FieldRows];
    private readonly BrightnessEntry[] _brightnessLookup = new BrightnessEntry[256];
    private readonly List<PropRowLayout> _propRows = [];
    private readonly string[] _monoRows = new string[Rows];
    private readonly FieldStamp _particleFieldStamp;
    private readonly FieldStamp _largeAttractorFieldStamp;
    private readonly FieldStamp _smallAttractorFieldStamp;

    private CGRect _eyebrowRect;
    private CGRect _titleRect;
    private CGRect _descriptionRect;
    private CGRect _sourcePanelRect;
    private CGRect _propPanelRect;
    private CGRect _monoPanelRect;
    private CGRect _sourceContentRect;
    private CGRect _propContentRect;
    private CGRect _monoContentRect;
    private double _attractor1X;
    private double _attractor1Y;
    private double _attractor2X;
    private double _attractor2Y;

    public VariableAsciiPageView()
    {
        _particleFieldStamp = CreateFieldStamp(SpriteRadius);
        _largeAttractorFieldStamp = CreateFieldStamp(LargeAttractorRadius);
        _smallAttractorFieldStamp = CreateFieldStamp(AttractorRadius);

        SeedParticles();
        BuildBrightnessLookup();
        AdvanceParticles(0);
        RenderFrame();

        _clock.Start();
        _timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(16), _ =>
        {
            AdvanceParticles(_clock.Elapsed.TotalMilliseconds);
            RenderFrame();
            NeedsDisplay = true;
        });
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var headerMaxWidth = MacTheme.N(720);
        var panelGap = MacTheme.N(28);
        var panelGridWidth = MacTheme.N(PanelWidth * 3) + panelGap * 2;
        var contentWidth = MacTheme.Max(availableSize.Width, panelGridWidth + MacTheme.PageMargin * 2);
        var centerX = contentWidth / 2;

        var eyebrowAttributes = MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.Color(180, 189, 208, 180));
        var titleAttributes = MacTheme.CreateAttributes(MacTheme.Sans(24, bold: true), MacTheme.Color(230, 255, 255, 255));
        var descriptionAttributes = MacTheme.CreateAttributes(MacTheme.Sans(13), MacTheme.Color(102, 255, 255, 255), 20, NSTextAlignment.Center);

        var y = MacTheme.N(32);
        var eyebrowSize = MacTheme.MeasureString("DEMO", eyebrowAttributes, headerMaxWidth);
        _eyebrowRect = new CGRect(centerX - headerMaxWidth / 2, y, headerMaxWidth, eyebrowSize.Height);
        y += eyebrowSize.Height + 8;

        var titleSize = MacTheme.MeasureString("Variable typographic ASCII", titleAttributes, headerMaxWidth);
        _titleRect = new CGRect(centerX - headerMaxWidth / 2, y, headerMaxWidth, titleSize.Height);
        y += titleSize.Height + 12;

        var descriptionWidth = MacTheme.N(720);
        var descriptionText = "Proportional font (Georgia) rendered at 3 font-weights × normal/italic. A shared particle-and-attractor brightness field drives all three panels, then characters are chosen by brightness and width to preserve the shape in proportional type.";
        var descriptionSize = MacTheme.MeasureString(descriptionText, descriptionAttributes, descriptionWidth);
        _descriptionRect = new CGRect(centerX - descriptionWidth / 2, y, descriptionWidth, descriptionSize.Height);
        y += descriptionSize.Height + 28;

        var gridLeft = centerX - panelGridWidth / 2;
        var panelTop = y;
        _sourcePanelRect = new CGRect(gridLeft, panelTop, PanelWidth, Rows * LineHeight + 54);
        _propPanelRect = new CGRect(_sourcePanelRect.Right + panelGap, panelTop, PanelWidth, Rows * LineHeight + 54);
        _monoPanelRect = new CGRect(_propPanelRect.Right + panelGap, panelTop, PanelWidth, Rows * LineHeight + 54);

        _sourceContentRect = new CGRect(_sourcePanelRect.X + 14, _sourcePanelRect.Y + 34, TargetRowWidth, Rows * LineHeight);
        _propContentRect = new CGRect(_propPanelRect.X + 14, _propPanelRect.Y + 34, TargetRowWidth, Rows * LineHeight);
        _monoContentRect = new CGRect(_monoPanelRect.X + 14, _monoPanelRect.Y + 34, TargetRowWidth, Rows * LineHeight);

        return new CGSize(contentWidth, MacTheme.Max(availableSize.Height, _monoPanelRect.Bottom + MacTheme.PageMargin));
    }

    protected override void LayoutPage(CGRect bounds)
    {
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);

        using var background = new NSGradient(
            new[]
            {
                MacTheme.Color(10, 10, 18),
                MacTheme.Color(6, 6, 10),
            },
            new nfloat[] { 0, 1 });
        background.DrawInRect(Bounds, 90);

        DrawHeader();
        DrawPanel(_sourcePanelRect, "SOURCE FIELD");
        DrawPanel(_propPanelRect, "PROPORTIONAL × 3 WEIGHTS × ITALIC");
        DrawPanel(_monoPanelRect, "MONOSPACE × SINGLE WEIGHT");
        DrawSourceField(_sourceContentRect);
        DrawPropRows(_propContentRect);
        DrawMonoRows(_monoContentRect);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Invalidate();
        }

        base.Dispose(disposing);
    }

    private void DrawHeader()
    {
        MacTheme.DrawWrappedString("DEMO", _eyebrowRect, MacTheme.CreateAttributes(MacTheme.Mono(12), MacTheme.Color(180, 189, 208, 180), alignment: NSTextAlignment.Center));
        MacTheme.DrawWrappedString("Variable typographic ASCII", _titleRect, MacTheme.CreateAttributes(MacTheme.Sans(24, bold: true), MacTheme.Color(230, 255, 255, 255), alignment: NSTextAlignment.Center));
        MacTheme.DrawWrappedString(
            "Proportional font (Georgia) rendered at 3 font-weights × normal/italic. A shared particle-and-attractor brightness field drives all three panels, then characters are chosen by brightness and width to preserve the shape in proportional type.",
            _descriptionRect,
            MacTheme.CreateAttributes(MacTheme.Sans(13), MacTheme.Color(102, 255, 255, 255), 20, NSTextAlignment.Center));
    }

    private static void DrawPanel(CGRect rect, string title)
    {
        MacTheme.DrawWrappedString(title, new CGRect(rect.X, rect.Y, rect.Width, 12), MacTheme.CreateAttributes(MacTheme.Sans(10), MacTheme.Color(76, 255, 255, 255), alignment: NSTextAlignment.Center));
        MacTheme.FillRoundedRect(new CGRect(rect.X, rect.Y + 20, rect.Width, rect.Height - 20), 10, MacTheme.Color(0, 0, 0, 102), MacTheme.Color(255, 255, 255, 24));
    }

    private void DrawSourceField(CGRect rect)
    {
        MacTheme.FillRect(rect, MacTheme.Color(0, 0, 0));
        var cellWidth = rect.Width / FieldCols;
        var cellHeight = rect.Height / FieldRows;
        for (var row = 0; row < FieldRows; row++)
        {
            var rowOffset = row * FieldCols;
            for (var column = 0; column < FieldCols; column++)
            {
                var brightness = Math.Clamp(_brightnessField[rowOffset + column], 0, 1);
                if (brightness <= 0.01f)
                {
                    continue;
                }

                var alpha = (byte)Math.Round(brightness * 255);
                MacTheme.FillRect(
                    new CGRect(rect.X + column * cellWidth, rect.Y + row * cellHeight, cellWidth + 0.5, cellHeight + 0.5),
                    MacTheme.Color(alpha, alpha, alpha));
            }
        }
    }

    private void DrawPropRows(CGRect rect)
    {
        for (var row = 0; row < _propRows.Count; row++)
        {
            var rowLayout = _propRows[row];
            var x = rect.X + (rect.Width - rowLayout.Width) / 2;
            var y = rect.Y + row * LineHeight;
            foreach (var run in rowLayout.Runs)
            {
                if (run.IsSpace)
                {
                    x += run.Width;
                    continue;
                }

                var attributes = MacTheme.CreateAttributes(run.Font, run.Color);
                MacTheme.DrawWrappedString(run.Text, new CGRect(x, y, run.Width + 2, LineHeight), attributes);
                x += run.Width;
            }
        }
    }

    private void DrawMonoRows(CGRect rect)
    {
        var monoAttributes = MacTheme.CreateAttributes(MacTheme.Mono(14), MacTheme.Color(130, 155, 210, 179), MacTheme.N(LineHeight), NSTextAlignment.Center);
        for (var row = 0; row < Rows; row++)
        {
            MacTheme.DrawWrappedString(_monoRows[row], new CGRect(rect.X, rect.Y + row * LineHeight, rect.Width, LineHeight), monoAttributes);
        }
    }

    private void SeedParticles()
    {
        for (var index = 0; index < ParticleCount; index++)
        {
            var angle = Random.Shared.NextDouble() * Math.PI * 2;
            var radius = Random.Shared.NextDouble() * 40 + 20;
            _particles.Add(new Particle(
                CanvasWidth / 2 + Math.Cos(angle) * radius,
                CanvasHeight / 2 + Math.Sin(angle) * radius,
                (Random.Shared.NextDouble() - 0.5) * 0.8,
                (Random.Shared.NextDouble() - 0.5) * 0.8));
        }
    }

    private void BuildBrightnessLookup()
    {
        var palette = new List<PaletteEntry>();
        foreach (var italic in ItalicOptions)
        {
            foreach (var weight in Weights)
            {
                var font = $"{(italic ? "italic " : string.Empty)}{weight} {AsciiFontSize}px {PropFamilyCss}";
                foreach (var ch in Charset)
                {
                    if (ch == ' ')
                    {
                        continue;
                    }

                    var width = MeasureWidth(ch, font);
                    if (width <= 0)
                    {
                        continue;
                    }

                    var brightness = EstimateBrightness(ch, weight, italic);
                    palette.Add(new PaletteEntry(ch, weight, italic, width, brightness));
                }
            }
        }

        var maxBrightness = palette.Count == 0 ? 1d : palette.Max(entry => entry.Brightness);
        if (maxBrightness > 0)
        {
            for (var index = 0; index < palette.Count; index++)
            {
                palette[index] = palette[index] with { Brightness = palette[index].Brightness / maxBrightness };
            }
        }

        palette.Sort((a, b) => a.Brightness.CompareTo(b.Brightness));
        for (var brightnessByte = 0; brightnessByte < 256; brightnessByte++)
        {
            var brightness = brightnessByte / 255d;
            var monoChar = MonoRamp[Math.Min(MonoRamp.Length - 1, (int)(brightness * MonoRamp.Length))];

            if (brightness < 0.03)
            {
                _brightnessLookup[brightnessByte] = new BrightnessEntry(monoChar, null);
                continue;
            }

            var best = FindBestPaletteEntry(palette, brightness);
            var alphaLevel = Math.Max(1, Math.Min(10, (int)Math.Round(brightness * 10)));
            _brightnessLookup[brightnessByte] = new BrightnessEntry(monoChar, new GlyphVariant(best.Character, best.Weight, best.Italic, alphaLevel));
        }
    }

    private static PaletteEntry FindBestPaletteEntry(IReadOnlyList<PaletteEntry> palette, double targetBrightness)
    {
        var lo = 0;
        var hi = palette.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) >> 1;
            if (palette[mid].Brightness < targetBrightness)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        var best = palette[lo];
        var bestScore = double.MaxValue;
        var start = Math.Max(0, lo - 15);
        var end = Math.Min(palette.Count, lo + 15);
        for (var index = start; index < end; index++)
        {
            var entry = palette[index];
            var brightnessError = Math.Abs(entry.Brightness - targetBrightness) * 2.5;
            var widthError = Math.Abs(entry.Width - TargetCellWidth) / TargetCellWidth;
            var score = brightnessError + widthError;
            if (score < bestScore)
            {
                best = entry;
                bestScore = score;
            }
        }

        return best;
    }

    private static double MeasureWidth(char ch, string font)
    {
        var prepared = PretextLayout.PrepareWithSegments(ch.ToString(), font);
        return prepared.Widths.Count > 0 ? prepared.Widths[0] : 0;
    }

    private static double EstimateBrightness(char ch, int weight, bool italic)
    {
        const int size = 28;
        using var image = new NSImage(new CGSize(size, size));
        image.LockFocus();
        try
        {
            var context = NSGraphicsContext.CurrentContext;
            context?.CGContext.ClearRect(new CGRect(0, 0, size, size));
            var attributes = MacTheme.CreateAttributes(CreatePropFont(weight, italic, size), MacTheme.WhiteBrush);
            new NSString(ch.ToString()).DrawInRect(new CGRect(1, 0, size - 2, size), attributes);
            context?.FlushGraphics();
        }
        finally
        {
            image.UnlockFocus();
        }

        using var bitmap = new NSBitmapImageRep(image.AsTiff() ?? throw new InvalidOperationException("Failed to capture glyph bitmap."));
        var bytesPerRow = (int)bitmap.BytesPerRow;
        var bytes = new byte[bytesPerRow * size];
        Marshal.Copy(bitmap.BitmapData, bytes, 0, bytes.Length);

        double sum = 0;
        for (var y = 0; y < size; y++)
        {
            var rowOffset = y * bytesPerRow;
            for (var x = 0; x < size; x++)
            {
                sum += bytes[rowOffset + x * 4 + 3];
            }
        }

        return sum / (255d * size * size);
    }

    private static NSFont CreatePropFont(int weight, bool italic)
        => CreatePropFont(weight, italic, MacTheme.N(AsciiFontSize));

    private static NSFont CreatePropFont(int weight, bool italic, nfloat size)
    {
        var font = MacTheme.Serif(size, italic: italic);
        var manager = NSFontManager.SharedFontManager;
        if (italic)
        {
            font = manager.ConvertFont(font, NSFontTraitMask.Italic) ?? font;
        }

        if (weight >= 500)
        {
            font = manager.ConvertWeight(true, font) ?? font;
        }

        if (weight >= 800)
        {
            font = manager.ConvertWeight(true, font) ?? font;
        }

        if (weight <= 300)
        {
            font = manager.ConvertWeight(false, font) ?? font;
        }

        return font;
    }

    private void AdvanceParticles(double nowMs)
    {
        _attractor1X = Math.Cos(nowMs * 0.0007) * CanvasWidth * 0.25 + CanvasWidth / 2d;
        _attractor1Y = Math.Sin(nowMs * 0.0011) * CanvasHeight * 0.3 + CanvasHeight / 2d;
        _attractor2X = Math.Cos(nowMs * 0.0013 + Math.PI) * CanvasWidth * 0.2 + CanvasWidth / 2d;
        _attractor2Y = Math.Sin(nowMs * 0.0009 + Math.PI) * CanvasHeight * 0.25 + CanvasHeight / 2d;

        for (var index = 0; index < _particles.Count; index++)
        {
            var particle = _particles[index];
            var d1x = _attractor1X - particle.X;
            var d1y = _attractor1Y - particle.Y;
            var d2x = _attractor2X - particle.X;
            var d2y = _attractor2Y - particle.Y;
            var dist1 = d1x * d1x + d1y * d1y;
            var dist2 = d2x * d2x + d2y * d2y;
            var ax = dist1 < dist2 ? d1x : d2x;
            var ay = dist1 < dist2 ? d1y : d2y;
            var dist = Math.Sqrt(Math.Min(dist1, dist2)) + 1;
            var force = dist1 < dist2 ? AttractorForceNear : AttractorForceFar;

            particle.Vx += ax / dist * force;
            particle.Vy += ay / dist * force;
            particle.Vx += (Random.Shared.NextDouble() - 0.5) * 0.25;
            particle.Vy += (Random.Shared.NextDouble() - 0.5) * 0.25;
            particle.Vx *= 0.97;
            particle.Vy *= 0.97;
            particle.X += particle.Vx;
            particle.Y += particle.Vy;

            if (particle.X < -SpriteRadius) particle.X += CanvasWidth + SpriteRadius * 2;
            if (particle.X > CanvasWidth + SpriteRadius) particle.X -= CanvasWidth + SpriteRadius * 2;
            if (particle.Y < -SpriteRadius) particle.Y += CanvasHeight + SpriteRadius * 2;
            if (particle.Y > CanvasHeight + SpriteRadius) particle.Y -= CanvasHeight + SpriteRadius * 2;
        }
    }

    private void RenderFrame()
    {
        for (var index = 0; index < _brightnessField.Length; index++)
        {
            _brightnessField[index] *= (float)FieldDecay;
        }

        foreach (var particle in _particles)
        {
            SplatFieldStamp(particle.X, particle.Y, _particleFieldStamp);
        }

        SplatFieldStamp(_attractor1X, _attractor1Y, _largeAttractorFieldStamp);
        SplatFieldStamp(_attractor2X, _attractor2Y, _smallAttractorFieldStamp);
        BuildRows();
    }

    private void BuildRows()
    {
        _propRows.Clear();
        var mono = new StringBuilder(Columns);
        var prop = new StringBuilder(Columns);

        for (var row = 0; row < Rows; row++)
        {
            mono.Clear();
            prop.Clear();
            var runs = new List<PropRun>();
            GlyphVariant? currentVariant = null;
            var fieldRowStart = row * FieldOversample * FieldCols;

            for (var col = 0; col < Columns; col++)
            {
                var fieldColStart = col * FieldOversample;
                float brightness = 0;
                for (var sampleY = 0; sampleY < FieldOversample; sampleY++)
                {
                    var sampleRowOffset = fieldRowStart + sampleY * FieldCols + fieldColStart;
                    for (var sampleX = 0; sampleX < FieldOversample; sampleX++)
                    {
                        brightness += _brightnessField[sampleRowOffset + sampleX];
                    }
                }

                var brightnessByte = Math.Min(255, (int)((brightness / (FieldOversample * FieldOversample)) * 255));
                var entry = _brightnessLookup[brightnessByte];
                mono.Append(entry.MonoChar);

                if (entry.PropVariant != currentVariant)
                {
                    FlushPropRun(runs, prop, currentVariant);
                    currentVariant = entry.PropVariant;
                }

                prop.Append(entry.PropVariant?.Character ?? ' ');
            }

            FlushPropRun(runs, prop, currentVariant);
            _monoRows[row] = mono.ToString();
            _propRows.Add(new PropRowLayout(runs, MacTheme.N(runs.Sum(run => (double)run.Width))));
        }
    }

    private static void FlushPropRun(List<PropRun> runs, StringBuilder buffer, GlyphVariant? variant)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        var text = buffer.ToString();
        if (variant is GlyphVariant glyph)
        {
            var font = CreatePropFont(glyph.Weight, glyph.Italic);
            var attributes = MacTheme.CreateAttributes(font, PropAlphaBrushes[glyph.AlphaLevel]);
            var width = MacTheme.MeasureString(text, attributes, 1000).Width;
            runs.Add(new PropRun(text, width, font, PropAlphaBrushes[glyph.AlphaLevel], false));
        }
        else
        {
            runs.Add(new PropRun(text, MacTheme.N(text.Length * TargetCellWidth), CreatePropFont(500, false), NSColor.Clear, true));
        }

        buffer.Clear();
    }

    private static NSColor[] CreatePropAlphaBrushes()
    {
        var brushes = new NSColor[11];
        brushes[0] = NSColor.Clear;
        for (var level = 1; level <= 10; level++)
        {
            brushes[level] = MacTheme.Color(196, 163, 90, (byte)Math.Round(level * 25.5));
        }

        return brushes;
    }

    private static float SpriteAlphaAt(double normalizedDistance)
    {
        if (normalizedDistance >= 1)
        {
            return 0;
        }

        if (normalizedDistance <= 0.35)
        {
            return 0.45f + (float)((0.15 - 0.45) * (normalizedDistance / 0.35));
        }

        return 0.15f * (float)(1 - ((normalizedDistance - 0.35) / 0.65));
    }

    private static FieldStamp CreateFieldStamp(int radiusPx)
    {
        var fieldRadiusX = radiusPx * FieldScaleX;
        var fieldRadiusY = radiusPx * FieldScaleY;
        var radiusX = (int)Math.Ceiling(fieldRadiusX);
        var radiusY = (int)Math.Ceiling(fieldRadiusY);
        var sizeX = radiusX * 2 + 1;
        var sizeY = radiusY * 2 + 1;
        var values = new float[sizeX * sizeY];

        for (var y = -radiusY; y <= radiusY; y++)
        {
            for (var x = -radiusX; x <= radiusX; x++)
            {
                var normalizedDistance = Math.Sqrt(Math.Pow(x / fieldRadiusX, 2) + Math.Pow(y / fieldRadiusY, 2));
                values[(y + radiusY) * sizeX + x + radiusX] = SpriteAlphaAt(normalizedDistance);
            }
        }

        return new FieldStamp(radiusX, radiusY, sizeX, sizeY, values);
    }

    private void SplatFieldStamp(double centerX, double centerY, FieldStamp stamp)
    {
        var gridCenterX = (int)Math.Round(centerX * FieldScaleX);
        var gridCenterY = (int)Math.Round(centerY * FieldScaleY);
        for (var y = -stamp.RadiusY; y <= stamp.RadiusY; y++)
        {
            var gridY = gridCenterY + y;
            if (gridY < 0 || gridY >= FieldRows)
            {
                continue;
            }

            var fieldRowOffset = gridY * FieldCols;
            var stampRowOffset = (y + stamp.RadiusY) * stamp.SizeX;
            for (var x = -stamp.RadiusX; x <= stamp.RadiusX; x++)
            {
                var gridX = gridCenterX + x;
                if (gridX < 0 || gridX >= FieldCols)
                {
                    continue;
                }

                var stampValue = stamp.Values[stampRowOffset + x + stamp.RadiusX];
                if (stampValue == 0)
                {
                    continue;
                }

                var fieldIndex = fieldRowOffset + gridX;
                _brightnessField[fieldIndex] = Math.Min(1, _brightnessField[fieldIndex] + stampValue);
            }
        }
    }

    private readonly record struct PaletteEntry(char Character, int Weight, bool Italic, double Width, double Brightness);

    private readonly record struct GlyphVariant(char Character, int Weight, bool Italic, int AlphaLevel);

    private readonly record struct BrightnessEntry(char MonoChar, GlyphVariant? PropVariant);

    private readonly record struct FieldStamp(int RadiusX, int RadiusY, int SizeX, int SizeY, float[] Values);

    private readonly record struct PropRun(string Text, nfloat Width, NSFont Font, NSColor Color, bool IsSpace);

    private readonly record struct PropRowLayout(IReadOnlyList<PropRun> Runs, nfloat Width);

    private sealed class Particle(double x, double y, double vx, double vy)
    {
        public double X { get; set; } = x;

        public double Y { get; set; } = y;

        public double Vx { get; set; } = vx;

        public double Vy { get; set; } = vy;
    }
}
