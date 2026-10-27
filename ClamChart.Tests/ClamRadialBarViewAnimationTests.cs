namespace ClamChart.Tests;

using ClamChart.Animation;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamRadialBarViewAnimationTests
{
    private const string Data = "ClamRadialBarViewData";

    private const string Center = "ClamRadialBarViewCenter";

    private const int Precision = 9;

    [Fact]
    public void FirstDataGrowsEachRingFortyMillisecondsApart()
    {
        // Arrange
        using var view = Chart(Entries(100, 100, 100));
        view.AnimationEasing = Easing.Linear;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.5);

        // Assert
        var stagger = 80d / 450;
        Assert.Contains((Data, 450u), driver.Starts);
        Assert.Equal(ChartEntranceAnimation.Grow, view.Interpolator.Entrance);
        Assert.Equal(ChartProgress.ForElement(0.5, 0, 3, stagger), view.Interpolator.GetRatio(0), Precision);
        Assert.Equal(ChartProgress.ForElement(0.5, 2, 3, stagger), view.Interpolator.GetRatio(2), Precision);

        // Act
        driver.Finish(Data);

        // Assert
        Assert.Equal(1, view.Interpolator.GetRatio(2));
    }

    [Fact]
    public void TheCenterCountsUpDuringTheEntrance()
    {
        // Arrange
        using var view = Chart(Entries(80, 40));
        view.AnimationEasing = Easing.Linear;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Center, 0.5);

        // Assert
        Assert.True(view.CenterCount.IsRunning);
        Assert.Equal(0.4, view.CenterCount.Current, Precision);
        driver.Finish(Center);
        Assert.Equal(0.8, view.CenterCount.Current, Precision);
    }

    [Fact]
    public void DataChangesMorphFromTheShownRatio()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(50));
        using var view = Quiet(entries);
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        entries[0] = new ChartEntry("C0", 100);
        using var second = view.Paint(300, 300);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal(0.75, view.Interpolator.GetRatio(0), Precision);
        Assert.True(view.CenterCount.IsRunning);
    }

    [Fact]
    public void RingsMoveInwardAndOutwardWhenRingsComeAndGo()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(50, 80));
        using var view = Quiet(entries);
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        entries.RemoveAt(0);
        using var second = view.Paint(300, 300);
        driver.Frame(Data, 0.5);

        // Assert
        var interpolator = view.Interpolator;
        Assert.Equal(2, interpolator.Count);
        Assert.Equal(0.5, interpolator.GetSlot(0), Precision);
        Assert.Equal(0.5, interpolator.GetAlpha(1), Precision);
        Assert.Equal(0.25, interpolator.GetRatio(1), Precision);

        // Act
        driver.Finish(Data);

        // Assert
        Assert.Equal(1, interpolator.Count);
    }

    [Fact]
    public void SelectingARingFadesTheOthersOver150Milliseconds()
    {
        // Arrange
        using var view = Quiet(Entries(50, 80));
        view.SelectionMode = ChartSelectionMode.Item;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        view.SelectedIndex = 0;

        // Assert
        Assert.Contains(("ClamRadialBarViewSelection", 150u), driver.Starts);
    }

    [Fact]
    public void UpdateAnimationCanBeTurnedOff()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(50));
        using var view = Quiet(entries);
        view.UpdateAnimationEnabled = false;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        entries[0] = new ChartEntry("C0", 100);
        using var second = view.Paint(300, 300);

        // Assert
        Assert.DoesNotContain(driver.Starts, static x => x.Name == Data);
        Assert.Equal(1, view.Interpolator.GetRatio(0));
    }

    [Fact]
    public void ReplacingTheItemsWithAnotherCountReplaysTheEntrance()
    {
        // Arrange
        using var view = Chart(Entries(50));
        var driver = Driver(view);
        using var first = view.Paint(300, 300);
        driver.Finish(Data);

        // Act
        view.ItemsSource = Entries(10, 20);
        using var second = view.Paint(300, 300);

        // Assert
        Assert.True(view.Interpolator.IsEntrance);
    }

    [Theory]
    [InlineData(ChartRadialBarMode.Basic, ChartFillMode.Solid)]
    [InlineData(ChartRadialBarMode.Basic, ChartFillMode.Gradient)]
    [InlineData(ChartRadialBarMode.Segmented, ChartFillMode.Solid)]
    public void AnimationFramesDoNotAllocate(ChartRadialBarMode mode, ChartFillMode fill)
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>([new ChartEntry("Move", 82), new ChartEntry("Exercise", 58), new ChartEntry("Stand", 31)]);
        using var view = Quiet(entries);
        view.DisplayMode = mode;
        view.FillMode = fill;
        view.CapStyle = ChartCapStyle.Round;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning });
        var driver = Driver(view);
        using var bitmap = view.Paint(300, 300);
        using var surface = SKSurface.Create(new SKImageInfo(600, 600));
        entries[0] = new ChartEntry("Move", 40);
        entries.RemoveAt(2);
        using var changed = view.Paint(300, 300);
        MeasureFrames(view, driver, surface.Canvas);

        // Act
        var allocated = MeasureFrames(view, driver, surface.Canvas);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        Assert.True(view.CenterCount.IsRunning);
        Assert.Equal(0, allocated);
    }

    private static TestClamRadialBarView Chart(IEnumerable entries)
    {
        var view = new TestClamRadialBarView { AnimationDriver = new TestAnimationDriver(), ItemsSource = entries };
        view.OnAttached();
        return view;
    }

    private static TestClamRadialBarView Quiet(IEnumerable entries)
    {
        var view = Chart(entries);
        view.EntranceAnimation = ChartEntranceAnimation.None;
        view.AnimationEasing = Easing.Linear;
        return view;
    }

    private static TestAnimationDriver Driver(TestClamRadialBarView view) => (TestAnimationDriver)view.AnimationDriver;

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static long MeasureFrames(TestClamRadialBarView view, TestAnimationDriver driver, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(2);
        var allocated = ChartAllocation.Measure(() =>
        {
            for (var i = 1; i <= 4; i++)
            {
                driver.Frame(Data, i * 0.2);
                if (driver.IsRunning(Center))
                {
                    driver.Frame(Center, i * 0.2);
                }

                view.Render(canvas, 300, 300);
            }
        });
        canvas.Restore();
        return allocated;
    }
}
