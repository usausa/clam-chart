namespace ClamChart.Tests;

using Microsoft.Maui;

using SkiaSharp;
using SkiaSharp.Views.Maui;

[Collection(nameof(ClamChartSettings))]
public sealed class ChartEffectTests
{
    private const string Effect = "ClamChartEffect";

    private const float Scale = 2;

    private static readonly SKColor Lime = new(0, 255, 0);

    private static readonly SKColor Critical = new(0xE0, 0x2F, 0x44);

    [Fact]
    public void GlowDrawsASoftHaloUnderTheLine()
    {
        // Arrange
        using var plain = Glowing(0);
        using var glowing = Glowing(8);

        // Act
        using var plainBitmap = plain.Paint(360, 240);
        using var glowBitmap = glowing.Paint(360, 240);

        // Assert
        var layout = glowing.ChartLayout;
        var x = layout.XAxis.ToPixel(0.5);
        var y = layout.YAxis.ToPixel(5);
        var halo = GetPixel(glowBitmap, x, y - 4);
        Assert.Equal(0, GetPixel(plainBitmap, x, y - 4).Alpha);
        Assert.InRange(halo.Alpha, 10, 60);
        Assert.InRange(halo.Green, 240, 255);
        Assert.InRange(halo.Red, 0, 15);
        Assert.Equal(Lime, GetPixel(glowBitmap, x, y));
    }

    [Fact]
    public void GlowIsLeftOutInMinimalDensity()
    {
        // Arrange
        using var view = Glowing(8);
        view.Density = ChartDensity.Minimal;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(0, GetPixel(bitmap, layout.XAxis.ToPixel(0.5), layout.YAxis.ToPixel(5) - 4).Alpha);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void GlowIsLeftOutAboveFiveHundredPoints(int count, bool expected)
    {
        // Arrange
        using var view = Glowing(8, count);

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var alpha = GetPixel(bitmap, layout.XAxis.ToPixel((count - 1) / 2d), layout.YAxis.ToPixel(5) - 4).Alpha;
        Assert.Equal(expected, alpha > 0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Double.NaN)]
    [InlineData(Double.PositiveInfinity)]
    public void GlowRadiusMustBeFiniteAndNotNegative(double value)
    {
        // Arrange
        var series = new LineSeries();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => series.GlowRadius = value);
    }

    [Fact]
    public void GlowAddsNoAllocationToAFrame()
    {
        // Arrange
        using var view = Glowing(0);
        using var plain = view.Paint(360, 240);
        using var surface = SKSurface.Create(new SKImageInfo(720, 480));
        var idle = MeasureFrame(view, surface.Canvas, 360, 240);
        ((LineSeries)view.Series[0]).GlowRadius = 8;
        using var glowing = view.Paint(360, 240);

        // Act
        var allocated = MeasureFrame(view, surface.Canvas, 360, 240);

        // Assert
        Assert.Equal(idle, allocated);
    }

    [Fact]
    public void LiveIndicatorDrawsTheLastPointWithoutMarkers()
    {
        // Arrange
        using var plain = Live(false);
        using var live = Live(true);

        // Act
        using var plainBitmap = plain.Paint(360, 240);
        using var liveBitmap = live.Paint(360, 240);

        // Assert
        var (x, y) = LastPoint(live);
        Assert.Equal(0, GetPixel(plainBitmap, x, y + 3).Alpha);
        Assert.Equal(Lime, GetPixel(liveBitmap, x, y + 3));
        Assert.Equal(0, GetPixel(liveBitmap, x, y + 8).Alpha);
    }

    [Fact]
    public void LiveIndicatorLoopsWhileShown()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);

        // Act
        using var first = view.Paint(360, 240);
        driver.Frame(Effect, 0.25);

        // Assert
        Assert.Equal([(Effect, 1800u)], driver.Starts);
        Assert.True(view.IsEffectAnimating);
        Assert.Equal(0.25, view.EffectPhase);

        // Act
        driver.Finish(Effect);
        using var second = view.Paint(360, 240);

        // Assert
        Assert.True(driver.IsRunning(Effect));
        Assert.Single(driver.Starts);

        // Act
        ((LineSeries)view.Series[0]).LiveIndicatorVisible = false;
        using var third = view.Paint(360, 240);

        // Assert
        Assert.False(driver.IsRunning(Effect));
        Assert.False(view.IsEffectAnimating);
    }

    [Fact]
    public void LiveIndicatorRingsGrowAndFade()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);
        using var first = view.Paint(360, 240);

        // Act
        driver.Frame(Effect, 0.25);
        using var bitmap = view.Paint(360, 240);

        // Assert
        var (x, y) = LastPoint(view);
        var outer = GetPixel(bitmap, x, y + 10);
        Assert.InRange(outer.Alpha, 24, 34);
        Assert.InRange(outer.Green, 240, 255);
        Assert.Equal(0, GetPixel(bitmap, x, y + 8).Alpha);
        Assert.True(GetPixel(bitmap, x, y + 6.5).Alpha > outer.Alpha);
        Assert.Equal(Lime, GetPixel(bitmap, x, y + 3));
    }

    [Theory]
    [InlineData(ChartOrientation.Vertical, 0, 0, 348, 240)]
    [InlineData(ChartOrientation.Horizontal, 0, 0, 360, 228)]
    public void LiveIndicatorMakesRoomAtTheEndOfTheXAxis(ChartOrientation orientation, double left, double top, double right, double bottom)
    {
        // Arrange
        using var view = Live(true);
        view.Density = ChartDensity.Minimal;
        view.Orientation = orientation;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var plot = view.ChartLayout.PlotBounds;
        Assert.Equal((left, top, right, bottom), (plot.X, plot.Y, plot.Right, plot.Bottom));
    }

    [Fact]
    public void LiveIndicatorRingsStayInsideTheView()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);
        view.Density = ChartDensity.Minimal;
        using var first = view.Paint(360, 240);

        // Act
        driver.Frame(Effect, 0.25);
        using var bitmap = view.Paint(360, 240);

        // Assert
        var (x, y) = LastPoint(view);
        Assert.Equal(348, x);
        Assert.InRange(GetPixel(bitmap, x + 10, y).Alpha, 24, 34);
    }

    [Fact]
    public void LiveIndicatorStaysStillWithoutAnimations()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);
        view.AnimationEnabled = false;

        // Act
        using var bitmap = view.Paint(360, 240);
        using var again = view.Paint(360, 240);

        // Assert
        var (x, y) = LastPoint(view);
        Assert.Empty(driver.Starts);
        Assert.False(view.IsEffectAnimating);
        Assert.Equal(Lime, GetPixel(again, x, y + 3));
        Assert.Equal(0, GetPixel(again, x, y + 10).Alpha);
    }

    [Fact]
    public void LiveIndicatorAddsNoAllocationToAFrame()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(false, driver);
        using var plain = view.Paint(360, 240);
        using var surface = SKSurface.Create(new SKImageInfo(720, 480));
        var idle = MeasureFrame(view, surface.Canvas, 360, 240);
        ((LineSeries)view.Series[0]).LiveIndicatorVisible = true;
        using var live = view.Paint(360, 240);
        driver.Frame(Effect, 0.3);

        // Act
        var allocated = MeasureFrame(view, surface.Canvas, 360, 240);

        // Assert
        Assert.True(view.IsEffectAnimating);
        Assert.Equal(idle, allocated);
    }

    [Fact]
    public void TurningAnimationsOffStopsTheEffect()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);
        using var first = view.Paint(360, 240);

        // Act
        view.AnimationEnabled = false;
        using var second = view.Paint(360, 240);

        // Assert
        Assert.False(driver.IsRunning(Effect));
        Assert.False(view.IsEffectAnimating);
        Assert.Single(driver.Starts);
    }

    [Fact]
    public void EffectsStayStillWhenTheApplicationTurnsAnimationsOff()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var chart = Live(true, driver);
        using var stat = Alert(95, ChartStatColorMode.Indicator, driver);
        ClamChartSettings.AnimationEnabled = false;
        try
        {
            // Act
            using var chartBitmap = chart.Paint(360, 240);
            using var statBitmap = stat.Paint(200, 120);

            // Assert
            Assert.Empty(driver.Starts);
            Assert.Equal(Critical, GetPixel(statBitmap, 2, 60));
        }
        finally
        {
            ClamChartSettings.AnimationEnabled = true;
        }
    }

    [Fact]
    public void BusyStopsTheEffect()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);
        using var first = view.Paint(360, 240);

        // Act
        view.IsBusy = true;
        using var second = view.Paint(360, 240);

        // Assert
        Assert.False(driver.IsRunning(Effect));
        Assert.True(view.IsSkeletonAnimating);
    }

    [Fact]
    public void HidingTheViewStopsTheEffect()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);
        using var first = view.Paint(360, 240);

        // Act
        view.IsVisible = false;

        // Assert
        Assert.False(driver.IsRunning(Effect));
        Assert.Contains(Effect, driver.Aborts);
    }

    [Fact]
    public void StoppedWindowPausesTheLoops()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var chart = Live(true, driver);
        using var busy = new TestChartView { AnimationDriver = driver, IsBusy = true };
        var window = new Window(new ContentPage { Content = new VerticalStackLayout { chart, busy } });
        using var first = chart.Paint(360, 240);
        busy.Paint(200, 100);

        // Act
        ((IWindow)window).Stopped();
        using var second = chart.Paint(360, 240);
        busy.Paint(200, 100);

        // Assert
        Assert.False(driver.IsRunning(Effect));
        Assert.False(busy.IsSkeletonAnimating);
        Assert.Equal(2, driver.Starts.Count);

        // Act
        ((IWindow)window).Resumed();
        using var third = chart.Paint(360, 240);
        busy.Paint(200, 100);

        // Assert
        Assert.True(driver.IsRunning(Effect));
        Assert.True(busy.IsSkeletonAnimating);
        Assert.Equal(4, driver.Starts.Count);
    }

    [Fact]
    public void FramesAreReportedAsAnimatingWhileTheEffectRuns()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Live(true, driver);
        var frames = new List<bool>();
        view.FrameRendered += (_, e) => frames.Add(e.Animating);

        // Act
        using var first = view.Paint(360, 240);
        using var second = view.Paint(360, 240);

        // Assert
        Assert.Equal([true, true], frames);
    }

    [Fact]
    public void SparklineLiveIndicatorMakesRoomForTheDot()
    {
        // Arrange
        using var plain = Spark(false);
        using var live = Spark(true);

        // Act
        using var plainBitmap = plain.Paint(120, 40);
        using var liveBitmap = live.Paint(120, 40);

        // Assert
        Assert.Equal((1, 119), (plain.ChartLayout.Plot.X, plain.ChartLayout.Plot.Right));
        Assert.Equal((3.5, 112.5), (live.ChartLayout.Plot.X, live.ChartLayout.Plot.Right));
        var (plainX, plainY) = LastPoint(plain);
        var (liveX, liveY) = LastPoint(live);
        Assert.Equal(0, GetPixel(plainBitmap, plainX, plainY + 2).Alpha);
        Assert.Equal(Lime, GetPixel(liveBitmap, liveX + 1, liveY + 1));
    }

    [Fact]
    public void SparklineLiveIndicatorLoops()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Spark(true, driver);
        using var first = view.Paint(120, 40);

        // Act
        driver.Frame(Effect, 0.5);
        using var bitmap = view.Paint(120, 40);

        // Assert
        var (x, y) = LastPoint(view);
        Assert.Equal([(Effect, 1800u)], driver.Starts);
        Assert.InRange(GetPixel(bitmap, x, y + 5).Alpha, 45, 64);
    }

    [Fact]
    public void SparklineLiveIndicatorStaysStillWithoutAnimations()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Spark(true, driver);
        view.AnimationEnabled = false;

        // Act
        using var bitmap = view.Paint(120, 40);

        // Assert
        var (x, y) = LastPoint(view);
        Assert.Empty(driver.Starts);
        Assert.Equal(Lime, GetPixel(bitmap, x + 1, y + 1));
        Assert.Equal(0, GetPixel(bitmap, x, y + 5).Alpha);
    }

    [Fact]
    public void SparklineBarsHaveNoLiveIndicator()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Spark(true, driver);
        view.Kind = ChartSparklineKind.Bar;

        // Act
        using var bitmap = view.Paint(120, 40);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.False(view.IsEffectAnimating);
    }

    [Fact]
    public void AlertPulsesTheIndicatorWhenCritical()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.Indicator, driver);
        using var first = view.Paint(200, 120);

        // Act
        driver.Frame(Effect, 0.5);
        using var bitmap = view.Paint(200, 120);

        // Assert
        var icon = view.ChartLayout.IconBounds;
        Assert.Equal([(Effect, 1200u)], driver.Starts);
        Assert.Equal(Critical, GetPixel(first, 2, 60));
        AssertColor(Critical.WithAlpha(153), GetPixel(bitmap, 2, 60));
        AssertColor(Critical.WithAlpha(153), GetPixel(bitmap, icon.CenterX, icon.CenterY));
    }

    [Fact]
    public void AlertFollowsTheCriticalLevel()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(80, ChartStatColorMode.Indicator, driver);

        // Act
        using var first = view.Paint(200, 120);

        // Assert
        Assert.False(driver.IsRunning(Effect));

        // Act
        view.Value = 95;
        using var second = view.Paint(200, 120);

        // Assert
        Assert.True(driver.IsRunning(Effect));

        // Act
        view.Value = 50;
        using var third = view.Paint(200, 120);

        // Assert
        Assert.False(driver.IsRunning(Effect));
    }

    [Fact]
    public void AlertPulsesTheBackground()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.Background, driver);
        using var first = view.Paint(200, 120);

        // Act
        driver.Frame(Effect, 0.5);
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.InRange(GetPixel(first, 100, 116).Alpha, 36, 40);
        Assert.InRange(GetPixel(bitmap, 100, 116).Alpha, 21, 25);
    }

    [Fact]
    public void AlertPulsesTheGradientBackground()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.BackgroundGradient, driver);
        using var first = view.Paint(200, 120);

        // Act
        driver.Frame(Effect, 0.5);
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.Equal(255, GetPixel(first, 100, 116).Alpha);
        Assert.Equal(153, GetPixel(bitmap, 100, 116).Alpha);
    }

    [Fact]
    public void AlertPulsesTheValueText()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.Value, driver);
        using var first = view.Paint(200, 120);

        // Act
        driver.Frame(Effect, 0.5);
        using var bitmap = view.Paint(200, 120);

        // Assert
        var bounds = view.ChartLayout.ValueBounds;
        Assert.Equal(255, GetMaxAlpha(first, bounds));
        Assert.InRange(GetMaxAlpha(bitmap, bounds), 150, 155);
    }

    [Fact]
    public void AlertStaysStillWithoutAnimations()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.Indicator, driver);
        view.AnimationEnabled = false;

        // Act
        using var bitmap = view.Paint(200, 120);
        using var again = view.Paint(200, 120);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.Equal(Critical, GetPixel(again, 2, 60));
    }

    [Fact]
    public void AlertNeedsAColorMode()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.None, driver);

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.Empty(driver.Starts);
    }

    [Fact]
    public void AlertFramesDoNotAllocate()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.BackgroundGradient, driver);
        using var bitmap = view.Paint(200, 120);
        using var surface = SKSurface.Create(new SKImageInfo(400, 240));
        MeasureEffectFrames(view, driver, surface.Canvas, 200, 120);

        // Act
        var allocated = MeasureEffectFrames(view, driver, surface.Canvas, 200, 120);

        // Assert
        Assert.True(view.IsEffectAnimating);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ImagesShowTheEffectsStill()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = Alert(95, ChartStatColorMode.Indicator, driver);
        using var first = view.Paint(200, 120);
        driver.Frame(Effect, 0.5);

        // Act
        using var image = view.RenderToImage(new Size(200, 120), Scale);
        using var bitmap = SKBitmap.FromImage(image);

        // Assert
        Assert.Equal(Critical, GetPixel(bitmap, 2, 60));
        Assert.True(driver.IsRunning(Effect));
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static int GetMaxAlpha(SKBitmap bitmap, ChartRect bounds)
    {
        var max = 0;
        for (var y = (int)(bounds.Y * Scale); y < (int)(bounds.Bottom * Scale); y++)
        {
            for (var x = (int)(bounds.X * Scale); x < (int)(bounds.Right * Scale); x++)
            {
                max = Math.Max(max, bitmap.GetPixel(x, y).Alpha);
            }
        }

        return max;
    }

    private static void AssertColor(SKColor expected, SKColor actual)
    {
        Assert.InRange(actual.Red, expected.Red - 2, expected.Red + 2);
        Assert.InRange(actual.Green, expected.Green - 2, expected.Green + 2);
        Assert.InRange(actual.Blue, expected.Blue - 2, expected.Blue + 2);
        Assert.Equal(expected.Alpha, actual.Alpha);
    }

    private static (double X, double Y) LastPoint(TestClamChartView view)
    {
        var layout = view.ChartLayout;
        var geometry = layout.Geometries[0];
        var last = geometry.Count - 1;
        return (layout.XAxis.ToPixel(geometry.X[last]), layout.YAxis.ToPixel(geometry.End[last]));
    }

    private static (double X, double Y) LastPoint(TestClamSparklineView view)
    {
        var layout = view.ChartLayout;
        return (layout.ToX(layout.LastIndex, layout.Scale), layout.ToY(2, layout.Scale));
    }

    private static long MeasureFrame(ChartViewBase view, SKCanvas canvas, float width, float height)
    {
        canvas.Save();
        canvas.Scale(Scale);
        view.Render(canvas, width, height);
        view.Render(canvas, width, height);
        var before = GC.GetAllocatedBytesForCurrentThread();
        view.Render(canvas, width, height);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }

    private static long MeasureEffectFrames(ChartViewBase view, TestAnimationDriver driver, SKCanvas canvas, float width, float height)
    {
        canvas.Save();
        canvas.Scale(Scale);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 1; i <= 4; i++)
        {
            driver.Frame(Effect, i * 0.2);
            view.Render(canvas, width, height);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }

    private static TestClamChartView Glowing(double radius, int count = 2)
    {
        var view = new TestClamChartView { YAxis = new NumericAxis { GridLinesVisible = false } };
        view.Series.Add(new LineSeries { ItemsSource = Enumerable.Repeat(5d, count).ToList(), Color = Colors.Lime, MarkerMode = ChartMarkerMode.None, GlowRadius = radius });
        return view;
    }

    private static TestClamChartView Live(bool visible, TestAnimationDriver? driver = null)
    {
        var view = new TestClamChartView { YAxis = new NumericAxis { GridLinesVisible = false }, EntranceAnimation = ChartEntranceAnimation.None };
        if (driver is not null)
        {
            view.AnimationDriver = driver;
        }

        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 2, 4, 3 }, Color = Colors.Lime, MarkerMode = ChartMarkerMode.None, LiveIndicatorVisible = visible });
        return view;
    }

    private static TestClamSparklineView Spark(bool visible, TestAnimationDriver? driver = null)
    {
        var view = new TestClamSparklineView { Values = [1, 3, 2], Color = Colors.Lime, Markers = ChartSparklineMarkers.None, LiveIndicatorVisible = visible };
        if (driver is not null)
        {
            view.AnimationDriver = driver;
        }

        return view;
    }

    private static TestClamStatView Alert(double value, ChartStatColorMode mode, TestAnimationDriver driver)
    {
        var view = new TestClamStatView { AnimationDriver = driver, Value = value, Title = "Sales", EntranceAnimation = ChartEntranceAnimation.None, ColorMode = mode, AlertAnimationEnabled = true };
        view.Thresholds.Add(new ChartThreshold { Value = 90, Level = ChartThresholdLevel.Critical });
        view.OnAttached();
        return view;
    }
}
