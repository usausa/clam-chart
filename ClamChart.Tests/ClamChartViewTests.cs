namespace ClamChart.Tests;

public sealed class ClamChartViewTests
{
    [Fact]
    public void SeriesAndAxesAreLogicalChildrenThatInheritTheBindingContext()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.BindingContext = "context";
        var series = new LineSeries();
        var axis = new NumericAxis();

        // Act
        view.Series.Add(series);
        view.YAxis = axis;

        // Assert
        Assert.Same(view, series.Parent);
        Assert.Equal("context", series.BindingContext);
        Assert.Same(view, axis.Parent);
        Assert.Equal("context", axis.BindingContext);

        // Act
        view.Series.Remove(series);
        view.YAxis = null;

        // Assert
        Assert.Null(series.Parent);
        Assert.Null(axis.Parent);
    }

    [Fact]
    public void ReplacingTheSeriesCollectionReplacesTheChildren()
    {
        // Arrange
        using var view = new TestClamChartView();
        var first = new LineSeries();
        var second = new AreaSeries();
        view.Series.Add(first);

        // Act
        view.Series = new ObservableCollection<ChartSeries> { second };

        // Assert
        Assert.Null(first.Parent);
        Assert.Same(view, second.Parent);
        Assert.Throws<ArgumentNullException>(() => view.Series = null!);
    }

    [Fact]
    public void SeriesCannotBeSharedByTwoCharts()
    {
        // Arrange
        using var first = new TestClamChartView();
        using var second = new TestClamChartView();
        var series = new LineSeries();
        first.Series.Add(series);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => second.Series.Add(series));
    }

    [Fact]
    public void DefaultXAxisFollowsTheData()
    {
        // Arrange
        using var categories = Chart(new LineSeries { ItemsSource = new List<ChartEntry> { new("A", 1), new("B", 2) } });
        using var times = Chart(new LineSeries { ItemsSource = new List<ChartTimePoint> { new(new DateTime(2026, 10, 19, 10, 0, 0), 1), new(new DateTime(2026, 10, 19, 11, 0, 0), 2) } });
        using var numbers = Chart(new LineSeries { ItemsSource = new List<double> { 3, 5, 4 } });

        // Act
        Paint(categories, times, numbers);

        // Assert
        Assert.Equal(ChartAxisKind.Category, categories.ChartLayout.XAxis.Options.Kind);
        Assert.Equal(ChartAxisKind.DateTime, times.ChartLayout.XAxis.Options.Kind);
        Assert.Equal(ChartAxisKind.Numeric, numbers.ChartLayout.XAxis.Options.Kind);
        Assert.Equal(0, numbers.ChartLayout.XAxis.Scale.Minimum);
        Assert.Equal(2, numbers.ChartLayout.XAxis.Scale.Maximum);
    }

    [Fact]
    public void TimeValuesAndOtherValuesCannotShareAnAxis()
    {
        // Arrange
        TestDispatcher.Current.RunPending();
        using var view = new TestClamChartView();
        view.XAxis = new NumericAxis();
        view.Series.Add(new LineSeries { ItemsSource = new List<ChartTimePoint> { new(new DateTime(2026, 10, 19), 1) } });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(TestDispatcher.Current.RunPending);

        // Act
        view.XAxis = new DateTimeAxis();
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 1 } });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(TestDispatcher.Current.RunPending);

        // Act
        view.XAxis = new CategoryAxis();
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Equal(2, view.ChartLayout.Geometries.Count);
        Assert.Equal(ChartAxisKind.Category, view.ChartLayout.XAxis.Options.Kind);
    }

    [Fact]
    public void AreasIncludeZeroAndLinesDoNot()
    {
        // Arrange
        using var line = Chart(new LineSeries { ItemsSource = new List<double> { 50, 60 } });
        using var area = Chart(new AreaSeries { ItemsSource = new List<double> { 50, 60 } });

        // Act
        Paint(line, area);

        // Assert
        Assert.True(line.ChartLayout.YAxis.Scale.Minimum > 0);
        Assert.Equal(0, area.ChartLayout.YAxis.Scale.Minimum);
    }

    [Fact]
    public void ScatterDoesNotIncludeZero()
    {
        // Arrange
        using var view = Chart(new ScatterSeries { ItemsSource = new List<ChartPoint> { new(1, 50), new(2, 60) } });

        // Act
        Paint(view);

        // Assert
        Assert.True(view.ChartLayout.YAxis.Scale.Minimum > 0);
        Assert.Equal(ChartAxisKind.Numeric, view.ChartLayout.XAxis.Options.Kind);
    }

    [Fact]
    public void AnnotationsAreLogicalChildrenThatInheritTheBindingContext()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.BindingContext = "context";
        var line = new ChartReferenceLine();
        var band = new ChartReferenceBand();

        // Act
        view.Annotations.Add(line);

        // Assert
        Assert.Same(view, line.Parent);
        Assert.Equal("context", line.BindingContext);

        // Act
        view.Annotations = new ObservableCollection<ChartAnnotation> { band };

        // Assert
        Assert.Null(line.Parent);
        Assert.Same(view, band.Parent);
        Assert.Throws<ArgumentNullException>(() => view.Annotations = null!);
        Assert.Throws<ArgumentNullException>(() => view.Thresholds = null!);
    }

    [Fact]
    public void AnnotationCannotBeSharedByTwoCharts()
    {
        // Arrange
        using var first = new TestClamChartView();
        using var second = new TestClamChartView();
        var line = new ChartReferenceLine();
        first.Annotations.Add(line);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => second.Annotations.Add(line));
    }

    [Fact]
    public void AnnotationValuesAreFiniteOrNaN()
    {
        // Arrange
        var line = new ChartReferenceLine();
        var band = new ChartReferenceBand();

        // Act & Assert
        Assert.True(Double.IsNaN(line.Value));
        Assert.True(Double.IsNaN(band.Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.Value = Double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => band.Start = Double.NegativeInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => band.End = Double.PositiveInfinity);
        line.Value = 5;
        band.End = Double.NaN;
        Assert.Equal(5, line.Value);
        Assert.Equal(ChartAnnotationAxis.Y, line.Axis);
        Assert.Equal(ChartAxisTarget.Primary, band.YAxis);
    }

    [Fact]
    public void ChangingAnAnnotationOrTheThresholdsUpdatesTheLayout()
    {
        // Arrange
        using var view = Chart(new LineSeries { ItemsSource = new List<double> { 10, 30 } });
        view.OnAttached();
        var line = new ChartReferenceLine { Value = 100 };
        view.Annotations.Add(line);
        Paint(view);
        var first = view.ChartLayout.YAxis.Scale.Maximum;

        // Act
        line.Value = 200;
        Paint(view);
        var second = view.ChartLayout.YAxis.Scale.Maximum;
        view.Annotations.Remove(line);
        view.Thresholds.Add(new ChartThreshold { Value = 20, Level = ChartThresholdLevel.Warning });
        Paint(view);

        // Assert
        Assert.Equal(100, first);
        Assert.Equal(200, second);
        Assert.Equal(30, view.ChartLayout.YAxis.Scale.Maximum);
        Assert.Single(view.ChartLayout.Thresholds);
        Assert.Empty(view.ChartLayout.Annotations);
        Assert.Null(line.Parent);
    }

    [Fact]
    public void SecondaryAxisIsCreatedForSecondarySeries()
    {
        // Arrange
        using var single = Chart(new LineSeries { ItemsSource = new List<double> { 1, 2 } });
        using var dual = Chart(new LineSeries { ItemsSource = new List<double> { 1, 2 } }, new LineSeries { ItemsSource = new List<double> { 50, 90 }, YAxis = ChartAxisTarget.Secondary });

        // Act
        Paint(single, dual);

        // Assert
        Assert.False(single.ChartLayout.HasSecondaryAxis);
        Assert.True(dual.ChartLayout.HasSecondaryAxis);
        Assert.Equal(AxisSide.Right, dual.ChartLayout.SecondaryYAxis.Side);
    }

    [Fact]
    public void LegendIsShownForSeveralSeriesInFullDensity()
    {
        // Arrange
        using var two = Chart(Titled("A"), Titled("B"));
        using var one = Chart(Titled("A"));
        using var hidden = Chart(Titled("A"), Titled("B"));
        hidden.LegendVisible = false;
        using var compact = Chart(Titled("A"), Titled("B"));

        // Act
        Paint(two, one, hidden);
        using var bitmap = compact.Paint(300, 150);

        // Assert
        Assert.True(two.ChartLayout.Legend.IsVisible);
        Assert.False(one.ChartLayout.Legend.IsVisible);
        Assert.False(hidden.ChartLayout.Legend.IsVisible);
        Assert.Equal(ChartDensity.Compact, compact.EffectiveDensity);
        Assert.False(compact.ChartLayout.Legend.IsVisible);
    }

    [Fact]
    public void LegendTableShowsTheSelectedValues()
    {
        // Arrange
        using var view = Chart(Titled("A"), Titled("B"));
        view.LegendMode = ChartLegendMode.Table;
        view.LegendValues = ChartLegendValues.Last | ChartLegendValues.Min;

        // Act
        Paint(view);

        // Assert
        var legend = view.ChartLayout.Legend;
        Assert.True(legend.IsTable);
        Assert.Equal(["Name", "Last *", "Min", "2", "1", "2", "1"], legend.Cells.Select(static x => x.Text));
    }

    [Fact]
    public void LegendTableDefaultsAndWidthValidation()
    {
        // Arrange
        using var view = new TestClamChartView();

        // Act & Assert
        Assert.Equal(ChartLegendMode.List, view.LegendMode);
        Assert.Equal(ChartLegendValues.None, view.LegendValues);
        Assert.Equal(180, view.LegendWidth);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.LegendWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.LegendWidth = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.LegendWidth = Double.PositiveInfinity);
    }

    [Fact]
    public void CompactDensityReducesLabelsLegendAndMarkers()
    {
        // Arrange
        var entries = Enumerable.Range(1, 12).Select(static i => new ChartEntry($"M{i}", i * 10)).ToList();
        using var view = Chart(new BarSeries { Title = new string('A', 30), ItemsSource = entries }, new LineSeries { Title = new string('B', 30), ItemsSource = entries });
        view.LegendVisible = true;
        view.DataLabelsMode = ChartDataLabelsMode.All;

        // Act
        using var bitmap = view.Paint(300, 150);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(ChartDensity.Compact, view.EffectiveDensity);
        Assert.Equal(["M1", "M12"], layout.XAxis.Labels.Select(static x => x.Text));
        Assert.InRange(layout.YAxis.Labels.Count, 2, 3);
        Assert.Single(layout.Legend.Items);
        Assert.Equal(1, layout.Legend.OverflowCount);
        Assert.Equal([(0, 11), (1, 11)], layout.DataLabels.Select(static x => (x.SeriesIndex, x.Index)));
        Assert.True(layout.LastMarkerOnly);
    }

    [Fact]
    public void ExplicitLabelCountIsKeptInCompactDensity()
    {
        // Arrange
        var entries = Enumerable.Range(1, 6).Select(static i => new ChartEntry($"M{i}", i)).ToList();
        using var view = Chart(new BarSeries { ItemsSource = entries });
        view.XAxis = new CategoryAxis { MaxLabelCount = 6 };

        // Act
        using var bitmap = view.Paint(300, 150);

        // Assert
        Assert.Equal(6, view.ChartLayout.XAxis.Labels.Count);
    }

    [Fact]
    public void MinimalDensityDrawsNoDataLabels()
    {
        // Arrange
        using var view = Chart(new LineSeries { ItemsSource = new List<double> { 1, 2 } });
        view.DataLabelsMode = ChartDataLabelsMode.All;

        // Act
        using var bitmap = view.Paint(150, 90);

        // Assert
        Assert.Empty(view.ChartLayout.DataLabels);
        Assert.True(view.ChartLayout.LastMarkerOnly);
    }

    [Fact]
    public void BarsIncludeZero()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = new List<ChartEntry> { new("A", 50), new("B", 60) } });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Equal(0, view.ChartLayout.YAxis.Scale.Minimum);
        Assert.Equal(ChartAxisKind.Category, view.ChartLayout.XAxis.Options.Kind);
    }

    [Fact]
    public void MinimalDensityHidesTheAxes()
    {
        // Arrange
        using var view = Chart(new AreaSeries { Title = "A", YAxis = ChartAxisTarget.Primary, ItemsSource = new List<double> { 1, 3, 2 } });
        view.YAxis = new NumericAxis { Title = "Sales" };

        // Act
        using var bitmap = view.Paint(150, 90);

        // Assert
        Assert.Equal(ChartDensity.Minimal, view.EffectiveDensity);
        Assert.Empty(view.ChartLayout.XAxis.Labels);
        Assert.Empty(view.ChartLayout.YAxis.Labels);
        Assert.False(view.ChartLayout.YAxis.ShowsTitle);
        Assert.Equal(view.ChartLayout.Bounds, view.ChartLayout.PlotBounds);
    }

    [Fact]
    public void CollectionChangesAreTrackedWhileAttached()
    {
        // Arrange
        TestDispatcher.Current.RunPending();
        var items = new ObservableCollection<double> { 1, 2, 3 };
        var series = new LineSeries { ItemsSource = items };
        using var view = Chart(series);
        view.OnAttached();
        TestDispatcher.Current.RunPending();

        // Act
        items.Add(4);
        TestDispatcher.Current.RunPending();

        // Assert
        Assert.Equal(4, series.Snapshot.Count);

        // Act
        view.OnDetached();
        items.Add(5);
        TestDispatcher.Current.RunPending();

        // Assert
        Assert.Equal(4, series.Snapshot.Count);
    }

    [Fact]
    public void RenderedPointsCountOnlyVisibleSeries()
    {
        // Arrange
        using var view = Chart(new LineSeries { ItemsSource = new List<double> { 1, 2, 3 } }, new LineSeries { ItemsSource = new List<double> { 1, 2 }, IsVisible = false });

        // Act
        TestDispatcher.Current.RunPending();

        // Assert
        Assert.Equal(3, view.RenderedPointCount);
    }

    [Fact]
    public void HorizontalChartsListTheCategoriesFromTheTop()
    {
        // Arrange
        using var view = Chart(new AreaSeries { ItemsSource = new List<ChartEntry> { new("A", 1), new("B", 2) } });
        view.Orientation = ChartOrientation.Horizontal;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var axis = view.ChartLayout.XAxis;
        Assert.True(axis.ToPixel(0) < axis.ToPixel(1));
        Assert.Equal(AxisSide.Left, axis.Side);
    }

    [Fact]
    public void SeriesBeyondThePaletteAreReportedOnce()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = new TestClamChartView();
        for (var i = 0; i < 9; i++)
        {
            view.Series.Add(new LineSeries { ItemsSource = new List<double> { i, i + 1 } });
        }

        // Act
        using var first = view.Paint(360, 240);
        using var second = view.Paint(360, 240);

        // Assert
        Assert.Equal(1, capture.Count("The series at index 8 has no color in the palette"));
        Assert.Equal(0, capture.Count("The series at index 7 has no color in the palette"));
    }

    [Fact]
    public void InvertedBoundsAreReportedOnceForEachChange()
    {
        // Arrange
        using var capture = new TraceCapture();
        var axis = new NumericAxis { Minimum = 10, Maximum = 5 };
        using var view = Chart(new LineSeries { ItemsSource = new List<double> { 1, 2 } });
        view.YAxis = axis;
        var message = "The axis Maximum is not greater than Minimum";
        var before = capture.Count(message);

        // Act
        using var first = view.Paint(360, 240);
        using var second = view.Paint(360, 240);

        // Assert
        Assert.Equal(before + 1, capture.Count(message));
        Assert.Equal(1, view.ChartLayout.YAxis.Scale.Minimum);
        Assert.Equal(2, view.ChartLayout.YAxis.Scale.Maximum);

        // Act
        axis.Maximum = 4;
        using var third = view.Paint(360, 240);

        // Assert
        Assert.Equal(before + 2, capture.Count(message));
    }

    [Fact]
    public void DisposeReleasesTheSnapshots()
    {
        // Arrange
        var series = new LineSeries { ItemsSource = new List<double> { 1, 2, 3 } };
        var view = Chart(series);
        TestDispatcher.Current.RunPending();

        // Act
        view.Dispose();

        // Assert
        Assert.Same(DataSnapshot.Empty, series.Snapshot);
    }

    private static LineSeries Titled(string title) => new() { Title = title, ItemsSource = new List<double> { 1, 2 } };

    private static TestClamChartView Chart(params ChartSeries[] series)
    {
        var view = new TestClamChartView();
        foreach (var item in series)
        {
            view.Series.Add(item);
        }

        return view;
    }

    private static void Paint(params TestClamChartView[] views)
    {
        foreach (var view in views)
        {
            using var bitmap = view.Paint(360, 240);
        }
    }
}
