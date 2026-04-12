namespace PretextSamples.Samples;

public sealed class VariableAsciiSampleView : UserControl
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
    private const string MonoRamp = " .`-_:,;^=+/|)\\!?0oOQ#%@";
    private static readonly int[] Weights = [300, 500, 800];
    private static readonly bool[] ItalicOptions = [false, true];
    private static readonly SolidColorBrush[] PropAlphaBrushes = CreatePropAlphaBrushes();

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly UiRenderScheduler _renderScheduler;
    private readonly Stopwatch _clock = new();
    private readonly Image _sourceImage;
    private readonly WriteableBitmap _sourceBitmap = new(CanvasWidth, CanvasHeight);
    private readonly Stream _sourcePixelStream;
    private readonly byte[] _sourcePixels = new byte[CanvasWidth * CanvasHeight * 4];
    private readonly SKBitmap _sourceSurface = new(CanvasWidth, CanvasHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
    private readonly SKCanvas _sourceSurfaceCanvas;
    private readonly Dictionary<int, SKImage> _spriteCache = [];
    private readonly SKPaint _sourceFadePaint = new()
    {
        Color = new SKColor(0, 0, 0, 46),
        BlendMode = SKBlendMode.SrcOver,
        IsAntialias = true,
    };
    private readonly SKPaint _sourceSpritePaint = new()
    {
        BlendMode = SKBlendMode.Plus,
        IsAntialias = true,
    };
    private readonly StackPanel _propRowsHost = new()
    {
        Spacing = 0,
        Width = TargetRowWidth,
        Height = Rows * LineHeight,
        HorizontalAlignment = HorizontalAlignment.Center,
    };
    private readonly StackPanel _monoRowsHost = new()
    {
        Spacing = 0,
        Width = TargetRowWidth,
        Height = Rows * LineHeight,
        HorizontalAlignment = HorizontalAlignment.Center,
    };
    private readonly List<TextBlock> _propRowPool = [];
    private readonly List<TextBlock> _monoRowPool = [];
    private readonly List<Particle> _particles = [];
    private readonly float[] _brightnessField = new float[FieldCols * FieldRows];
    private readonly BrightnessEntry[] _brightnessLookup = new BrightnessEntry[256];
    private readonly FieldStamp _particleFieldStamp;
    private readonly FieldStamp _largeAttractorFieldStamp;
    private readonly FieldStamp _smallAttractorFieldStamp;
    private double _attractor1X;
    private double _attractor1Y;
    private double _attractor2X;
    private double _attractor2Y;

    public VariableAsciiSampleView()
    {
        _renderScheduler = new UiRenderScheduler(DispatcherQueue, Render);
        _sourceSurfaceCanvas = new SKCanvas(_sourceSurface);
        _sourceSurfaceCanvas.Clear(SKColors.Black);
        _sourcePixelStream = _sourceBitmap.PixelBuffer.AsStream();
        _particleFieldStamp = CreateFieldStamp(SpriteRadius);
        _largeAttractorFieldStamp = CreateFieldStamp(LargeAttractorRadius);
        _smallAttractorFieldStamp = CreateFieldStamp(AttractorRadius);
        _sourceImage = new Image
        {
            Source = _sourceBitmap,
            Width = TargetRowWidth,
            Height = Rows * LineHeight,
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        SeedParticles();
        BuildBrightnessLookup();
        BuildRowPools();
        UpdateSourceBitmap();

        var stack = new StackPanel
        {
            Spacing = 28,
            Padding = new Thickness(20, 32, 20, 60),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        stack.Children.Add(new TextBlock
        {
            Text = "Variable Typographic ASCII",
            FontFamily = new FontFamily("Helvetica Neue"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = SampleTheme.Brush(230, 255, 255, 255),
            HorizontalTextAlignment = TextAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Proportional font (Georgia) rendered at 3 font-weights × normal/italic — each variant measured by pretext for precise width. A shared particle-and-attractor brightness field drives all three panels, then characters are chosen by brightness AND width to preserve the shape in proportional type.",
            FontFamily = new FontFamily("Helvetica Neue"),
            FontSize = 13,
            Foreground = SampleTheme.Brush(102, 255, 255, 255),
            MaxWidth = 720,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });

        var grid = new Grid
        {
            ColumnSpacing = 28,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        for (var i = 0; i < 3; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        }

        var sourcePanel = BuildAsciiPanel("SOURCE FIELD", new Border
        {
            Width = TargetRowWidth,
            Height = Rows * LineHeight,
            Background = SampleTheme.Brush(255, 0, 0, 0),
            Child = _sourceImage,
        }, PanelWidth);
        var propPanel = BuildAsciiPanel("PROPORTIONAL × 3 WEIGHTS × ITALIC", _propRowsHost, PanelWidth);
        var monoPanel = BuildAsciiPanel("MONOSPACE × SINGLE WEIGHT", _monoRowsHost, PanelWidth);
        grid.Children.Add(sourcePanel);
        Grid.SetColumn(propPanel, 1);
        grid.Children.Add(propPanel);
        Grid.SetColumn(monoPanel, 2);
        grid.Children.Add(monoPanel);
        stack.Children.Add(grid);

        Content = new Grid
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops =
                {
                    new GradientStop { Offset = 0, Color = ColorHelper.FromArgb(255, 10, 10, 18) },
                    new GradientStop { Offset = 1, Color = ColorHelper.FromArgb(255, 6, 6, 10) },
                },
            },
            Children =
            {
                new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Content = stack,
                },
            },
        };

        Loaded += (_, _) =>
        {
            _clock.Restart();
            AdvanceParticles(0);
            Render();
            _timer.Start();
        };
        Unloaded += (_, _) => _timer.Stop();
        _timer.Tick += (_, _) =>
        {
            AdvanceParticles(_clock.Elapsed.TotalMilliseconds);
            _renderScheduler.Schedule();
        };
    }

    private static Border BuildAsciiPanel(string title, UIElement content, double width)
    {
        var stack = new StackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontFamily = new FontFamily("Helvetica Neue"),
            FontSize = 10,
            CharacterSpacing = 150,
            Foreground = SampleTheme.Brush(76, 255, 255, 255),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        });
        stack.Children.Add(new Border
        {
            Width = width,
            BorderBrush = SampleTheme.Brush(24, 255, 255, 255),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Background = SampleTheme.Brush(102, 0, 0, 0),
            Child = content,
        });
        return new Border
        {
            Background = null,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = stack,
        };
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

    private void BuildRowPools()
    {
        for (var row = 0; row < Rows; row++)
        {
            var propRow = new TextBlock
            {
                FontFamily = new FontFamily(PropFamilyDisplay),
                FontSize = AsciiFontSize,
                Foreground = PropAlphaBrushes[10],
                LineHeight = LineHeight,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            _propRowsHost.Children.Add(propRow);
            _propRowPool.Add(propRow);

            var monoRow = new TextBlock
            {
                FontFamily = new FontFamily("Courier New"),
                FontSize = AsciiFontSize,
                Foreground = SampleTheme.Brush(179, 130, 155, 210),
                TextWrapping = TextWrapping.NoWrap,
                LineHeight = LineHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            _monoRowsHost.Children.Add(monoRow);
            _monoRowPool.Add(monoRow);
        }
    }

    private static SolidColorBrush[] CreatePropAlphaBrushes()
    {
        var brushes = new SolidColorBrush[11];
        for (var level = 1; level <= 10; level++)
        {
            brushes[level] = SampleTheme.Brush((byte)Math.Round(level * 25.5), 196, 163, 90);
        }

        return brushes;
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
        using var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using var typeface = CreateTypeface(weight, italic);
        using var font = new SKFont(typeface, size)
        {
            Subpixel = true,
        };
        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
        };

        var metrics = font.Metrics;
        var baseline = size / 2f - ((metrics.Ascent + metrics.Descent) / 2f);
        canvas.DrawText(ch.ToString(), 1, baseline, SKTextAlign.Left, font, paint);

        double sum = 0;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                sum += bitmap.GetPixel(x, y).Alpha;
            }
        }

        return sum / (255d * size * size);
    }

    private static SKTypeface CreateTypeface(int weight, bool italic)
    {
        var style = new SKFontStyle(weight, (int)SKFontStyleWidth.Normal, italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        return SKTypeface.FromFamilyName(PropFamilyDisplay, style) ?? SKTypeface.Default;
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

    private void Render()
    {
        for (var index = 0; index < _brightnessField.Length; index++)
        {
            _brightnessField[index] *= (float)FieldDecay;
        }

        RenderSourceField();

        foreach (var particle in _particles)
        {
            SplatFieldStamp(particle.X, particle.Y, _particleFieldStamp);
        }
        SplatFieldStamp(_attractor1X, _attractor1Y, _largeAttractorFieldStamp);
        SplatFieldStamp(_attractor2X, _attractor2Y, _smallAttractorFieldStamp);

        var mono = new StringBuilder(Columns);
        var prop = new StringBuilder(Columns);
        for (var row = 0; row < Rows; row++)
        {
            mono.Clear();
            prop.Clear();
            var propRow = _propRowPool[row];
            propRow.Inlines.Clear();
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
                    FlushPropRun(propRow, prop, currentVariant);
                    currentVariant = entry.PropVariant;
                }

                prop.Append(entry.PropVariant?.Character ?? ' ');
            }

            FlushPropRun(propRow, prop, currentVariant);
            _monoRowPool[row].Text = mono.ToString();
        }
    }

    private void FlushPropRun(TextBlock rowBlock, StringBuilder buffer, GlyphVariant? variant)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        var run = new Run
        {
            Text = buffer.ToString(),
        };
        if (variant is GlyphVariant glyph)
        {
            run.FontWeight = glyph.Weight switch
            {
                300 => FontWeights.Light,
                500 => FontWeights.Medium,
                _ => FontWeights.ExtraBold,
            };
            run.FontStyle = glyph.Italic ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal;
            run.Foreground = PropAlphaBrushes[glyph.AlphaLevel];
        }

        rowBlock.Inlines.Add(run);
        buffer.Clear();
    }

    private void RenderSourceField()
    {
        _sourceSurfaceCanvas.DrawRect(SKRect.Create(CanvasWidth, CanvasHeight), _sourceFadePaint);
        var particleSprite = GetSpriteImage(SpriteRadius);
        for (var index = 0; index < _particles.Count; index++)
        {
            var particle = _particles[index];
            _sourceSurfaceCanvas.DrawImage(particleSprite, (float)(particle.X - SpriteRadius), (float)(particle.Y - SpriteRadius), _sourceSpritePaint);
        }

        _sourceSurfaceCanvas.DrawImage(GetSpriteImage(LargeAttractorRadius), (float)(_attractor1X - LargeAttractorRadius), (float)(_attractor1Y - LargeAttractorRadius), _sourceSpritePaint);
        _sourceSurfaceCanvas.DrawImage(GetSpriteImage(AttractorRadius), (float)(_attractor2X - AttractorRadius), (float)(_attractor2Y - AttractorRadius), _sourceSpritePaint);
        UpdateSourceBitmap();
    }

    private SKImage GetSpriteImage(int radius)
    {
        if (_spriteCache.TryGetValue(radius, out var image))
        {
            return image;
        }

        using var bitmap = new SKBitmap(radius * 2, radius * 2, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(radius, radius),
                radius,
                [new SKColor(255, 255, 255, 115), new SKColor(255, 255, 255, 38), new SKColor(255, 255, 255, 0)],
                [0f, 0.35f, 1f],
                SKShaderTileMode.Clamp),
        };
        canvas.DrawRect(SKRect.Create(radius * 2, radius * 2), paint);

        image = SKImage.FromBitmap(bitmap);
        _spriteCache[radius] = image;
        return image;
    }

    private void UpdateSourceBitmap()
    {
        Marshal.Copy(_sourceSurface.GetPixels(), _sourcePixels, 0, _sourcePixels.Length);
        _sourcePixelStream.Position = 0;
        _sourcePixelStream.Write(_sourcePixels, 0, _sourcePixels.Length);
        _sourceBitmap.Invalidate();
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

    private sealed class Particle(double x, double y, double vx, double vy)
    {
        public double X { get; set; } = x;

        public double Y { get; set; } = y;

        public double Vx { get; set; } = vx;

        public double Vy { get; set; } = vy;
    }
}
