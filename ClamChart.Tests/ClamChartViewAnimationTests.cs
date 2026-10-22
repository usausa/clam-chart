namespace ClamChart.Tests;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamChartViewAnimationTests
{
    private const string Data = "ClamChartViewData";

    private const string Selection = "ClamChartViewSelection";

    private const float Scale = 2;

    [Fact]
    public void FirstDataPlaysTheEntranceAndDrawsTheInterpolatedState()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        var driver = Driver(view);

        // Act
        using var first = view.Paint(360, 240);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(360, 240);

        // Assert
        var bar = GetBarBounds(view, 0, 2);
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.True(view.IsAnimating);
        Assert.True(view.Interpolator.IsEntrance);
        Assert.Equal(0, GetPixel(middle, bar.MidX, bar.Top + 4).Alpha);

        // Act
        driver.Finish(Data);
        using var last = view.Paint(360, 240);

        // Assert
        Assert.False(view.IsAnimating);
        Assert.Equal(255, GetPixel(last, bar.MidX, bar.Top + 4).Alpha);
    }

    [Fact]
    public void RevealDrawsTheLineFromTheStartAndFadesTheLabelsInLast()
    {
        // Arrange
        using var view = Chart(new LineSeries { ItemsSource = Entries(10.3, 10.3, 10.3, 10.3) });
        view.DataLabelsMode = ChartDataLabelsMode.All;
        view.AnimationEasing = Easing.Linear;
        var driver = Driver(view);
        using var first = view.Paint(360, 240);

        // Act
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var y = layout.YAxis.ToPixel(10.3);
        Assert.Equal(255, GetPixel(middle, layout.XAxis.ToPixel(0.5), y).Alpha);
        Assert.Equal(0, GetPixel(middle, layout.XAxis.ToPixel(2.5), y).Alpha);
        Assert.Equal(0, view.Interpolator.OverlayAlpha);

        // Act
        driver.Frame(Data, 0.85);

        // Assert
        Assert.Equal(0.5, view.Interpolator.OverlayAlpha, 9);
    }

    [Fact]
    public void DisabledAnimationsNeverStart()
    {
        // Arrange
        using var disabled = Chart(new BarSeries { ItemsSource = Entries(10, 20) });
        disabled.AnimationEnabled = false;
        using var zero = Chart(new BarSeries { ItemsSource = Entries(10, 20) });
        zero.AnimationDuration = 0;
        using var none = Chart(new BarSeries { ItemsSource = Entries(10, 20) });
        none.EntranceAnimation = ChartEntranceAnimation.None;
        using var detached = Chart(new BarSeries { ItemsSource = Entries(10, 20) });
        Driver(detached).CanStart = false;

        // Act
        Paint(disabled, zero, none, detached);

        // Assert
        Assert.Empty(Driver(disabled).Starts);
        Assert.Empty(Driver(zero).Starts);
        Assert.Empty(Driver(none).Starts);
        Assert.Empty(Driver(detached).Starts);
        Assert.Throws<ArgumentOutOfRangeException>(() => zero.AnimationDuration = -1);
    }

    [Fact]
    public void GlobalSettingTurnsTheAnimationsOff()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20) });
        try
        {
            ClamChartSettings.AnimationEnabled = false;

            // Act
            using var bitmap = view.Paint(360, 240);

            // Assert
            Assert.Empty(Driver(view).Starts);
        }
        finally
        {
            ClamChartSettings.AnimationEnabled = true;
        }
    }

    [Fact]
    public void GlobalScaleStretchesTheLength()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20) });
        try
        {
            ClamChartSettings.AnimationScale = 2;
            ClamChartSettings.RespectSystemAnimationScale = false;

            // Act
            using var bitmap = view.Paint(360, 240);

            // Assert
            Assert.Equal([(Data, 900u)], Driver(view).Starts);
        }
        finally
        {
            ClamChartSettings.AnimationScale = 1;
            ClamChartSettings.RespectSystemAnimationScale = true;
        }
    }

    [Fact]
    public void DataChangesMorphFromTheShownValuesAndRestartFromThere()
    {
        // Arrange
        var items = new ObservableCollection<double> { 10, 20, 30 };
        using var view = Quiet(new LineSeries { ItemsSource = items });
        var driver = Driver(view);
        using var first = view.Paint(360, 240);
        var maximum = view.ChartLayout.YAxis.Scale.Maximum;

        // Act
        items[1] = 50;
        using var changed = view.Paint(360, 240);
        driver.Frame(Data, 0.5);

        // Assert
        var display = view.Interpolator.GetGeometry(view.ChartLayout, 0);
        var scale = view.ChartLayout.YAxis.DisplayScale.Maximum;
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(35, display.End[1], 9);
        Assert.InRange(scale, maximum + 1, view.ChartLayout.YAxis.Scale.Maximum - 1);

        // Act
        items[1] = 20;
        using var interrupted = view.Paint(360, 240);

        // Assert
        Assert.Equal(2, driver.Starts.Count);
        Assert.Equal(35, view.Interpolator.GetGeometry(view.ChartLayout, 0).End[1], 9);
        Assert.Equal(scale, view.ChartLayout.YAxis.DisplayScale.Maximum, 9);
    }

    [Fact]
    public void StreamingFlowsTheHeadOutAndGrowsTheTail()
    {
        // Arrange
        var items = new ObservableCollection<double> { 10, 20, 30 };
        using var view = Quiet(new LineSeries { ItemsSource = items });
        var driver = Driver(view);
        using var first = view.Paint(360, 240);

        // Act
        items.Add(40);
        items.RemoveAt(0);
        using var changed = view.Paint(360, 240);

        // Assert
        var display = view.Interpolator.GetGeometry(view.ChartLayout, 0);
        Assert.Equal([0d, 1, 2, 2], display.X.ToArray());

        // Act
        driver.Frame(Data, 1);

        // Assert
        Assert.Equal([-1d, 0, 1, 2], view.Interpolator.GetGeometry(view.ChartLayout, 0).X.ToArray());
    }

    [Fact]
    public void UpdatesCanBeStillWhileLegendTogglesAnimate()
    {
        // Arrange
        var items = new ObservableCollection<double> { 10, 20, 30 };
        var series = new LineSeries { ItemsSource = items };
        using var view = Quiet(series, new LineSeries { ItemsSource = Entries(5, 6, 7) });
        view.UpdateAnimationEnabled = false;
        var driver = Driver(view);
        using var first = view.Paint(360, 240);

        // Act
        items[0] = 15;
        using var updated = view.Paint(360, 240);

        // Assert
        Assert.Empty(driver.Starts);

        // Act
        series.IsVisible = false;
        using var hidden = view.Paint(360, 240);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Single(driver.Starts);
        Assert.Equal(0.5, view.Interpolator.GetOpacity(0, false), 9);
    }

    [Fact]
    public void SelectionDimsOverOneHundredFiftyMilliseconds()
    {
        // Arrange
        using var view = Quiet(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        view.SelectionMode = ChartSelectionMode.Item;
        var driver = Driver(view);
        using var first = view.Paint(360, 240);

        // Act
        view.SelectedIndex = 1;
        driver.Frame(Selection, 0.5);
        using var middle = view.Paint(360, 240);
        driver.Finish(Selection);
        using var last = view.Paint(360, 240);

        // Assert
        var other = GetBarBounds(view, 0, 0);
        var dimmed = ChartStyles.Light.DimmedOpacity;
        var half = (int)Math.Round(255 * (1 + ((dimmed - 1) * 0.5)));
        Assert.Equal([(Selection, 150u)], driver.Starts);
        Assert.InRange(GetPixel(middle, other.MidX, other.MidY).Alpha, half - 2, half + 2);
        Assert.Equal((byte)Math.Round(255 * dimmed), GetPixel(last, other.MidX, other.MidY).Alpha);
    }

    [Fact]
    public void CompletingAndHidingStopAtTheFinalState()
    {
        // Arrange
        var items = new ObservableCollection<double> { 10, 20, 30 };
        using var view = Quiet(new LineSeries { ItemsSource = items });
        var driver = Driver(view);
        using var first = view.Paint(360, 240);
        items[1] = 50;
        using var changed = view.Paint(360, 240);
        driver.Frame(Data, 0.5);

        // Act
        view.CompleteAnimations();

        // Assert
        Assert.False(view.IsAnimating);
        Assert.False(view.Interpolator.IsMorphing);
        Assert.Contains(Data, driver.Aborts);
        Assert.Equal(view.ChartLayout.YAxis.Scale, view.ChartLayout.YAxis.DisplayScale);

        // Act
        items[1] = 10;
        using var again = view.Paint(360, 240);
        view.IsVisible = false;

        // Assert
        Assert.False(view.IsAnimating);
        Assert.Equal(2, driver.Aborts.Count);
    }

    [Fact]
    public void ReplacingTheItemsSourceReplaysTheEntranceOnlyWhenTheCountChanges()
    {
        // Arrange
        var series = new BarSeries { ItemsSource = Entries(10, 20, 30) };
        using var view = Chart(series);
        var driver = Driver(view);
        using var first = view.Paint(360, 240);
        driver.Finish(Data);

        // Act
        series.ItemsSource = Entries(5, 6, 7);
        using var same = view.Paint(360, 240);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        driver.Finish(Data);

        // Act
        series.ItemsSource = Entries(5, 6);
        using var shorter = view.Paint(360, 240);

        // Assert
        Assert.True(view.Interpolator.IsEntrance);
        Assert.Equal(3, driver.Starts.Count);
    }

    [Fact]
    public void PlayEntranceAnimationReplaysIt()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        var driver = Driver(view);
        using var first = view.Paint(360, 240);
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
        var items = new ObservableCollection<double> { 10, 20, 30, 40 };
        using var view = Quiet(new LineSeries { ItemsSource = items }, new BarSeries { ItemsSource = Entries(5, 6, 7, 8) });
        var driver = Driver(view);
        using var first = view.Paint(360, 240);
        using var surface = SKSurface.Create(new SKImageInfo(720, 480));
        var idle = MeasureFrames(view, driver, surface.Canvas, false);
        items.Add(50);
        items.RemoveAt(0);
        using var changed = view.Paint(360, 240);

        // Act
        MeasureFrames(view, driver, surface.Canvas, true);
        var moving = MeasureFrames(view, driver, surface.Canvas, true);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal(idle, moving);
    }

    [Fact]
    public void ScatterFadesInWhileItsPointsGrowToTheirSize()
    {
        // Arrange
        using var view = Scatter();
        view.AnimationEasing = Easing.Linear;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(360, 240);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(360, 240);
        var entrance = view.Interpolator.GetEntrance(ChartSeriesKind.Scatter);
        driver.Finish(Data);
        using var last = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var x = layout.XAxis.ToPixel(5);
        var y = layout.YAxis.ToPixel(4.3);
        Assert.Equal(ChartEntranceAnimation.Fade, entrance);
        Assert.Equal(64, GetPixel(middle, x, y).Alpha);
        Assert.Equal(0, GetPixel(middle, x + 10, y).Alpha);
        Assert.Equal(128, GetPixel(last, x, y).Alpha);
        Assert.Equal(255, GetPixel(last, x + 10, y).Alpha);
    }

    [Fact]
    public void ScatterPointsRiseFromTheBaselineWithGrow()
    {
        // Arrange
        using var view = Scatter();
        view.EntranceAnimation = ChartEntranceAnimation.Grow;

        // Act
        using var first = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var x = layout.XAxis.ToPixel(5);
        Assert.Equal(0, GetPixel(first, x, layout.YAxis.ToPixel(4.3)).Alpha);
        Assert.Equal(128, GetPixel(first, x, layout.YAxis.ToPixel(0) - 3).Alpha);
    }

    [Fact]
    public void ScatterAndOverlayFramesDoNotAllocate()
    {
        // Arrange
        var line = new List<ChartPoint> { new(0, 20), new(4, 50) };
        var points = new ObservableCollection<ChartPoint> { new(0, 10), new(1, 40), new(2, 30), new(3, 60) };
        using var plain = Quiet(new LineSeries { ItemsSource = line }, new ScatterSeries { ItemsSource = points.ToList() });
        using var view = Quiet(new LineSeries { ItemsSource = line }, new ScatterSeries { ItemsSource = points });
        view.Thresholds = [new ChartThreshold { Level = ChartThresholdLevel.Good, Label = "Base" }, new ChartThreshold { Value = 35, Level = ChartThresholdLevel.Critical, Label = "Limit" }];
        view.ThresholdsMode = ChartThresholdsMode.Lines | ChartThresholdsMode.Bands | ChartThresholdsMode.ColorSeries;
        view.Annotations.Add(new ChartReferenceLine { Value = 45, Label = "Target", DashPattern = [2, 1] });
        view.Annotations.Add(new ChartReferenceBand { Axis = ChartAnnotationAxis.X, Start = 1, End = 2, Label = "Window" });
        var driver = Driver(view);
        using var plainFirst = plain.Paint(360, 240);
        using var first = view.Paint(360, 240);
        using var surface = SKSurface.Create(new SKImageInfo(720, 480));
        var baseline = MeasureFrames(plain, Driver(plain), surface.Canvas, false);
        var idle = MeasureFrames(view, driver, surface.Canvas, false);
        points.Add(new ChartPoint(4, 20));
        points.RemoveAt(0);
        using var changed = view.Paint(360, 240);

        // Act
        MeasureFrames(view, driver, surface.Canvas, true);
        var moving = MeasureFrames(view, driver, surface.Canvas, true);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal(baseline, idle);
        Assert.Equal(idle, moving);
    }

    private static TestClamChartView Scatter()
    {
        var view = Chart(new ScatterSeries { ItemsSource = new List<ChartPoint> { new(0, 0), new(10, 10), new(5, 4.3) }, MarkerSize = 20, Color = Colors.Blue });
        view.YAxis = new NumericAxis { GridLinesVisible = false };
        return view;
    }

    [Fact]
    public void RangeBarGrowsFromTheMiddleOfItsRange()
    {
        // Arrange
        using var view = Chart(new RangeBarSeries { ItemsSource = new List<ChartEntry> { new("C0", 30, Target: 10) }, LowSelector = static x => ((ChartEntry)x).Target });
        view.AnimationEasing = Easing.Linear;
        view.YAxis = new NumericAxis { Minimum = 0, Maximum = 40, GridLinesVisible = false };
        var driver = Driver(view);
        using var first = view.Paint(360, 240);

        // Act
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(360, 240);

        // Assert
        var axis = view.ChartLayout.YAxis;
        var x = GetBarBounds(view, 0, 0).MidX;
        Assert.Equal(255, GetPixel(middle, x, axis.ToPixel(20)).Alpha);
        Assert.Equal(0, GetPixel(middle, x, axis.ToPixel(28)).Alpha);
        Assert.Equal(0, GetPixel(middle, x, axis.ToPixel(12)).Alpha);
    }

    private static TestClamChartView Chart(params ChartSeries[] series)
    {
        var view = new TestClamChartView { AnimationDriver = new TestAnimationDriver() };
        foreach (var item in series)
        {
            view.Series.Add(item);
        }

        view.OnAttached();
        return view;
    }

    private static TestClamChartView Quiet(params ChartSeries[] series)
    {
        var view = Chart(series);
        view.EntranceAnimation = ChartEntranceAnimation.None;
        view.AnimationEasing = Easing.Linear;
        return view;
    }

    private static TestAnimationDriver Driver(TestClamChartView view) => (TestAnimationDriver)view.AnimationDriver;

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static void Paint(params TestClamChartView[] views)
    {
        foreach (var view in views)
        {
            using var bitmap = view.Paint(360, 240);
        }
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static SKRect GetBarBounds(TestClamChartView view, int seriesIndex, int index)
    {
        var layout = view.ChartLayout;
        var geometry = layout.Geometries[seriesIndex];
        var (from, to) = geometry.GetBarRange(geometry.X[index]);
        var left = (float)layout.XAxis.ToPixel(from);
        var right = (float)layout.XAxis.ToPixel(to);
        var top = (float)layout.YAxis.ToPixel(geometry.End[index]);
        var bottom = (float)layout.YAxis.ToPixel(geometry.Start[index]);
        return new SKRect(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
    }

    private static long MeasureFrames(TestClamChartView view, TestAnimationDriver driver, SKCanvas canvas, bool moving)
    {
        canvas.Save();
        canvas.Scale(Scale);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 1; i <= 4; i++)
        {
            if (moving)
            {
                driver.Frame(Data, i * 0.2);
            }

            view.Render(canvas, 360, 240);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }
}
