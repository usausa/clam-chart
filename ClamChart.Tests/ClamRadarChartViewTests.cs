namespace ClamChart.Tests;

public sealed class ClamRadarChartViewTests
{
    [Fact]
    public void DefaultsFollowTheSpecification()
    {
        // Arrange
        using var view = new ClamRadarChartView();
        var series = new RadarSeries();

        // Assert
        Assert.Empty(view.Series);
        Assert.Null(view.Minimum);
        Assert.Null(view.Maximum);
        Assert.Equal(ChartRadarGridShape.Polygon, view.GridShape);
        Assert.Equal(5, view.LevelCount);
        Assert.Null(view.LegendVisible);
        Assert.Equal(ChartLegendPosition.Bottom, view.LegendPosition);
        Assert.False(view.LegendToggleEnabled);
        Assert.Equal(ChartSelectionMode.None, view.SelectionMode);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Null(view.SelectedSeries);
        Assert.Equal(ChartTrackballMode.None, view.TrackballMode);
        Assert.True(view.TooltipVisible);
        Assert.Equal(-1, view.TrackedIndex);
        Assert.Equal(2d, series.Thickness);
        Assert.Equal(0.15, series.FillOpacity);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new ClamRadarChartView();
        var series = new RadarSeries();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.LevelCount = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Minimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Maximum = Double.PositiveInfinity);
        Assert.Throws<ArgumentNullException>(() => view.Series = null!);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.FillOpacity = 1.5);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.FillOpacity = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Thickness = -1);
        view.Minimum = null;
        series.FillOpacity = 0;
        Assert.Equal(0, series.FillOpacity);
    }

    [Fact]
    public void RadarSeriesCannotBeUsedByTheCartesianChart()
    {
        // Arrange
        using var view = new ClamChartView();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => view.Series.Add(new RadarSeries()));
    }

    [Fact]
    public void SeriesBelongsToOneChart()
    {
        // Arrange
        using var first = new ClamRadarChartView();
        using var second = new ClamRadarChartView();
        var series = new RadarSeries();
        first.Series.Add(series);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => second.Series.Add(series));
        first.Series.Clear();
        second.Series.Add(series);
        Assert.Same(second, series.Parent);
    }

    [Fact]
    public void SummaryNamesTheHighestAndLowestItems()
    {
        // Arrange
        using var view = new TestClamRadarChartView { Culture = CultureInfo.InvariantCulture };
        view.Series.Add(new RadarSeries { Title = "A", ItemsSource = Entries(("Speed", 80), ("Power", 40), ("Range", 60)) });
        view.Series.Add(new RadarSeries { Title = "B", ItemsSource = Entries(("Speed", 50), ("Power", 90)) });

        // Act
        using var bitmap = view.Paint(360, 300);

        // Assert
        Assert.Equal("A: 3 items, max 80 (Speed), min 40 (Power); B: 2 items, max 90 (Power), min 50 (Speed)", SemanticProperties.GetDescription(view));
    }

    [Fact]
    public void InvertedBoundsAreReportedOnce()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = new TestClamRadarChartView { Minimum = 10, Maximum = 5 };
        view.Series.Add(new RadarSeries { ItemsSource = Entries(("A", 3), ("B", 4), ("C", 2)) });
        var message = "The radar chart Maximum is not greater than Minimum";
        var before = capture.Count(message);

        // Act
        using var first = view.Paint(300, 300);
        view.LevelCount = 4;
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(before + 1, capture.Count(message));
        Assert.Equal(0, view.ChartLayout.Scale.Minimum);
        Assert.Equal(4, view.ChartLayout.Scale.Maximum);
    }

    [Fact]
    public void ChangingTheSeriesUpdatesTheAxes()
    {
        // Arrange
        using var view = new TestClamRadarChartView();
        var items = new ObservableCollection<ChartEntry>([new("A", 1), new("B", 2), new("C", 3)]);
        view.Series.Add(new RadarSeries { ItemsSource = items });
        view.OnAttached();
        using var first = view.Paint(300, 300);

        // Act
        items.Add(new ChartEntry("D", 4));
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(["A", "B", "C", "D"], view.ChartLayout.Axes.Select(static x => x.Text));
        Assert.Equal(4, view.RenderedPointCount);
    }

    private static List<ChartEntry> Entries(params (string Label, double Value)[] values) =>
        [.. values.Select(static x => new ChartEntry(x.Label, x.Value))];
}
