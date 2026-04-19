namespace PretextSamples.MacOS;

internal sealed class VariableAsciiPageView : SamplePageView
{
    private const int Columns = 50;
    private const int Rows = 28;
    private const string MonoRamp = " .`-_:,;^=+/|)\\\\!?0oOQ#%@";
    private const string PropRamp = " .,:;!+-=*#@%&abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly List<Particle> _particles = [];
    private readonly NSTimer _timer;
    private CGRect _panelRect;
    private nfloat _headerBottom;

    public VariableAsciiPageView()
    {
        SeedParticles();
        _timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(16), _ =>
        {
            AdvanceParticles();
            NeedsDisplay = true;
        });
    }

    protected override CGSize MeasurePage(CGSize availableSize)
    {
        var contentWidth = MacTheme.Max(MacTheme.N(980), availableSize.Width);
        _headerBottom = MacTheme.MeasureHeaderHeight(contentWidth, "DEMO", "Variable typographic ASCII", "A particle field is rendered natively on macOS twice: once with a monospace ramp and once with proportional glyph choices.");
        _panelRect = new CGRect(MacTheme.PageMargin, _headerBottom + 18, contentWidth - MacTheme.PageMargin * 2, 700);
        return new CGSize(contentWidth, _panelRect.Bottom + MacTheme.PageMargin);
    }

    protected override void LayoutPage(CGRect bounds)
    {
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);
        MacTheme.FillRect(Bounds, MacTheme.PageBrush);
        MacTheme.DrawHeader(Bounds, "DEMO", "Variable typographic ASCII", "A particle field is rendered natively on macOS twice: once with a monospace ramp and once with proportional glyph choices.");

        MacTheme.FillRoundedRect(_panelRect, 24, MacTheme.Color(0x10, 0x10, 0x12), MacTheme.Color(0x20, 0x20, 0x24));
        var field = BuildField();

        var sourceRect = new CGRect(_panelRect.X + 20, _panelRect.Y + 26, 220, 220);
        DrawField(sourceRect, field);
        DrawAsciiPanel(new CGRect(_panelRect.X + 270, _panelRect.Y + 26, _panelRect.Width - 290, 300), "Proportional field", field, PropRamp, MacTheme.Serif(14), MacTheme.Color(0xE8, 0xE1, 0xD4));
        DrawAsciiPanel(new CGRect(_panelRect.X + 270, _panelRect.Y + 350, _panelRect.Width - 290, 300), "Monospace field", field, MonoRamp, MacTheme.Mono(13), MacTheme.Color(0xCF, 0xD7, 0xE1));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Invalidate();
        }

        base.Dispose(disposing);
    }

    private void DrawField(CGRect rect, float[,] field)
    {
        MacTheme.DrawWrappedString("Source field", new CGRect(rect.X, rect.Y - 20, rect.Width, 16), MacTheme.CreateAttributes(MacTheme.Sans(13, bold: true), MacTheme.WhiteBrush));
        var cellWidth = rect.Width / Columns;
        var cellHeight = rect.Height / Rows;
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var brightness = field[row, column];
                var value = (byte)Math.Clamp(brightness * 255, 0, 255);
                MacTheme.FillRect(new CGRect(rect.X + column * cellWidth, rect.Y + row * cellHeight, cellWidth + 0.5, cellHeight + 0.5), MacTheme.Color(value, value, value));
            }
        }
    }

    private void DrawAsciiPanel(CGRect rect, string title, float[,] field, string ramp, NSFont font, NSColor color)
    {
        MacTheme.DrawWrappedString(title, new CGRect(rect.X, rect.Y - 20, rect.Width, 16), MacTheme.CreateAttributes(MacTheme.Sans(13, bold: true), MacTheme.WhiteBrush));
        var attrs = MacTheme.CreateAttributes(font, color, 16);
        var chars = new char[Columns];
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var index = (int)Math.Round(Math.Clamp(field[row, column], 0, 1) * (ramp.Length - 1));
                chars[column] = ramp[index];
            }

            MacTheme.DrawWrappedString(new string(chars), new CGRect(rect.X, rect.Y + row * 16, rect.Width, 16), attrs);
        }
    }

    private float[,] BuildField()
    {
        var field = new float[Rows, Columns];
        foreach (var particle in _particles)
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var dx = column - particle.X;
                    var dy = row - particle.Y;
                    var distanceSq = dx * dx + dy * dy;
                    var contribution = (float)Math.Exp(-distanceSq / (particle.Radius * particle.Radius));
                    field[row, column] = Math.Clamp(field[row, column] + contribution * 0.9f, 0, 1);
                }
            }
        }

        return field;
    }

    private void SeedParticles()
    {
        _particles.Clear();
        for (var index = 0; index < 12; index++)
        {
            _particles.Add(new Particle(index * 3 % Columns, index * 5 % Rows, 0.18 + index * 0.01, 0.14 + index * 0.008, 4 + index % 4));
        }
    }

    private void AdvanceParticles()
    {
        for (var index = 0; index < _particles.Count; index++)
        {
            var particle = _particles[index];
            var x = particle.X + particle.Vx;
            var y = particle.Y + particle.Vy;
            var vx = particle.Vx;
            var vy = particle.Vy;
            if (x < 0 || x > Columns - 1)
            {
                vx = -vx;
                x = Math.Clamp(x, 0, Columns - 1);
            }

            if (y < 0 || y > Rows - 1)
            {
                vy = -vy;
                y = Math.Clamp(y, 0, Rows - 1);
            }

            _particles[index] = particle with { X = x, Y = y, Vx = vx, Vy = vy };
        }
    }

    private readonly record struct Particle(double X, double Y, double Vx, double Vy, double Radius);
}
