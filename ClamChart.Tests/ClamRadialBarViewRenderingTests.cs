namespace ClamChart.Tests;

using SkiaSharp;

public sealed class ClamRadialBarViewRenderingTests
{
    private const float Scale = 2;

    private const double Radius = 137.5;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    private static readonly SKColor Orange = new(0xEB, 0x68, 0x34);

    private static readonly SKColor Good = new(0x56, 0xA6, 0x4B);

    private static readonly SKColor Warning = new(0xF2, 0xCC, 0x0C);

    private static readonly SKColor Critical = new(0xE0, 0x2F, 0x44);

    private static readonly SKColor Track = new(0, 0, 0, 18);

    private static readonly SKColor UnlitSegment = new(36, 41, 46, 77);

    [Fact]
    public void BasicDrawsTheValueOverANeutralTrack()
    {
        // Arrange
        using var view = Ring(50);

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(Blue, GetPixel(bitmap, 90), ColorComparer.Loose);
        Assert.Equal(Blue, GetPixel(bitmap, 175), ColorComparer.Loose);
        Assert.Equal(Track, GetPixel(bitmap, 270), ColorComparer.Loose);
    }

    [Fact]
    public void FlatEndsStopAtTheValueAndRoundEndsGoPastIt()
    {
        // Arrange
        using var flat = Ring(50);
        using var round = Ring(50);
        round.CapStyle = ChartCapStyle.Round;

        // Act
        using var first = flat.Paint(300, 300);
        using var second = round.Paint(300, 300);

        // Assert
        Assert.Equal(Track, GetPixel(first, 182.5), ColorComparer.Loose);
        Assert.Equal(Blue, GetPixel(second, 182.5), ColorComparer.Loose);
    }

    [Fact]
    public void ThresholdsColorTheRingByItsPercentage()
    {
        // Arrange
        using var view = Ring(50);
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 40, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(Critical, GetPixel(bitmap, 90), ColorComparer.Loose);
    }

    [Fact]
    public void SegmentsLightUpBeforeTheValue()
    {
        // Arrange
        using var view = Ring(50);
        view.DisplayMode = ChartRadialBarMode.Segmented;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 25, Level = ChartThresholdLevel.Warning });

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(Good, GetPixel(bitmap, 45), ColorComparer.Loose);
        Assert.Equal(Warning, GetPixel(bitmap, 153), ColorComparer.Loose);
        Assert.Equal(UnlitSegment, GetPixel(bitmap, 279), ColorComparer.Loose);
        Assert.True(GetPixel(bitmap, 18).Alpha < 100);
    }

    [Fact]
    public void WrapDrawsTheNextLapInADarkerColor()
    {
        // Arrange
        using var view = Ring(130);
        view.OverflowMode = ChartOverflowMode.Wrap;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var darker = GetPixel(bitmap, 45);
        Assert.Equal(Blue, GetPixel(bitmap, 200), ColorComparer.Loose);
        Assert.True(GetLightness(darker) < GetLightness(Blue) - 10);
    }

    [Fact]
    public void ThresholdGradientsChangeAtTheThresholds()
    {
        // Arrange
        using var view = Ring(100);
        view.FillMode = ChartFillMode.Gradient;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning });
        view.Thresholds.Add(new ChartThreshold { Value = 80, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(Good, GetPixel(bitmap, 2), ColorComparer.Loose);
        Assert.Equal(Warning, GetPixel(bitmap, 216), ColorComparer.Loose);
        Assert.Equal(Critical, GetPixel(bitmap, 330), ColorComparer.Loose);
    }

    [Fact]
    public void OneColorGradientsGetLighterTowardTheStartInLight()
    {
        // Arrange
        using var view = Ring(75);
        view.FillMode = ChartFillMode.Gradient;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.True(GetLightness(GetPixel(bitmap, 5)) > GetLightness(GetPixel(bitmap, 260)));
        Assert.Equal(Track, GetPixel(bitmap, 300), ColorComparer.Loose);
    }

    [Theory]
    [InlineData(ChartFillMode.Solid)]
    [InlineData(ChartFillMode.Gradient)]
    public void GlowSpreadsOutsideTheValueOnly(ChartFillMode fill)
    {
        // Arrange
        using var view = Ring(50);
        view.FillMode = fill;
        view.BarGlow = true;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var outside = view.ChartLayout.OuterRadius + 2;
        Assert.True(GetPixel(bitmap, 90, outside).Alpha > 20);
        Assert.Equal(0, GetPixel(bitmap, 270, outside).Alpha);
    }

    [Fact]
    public void SegmentedGlowCoversEveryCell()
    {
        // Arrange
        using var view = Ring(50);
        view.DisplayMode = ChartRadialBarMode.Segmented;
        view.BarGlow = true;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var outside = view.ChartLayout.OuterRadius + 2;
        var lit = GetPixel(bitmap, 90, outside).Alpha;
        var unlit = GetPixel(bitmap, 270, outside).Alpha;
        Assert.True(lit > 20);
        Assert.True(unlit > 0);
        Assert.True(unlit < lit);
    }

    [Fact]
    public void GlowLeavesTheMarginOfGrafana()
    {
        // Arrange
        using var view = Ring(50);
        view.BarGlow = true;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(150 - (300 * 0.02), view.ChartLayout.OuterRadius, 0.001);
    }

    [Fact]
    public void GradientGlowDoesNotWrapPastTheStart()
    {
        // Arrange
        using var view = Ring(50);
        view.FillMode = ChartFillMode.Gradient;
        view.CapStyle = ChartCapStyle.Round;
        view.BarGlow = true;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.True(GetPixel(bitmap, 350, view.ChartLayout.GetRingRadius(0)).Alpha < 30);
    }

    [Fact]
    public void TheCenterShowsThePercentage()
    {
        // Arrange
        using var view = Ring(50);
        view.CenterValueMode = ChartCenterValueMode.Auto;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var center = view.ChartLayout.Center;
        Assert.Equal("50%", center.Value);
        Assert.True(CountColor(bitmap, center.ValueBounds, new SKColor(0x0B, 0x0B, 0x0B)) > 0);
    }

    [Fact]
    public void TheSelectedRingKeepsItsColorAndTheOthersFade()
    {
        // Arrange
        using var view = Rings(new ChartEntry("A", 50), new ChartEntry("B", 50));
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 1;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Blue.WithAlpha(89), GetPixel(bitmap, 90, layout.GetRingRadius(0)), ColorComparer.Loose);
        Assert.Equal(Orange, GetPixel(bitmap, 90, layout.GetRingRadius(1)), ColorComparer.Loose);
    }

    [Fact]
    public void TheLegendIsDrawnBesideTheRings()
    {
        // Arrange
        using var view = Rings(new ChartEntry("Move", 50), new ChartEntry("Stand", 75));
        view.LegendVisible = true;

        // Act
        using var bitmap = view.Paint(400, 240);

        // Assert
        var legend = view.ChartLayout.Legend;
        Assert.True(legend.IsVisible);
        Assert.True(CountColor(bitmap, legend.Bounds, new SKColor(0x52, 0x51, 0x4E)) > 0);
        Assert.True(CountColor(bitmap, legend.Items[1].Marker, Orange) > 0);
    }

    [Theory]
    [InlineData(ChartRadialBarMode.Basic, ChartFillMode.Solid, ChartCapStyle.Flat, false)]
    [InlineData(ChartRadialBarMode.Basic, ChartFillMode.Solid, ChartCapStyle.Round, false)]
    [InlineData(ChartRadialBarMode.Basic, ChartFillMode.Gradient, ChartCapStyle.Round, false)]
    [InlineData(ChartRadialBarMode.Segmented, ChartFillMode.Solid, ChartCapStyle.Flat, false)]
    [InlineData(ChartRadialBarMode.Basic, ChartFillMode.Solid, ChartCapStyle.Round, true)]
    [InlineData(ChartRadialBarMode.Basic, ChartFillMode.Gradient, ChartCapStyle.Round, true)]
    [InlineData(ChartRadialBarMode.Segmented, ChartFillMode.Solid, ChartCapStyle.Flat, true)]
    public void FramesDoNotAllocate(ChartRadialBarMode mode, ChartFillMode fill, ChartCapStyle cap, bool glow)
    {
        // Arrange
        using var view = new TestClamRadialBarView
        {
            AnimationDriver = new TestAnimationDriver(),
            ItemsSource = new List<ChartEntry> { new("Move", 120), new("Exercise", 58), new("Stand", 31, Maximum: 50) },
            EntranceAnimation = ChartEntranceAnimation.None,
            DisplayMode = mode,
            FillMode = fill,
            CapStyle = cap,
            BarGlow = glow,
            ThresholdMarkersVisible = true,
            OverflowMode = ChartOverflowMode.Wrap,
            LegendVisible = true,
            LegendValueMode = ChartLegendValueMode.Percent,
            SelectionMode = ChartSelectionMode.Item,
            SelectedIndex = 1
        };
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning });
        using var bitmap = view.Paint(400, 240);
        using var surface = SKSurface.Create(new SKImageInfo(800, 480));
        var canvas = surface.Canvas;
        view.Render(canvas, 400, 240);

        // Act
        var allocated = ChartAllocation.Measure(() =>
        {
            for (var i = 0; i < 4; i++)
            {
                view.Render(canvas, 400, 240);
            }
        });

        // Assert
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ThresholdMarkersColorTheRangesOutsideTheRing()
    {
        // Arrange
        using var view = Ring(50);
        view.ThresholdMarkersVisible = true;
        view.Thresholds.Add(new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Warning });
        view.Thresholds.Add(new ChartThreshold { Value = 80, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(4, layout.MarkerWidth);
        Assert.Equal(148, layout.MarkerRadius);
        Assert.Equal(Blue, GetPixel(bitmap, 90, 148), ColorComparer.Loose);
        Assert.Equal(Warning, GetPixel(bitmap, 234, 148), ColorComparer.Loose);
        Assert.Equal(Critical, GetPixel(bitmap, 324, 148), ColorComparer.Loose);
        Assert.Equal(0, GetPixel(bitmap, 90, 145).Alpha);
        Assert.Equal(Warning, GetPixel(bitmap, 90, 132), ColorComparer.Loose);
    }

    [Fact]
    public void ThresholdMarkersNeedThresholds()
    {
        // Arrange
        using var view = Ring(50);
        view.ThresholdMarkersVisible = true;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(0, view.ChartLayout.MarkerWidth);
        Assert.Equal(150, view.ChartLayout.OuterRadius);
    }

    private static TestClamRadialBarView Ring(double value) => Rings(new ChartEntry("A", value));

    private static TestClamRadialBarView Rings(params ChartEntry[] entries)
    {
        var view = new TestClamRadialBarView
        {
            AnimationDriver = new TestAnimationDriver(),
            ItemsSource = entries.ToList(),
            EntranceAnimation = ChartEntranceAnimation.None,
            ContentPadding = new Thickness(0),
            CenterValueMode = ChartCenterValueMode.None,
            LegendVisible = false
        };
        view.OnAttached();
        return view;
    }

    private static SKColor GetPixel(SKBitmap bitmap, double angle, double radius = Radius)
    {
        var (x, y) = PieLayout.GetDirection(angle);
        return bitmap.GetPixel((int)((150 + (x * radius)) * Scale), (int)((150 + (y * radius)) * Scale));
    }

    private static float GetLightness(SKColor color)
    {
        color.ToHsl(out _, out _, out var lightness);
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

        public int GetHashCode(SKColor obj) => obj.GetHashCode();
    }
}
