namespace ClamChart.Tests;

using ClamChart.Animation;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamBarGaugeViewAnimationTests
{
    private const string Data = "ClamBarGaugeViewData";

    private const int Precision = 9;

    [Fact]
    public void FirstDataGrowsEachRowFortyMillisecondsApart()
    {
        // Arrange
        using var view = Chart(Entries(100, 100, 100));
        view.AnimationEasing = Easing.Linear;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 120);
        driver.Frame(Data, 0.5);

        // Assert
        var stagger = 80d / 450;
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(ChartEntranceAnimation.Grow, view.Interpolator.Entrance);
        Assert.Equal(ChartProgress.ForElement(0.5, 0, 3, stagger), view.Interpolator.GetRatio(0), Precision);
        Assert.Equal(ChartProgress.ForElement(0.5, 2, 3, stagger), view.Interpolator.GetRatio(2), Precision);
        Assert.True(view.Interpolator.GetRatio(0) > view.Interpolator.GetRatio(2));

        // Act
        driver.Finish(Data);

        // Assert
        Assert.False(view.IsAnimating);
        Assert.Equal(1, view.Interpolator.GetRatio(2));
    }

    [Fact]
    public void ManyRowsKeepTheStaggerWithinThirtyPercent()
    {
        // Arrange
        using var view = Chart(Entries(100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100));
        view.AnimationEasing = Easing.Linear;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 600);
        driver.Frame(Data, 0.3);

        // Assert
        Assert.Equal(0, view.Interpolator.GetRatio(14), Precision);
        driver.Frame(Data, 1);
        Assert.Equal(1, view.Interpolator.GetRatio(14), Precision);
    }

    [Fact]
    public void DataChangesMorphFromTheShownRatio()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(50));
        using var view = Quiet(entries);
        var driver = Driver(view);
        using var first = view.Paint(300, 60);

        // Act
        entries[0] = new ChartEntry("C0", 100);
        using var second = view.Paint(300, 60);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal(0.75, view.Interpolator.GetRatio(0), Precision);

        // Act
        entries[0] = new ChartEntry("C0", 0);
        using var third = view.Paint(300, 60);

        // Assert
        Assert.Equal(0.75, view.Interpolator.GetRatio(0), Precision);
        driver.Frame(Data, 1);
        Assert.Equal(0, view.Interpolator.GetRatio(0), Precision);
    }

    [Fact]
    public void AddedRowsGrowAndRemovedRowsShrinkAway()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(50, 80));
        using var view = Quiet(entries);
        var driver = Driver(view);
        using var first = view.Paint(300, 120);

        // Act
        entries.RemoveAt(0);
        entries.Add(new ChartEntry("C2", 30));
        using var second = view.Paint(300, 120);
        driver.Frame(Data, 0.5);

        // Assert
        var interpolator = view.Interpolator;
        Assert.Equal(3, interpolator.Count);
        Assert.Equal(0.5, interpolator.GetSlot(0), Precision);
        Assert.Equal(0.8, interpolator.GetRatio(0), Precision);
        Assert.Equal(0.15, interpolator.GetRatio(1), Precision);
        Assert.Equal(0.5, interpolator.GetAlpha(1), Precision);
        Assert.Equal(0.25, interpolator.GetRatio(2), Precision);
        Assert.Equal(0.5, interpolator.GetAlpha(2), Precision);

        // Act
        driver.Finish(Data);

        // Assert
        Assert.Equal(2, interpolator.Count);
    }

    [Fact]
    public void UpdateAnimationCanBeTurnedOff()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(50));
        using var view = Quiet(entries);
        view.UpdateAnimationEnabled = false;
        var driver = Driver(view);
        using var first = view.Paint(300, 60);

        // Act
        entries[0] = new ChartEntry("C0", 100);
        using var second = view.Paint(300, 60);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.False(view.Interpolator.IsMorphing);
        Assert.Equal(1, view.Interpolator.GetRatio(0));
    }

    [Fact]
    public void DisabledAnimationDrawsTheFinalStateAtOnce()
    {
        // Arrange
        using var view = Chart(Entries(50));
        view.AnimationEnabled = false;
        var driver = Driver(view);

        // Act
        using var bitmap = view.Paint(300, 60);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.False(view.Interpolator.IsEntrance);
        Assert.Equal(0.5, view.Interpolator.GetRatio(0));
    }

    [Fact]
    public void CompleteAnimationsShowsTheFinalState()
    {
        // Arrange
        using var view = Chart(Entries(50));
        var driver = Driver(view);
        using var first = view.Paint(300, 60);

        // Act
        view.CompleteAnimations();

        // Assert
        Assert.False(view.IsAnimating);
        Assert.False(view.Interpolator.IsEntrance);
        Assert.Equal([Data], driver.Aborts);
    }

    [Fact]
    public void ReplacingTheItemsWithAnotherCountReplaysTheEntrance()
    {
        // Arrange
        using var view = Chart(Entries(50));
        var driver = Driver(view);
        using var first = view.Paint(300, 120);
        driver.Finish(Data);

        // Act
        view.ItemsSource = Entries(10, 20);
        using var second = view.Paint(300, 120);

        // Assert
        Assert.Equal(2, driver.Starts.Count);
        Assert.True(view.Interpolator.IsEntrance);
    }

    [Fact]
    public void PlayEntranceAnimationStartsItAgain()
    {
        // Arrange
        using var view = Chart(Entries(50));
        var driver = Driver(view);
        using var first = view.Paint(300, 60);
        driver.Finish(Data);

        // Act
        view.PlayEntranceAnimation();

        // Assert
        Assert.Equal(2, driver.Starts.Count);
        Assert.True(view.Interpolator.IsEntrance);
    }

    [Theory]
    [InlineData(ChartBarGaugeMode.Basic, ChartFillMode.Solid)]
    [InlineData(ChartBarGaugeMode.Basic, ChartFillMode.Gradient)]
    [InlineData(ChartBarGaugeMode.Segmented, ChartFillMode.Solid)]
    public void AnimationFramesDoNotAllocate(ChartBarGaugeMode mode, ChartFillMode fill)
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>([new ChartEntry("Tokyo", 82, Target: 90), new ChartEntry("Osaka", 58), new ChartEntry("Nagoya", 31)]);
        using var view = Quiet(entries);
        view.DisplayMode = mode;
        view.FillMode = fill;
        view.ValueMode = ChartBarGaugeValueMode.Value;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning });
        var driver = Driver(view);
        using var bitmap = view.Paint(360, 120);
        using var surface = SKSurface.Create(new SKImageInfo(720, 240));
        entries[0] = new ChartEntry("Tokyo", 40, Target: 90);
        entries.RemoveAt(2);
        using var changed = view.Paint(360, 120);
        MeasureFrames(view, driver, surface.Canvas);

        // Act
        var allocated = MeasureFrames(view, driver, surface.Canvas);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal(0, allocated);
    }

    private static TestClamBarGaugeView Chart(IEnumerable entries)
    {
        var view = new TestClamBarGaugeView { AnimationDriver = new TestAnimationDriver(), ItemsSource = entries };
        view.OnAttached();
        return view;
    }

    private static TestClamBarGaugeView Quiet(IEnumerable entries)
    {
        var view = Chart(entries);
        view.EntranceAnimation = ChartEntranceAnimation.None;
        view.AnimationEasing = Easing.Linear;
        return view;
    }

    private static TestAnimationDriver Driver(TestClamBarGaugeView view) => (TestAnimationDriver)view.AnimationDriver;

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static long MeasureFrames(TestClamBarGaugeView view, TestAnimationDriver driver, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(2);
        var allocated = ChartAllocation.Measure(() =>
        {
            for (var i = 1; i <= 4; i++)
            {
                driver.Frame(Data, i * 0.2);
                view.Render(canvas, 360, 120);
            }
        });
        canvas.Restore();
        return allocated;
    }
}
