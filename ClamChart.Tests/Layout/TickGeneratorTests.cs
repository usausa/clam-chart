namespace ClamChart.Tests.Layout;

public sealed class TickGeneratorTests
{
    private static readonly TimeLabelWidths Widths = new(45, 30, 28, 40);

    [Theory]
    [InlineData(200, TickGenerator.VerticalLabelSpacing, null, 5)]
    [InlineData(239, TickGenerator.VerticalLabelSpacing, null, 5)]
    [InlineData(240, TickGenerator.VerticalLabelSpacing, null, 6)]
    [InlineData(360, TickGenerator.HorizontalLabelSpacing, null, 5)]
    [InlineData(63, TickGenerator.HorizontalLabelSpacing, null, 1)]
    [InlineData(200, TickGenerator.VerticalLabelSpacing, 3, 2)]
    [InlineData(200, TickGenerator.VerticalLabelSpacing, 1, 1)]
    [InlineData(200, TickGenerator.VerticalLabelSpacing, 0, 1)]
    [InlineData(Double.NaN, TickGenerator.VerticalLabelSpacing, null, 1)]
    [InlineData(1e12, TickGenerator.VerticalLabelSpacing, null, 1000)]
    public void MaxDivisionsComeFromTheLengthOrTheLabelCount(double length, double spacing, int? maxLabelCount, int expected)
    {
        // Act & Assert
        Assert.Equal(expected, TickGenerator.GetMaxDivisions(length, spacing, maxLabelCount));
    }

    [Fact]
    public void NumericTicksIncludeBothNiceEnds()
    {
        // Act & Assert
        Assert.Equal([0d, 20, 40, 60, 80, 100], Numeric(new AxisScale(0, 100, 20)));
    }

    [Fact]
    public void NumericTicksStayInsideAnUnexpandedRange()
    {
        // Act & Assert
        Assert.Equal([20d, 40, 60, 80], Numeric(new AxisScale(3, 97, 20)));
    }

    [Fact]
    public void NumericTicksHaveNoRoundingNoise()
    {
        // Act & Assert
        Assert.Equal([0, 0.1, 0.2, 0.3], Numeric(new AxisScale(0, 0.3, 0.1)));
        Assert.Equal([-0.3, -0.2, -0.1, 0], Numeric(new AxisScale(-0.3, 0, 0.1)));
    }

    [Fact]
    public void NumericTicksOfExtremeScales()
    {
        // Act & Assert
        Assert.Equal([0, 2.5e-12, 5e-12, 7.5e-12, 1e-11], Numeric(new AxisScale(0, 1e-11, 2.5e-12)));
        Assert.Equal([0, 2.5e12, 5e12, 7.5e12, 1e13], Numeric(new AxisScale(0, 1e13, 2.5e12)));
    }

    [Fact]
    public void NumericTicksAreLimited()
    {
        // Act
        var ticks = Numeric(new AxisScale(0, 1e6, 1));

        // Assert
        Assert.Equal(TickGenerator.MaxTickCount, ticks.Count);
        Assert.Equal(0, ticks[0]);
        Assert.Equal(1001, ticks[1]);
    }

    [Fact]
    public void NumericTicksOfAnInvalidScaleAreEmpty()
    {
        // Act & Assert
        Assert.Empty(Numeric(new AxisScale(0, 100, 0)));
        Assert.Empty(Numeric(new AxisScale(100, 0, 20)));
    }

    [Fact]
    public void CategoryTicksAreTheIndexes()
    {
        // Arrange
        var ticks = new List<double>();

        // Act
        TickGenerator.Category(5, ticks);

        // Assert
        Assert.Equal([0d, 1, 2, 3, 4], ticks);
    }

    [Fact]
    public void CategoryPositionsDependOnThePlacement()
    {
        // Arrange
        var between = AxisRangeResolver.ResolveCategory(5, ChartLabelPlacement.BetweenTicks);
        var on = AxisRangeResolver.ResolveCategory(5, ChartLabelPlacement.OnTicks);

        // Act & Assert
        Assert.Equal(0.1, (0 - between.Minimum) / between.Span, 12);
        Assert.Equal(0.9, (4 - between.Minimum) / between.Span, 12);
        Assert.Equal(0, (0 - on.Minimum) / on.Span);
        Assert.Equal(1, (4 - on.Minimum) / on.Span);
    }

    [Fact]
    public void AlignedTicksAreAtTheHeightsOfThePrimaryTicks()
    {
        // Arrange
        var primary = new AxisScale(0, 100, 20);

        // Act & Assert
        Assert.Equal([0d, 10, 20, 30, 40, 50], Aligned(primary, new AxisScale(0, 50, 10)));
    }

    [Fact]
    public void AlignedTicksOfFixedBoundsKeepTheEnds()
    {
        // Act
        var ticks = Aligned(new AxisScale(0, 30, 10), new AxisScale(0, 100, 100.0 / 3));

        // Assert
        Assert.Equal(4, ticks.Count);
        Assert.Equal(0, ticks[0]);
        Assert.Equal(33.333333333, ticks[1], 9);
        Assert.Equal(66.666666667, ticks[2], 9);
        Assert.Equal(100, ticks[3]);
    }

    [Fact]
    public void AlignedTicksOfAPrimaryWhoseEndsAreNotTicksAreNice()
    {
        // Arrange
        var primary = new AxisScale(3, 97, 20);
        var secondary = AxisRangeResolver.ResolveAligned(0, 37, new AxisRangeOptions(), primary);

        // Act & Assert
        Assert.Equal([0d, 10, 20, 30], Aligned(primary, secondary));
    }

    [Fact]
    public void AlignedTicksOffTheMultiplesHaveNoRoundingNoise()
    {
        // Arrange
        var primary = new AxisScale(0, 100, 20);
        var secondary = AxisRangeResolver.ResolveAligned(5, 37, new AxisRangeOptions { Minimum = 0.1 }, primary);

        // Act & Assert
        Assert.Equal([0.1, 10.1, 20.1, 30.1, 40.1, 50.1], Aligned(primary, secondary));
    }

    [Theory]
    [InlineData("00:00:30", ChartDateTimeIntervalType.Seconds, 10)]
    [InlineData("00:10:00", ChartDateTimeIntervalType.Minutes, 2)]
    [InlineData("01:00:00", ChartDateTimeIntervalType.Minutes, 15)]
    [InlineData("1.00:00:00", ChartDateTimeIntervalType.Hours, 6)]
    [InlineData("7.00:00:00", ChartDateTimeIntervalType.Days, 2)]
    [InlineData("30.00:00:00", ChartDateTimeIntervalType.Weeks, 1)]
    [InlineData("90.00:00:00", ChartDateTimeIntervalType.Months, 1)]
    [InlineData("365.00:00:00", ChartDateTimeIntervalType.Months, 3)]
    [InlineData("2920.00:00:00", ChartDateTimeIntervalType.Years, 2)]
    [InlineData("3285000.00:00:00", ChartDateTimeIntervalType.Years, 2000)]
    public void TimeIntervalIsTheSmallestStepThatFits(string span, ChartDateTimeIntervalType unit, int count)
    {
        // Act
        var interval = TickGenerator.SelectTimeInterval(TimeSpan.Parse(span, CultureInfo.InvariantCulture).Ticks, 360, 5, 16, Widths);

        // Assert
        Assert.Equal(new TimeInterval(unit, count), interval);
    }

    [Fact]
    public void TimeIntervalStartsFromTheMinimumUnit()
    {
        // Act & Assert
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Days, 1), TickGenerator.SelectTimeInterval(TimeSpan.FromDays(1).Ticks, 360, 5, 16, Widths, ChartDateTimeIntervalType.Days));
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Weeks, 1), TickGenerator.SelectTimeInterval(TimeSpan.FromDays(30).Ticks, 360, 5, 16, Widths, ChartDateTimeIntervalType.Weeks));
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Months, 1), TickGenerator.SelectTimeInterval(TimeSpan.FromHours(1).Ticks, 360, 5, 16, Widths, ChartDateTimeIntervalType.Months));
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Days, 1), TickGenerator.SelectTimeInterval(0, 360, 5, 16, Widths, ChartDateTimeIntervalType.Days));
    }

    [Fact]
    public void TimeIntervalLeavesRoomForTheLabels()
    {
        // Arrange
        var widths = new TimeLabelWidths(45, 60, 28, 40);

        // Act
        var interval = TickGenerator.SelectTimeInterval(TimeSpan.FromMinutes(10).Ticks, 360, 100, 16, widths);

        // Assert
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Minutes, 5), interval);
    }

    [Fact]
    public void TimeIntervalOfAnEmptySpanIsOneSecond()
    {
        // Act & Assert
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Seconds, 1), TickGenerator.SelectTimeInterval(0, 360, 5, 16, Widths));
    }

    [Fact]
    public void TimeIntervalFallsBackToTheLargestStep()
    {
        // Act & Assert
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Years, 5000), TickGenerator.SelectTimeInterval(1e19, 360, 5, 16, Widths));
    }

    [Fact]
    public void MinuteTicksAreAlignedToTheClock()
    {
        // Act
        var ticks = Time(new DateTime(2026, 10, 19, 10, 3, 0), new DateTime(2026, 10, 19, 11, 1, 0), new TimeInterval(ChartDateTimeIntervalType.Minutes, 15));

        // Assert
        Assert.Equal(
            [
                new DateTime(2026, 10, 19, 10, 15, 0),
                new DateTime(2026, 10, 19, 10, 30, 0),
                new DateTime(2026, 10, 19, 10, 45, 0),
                new DateTime(2026, 10, 19, 11, 0, 0)
            ],
            ticks);
    }

    [Fact]
    public void TicksIncludeBothEndsOnABoundary()
    {
        // Act
        var ticks = Time(new DateTime(2026, 10, 19, 10, 0, 0), new DateTime(2026, 10, 19, 11, 0, 0), new TimeInterval(ChartDateTimeIntervalType.Minutes, 15));

        // Assert
        Assert.Equal(5, ticks.Length);
        Assert.Equal(new DateTime(2026, 10, 19, 10, 0, 0), ticks[0]);
        Assert.Equal(new DateTime(2026, 10, 19, 11, 0, 0), ticks[^1]);
    }

    [Fact]
    public void HourTicksCrossMidnight()
    {
        // Act
        var ticks = Time(new DateTime(2026, 10, 18, 20, 0, 0), new DateTime(2026, 10, 19, 13, 0, 0), new TimeInterval(ChartDateTimeIntervalType.Hours, 6));

        // Assert
        Assert.Equal([new DateTime(2026, 10, 19, 0, 0, 0), new DateTime(2026, 10, 19, 6, 0, 0), new DateTime(2026, 10, 19, 12, 0, 0)], ticks);
    }

    [Fact]
    public void DayTicksKeepARegularSpacingAcrossMonths()
    {
        // Arrange
        var start = new DateTime(2026, 1, 30, 0, 0, 0);
        var end = new DateTime(2026, 2, 6, 0, 0, 0);

        // Act
        var ticks = Time(start, end, new TimeInterval(ChartDateTimeIntervalType.Days, 2));

        // Assert
        Assert.True(ticks.Length >= 3);
        Assert.True(ticks[0] >= start);
        Assert.True(ticks[0] < start.AddDays(2));
        Assert.True(ticks[^1] <= end);
        Assert.All(ticks, static x => Assert.Equal(TimeSpan.Zero, x.TimeOfDay));
        Assert.All(ticks.Zip(ticks.Skip(1)), static x => Assert.Equal(TimeSpan.FromDays(2), x.Second - x.First));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, 4)]
    [InlineData(DayOfWeek.Monday, 5)]
    public void WeekTicksStartOnTheFirstDayOfTheWeek(DayOfWeek firstDayOfWeek, int firstDay)
    {
        // Act
        var ticks = Time(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), new TimeInterval(ChartDateTimeIntervalType.Weeks, 1), firstDayOfWeek);

        // Assert
        Assert.Equal(4, ticks.Length);
        Assert.Equal(new DateTime(2026, 10, firstDay), ticks[0]);
        Assert.All(ticks, x => Assert.Equal(firstDayOfWeek, x.DayOfWeek));
    }

    [Fact]
    public void MonthTicksAreOnTheFirstDay()
    {
        // Act
        var ticks = Time(new DateTime(2026, 1, 15), new DateTime(2026, 5, 1), new TimeInterval(ChartDateTimeIntervalType.Months, 1));

        // Assert
        Assert.Equal([new DateTime(2026, 2, 1), new DateTime(2026, 3, 1), new DateTime(2026, 4, 1), new DateTime(2026, 5, 1)], ticks);
    }

    [Fact]
    public void QuarterTicksAreAlignedToTheYear()
    {
        // Act
        var ticks = Time(new DateTime(2025, 11, 10), new DateTime(2026, 12, 31), new TimeInterval(ChartDateTimeIntervalType.Months, 3));

        // Assert
        Assert.Equal([new DateTime(2026, 1, 1), new DateTime(2026, 4, 1), new DateTime(2026, 7, 1), new DateTime(2026, 10, 1)], ticks);
    }

    [Fact]
    public void HalfYearTicksStartOnTheBoundaryMonth()
    {
        // Act
        var ticks = Time(new DateTime(2025, 3, 1), new DateTime(2026, 12, 31), new TimeInterval(ChartDateTimeIntervalType.Months, 6));

        // Assert
        Assert.Equal([new DateTime(2025, 7, 1), new DateTime(2026, 1, 1), new DateTime(2026, 7, 1)], ticks);
    }

    [Fact]
    public void MonthTicksIncludeAStartOnTheFirstDay()
    {
        // Act
        var ticks = Time(new DateTime(2026, 2, 1), new DateTime(2026, 4, 15), new TimeInterval(ChartDateTimeIntervalType.Months, 1));

        // Assert
        Assert.Equal([new DateTime(2026, 2, 1), new DateTime(2026, 3, 1), new DateTime(2026, 4, 1)], ticks);
    }

    [Fact]
    public void YearTicksAreMultiplesOfTheCount()
    {
        // Act
        var ticks = Time(new DateTime(2019, 6, 1), new DateTime(2026, 1, 1), new TimeInterval(ChartDateTimeIntervalType.Years, 2));

        // Assert
        Assert.Equal([new DateTime(2020, 1, 1), new DateTime(2022, 1, 1), new DateTime(2024, 1, 1), new DateTime(2026, 1, 1)], ticks);
    }

    [Fact]
    public void TicksNearTheEndsOfTheCalendarDoNotOverflow()
    {
        // Act & Assert
        Assert.Equal([new DateTime(9999, 1, 1)], Time(new DateTime(9998, 6, 1), DateTime.MaxValue, new TimeInterval(ChartDateTimeIntervalType.Years, 1)));
        Assert.Equal([new DateTime(9999, 12, 1)], Time(new DateTime(9999, 11, 15), DateTime.MaxValue, new TimeInterval(ChartDateTimeIntervalType.Months, 1)));
        Assert.Equal(new DateTime(1, 1, 7), Time(DateTime.MinValue, new DateTime(1, 2, 1), new TimeInterval(ChartDateTimeIntervalType.Weeks, 1))[0]);
    }

    [Fact]
    public void TimeTicksAreLimited()
    {
        // Act
        var ticks = Time(new DateTime(2026, 10, 19), new DateTime(2026, 10, 20), new TimeInterval(ChartDateTimeIntervalType.Seconds, 1));

        // Assert
        Assert.Equal(TickGenerator.MaxTickCount, ticks.Length);
    }

    [Fact]
    public void TimeTicksOfAnInvertedRangeAreEmpty()
    {
        // Act & Assert
        Assert.Empty(Time(new DateTime(2026, 10, 20), new DateTime(2026, 10, 19), new TimeInterval(ChartDateTimeIntervalType.Hours, 1)));
    }

    private static List<double> Numeric(AxisScale scale)
    {
        var ticks = new List<double>();
        TickGenerator.Numeric(scale, ticks);
        return ticks;
    }

    private static List<double> Aligned(AxisScale primary, AxisScale secondary)
    {
        var primaryTicks = Numeric(primary);
        var ticks = new List<double>();
        TickGenerator.Aligned(primary, primaryTicks, secondary, ticks);
        return ticks;
    }

    private static DateTime[] Time(DateTime start, DateTime end, TimeInterval interval, DayOfWeek firstDayOfWeek = DayOfWeek.Sunday)
    {
        var ticks = new List<double>();
        TickGenerator.Time(start.Ticks, end.Ticks, interval, firstDayOfWeek, ticks);
        return ticks.Select(static x => new DateTime((long)x)).ToArray();
    }
}
