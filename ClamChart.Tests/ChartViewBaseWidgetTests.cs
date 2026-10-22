namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

[Collection(nameof(ClamChartSettings))]
public sealed class ChartViewBaseWidgetTests
{
    private const string Skeleton = "ClamChartSkeleton";

    [Fact]
    public void BusyDrawsTheSkeletonInsteadOfTheChart()
    {
        // Arrange
        using var view = new TestChartView { IsBusy = true };

        // Act
        view.Paint(200, 100);

        // Assert
        Assert.Equal(0, view.Renders);

        // Act
        view.IsBusy = false;
        view.Paint(200, 100);

        // Assert
        Assert.Equal(1, view.Renders);
        Assert.Equal(1, view.EntranceStarts);
    }

    [Fact]
    public void BusyRingIsDrawnInTheSkeletonColor()
    {
        // Arrange
        using var view = new TestClamPieChartView { IsBusy = true, ItemsSource = new List<ChartEntry> { new("A", 1) } };

        // Act
        using var bitmap = view.Paint(200, 200);

        // Assert
        var radius = (200 - 16) / 2f;
        var color = ChartStyles.Light.SkeletonColor.ToSKColor();
        Assert.Equal(color, bitmap.GetPixel(200, (int)((100 - radius + (radius * 0.15f)) * 2)));
        Assert.Equal(0, bitmap.GetPixel(200, 200).Alpha);
    }

    [Fact]
    public void BusyShimmerLoopsWhileBusyAndStops()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = new TestChartView { AnimationDriver = driver };

        // Act
        view.IsBusy = true;
        driver.Frame(Skeleton, 0.5);

        // Assert
        Assert.Equal([(Skeleton, 1600u)], driver.Starts);
        Assert.True(view.IsSkeletonAnimating);
        Assert.Equal(0.5, view.SkeletonPhase);

        // Act
        driver.Finish(Skeleton);

        // Assert
        Assert.True(driver.IsRunning(Skeleton));
        Assert.True(view.IsSkeletonAnimating);

        // Act
        view.IsBusy = false;

        // Assert
        Assert.False(driver.IsRunning(Skeleton));
        Assert.False(view.IsSkeletonAnimating);
        Assert.True(Double.IsNaN(view.SkeletonPhase));
    }

    [Fact]
    public void BusySkeletonIsStillWhenAnimationsAreOff()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = new TestChartView { AnimationDriver = driver, AnimationEnabled = false };

        // Act
        view.IsBusy = true;
        view.Paint(200, 100);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.True(Double.IsNaN(view.SkeletonPhase));
    }

    [Fact]
    public void BusyViewLetsTouchesThrough()
    {
        // Arrange
        using var view = new TestClamChartView { TouchMode = ChartTouchMode.Interactive };

        // Act
        view.IsBusy = true;

        // Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
    }

    [Fact]
    public void SkeletonFollowsTheShapeOfTheChart()
    {
        // Arrange
        using var line = new TestClamChartView();
        line.Series.Add(new LineSeries());
        using var bars = new TestClamChartView();
        bars.Series.Add(new BarSeries());
        using var ranking = new TestClamChartView { Orientation = ChartOrientation.Horizontal };
        ranking.Series.Add(new BarSeries());
        using var pie = new TestClamPieChartView();
        using var sparkline = new TestClamSparklineView { Kind = ChartSparklineKind.WinLoss };
        using var stat = new TestClamStatView();
        using var gauge = new TestClamBarGaugeView();
        using var radial = new TestClamRadialBarView();

        // Act & Assert
        Assert.Equal(SkeletonShape.Line, line.SkeletonShape);
        Assert.Equal(SkeletonShape.Bars, bars.SkeletonShape);
        Assert.Equal(SkeletonShape.Rows, ranking.SkeletonShape);
        Assert.Equal(SkeletonShape.Ring, pie.SkeletonShape);
        Assert.Equal(SkeletonShape.Bars, sparkline.SkeletonShape);
        Assert.Equal(SkeletonShape.Tile, stat.SkeletonShape);
        Assert.Equal(SkeletonShape.Rows, gauge.SkeletonShape);
        Assert.Equal(SkeletonShape.Ring, radial.SkeletonShape);
    }

    [Fact]
    public void RenderToImageDrawsAtTheGivenSizeAndRestoresTheLayout()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 2, 2 } });
        using var screen = view.Paint(360, 240);

        // Act
        using var image = view.RenderToImage(new Size(200, 100), 3);
        using var bitmap = SKBitmap.FromImage(image);

        // Assert
        Assert.Equal(600, image.Width);
        Assert.Equal(300, image.Height);
        Assert.Equal(196, view.ChartLayout.Bounds.Right);
        Assert.Contains(Enumerable.Range(0, bitmap.Height), y => bitmap.GetPixel(300, y) == new SKColor(0x2A, 0x78, 0xD6));

        // Act
        using var after = view.Paint(360, 240);

        // Assert
        Assert.Equal(352, view.ChartLayout.Bounds.Right);
    }

    [Fact]
    public void RenderToImageWorksWithoutAHandler()
    {
        // Arrange
        using var view = new ClamSparklineView { Values = [1, 3, 2] };

        // Act
        using var image = view.RenderToImage(new Size(60, 20), 1);

        // Assert
        Assert.Equal(60, image.Width);
        Assert.Equal(20, image.Height);
    }

    [Fact]
    public void RenderToImageRejectsAnEmptySize()
    {
        // Arrange
        using var view = new ClamSparklineView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.RenderToImage(new Size(0, 20), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => view.RenderToImage(new Size(20, 20), 0));
    }

    [Fact]
    public async Task SaveAsPngWritesAPng()
    {
        // Arrange
        using var view = new ClamSparklineView { Values = [1, 3, 2] };
        using var stream = new MemoryStream();

        // Act
        await view.SaveAsPngAsync(stream, new Size(60, 20), 2);

        // Assert
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], stream.ToArray().Take(4));
    }

    [Fact]
    public void SummaryDescribesEachSeries()
    {
        // Arrange
        using var view = new TestClamChartView { Culture = CultureInfo.InvariantCulture };
        view.Series.Add(new LineSeries { Title = "Sales", ItemsSource = Entries(10, 30, 20) });
        view.Series.Add(new LineSeries { Title = "Cost", ItemsSource = Entries(5, 4, 6) });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Equal("Sales: 3 points, max 30 (C1), min 10 (C0); Cost: 3 points, max 6 (C2), min 4 (C1)", SemanticProperties.GetDescription(view));
    }

    [Fact]
    public void SummaryOfARangeUsesItsLowEnd()
    {
        // Arrange
        using var view = new TestClamChartView { Culture = CultureInfo.InvariantCulture };
        view.Series.Add(new RangeBarSeries { Title = "Temp", ItemsSource = new List<ChartEntry> { new("C0", 25, Target: 12), new("C1", 18, Target: 8) }, LowSelector = static x => ((ChartEntry)x).Target });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Equal("Temp: 2 points, max 25 (C0), min 8 (C1)", SemanticProperties.GetDescription(view));
    }

    [Fact]
    public void SummaryIsJapaneseForAJapaneseCulture()
    {
        // Arrange
        using var view = new TestClamChartView { Culture = CultureInfo.GetCultureInfo("ja-JP") };
        view.Series.Add(new LineSeries { Title = "売上", ItemsSource = Entries(10, 30, 20) });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Equal("売上、3 点、最大 30（C1）、最小 10（C0）", SemanticProperties.GetDescription(view));
    }

    [Fact]
    public void SummaryKeepsTheDescriptionOfTheApplication()
    {
        // Arrange
        using var view = new TestClamChartView { Culture = CultureInfo.InvariantCulture };
        view.Series.Add(new LineSeries { ItemsSource = Entries(10, 30) });
        SemanticProperties.SetDescription(view, "Revenue");

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Equal("Revenue", SemanticProperties.GetDescription(view));
    }

    [Fact]
    public void SummaryFormatterReplacesTheText()
    {
        // Arrange
        using var view = new TestClamChartView { Culture = CultureInfo.InvariantCulture };
        view.Series.Add(new LineSeries { ItemsSource = Entries(10, 30) });
        using var first = view.Paint(360, 240);

        // Act
        view.AutomationSummaryFormatter = static chart => "Custom " + ((ClamChartView)chart).Series.Count.ToString(CultureInfo.InvariantCulture);
        using var second = view.Paint(360, 240);

        // Assert
        Assert.Equal("Custom 1", SemanticProperties.GetDescription(view));
    }

    [Fact]
    public void SummaryOfTheOtherViews()
    {
        // Arrange
        using var pie = new TestClamPieChartView { Culture = CultureInfo.InvariantCulture, ItemsSource = new List<ChartEntry> { new("A", 3), new("B", 1) } };
        using var sparkline = new TestClamSparklineView { Culture = CultureInfo.InvariantCulture, Values = [1, 5, 3] };
        using var stat = new TestClamStatView { Culture = CultureInfo.InvariantCulture, Title = "CPU", Value = 42, Unit = "%" };
        using var gauge = new TestClamBarGaugeView { Culture = CultureInfo.InvariantCulture, ItemsSource = new List<ChartEntry> { new("disk", 40) }, Maximum = 100 };

        // Act
        using var pieBitmap = pie.Paint(200, 200);
        using var sparklineBitmap = sparkline.Paint(100, 30);
        using var statBitmap = stat.Paint(200, 100);
        using var gaugeBitmap = gauge.Paint(300, 100);

        // Assert
        Assert.Equal("2 items, total 4, A 75 %, B 25 %", SemanticProperties.GetDescription(pie));
        Assert.Equal("3 points, last 3, max 5, min 1", SemanticProperties.GetDescription(sparkline));
        Assert.Equal("CPU: 42 %", SemanticProperties.GetDescription(stat));
        Assert.StartsWith("disk ", SemanticProperties.GetDescription(gauge), StringComparison.Ordinal);
    }

    [Fact]
    public void HapticFeedbackIsOffByDefault()
    {
        // Arrange
        using var view = new TestClamChartView();

        // Act & Assert
        Assert.False(view.HapticFeedbackEnabled);
    }

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];
}
