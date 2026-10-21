namespace ClamChart.Tests;

using ClamChart.Tests.Data;

using SkiaSharp.Views.Maui;

public sealed class ClamSparklineViewTests
{
    [Fact]
    public void DefaultsFollowTheSpecification()
    {
        // Arrange
        using var view = new ClamSparklineView();

        // Assert
        Assert.Equal(ChartSparklineKind.Line, view.Kind);
        Assert.Equal(ChartSparklineMarkers.Last, view.Markers);
        Assert.False(view.Smooth);
        Assert.True(view.TrackItemChanges);
        Assert.Null(view.Thickness);
        Assert.Null(view.Minimum);
        Assert.Null(view.Maximum);
        Assert.Null(view.ReferenceValue);
        Assert.Null(view.NormalRangeMinimum);
        Assert.Null(view.NormalRangeMaximum);
        Assert.Equal(ChartSparklineEmphasis.None, view.EmphasisMode);
        Assert.Empty(view.Thresholds);
        Assert.Equal(ChartThresholdsMode.None, view.ThresholdsMode);
        Assert.Equal(ChartEntranceAnimation.Auto, view.EntranceAnimation);
    }

    [Fact]
    public void DensityIsAlwaysMinimal()
    {
        // Arrange
        using var view = Spark([1, 2, 3]);
        view.Density = ChartDensity.Full;

        // Act
        using var bitmap = view.Paint(400, 300);

        // Assert
        Assert.Equal(ChartDensity.Minimal, view.EffectiveDensity);
        Assert.Equal(new ChartRect(0, 0, 400, 300), view.ChartLayout.Bounds);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new ClamSparklineView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Thickness = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Minimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Maximum = Double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ReferenceValue = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.NormalRangeMinimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.NormalRangeMaximum = Double.NegativeInfinity);
        Assert.Throws<ArgumentNullException>(() => view.Thresholds = null!);
        view.Minimum = null;
        view.Thickness = 0;
        Assert.Equal(0, view.Thickness);
    }

    [Fact]
    public void ArrayIsReadInPlace()
    {
        // Arrange
        var values = new[] { 1d, 2, 3 };
        using var view = Spark(values);
        using var first = view.Paint(100, 30);

        // Act
        values[2] = 30;
        view.Invalidate();
        using var second = view.Paint(100, 30);

        // Assert
        Assert.Equal(30, view.ChartLayout.Scale.Maximum);
        Assert.Equal(3, view.RenderedPointCount);
    }

    [Fact]
    public void ObservableValuesFollowTheChanges()
    {
        // Arrange
        var values = new ObservableCollection<double> { 1, 2, 3 };
        using var view = Spark(values);
        view.OnAttached();
        using var first = view.Paint(100, 30);

        // Act
        values.Add(10);
        using var second = view.Paint(100, 30);

        // Assert
        Assert.Equal(4, view.ChartLayout.Count);
        Assert.Equal(10, view.ChartLayout.Scale.Maximum);
    }

    [Fact]
    public void ThresholdChangesAreFollowedWhileAttached()
    {
        // Arrange
        using var view = Spark([1, 2, 3]);
        view.OnAttached();
        using var first = view.Paint(100, 30);

        // Act
        view.Thresholds.Add(new ChartThreshold { Value = 2, Level = ChartThresholdLevel.Warning });
        using var second = view.Paint(100, 30);
        view.OnDetached();
        view.Thresholds.Add(new ChartThreshold { Value = 3, Level = ChartThresholdLevel.Critical });
        using var third = view.Paint(100, 30);

        // Assert
        Assert.Single(view.ChartLayout.Thresholds);
    }

    [Fact]
    public void EnumeratedValuesAreCollected()
    {
        // Arrange
        using var view = Spark(Enumerable.Range(1, 40).Select(static x => (double)x));

        // Act
        using var bitmap = view.Paint(100, 30);

        // Assert
        Assert.Equal(40, view.ChartLayout.Count);
        Assert.Equal(40, view.ChartLayout.Scale.Maximum);
    }

    [Fact]
    public void ItemsAreReadWithTheAccessors()
    {
        // Arrange
        using var view = new TestClamSparklineView();
        view.ItemsSource = new List<TestSalesItem> { new() { Sales = 4 }, new() { Sales = 8 } };
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";

        // Act
        using var bitmap = view.Paint(100, 30);

        // Assert
        Assert.Equal(2, view.ChartLayout.Count);
        Assert.Equal(8, view.ChartLayout.Scale.Maximum);
    }

    [Fact]
    public void ItemChangesAreFollowedWhileTracked()
    {
        // Arrange
        var item = new TestSalesItem { Sales = 4 };
        using var view = new TestClamSparklineView();
        view.ItemsSource = new List<TestSalesItem> { item, new() { Sales = 8 } };
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";
        view.OnAttached();
        using var first = view.Paint(100, 30);

        // Act
        item.Sales = 20;
        using var second = view.Paint(100, 30);

        // Assert
        Assert.Equal(20, view.ChartLayout.Scale.Maximum);
    }

    [Fact]
    public void SelectorIsUsedBeforeTheKey()
    {
        // Arrange
        using var view = new TestClamSparklineView();
        view.ItemsSource = new List<TestSalesItem> { new() { Sales = 4, Count = 40 } };
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";
        view.ValueSelector = static x => ((TestSalesItem)x).Count;
        view.Markers = ChartSparklineMarkers.None;

        // Act
        using var bitmap = view.Paint(100, 30);

        // Assert
        Assert.Equal(40, view.ChartLayout.Scale.Maximum - SparklineLayout.FlatRange);
    }

    [Fact]
    public void ValuesWinOverItems()
    {
        // Arrange
        using var view = Spark([1, 2]);
        view.ItemsSource = new List<double> { 5, 6, 7 };

        // Act
        using var bitmap = view.Paint(100, 30);

        // Assert
        Assert.Equal(2, view.ChartLayout.Count);
    }

    [Fact]
    public void InvertedBoundsAreReportedOnce()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = Spark([1, 2]);
        view.Minimum = 5;
        view.Maximum = 1;

        // Act
        using var first = view.Paint(100, 30);
        view.Kind = ChartSparklineKind.Area;
        using var second = view.Paint(100, 30);

        // Assert
        Assert.Equal(1, capture.Count("sparkline Maximum is not greater"));
    }

    [Fact]
    public void TouchesPassThroughUnlessTheChartIsTapped()
    {
        // Arrange
        using var view = Spark([1, 2]);
        using var bitmap = view.Paint(100, 30);
        var tapped = 0;

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        Assert.False(view.HandleTouch(1, SKTouchAction.Pressed, new Point(50, 15), 0));
        view.ChartTapped += (_, _) => tapped++;
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(50, 15), 0);
        view.HandleTouch(1, SKTouchAction.Released, new Point(50, 15), 10);
        Assert.Equal(1, tapped);
    }

    private static TestClamSparklineView Spark(IEnumerable<double> values) => new() { Values = values };
}
