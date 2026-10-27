namespace ClamChart.Tests;

using SkiaSharp;

public sealed class ClamStatViewRenderingTests
{
    private const float Scale = 2;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    private static readonly SKColor Purple = new(0xB8, 0x77, 0xD9);

    private static readonly SKColor Warning = new(0xF2, 0xCC, 0x0C);

    private static readonly SKColor Critical = new(0xE0, 0x2F, 0x44);

    private static readonly SKColor Positive = new(0x56, 0xA6, 0x4B);

    private static readonly SKColor DarkInk = new(32, 34, 38);

    private static readonly SKColor LightInk = new(247, 248, 250);

    [Fact]
    public void BackgroundModeTintsTheWholeTile()
    {
        // Arrange
        using var view = Stat(75);
        view.ColorMode = ChartStatColorMode.Background;
        view.Thresholds.Add(new ChartThreshold { Value = 70, Level = ChartThresholdLevel.Warning });

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        var pixel = GetPixel(bitmap, 100, 116);
        Assert.InRange(pixel.Alpha, 36, 40);
        Assert.Equal(Warning.WithAlpha(pixel.Alpha), pixel, ColorComparer.Loose);
    }

    [Fact]
    public void IndicatorDrawsTheBandAndTheIcon()
    {
        // Arrange
        using var view = Stat(95);
        view.ColorMode = ChartStatColorMode.Indicator;
        view.Thresholds.Add(new ChartThreshold { Value = 90, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        var icon = view.ChartLayout.IconBounds;
        Assert.Equal(Critical, GetPixel(bitmap, 2, 60));
        Assert.Equal(0, GetPixel(bitmap, 8, 60).Alpha);
        Assert.Equal(Critical, GetPixel(bitmap, icon.CenterX, icon.CenterY));
    }

    [Fact]
    public void NoColorModeDrawsNoState()
    {
        // Arrange
        using var view = Stat(95);
        view.Thresholds.Add(new ChartThreshold { Value = 90, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.Equal(0, GetPixel(bitmap, 2, 60).Alpha);
        Assert.Equal(0, GetPixel(bitmap, 100, 116).Alpha);
    }

    [Fact]
    public void ValueModeWritesTheValueInTheTileColor()
    {
        // Arrange
        using var plain = Stat(42);
        plain.Fill = Color.FromArgb("#B877D9");
        using var colored = Stat(42);
        colored.Fill = Color.FromArgb("#B877D9");
        colored.ColorMode = ChartStatColorMode.Value;

        // Act
        using var first = plain.Paint(200, 120);
        using var second = colored.Paint(200, 120);

        // Assert
        Assert.Equal(0, CountColor(first, plain.ChartLayout.ValueBounds, Purple));
        Assert.True(CountColor(second, colored.ChartLayout.ValueBounds, Purple) > 20);
    }

    [Fact]
    public void GradientTextFollowsTheBrightnessOfTheColor()
    {
        // Arrange
        using var yellow = Stat(42);
        yellow.ColorMode = ChartStatColorMode.Background;
        yellow.FillMode = ChartFillMode.Gradient;
        yellow.Fill = Color.FromArgb("#FADE2A");
        using var blue = Stat(42);
        blue.ColorMode = ChartStatColorMode.Background;
        blue.FillMode = ChartFillMode.Gradient;
        blue.Fill = Color.FromArgb("#5794F2");

        // Act
        using var first = yellow.Paint(200, 120);
        using var second = blue.Paint(200, 120);

        // Assert
        Assert.True(CountColor(first, yellow.ChartLayout.ValueBounds, DarkInk) > 20);
        Assert.Equal(0, CountColor(first, yellow.ChartLayout.ValueBounds, LightInk));
        Assert.True(CountColor(second, blue.ChartLayout.ValueBounds, LightInk) > 20);
    }

    [Fact]
    public void GradientLightensInTheLightThemeAndDarkensInTheDarkTheme()
    {
        // Arrange
        using var light = Stat(42);
        light.ColorMode = ChartStatColorMode.Background;
        light.FillMode = ChartFillMode.Gradient;
        light.Fill = Color.FromArgb("#2A78D6");
        using var dark = Stat(42);
        dark.ApplyStyle(ChartStyles.Dark);
        dark.GradientStrength = 1;
        dark.ColorMode = ChartStatColorMode.Background;
        dark.FillMode = ChartFillMode.Gradient;
        dark.Fill = Color.FromArgb("#2A78D6");

        // Act
        using var first = light.Paint(200, 120);
        using var second = dark.Paint(200, 120);

        // Assert
        Blue.ToHsl(out _, out _, out var lightness);
        Assert.Equal(255, GetPixel(first, 1, 1).Alpha);
        Assert.InRange(GetLightness(first, 1, 1), lightness + 9.5, lightness + 11.5);
        Assert.InRange(GetLightness(first, 199, 119), lightness + 2.5, lightness + 4.5);
        Assert.InRange(GetLightness(second, 1, 1), lightness - 16, lightness - 14);
        Assert.InRange(GetLightness(second, 199, 119), lightness - 6, lightness - 4);
    }

    [Fact]
    public void DeltaArrowUsesTheToneColor()
    {
        // Arrange
        using var view = Stat(110);
        view.PreviousValue = 100;

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        var arrow = view.ChartLayout.ArrowBounds;
        var middle = GetMiddle(view.ChartLayout.DeltaBounds, view.ChartLayout.DeltaFontSize);
        Assert.Equal(Positive, GetPixel(bitmap, arrow.CenterX, middle));
    }

    [Fact]
    public void TrendIsTheTileColorWithAFaintFill()
    {
        // Arrange
        using var view = Stat(10);
        view.TrendValues = [0, 10, 0, 10];

        // Act
        using var bitmap = view.Paint(200, 160);

        // Assert
        var layout = view.TrendLayout;
        var first = GetPixel(bitmap, layout.ToX(0.5, layout.Scale), layout.ToY(5, layout.Scale));
        var last = GetPixel(bitmap, layout.ToX(2.5, layout.Scale), layout.ToY(5, layout.Scale));
        var fill = GetPixel(bitmap, layout.ToX(1, layout.Scale), layout.ToY(3, layout.Scale));
        Assert.True(first.Alpha > 200);
        Assert.True(last.Alpha > 200);
        Assert.InRange(fill.Alpha, 45, 57);
        Assert.Equal(1, layout.Thickness);
        Assert.Equal(ChartSparklineMarkers.None, layout.Markers);
    }

    [Fact]
    public void TrendFollowsTheThresholdWithoutAColorMode()
    {
        // Arrange
        using var view = Stat(95);
        view.Thresholds.Add(new ChartThreshold { Value = 90, Level = ChartThresholdLevel.Critical });
        view.TrendKind = ChartSparklineKind.Line;
        view.TrendValues = [0, 10];

        // Act
        using var bitmap = view.Paint(200, 160);

        // Assert
        var layout = view.TrendLayout;
        Assert.Equal(Critical, GetPixel(bitmap, layout.ToX(0.5, layout.Scale), layout.ToY(5, layout.Scale)).WithAlpha(255), ColorComparer.Loose);
        Assert.Equal(ChartThresholdLevel.None, view.ChartLayout.IconLevel);
    }

    [Fact]
    public void BackgroundTrendFadesTowardsTheTop()
    {
        // Arrange
        using var view = Stat(10);
        view.TrendKind = ChartSparklineKind.Line;
        view.TrendPlacement = ChartTrendPlacement.Background;
        view.TrendValues = [0, 10];

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        var layout = view.TrendLayout;
        var low = GetPixel(bitmap, layout.ToX(0.1, layout.Scale), layout.ToY(1, layout.Scale));
        var high = GetPixel(bitmap, layout.ToX(0.9, layout.Scale), layout.ToY(9, layout.Scale));
        Assert.Equal(new ChartRect(0, 48, 200, 72), view.ChartLayout.TrendBounds);
        Assert.True(low.Alpha > 200);
        Assert.InRange(high.Alpha, 20, 120);
    }

    private static TestClamStatView Stat(double value)
    {
        var view = new TestClamStatView { AnimationDriver = new TestAnimationDriver(), Value = value, Title = "Sales", EntranceAnimation = ChartEntranceAnimation.None };
        view.OnAttached();
        return view;
    }

    private static double GetMiddle(ChartRect bounds, double fontSize)
    {
        using var painter = new ChartTextPainter("sans-serif");
        var metrics = painter.GetMetrics((float)fontSize);
        var baseline = bounds.Y + ((bounds.Height - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
        return baseline - ((metrics.CapHeight > 0 ? metrics.CapHeight : fontSize * 0.7) / 2);
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static float GetLightness(SKBitmap bitmap, double x, double y)
    {
        GetPixel(bitmap, x, y).ToHsl(out _, out _, out var lightness);
        return lightness;
    }

    private static int CountColor(SKBitmap bitmap, ChartRect bounds, SKColor color)
    {
        var count = 0;
        for (var y = (int)(bounds.Y * Scale); y < (int)(bounds.Bottom * Scale); y++)
        {
            for (var x = (int)(bounds.X * Scale); x < (int)(bounds.Right * Scale); x++)
            {
                if (ColorComparer.Loose.Equals(bitmap.GetPixel(x, y), color))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private sealed class ColorComparer : IEqualityComparer<SKColor>
    {
        public static ColorComparer Loose { get; } = new();

        public bool Equals(SKColor x, SKColor y) =>
            (Math.Abs(x.Red - y.Red) <= 12) && (Math.Abs(x.Green - y.Green) <= 12) && (Math.Abs(x.Blue - y.Blue) <= 12) && (Math.Abs(x.Alpha - y.Alpha) <= 2);

        public int GetHashCode(SKColor obj) => 0;
    }
}
