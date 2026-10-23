namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamPieChartViewAnimationTests
{
    private const string Data = "ClamPieChartViewData";

    private const string Selection = "ClamPieChartViewSelection";

    private const string Center = "ClamPieChartViewCenter";

    private const float Scale = 2;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    private static readonly SKColor Orange = new(0xEB, 0x68, 0x34);

    [Fact]
    public void FirstDataGrowsFromTheStartAngle()
    {
        // Arrange
        using var view = Chart(Entries(1, 1));
        view.AnimationEasing = Easing.Linear;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.True(view.IsAnimating);
        Assert.Equal(ChartEntranceAnimation.Grow, view.Interpolator.Entrance);
        Assert.Equal(Blue, GetPixel(middle, Ring(layout, 45)));
        Assert.Equal(Orange, GetPixel(middle, Ring(layout, 135)));
        Assert.Equal(0, GetPixel(middle, Ring(layout, 270)).Alpha);

        // Act
        driver.Finish(Data);
        using var last = view.Paint(300, 300);

        // Assert
        Assert.False(view.IsAnimating);
        Assert.Equal(Orange, GetPixel(last, Ring(layout, 270)));
    }

    [Fact]
    public void RevealDrawsTheSlicesWhereTheyEnd()
    {
        // Arrange
        using var view = Chart(Entries(1, 1));
        view.AnimationEasing = Easing.Linear;
        view.EntranceAnimation = ChartEntranceAnimation.Reveal;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.75);
        using var middle = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Blue, GetPixel(middle, Ring(layout, 90)));
        Assert.Equal(Orange, GetPixel(middle, Ring(layout, 200)));
        Assert.Equal(0, GetPixel(middle, Ring(layout, 300)).Alpha);
    }

    [Fact]
    public void FadeRaisesTheOpacity()
    {
        // Arrange
        using var view = Chart(Entries(1, 1));
        view.AnimationEasing = Easing.Linear;
        view.EntranceAnimation = ChartEntranceAnimation.Fade;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 300);

        // Assert
        Assert.InRange(GetPixel(middle, Ring(view.ChartLayout, 90)).Alpha, 125, 130);
    }

    [Fact]
    public void TheCenterCountsUpFromTheStartOfTheEntrance()
    {
        // Arrange
        using var view = Chart(Entries(10, 10));
        view.AnimationEasing = Easing.Linear;
        view.CenterValueMode = ChartCenterValueMode.Auto;
        view.CenterLabel = "Total";
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.6);
        driver.Frame(Center, 0.6);
        using var middle = view.Paint(300, 300);
        var counted = view.CenterCount.Current;
        driver.Finish(Data);
        driver.Finish(Center);
        using var last = view.Paint(300, 300);

        // Assert
        var hole = Hole(view.ChartLayout);
        Assert.Equal([(Data, 450u), (Center, 450u)], driver.Starts);
        Assert.Equal(12, counted);
        Assert.True(CountInk(first, hole) > 20);
        Assert.True(CountInk(middle, hole) > 20);
        Assert.True(CountInk(last, hole) > 20);
        Assert.False(view.CenterCount.IsRunning);
        Assert.Equal(20, view.CenterCount.Current);
    }

    [Fact]
    public void DataChangesCountTheCenterFromTheShownValue()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(10, 10));
        using var view = Quiet(entries);
        view.CenterValueMode = ChartCenterValueMode.Auto;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        entries[0] = new ChartEntry("C0", 30);
        using var second = view.Paint(300, 300);
        driver.Frame(Center, 0.5);
        var half = view.CenterCount.Current;
        entries[1] = new ChartEntry("C1", 30);
        using var third = view.Paint(300, 300);

        // Assert
        Assert.Equal(30, half);
        Assert.Equal("60", view.ChartLayout.Center.Value);
        Assert.True(view.CenterCount.IsRunning);
        Assert.Equal(30, view.CenterCount.Current);
    }

    [Fact]
    public void TheCenterDoesNotCountWithoutUpdateAnimations()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(10, 10));
        using var view = Quiet(entries);
        view.CenterValueMode = ChartCenterValueMode.Auto;
        view.UpdateAnimationEnabled = false;
        using var first = view.Paint(300, 300);

        // Act
        entries[0] = new ChartEntry("C0", 30);
        using var second = view.Paint(300, 300);

        // Assert
        Assert.False(view.CenterCount.IsRunning);
        Assert.Equal(40, view.CenterCount.Current);
    }

    [Fact]
    public void DataChangesMorphTheFractionsFromTheShownState()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(1, 1));
        using var view = Quiet(entries);
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        entries[0] = new ChartEntry("C0", 3);
        using var second = view.Paint(300, 300);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal(0.625, view.Interpolator.GetSlice(0).End, 9);

        // Act
        entries[0] = new ChartEntry("C0", 1);
        using var third = view.Paint(300, 300);

        // Assert
        Assert.Equal(0.625, view.Interpolator.GetSlice(0).End, 9);
        driver.Frame(Data, 1);
        Assert.Equal(0.5, view.Interpolator.GetSlice(0).End, 9);
    }

    [Fact]
    public void AppendedSliceGrowsFromTheEndOfTheSweep()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(1, 1));
        using var view = Quiet(entries);
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        entries.Add(new ChartEntry("C2", 2));
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(3, view.Interpolator.Count);
        Assert.Equal((1d, 1d), view.Interpolator.GetSlice(2));
        driver.Frame(Data, 1);
        Assert.Equal(0.5, view.Interpolator.GetSlice(2).Start, 9);
    }

    [Fact]
    public void UpdateAnimationCanBeTurnedOff()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(1, 1));
        using var view = Quiet(entries);
        view.UpdateAnimationEnabled = false;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        entries[0] = new ChartEntry("C0", 3);
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.False(view.Interpolator.IsMorphing);
        Assert.Equal(0.75, view.Interpolator.GetSlice(0).End, 9);
    }

    [Fact]
    public void HiddenSliceShrinksEvenWithoutUpdateAnimations()
    {
        // Arrange
        using var view = Quiet(Entries(1, 1));
        view.UpdateAnimationEnabled = false;
        view.LegendVisible = true;
        view.LegendToggleEnabled = true;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);
        var legend = view.ChartLayout.Legend.Items[0].Bounds;
        var point = new Point(legend.CenterX, legend.CenterY);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
        using var second = view.Paint(300, 300);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal((0d, 0.25), view.Interpolator.GetSlice(0));
        Assert.Equal((0.25, 1d), view.Interpolator.GetSlice(1));

        // Act
        driver.Finish(Data);

        // Assert
        Assert.Equal(1, view.Interpolator.Count);
        Assert.Equal((0d, 1d), view.Interpolator.GetSlice(0));
    }

    [Fact]
    public void SelectedSliceMovesOutOver150Milliseconds()
    {
        // Arrange
        using var view = Quiet(Entries(1, 1));
        view.SelectionMode = ChartSelectionMode.Item;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);
        var layout = view.ChartLayout;

        // Act
        view.SelectedIndex = 0;
        driver.Frame(Selection, 0.5);
        using var middle = view.Paint(300, 300);

        // Assert
        Assert.Equal([(Selection, 150u)], driver.Starts);
        Assert.Equal(Blue, GetPixel(middle, Polar(layout, 90, layout.OuterRadius + 1.5)));
        Assert.Equal(0, GetPixel(middle, Polar(layout, 90, layout.OuterRadius + 4.5)).Alpha);
        Assert.InRange(GetPixel(middle, Ring(layout, 270)).Alpha, 170, 175);

        // Act
        driver.Finish(Selection);
        using var last = view.Paint(300, 300);

        // Assert
        Assert.Equal(Blue, GetPixel(last, Polar(layout, 90, layout.OuterRadius + 4.5)));
    }

    [Fact]
    public void SelectionFromDataChangesDoesNotAnimate()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(1, 1));
        using var view = Quiet(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);
        view.SelectedItem = entries[1];
        driver.Finish(Selection);
        driver.Starts.Clear();

        // Act
        entries.Insert(0, new ChartEntry("New", 1));
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(2, view.SelectedIndex);
        Assert.DoesNotContain(driver.Starts, static x => x.Name == Selection);
    }

    [Fact]
    public void DisabledAnimationDrawsTheFinalStateAtOnce()
    {
        // Arrange
        using var view = Chart(Entries(1, 1));
        view.AnimationEnabled = false;
        var driver = Driver(view);

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.False(view.Interpolator.IsEntrance);
        Assert.Equal(Orange, GetPixel(bitmap, Ring(view.ChartLayout, 270)));
    }

    [Fact]
    public void CompleteAnimationsShowsTheFinalState()
    {
        // Arrange
        using var view = Chart(Entries(1, 1));
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

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
        using var view = Chart(Entries(1, 1));
        var driver = Driver(view);
        using var first = view.Paint(300, 300);
        driver.Finish(Data);

        // Act
        view.ItemsSource = Entries(1, 2, 3);
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(2, driver.Starts.Count);
        Assert.True(view.Interpolator.IsEntrance);
    }

    [Fact]
    public void PlayEntranceAnimationStartsItAgain()
    {
        // Arrange
        using var view = Chart(Entries(1, 1));
        var driver = Driver(view);
        using var first = view.Paint(300, 300);
        driver.Finish(Data);

        // Act
        view.PlayEntranceAnimation();

        // Assert
        Assert.Equal(2, driver.Starts.Count);
        Assert.True(view.Interpolator.IsEntrance);
    }

    [Fact]
    public void AnimationFramesDoNotAllocate()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(1, 2, 3));
        using var view = Quiet(entries);
        view.LabelsMode = ChartPieLabelsMode.Inside;
        view.CenterValueMode = ChartCenterValueMode.Auto;
        var driver = Driver(view);
        using var bitmap = view.Paint(340, 340);
        using var surface = SKSurface.Create(new SKImageInfo(680, 680));
        entries[0] = new ChartEntry("C0", 4);
        using var changed = view.Paint(340, 340);
        MeasureFrames(view, driver, surface.Canvas);

        // Act
        var allocated = MeasureFrames(view, driver, surface.Canvas);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        Assert.True(view.CenterCount.IsRunning);
        Assert.Equal(0, allocated);
    }

    private static TestClamPieChartView Chart(IEnumerable entries)
    {
        var view = new TestClamPieChartView { AnimationDriver = new TestAnimationDriver(), ItemsSource = entries, CenterValueMode = ChartCenterValueMode.None };
        view.OnAttached();
        return view;
    }

    private static TestClamPieChartView Quiet(IEnumerable entries)
    {
        var view = Chart(entries);
        view.EntranceAnimation = ChartEntranceAnimation.None;
        view.AnimationEasing = Easing.Linear;
        return view;
    }

    private static TestAnimationDriver Driver(TestClamPieChartView view) => (TestAnimationDriver)view.AnimationDriver;

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static SKColor GetPixel(SKBitmap bitmap, (double X, double Y) point) => bitmap.GetPixel((int)(point.X * Scale), (int)(point.Y * Scale));

    private static (double X, double Y) Polar(PieLayout layout, double angle, double radius)
    {
        var (x, y) = PieLayout.GetDirection(angle);
        return (layout.CenterX + (x * radius), layout.CenterY + (y * radius));
    }

    private static (double X, double Y) Ring(PieLayout layout, double angle) => Polar(layout, angle, (layout.OuterRadius + layout.InnerRadius) / 2);

    private static SKRect Hole(PieLayout layout)
    {
        var half = (float)(layout.InnerRadius * 0.7);
        return new SKRect((float)layout.CenterX - half, (float)layout.CenterY - half, (float)layout.CenterX + half, (float)layout.CenterY + half);
    }

    private static int CountInk(SKBitmap bitmap, SKRect bounds)
    {
        var count = 0;
        for (var y = (int)(bounds.Top * Scale); y < (int)(bounds.Bottom * Scale); y++)
        {
            for (var x = (int)(bounds.Left * Scale); x < (int)(bounds.Right * Scale); x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static long MeasureFrames(TestClamPieChartView view, TestAnimationDriver driver, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 1; i <= 4; i++)
        {
            driver.Frame(Data, i * 0.2);
            if (driver.IsRunning(Center))
            {
                driver.Frame(Center, i * 0.2);
            }

            view.Render(canvas, 340, 340);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }
}
