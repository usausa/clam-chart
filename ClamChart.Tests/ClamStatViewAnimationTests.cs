namespace ClamChart.Tests;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamStatViewAnimationTests
{
    private const string Value = "ClamStatViewValue";

    private const string Trend = "ClamStatViewTrend";

    private const float Scale = 2;

    [Fact]
    public void EntranceCountsUpFromZero()
    {
        // Arrange
        using var view = Stat(1234);
        var driver = Driver(view);

        // Act
        using var first = view.Paint(200, 120);
        driver.Frame(Value, 0.5);

        // Assert
        Assert.Equal([(Value, 450u)], driver.Starts);
        Assert.True(view.IsAnimating);
        Assert.True(view.CountUp.IsEntrance);
        Assert.Equal(617, view.CountUp.Current);

        // Act
        driver.Finish(Value);

        // Assert
        Assert.False(view.IsAnimating);
        Assert.Equal(1234, view.CountUp.Current);
    }

    [Fact]
    public void TrendIsRevealedWithTheEntrance()
    {
        // Arrange
        using var view = Stat(3);
        view.TrendValues = [1, 2, 3];
        var driver = Driver(view);

        // Act
        using var first = view.Paint(200, 120);

        // Assert
        Assert.Contains((Value, 450u), driver.Starts);
        Assert.Contains((Trend, 450u), driver.Starts);
        Assert.Equal(ChartEntranceAnimation.Reveal, view.TrendInterpolator.Entrance);
    }

    [Fact]
    public void UpdatesCountFromTheShownValue()
    {
        // Arrange
        using var view = Quiet(100);
        var driver = Driver(view);
        using var first = view.Paint(200, 120);

        // Act
        view.Value = 200;
        driver.Frame(Value, 0.5);
        var half = view.CountUp.Current;
        view.Value = 100;
        var restarted = view.CountUp.Current;
        driver.Frame(Value, 0.5);

        // Assert
        Assert.Equal(150, half);
        Assert.Equal(150, restarted);
        Assert.Equal(125, view.CountUp.Current);
        Assert.False(view.CountUp.IsEntrance);
    }

    [Fact]
    public void ChangesDuringTheEntranceKeepCounting()
    {
        // Arrange
        using var view = Stat(100);
        var driver = Driver(view);
        using var first = view.Paint(200, 120);
        driver.Frame(Value, 0.5);

        // Act
        view.Value = 300;

        // Assert
        Assert.True(view.CountUp.IsEntrance);
        Assert.Equal(150, view.CountUp.Current);
        Assert.Single(driver.Starts);
    }

    [Fact]
    public void EntranceWaitsForAValue()
    {
        // Arrange
        using var view = Stat(Double.NaN);
        var driver = Driver(view);
        using var first = view.Paint(200, 120);

        // Act
        view.Value = 80;
        using var second = view.Paint(200, 120);
        driver.Frame(Value, 0.25);

        // Assert
        Assert.True(view.CountUp.IsEntrance);
        Assert.Equal(20, view.CountUp.Current);
    }

    [Fact]
    public void NoCountingWithoutAnimations()
    {
        // Arrange
        using var disabled = Stat(1234);
        disabled.AnimationEnabled = false;
        using var noUpdate = Quiet(10);
        noUpdate.UpdateAnimationEnabled = false;

        // Act
        using var first = disabled.Paint(200, 120);
        using var second = noUpdate.Paint(200, 120);
        noUpdate.Value = 20;

        // Assert
        Assert.Empty(Driver(disabled).Starts);
        Assert.Equal(1234, disabled.CountUp.Current);
        Assert.Empty(Driver(noUpdate).Starts);
        Assert.Equal(20, noUpdate.CountUp.Current);
    }

    [Fact]
    public void CompletingShowsTheFinalValue()
    {
        // Arrange
        using var view = Stat(1234);
        var driver = Driver(view);
        using var first = view.Paint(200, 120);
        driver.Frame(Value, 0.5);

        // Act
        view.CompleteAnimations();

        // Assert
        Assert.False(view.IsAnimating);
        Assert.Equal(1234, view.CountUp.Current);
    }

    [Fact]
    public void DeltaFadesInAtTheEndOfTheEntrance()
    {
        // Arrange
        using var view = Stat(110);
        view.PreviousValue = 100;
        var driver = Driver(view);
        using var first = view.Paint(200, 120);

        // Act
        driver.Frame(Value, 0.6);
        var hidden = view.CountUp.OverlayAlpha;
        driver.Frame(Value, 0.85);
        var half = view.CountUp.OverlayAlpha;
        driver.Finish(Value);

        // Assert
        Assert.Equal(0, hidden);
        Assert.Equal(0.5, half, 9);
        Assert.Equal(1, view.CountUp.OverlayAlpha);
    }

    [Fact]
    public void FormattedValuesCountWithoutRounding()
    {
        // Arrange
        using var view = Stat(1);
        view.ValueFormat = "P1";
        var driver = Driver(view);

        // Act
        using var first = view.Paint(200, 120);
        driver.Frame(Value, 0.123);

        // Assert
        Assert.Equal(0.123, view.CountUp.Current, 9);
    }

    [Fact]
    public void PlayingTheEntranceAgainCountsFromZero()
    {
        // Arrange
        using var view = Quiet(50);
        var driver = Driver(view);
        using var first = view.Paint(200, 120);

        // Act
        view.EntranceAnimation = ChartEntranceAnimation.Auto;
        view.PlayEntranceAnimation();
        driver.Frame(Value, 0.5);

        // Assert
        Assert.True(view.CountUp.IsEntrance);
        Assert.Equal(25, view.CountUp.Current);
    }

    [Fact]
    public void CountingFramesDoNotAllocate()
    {
        // Arrange
        using var view = Quiet(1000);
        view.Unit = "JPY";
        view.PreviousValue = 900;
        view.TrendValues = new ObservableCollection<double> { 1, 3, 2, 4 };
        view.TrendKind = ChartSparklineKind.Bar;
        var driver = Driver(view);
        using var bitmap = view.Paint(200, 120);
        using var surface = SKSurface.Create(new SKImageInfo(400, 240));
        view.Value = 2000;
        MeasureFrames(view, driver, surface.Canvas);

        // Act
        var allocated = MeasureFrames(view, driver, surface.Canvas);

        // Assert
        Assert.True(view.CountUp.IsRunning);
        Assert.Equal(0, allocated);
    }

    private static TestClamStatView Stat(double value)
    {
        var view = new TestClamStatView { AnimationDriver = new TestAnimationDriver(), Value = value, Title = "Sales", AnimationEasing = Easing.Linear };
        view.OnAttached();
        return view;
    }

    private static TestClamStatView Quiet(double value)
    {
        var view = Stat(value);
        view.EntranceAnimation = ChartEntranceAnimation.None;
        return view;
    }

    private static TestAnimationDriver Driver(TestClamStatView view) => (TestAnimationDriver)view.AnimationDriver;

    private static long MeasureFrames(TestClamStatView view, TestAnimationDriver driver, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        var allocated = ChartAllocation.Measure(() =>
        {
            for (var i = 1; i <= 4; i++)
            {
                driver.Frame(Value, i * 0.2);
                view.Render(canvas, 200, 120);
            }
        });
        canvas.Restore();
        return allocated;
    }
}
