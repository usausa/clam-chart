namespace ClamChart.Tests;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamRadarChartViewAnimationTests
{
    private const string Data = "ClamRadarChartViewData";

    private const string Selection = "ClamRadarChartViewSelection";

    private const float Scale = 2;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    [Fact]
    public void FirstDataGrowsFromTheCenter()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 10, 10, 10));
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var radius = layout.Radius;
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(ChartEntranceAnimation.Grow, view.Interpolator.Entrance);
        Assert.Equal(Blue, GetPixel(middle, layout.CenterX + (radius * 0.25), layout.CenterY - (radius * 0.25)));
        Assert.NotEqual(Blue, GetPixel(middle, layout.CenterX + (radius * 0.5), layout.CenterY - (radius * 0.5)));

        // Act
        driver.Finish(Data);
        using var last = view.Paint(300, 300);

        // Assert
        Assert.False(view.IsAnimating);
        Assert.Equal(Blue, GetPixel(last, layout.CenterX + (radius * 0.5), layout.CenterY - (radius * 0.5)));
    }

    [Fact]
    public void FadeRaisesTheOpacity()
    {
        // Arrange
        using var view = Chart(Series("First", 9, 9, 9, 9));
        view.Maximum = 10;
        view.EntranceAnimation = ChartEntranceAnimation.Fade;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.InRange(GetPixel(middle, layout.CenterX + (layout.Radius * 0.45), layout.CenterY - (layout.Radius * 0.45)).Alpha, 120, 140);
    }

    [Fact]
    public void RevealSweepsClockwiseFromTheTop()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 10, 10, 10));
        view.EntranceAnimation = ChartEntranceAnimation.Reveal;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 300);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var half = layout.Radius * 0.5;
        Assert.Equal(Blue, GetPixel(middle, layout.CenterX + half, layout.CenterY + half));
        Assert.NotEqual(Blue, GetPixel(middle, layout.CenterX - half, layout.CenterY + half));
    }

    [Fact]
    public void UpdateMovesTheVerticesAndTheLevels()
    {
        // Arrange
        var series = Series("First", 10, 10, 10, 10);
        using var view = Quiet(series);
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        series.ItemsSource = Entries(20, 10, 10, 10);
        using var second = view.Paint(300, 300);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(15, view.Interpolator.GetValue(0, 0));
        Assert.Equal(15, view.Interpolator.Scale.Maximum);
        Assert.Equal(20, view.ChartLayout.Scale.Maximum);
    }

    [Fact]
    public void NewAxesReplaceTheShapeWithoutMorphing()
    {
        // Arrange
        var series = Series("First", 10, 10, 10, 10);
        using var view = Quiet(series);
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        series.ItemsSource = Entries(10, 10, 10, 10, 10);
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.False(view.Interpolator.IsMorphing);
        Assert.Equal(5, view.ChartLayout.AxisCount);
    }

    [Fact]
    public void HidingASeriesFadesItOut()
    {
        // Arrange
        var second = Series("Second", 20, 20, 20, 20);
        using var view = Quiet(Series("First", 10, 10, 10, 10), second);
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        second.IsVisible = false;
        using var hidden = view.Paint(300, 300);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal(0.5, view.Interpolator.GetOpacity(1));
        Assert.Equal(1, view.Interpolator.GetOpacity(0));
        Assert.Equal(15, view.Interpolator.Scale.Maximum);

        // Act
        driver.Finish(Data);

        // Assert
        Assert.Equal(0, view.Interpolator.GetOpacity(1));
    }

    [Fact]
    public void SelectionChangesOverAShortAnimation()
    {
        // Arrange
        var second = Series("Second", 5, 5, 5, 5);
        using var view = Quiet(Series("First", 10, 10, 10, 10), second);
        view.SelectionMode = ChartSelectionMode.Item;
        var driver = Driver(view);
        using var first = view.Paint(300, 300);

        // Act
        view.SelectedSeries = second;
        view.SelectedIndex = 0;

        // Assert
        Assert.Equal([(Selection, 150u)], driver.Starts);
        Assert.True(view.IsAnimating);

        // Act
        driver.Finish(Selection);

        // Assert
        Assert.False(view.IsAnimating);
    }

    [Fact]
    public void FramesRebuildOnlyThePathsWhileMoving()
    {
        // Arrange
        var series = Series("First", 10, 10, 10, 10);
        using var view = Quiet(series, Series("Second", 5, 5, 5, 5));
        var driver = Driver(view);
        using var first = view.Paint(300, 300);
        using var surface = SKSurface.Create(new SKImageInfo(600, 600));
        var idle = MeasureFrames(view, driver, surface.Canvas, false);
        series.ItemsSource = Entries(20, 10, 10, 10);
        using var second = view.Paint(300, 300);
        MeasureFrames(view, driver, surface.Canvas, true);

        // Act
        series.ItemsSource = Entries(10, 10, 10, 10);
        using var third = view.Paint(300, 300);
        var moving = MeasureFrames(view, driver, surface.Canvas, true);

        // Assert
        Assert.Equal(0, idle);
        Assert.InRange(moving, 1, 4 * 3 * 200);
    }

    private static TestClamRadarChartView Chart(params RadarSeries[] series)
    {
        var view = new TestClamRadarChartView { AnimationDriver = new TestAnimationDriver(), AnimationEasing = Easing.Linear };
        foreach (var item in series)
        {
            view.Series.Add(item);
        }

        view.OnAttached();
        return view;
    }

    private static TestClamRadarChartView Quiet(params RadarSeries[] series)
    {
        var view = Chart(series);
        view.EntranceAnimation = ChartEntranceAnimation.None;
        return view;
    }

    private static TestAnimationDriver Driver(TestClamRadarChartView view) => (TestAnimationDriver)view.AnimationDriver;

    private static RadarSeries Series(string title, params double[] values) => new() { Title = title, ItemsSource = Entries(values) };

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry(((char)('A' + index)).ToString(), value))];

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static long MeasureFrames(TestClamRadarChartView view, TestAnimationDriver driver, SKCanvas canvas, bool moving)
    {
        canvas.Save();
        canvas.Scale(Scale);
        view.Render(canvas, 300, 300);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 1; i <= 4; i++)
        {
            if (moving)
            {
                driver.Frame(Data, i * 0.2);
            }

            view.Render(canvas, 300, 300);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }
}
