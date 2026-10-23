namespace ClamChart.Tests;

public sealed class ChartSeriesTests
{
    [Fact]
    public void LineSeriesRejectsNegativeAndNonFiniteSizes()
    {
        // Arrange
        var series = new LineSeries();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Thickness = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Thickness = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.MarkerSize = Double.PositiveInfinity);
        series.Thickness = 0;
        Assert.Equal(0, series.Thickness);
        Assert.Equal(ChartMarkerShape.Circle, series.MarkerShape);
    }

    [Fact]
    public void AreaSeriesRejectsInvalidOpacitiesAndBaselines()
    {
        // Arrange
        var series = new AreaSeries();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => series.FillOpacity = 1.5);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.FillOpacity = -0.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.BaselineValue = Double.NaN);
        series.FillOpacity = 1;
        Assert.Equal(1, series.FillOpacity);
    }

    [Fact]
    public void BarSeriesRejectsInvalidSizesAndRatios()
    {
        // Arrange
        var series = new BarSeries();

        // Act & Assert
        Assert.Equal(CartesianSeries.DefaultBarWidthRatio, series.BarWidthRatio);
        Assert.False(series.TrackVisible);
        Assert.Equal(ChartFillMode.Solid, series.FillMode);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.BarWidthRatio = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.BarWidthRatio = 1.5);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.BarWidthRatio = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.MaxBarWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.CornerRadius = -1);
        series.BarWidthRatio = 1;
        series.CornerRadius = 0;
        Assert.Equal(1, series.BarWidthRatio);
        Assert.Equal(0, series.CornerRadius);
    }

    [Fact]
    public void ScatterSeriesRejectsInvalidSizesAndOpacities()
    {
        // Arrange
        var series = new ScatterSeries();

        // Act & Assert
        Assert.Equal(ChartMarkerShape.Circle, series.MarkerShape);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.MarkerSize = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.MarkerSize = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.FillOpacity = 1.5);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.FillOpacity = Double.NaN);
        series.MarkerSize = 0;
        series.FillOpacity = 0;
        Assert.Equal(0, series.MarkerSize);
        Assert.Equal(0, series.FillOpacity);
        Assert.Equal(ChartSeriesKind.Scatter, series.CreateLayoutSeries().Kind);
    }

    [Fact]
    public void RangeBarSeriesReadsBothEndsWithTheBarSettings()
    {
        // Arrange
        var series = new RangeBarSeries { LowKey = "low", HighKey = "high", BarWidthRatio = 0.8, MaxBarWidth = 32 };

        // Act
        var input = series.CreateLayoutSeries();
        var options = series.CreateValueOptions(null, "day");

        // Assert
        Assert.False(series.TrackVisible);
        Assert.Null(series.CornerRadius);
        Assert.Equal(ChartSeriesKind.RangeBar, input.Kind);
        Assert.Equal(0.8, input.BarWidthRatio);
        Assert.Equal(32, input.MaxBarWidth);
        Assert.Equal("day", options.XKey);
        Assert.Equal("high", options.YKey);
        Assert.Equal("low", options.LowKey);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.BarWidthRatio = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.CornerRadius = -1);
    }

    [Fact]
    public void BubbleSeriesHasSizeLimitsAndReadsTheSizeKey()
    {
        // Arrange
        var series = new BubbleSeries { SizeKey = "population" };

        // Act
        var input = series.CreateLayoutSeries();
        var options = series.CreateValueOptions(null, null);

        // Assert
        Assert.Equal(8, series.MinBubbleSize);
        Assert.Equal(40, series.MaxBubbleSize);
        Assert.Equal(ChartSeriesKind.Scatter, input.Kind);
        Assert.True(input.IsBubble);
        Assert.Equal(8, input.MinBubbleSize);
        Assert.Equal(40, input.MaxBubbleSize);
        Assert.True(series.SizeIncludesZero);
        Assert.True(input.SizeIncludesZero);
        Assert.False(new BubbleSeries { SizeIncludesZero = false }.CreateLayoutSeries().SizeIncludesZero);
        Assert.Equal("population", options.SizeKey);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.MaxBubbleSize = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.MinBubbleSize = Double.NaN);
    }

    [Fact]
    public void BarLayoutInputCarriesTheBarSettings()
    {
        // Arrange
        var series = new BarSeries { StackGroup = "total", BarWidthRatio = 0.8, MaxBarWidth = 32, ValueFormat = "{0}円" };

        // Act
        var input = series.CreateLayoutSeries();

        // Assert
        Assert.Equal(ChartSeriesKind.Bar, input.Kind);
        Assert.Equal("total", input.StackGroup);
        Assert.Equal(0.8, input.BarWidthRatio);
        Assert.Equal(32, input.MaxBarWidth);
        Assert.Equal("5円", input.ValueFormat.Format(5, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void VisibilityIsBoundBothWays()
    {
        // Act & Assert
        Assert.Equal(BindingMode.TwoWay, ChartSeries.IsVisibleProperty.DefaultBindingMode);
    }

    [Fact]
    public void LayoutInputCarriesTheSeriesSettings()
    {
        // Arrange
        var series = new AreaSeries { Title = "Sales", YAxis = ChartAxisTarget.Secondary, StackGroup = "total", BaselineValue = 5, IsVisible = false, LegendVisible = false };

        // Act
        var input = series.CreateLayoutSeries();

        // Assert
        Assert.Equal(ChartSeriesKind.Area, input.Kind);
        Assert.Equal(ChartAxisTarget.Secondary, input.Axis);
        Assert.Equal("total", input.StackGroup);
        Assert.Equal(5, input.BaselineValue);
        Assert.False(input.IsVisible);
        Assert.False(input.LegendVisible);
        Assert.Equal("Sales", input.Title);
        Assert.Same(DataSnapshot.Empty, input.Data);
        Assert.True(input.ValueFormat.IsDefault);
        Assert.Null(input.ValueFormatter);
    }
}
