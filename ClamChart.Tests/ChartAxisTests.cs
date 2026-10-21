namespace ClamChart.Tests;

public sealed class ChartAxisTests
{
    private static readonly AxisContext Context = new(true, true, false, true, null, CultureInfo.InvariantCulture);

    [Fact]
    public void NumericAxisRejectsNonFiniteBoundsAndNonPositiveIntervals()
    {
        // Arrange
        var axis = new NumericAxis();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Minimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Maximum = Double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.SoftMinimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.SoftMaximum = Double.NegativeInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Interval = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Interval = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.MaxLabelCount = 0);
        axis.Minimum = -5;
        axis.Interval = 2.5;
        axis.MaxLabelCount = 1;
        Assert.Equal(-5, axis.Minimum);
        Assert.Equal(2.5, axis.Interval);
        Assert.Equal(1, axis.MaxLabelCount);
    }

    [Fact]
    public void DateTimeAxisRejectsNonPositiveIntervals()
    {
        // Arrange
        var axis = new DateTimeAxis();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Interval = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Interval = Double.NaN);
    }

    [Fact]
    public void NumericOptionsUseTheContextOnlyForUnsetValues()
    {
        // Arrange
        var axis = new NumericAxis { Minimum = 1, Maximum = 9, SoftMinimum = 0, Interval = 2, NiceRange = false, Title = "Sales", LabelFormat = "{0}%" };

        // Act
        var options = axis.CreateOptions(Context with { IncludeZero = true });

        // Assert
        Assert.Equal(ChartAxisKind.Numeric, options.Kind);
        Assert.True(options.IsVisible);
        Assert.Equal("Sales", options.Title);
        Assert.Equal(new AxisRangeOptions { Minimum = 1, Maximum = 9, SoftMinimum = 0, Interval = 2, IncludeZero = true, NiceRange = false }, options.Range);
        Assert.Equal("50%", options.LabelFormat.Format(50, CultureInfo.InvariantCulture));
        Assert.True(options.GridLinesVisible);

        // Act
        axis.IncludeZero = false;
        axis.IsVisible = false;
        axis.GridLinesVisible = true;
        options = axis.CreateOptions(Context with { IncludeZero = true, TitleVisible = false });

        // Assert
        Assert.False(options.Range.IncludeZero);
        Assert.False(options.IsVisible);
        Assert.True(options.GridLinesVisible);
        Assert.Null(options.Title);
    }

    [Fact]
    public void GridLinesFollowTheVisibilityOfAValueAxis()
    {
        // Arrange
        var axis = new NumericAxis();

        // Act & Assert
        Assert.True(axis.CreateOptions(Context).GridLinesVisible);
        Assert.False(axis.CreateOptions(Context with { ValueAxis = false }).GridLinesVisible);
        Assert.False(axis.CreateOptions(Context with { Visible = false }).GridLinesVisible);
    }

    [Fact]
    public void ExplicitLabelCountWinsOverTheDensity()
    {
        // Arrange
        var axis = new CategoryAxis();

        // Act & Assert
        Assert.Null(axis.CreateOptions(Context).MaxLabelCount);
        Assert.Equal(2, axis.CreateOptions(Context with { MaxLabelCount = 2 }).MaxLabelCount);
        axis.MaxLabelCount = 5;
        Assert.Equal(5, axis.CreateOptions(Context with { MaxLabelCount = 2 }).MaxLabelCount);
    }

    [Fact]
    public void DateTimeOptionsConvertTheIntervalAndTheBounds()
    {
        // Arrange
        var minimum = new DateTime(2026, 10, 19, 9, 0, 0);
        var maximum = new DateTime(2026, 10, 19, 1, 0, 0, DateTimeKind.Utc);
        var axis = new DateTimeAxis { Minimum = minimum, Maximum = maximum, IntervalType = ChartDateTimeIntervalType.Hours, Interval = 1.5 };

        // Act
        var options = axis.CreateOptions(Context with { Culture = CultureInfo.GetCultureInfo("de-DE") });

        // Assert
        Assert.Equal(ChartAxisKind.DateTime, options.Kind);
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Minutes, 90), options.TimeInterval);
        Assert.Equal(ChartDateTimeIntervalType.Hours, options.MinimumTimeUnit);
        Assert.Equal(DayOfWeek.Monday, options.FirstDayOfWeek);
        Assert.Equal(minimum.Ticks, options.Range.Minimum);
        Assert.Equal(maximum.ToLocalTime().Ticks, options.Range.Maximum);
    }

    [Fact]
    public void DateTimeWindowReplacesTheMinimum()
    {
        // Arrange
        var minimum = new DateTime(2026, 10, 21, 11, 0, 0);
        var maximum = new DateTime(2026, 10, 21, 10, 0, 0);
        var axis = new DateTimeAxis { Minimum = minimum, Maximum = maximum, WindowSize = TimeSpan.FromMinutes(5) };

        // Act
        var options = axis.CreateOptions(Context);

        // Assert
        Assert.Null(options.Range.Minimum);
        Assert.Equal(maximum.Ticks, options.Range.Maximum);
        Assert.Equal(TimeSpan.FromMinutes(5).Ticks, options.Range.Window);
        Assert.False(AxisRangeResolver.HasInvertedBounds(options.Range));
    }

    [Fact]
    public void DateTimeAxisRejectsNonPositiveWindows()
    {
        // Arrange
        var axis = new DateTimeAxis();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.WindowSize = TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.WindowSize = TimeSpan.FromSeconds(-1));
        axis.WindowSize = null;
        Assert.Null(axis.WindowSize);
    }

    [Fact]
    public void DateTimeIntervalNeedsAUnit()
    {
        // Arrange
        var axis = new DateTimeAxis { Interval = 2 };

        // Act & Assert
        Assert.Null(axis.CreateOptions(Context).TimeInterval);

        // Act
        axis.IntervalType = ChartDateTimeIntervalType.Days;
        axis.Interval = null;
        var options = axis.CreateOptions(Context);

        // Assert
        Assert.Null(options.TimeInterval);
        Assert.Equal(ChartDateTimeIntervalType.Days, options.MinimumTimeUnit);
    }

    [Fact]
    public void DateTimeFormatterReceivesTheTime()
    {
        // Arrange
        var axis = new DateTimeAxis { LabelFormatter = static x => x.ToString("yyyy-MM", CultureInfo.InvariantCulture) };

        // Act
        var formatter = axis.CreateOptions(Context).LabelFormatter;

        // Assert
        Assert.NotNull(formatter);
        Assert.Equal("2026-10", formatter(new DateTime(2026, 10, 19).Ticks));
    }

    [Fact]
    public void CategoryOptionsKeepThePlacementAndTheFormatter()
    {
        // Arrange
        Func<string, string> formatter = static x => x.ToUpperInvariant();
        var axis = new CategoryAxis { LabelPlacement = ChartLabelPlacement.OnTicks, LabelFormatter = formatter };

        // Act
        var options = axis.CreateOptions(Context with { ValueAxis = false });

        // Assert
        Assert.Equal(ChartAxisKind.Category, options.Kind);
        Assert.Equal(ChartLabelPlacement.OnTicks, options.LabelPlacement);
        Assert.Same(formatter, options.CategoryFormatter);
        Assert.False(options.GridLinesVisible);
    }
}
