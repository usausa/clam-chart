namespace ClamChart.Tests.Layout;

public sealed class TimeIntervalTests
{
    [Theory]
    [InlineData(ChartDateTimeIntervalType.Seconds, 10, "HH:mm:ss")]
    [InlineData(ChartDateTimeIntervalType.Seconds, 90, "HH:mm:ss")]
    [InlineData(ChartDateTimeIntervalType.Seconds, 120, "HH:mm")]
    [InlineData(ChartDateTimeIntervalType.Minutes, 15, "HH:mm")]
    [InlineData(ChartDateTimeIntervalType.Hours, 6, "HH:mm")]
    [InlineData(ChartDateTimeIntervalType.Hours, 36, "HH:mm")]
    [InlineData(ChartDateTimeIntervalType.Hours, 24, "M/d")]
    [InlineData(ChartDateTimeIntervalType.Days, 2, "M/d")]
    [InlineData(ChartDateTimeIntervalType.Weeks, 1, "M/d")]
    [InlineData(ChartDateTimeIntervalType.Months, 1, "M/d")]
    [InlineData(ChartDateTimeIntervalType.Months, 3, "yyyy/M")]
    [InlineData(ChartDateTimeIntervalType.Years, 2, "yyyy/M")]
    public void DefaultFormatFollowsTheInterval(ChartDateTimeIntervalType unit, int count, string format)
    {
        // Act & Assert
        Assert.Equal(format, new TimeInterval(unit, count).DefaultFormat);
    }

    [Fact]
    public void LabelPrecisionFollowsTheInterval()
    {
        // Act & Assert
        Assert.Equal(TimeLabelPrecision.Second, new TimeInterval(ChartDateTimeIntervalType.Seconds, 30).LabelPrecision);
        Assert.Equal(TimeLabelPrecision.Minute, new TimeInterval(ChartDateTimeIntervalType.Hours, 12).LabelPrecision);
        Assert.Equal(TimeLabelPrecision.Day, new TimeInterval(ChartDateTimeIntervalType.Months, 1).LabelPrecision);
        Assert.Equal(TimeLabelPrecision.Month, new TimeInterval(ChartDateTimeIntervalType.Months, 6).LabelPrecision);
    }

    [Fact]
    public void MinimumTicksUseTheShortestMonthAndYear()
    {
        // Act & Assert
        Assert.Equal(TimeSpan.FromMinutes(15).Ticks, new TimeInterval(ChartDateTimeIntervalType.Minutes, 15).MinimumTicks);
        Assert.Equal(TimeSpan.FromDays(14).Ticks, new TimeInterval(ChartDateTimeIntervalType.Weeks, 2).MinimumTicks);
        Assert.Equal(TimeSpan.FromDays(28).Ticks, new TimeInterval(ChartDateTimeIntervalType.Months, 1).MinimumTicks);
        Assert.Equal(TimeSpan.FromDays(365).Ticks, new TimeInterval(ChartDateTimeIntervalType.Years, 1).MinimumTicks);
    }

    [Theory]
    [InlineData(ChartDateTimeIntervalType.Hours, 1.5, ChartDateTimeIntervalType.Minutes, 90)]
    [InlineData(ChartDateTimeIntervalType.Weeks, 0.5, ChartDateTimeIntervalType.Hours, 84)]
    [InlineData(ChartDateTimeIntervalType.Days, 2, ChartDateTimeIntervalType.Days, 2)]
    [InlineData(ChartDateTimeIntervalType.Months, 1.4, ChartDateTimeIntervalType.Months, 1)]
    [InlineData(ChartDateTimeIntervalType.Years, 2.5, ChartDateTimeIntervalType.Years, 3)]
    [InlineData(ChartDateTimeIntervalType.Seconds, 0.4, ChartDateTimeIntervalType.Seconds, 1)]
    public void FixedIntervalUsesWholeUnits(ChartDateTimeIntervalType unit, double count, ChartDateTimeIntervalType expectedUnit, int expectedCount)
    {
        // Act & Assert
        Assert.Equal(new TimeInterval(expectedUnit, expectedCount), TimeInterval.FromUnits(unit, count));
    }

    [Fact]
    public void MinimumTicksAreLimitedToTheCalendar()
    {
        // Act & Assert
        Assert.Equal(DateTime.MaxValue.Ticks, new TimeInterval(ChartDateTimeIntervalType.Weeks, Int32.MaxValue).MinimumTicks);
        Assert.Equal(TimeSpan.TicksPerSecond, new TimeInterval(ChartDateTimeIntervalType.Seconds, 0).MinimumTicks);
    }

    [Fact]
    public void ValuePrecisionIsTheFinestPartOfTheTimes()
    {
        // Arrange
        double[] months = [new DateTime(2026, 1, 1).Ticks, new DateTime(2026, 2, 1).Ticks];
        double[] days = [new DateTime(2026, 1, 1).Ticks, new DateTime(2026, 1, 2).Ticks];
        double[] minutes = [new DateTime(2026, 1, 1).Ticks, new DateTime(2026, 1, 1, 14, 0, 0).Ticks];
        double[] seconds = [Double.NaN, new DateTime(2026, 1, 1, 14, 0, 30).Ticks];

        // Act & Assert
        Assert.Equal(TimeLabelPrecision.Month, TimeInterval.GetValuePrecision(months, TimeLabelPrecision.Month));
        Assert.Equal(TimeLabelPrecision.Day, TimeInterval.GetValuePrecision(days, TimeLabelPrecision.Month));
        Assert.Equal(TimeLabelPrecision.Minute, TimeInterval.GetValuePrecision(minutes, TimeLabelPrecision.Month));
        Assert.Equal(TimeLabelPrecision.Second, TimeInterval.GetValuePrecision(seconds, TimeLabelPrecision.Month));
        Assert.Equal(TimeLabelPrecision.Minute, TimeInterval.GetValuePrecision(months, TimeLabelPrecision.Minute));
    }

    [Fact]
    public void TrackingFormatAddsTheDateWhenTheRangeSpansDays()
    {
        // Act & Assert
        Assert.Equal("HH:mm:ss", TimeInterval.GetTrackingFormat(TimeLabelPrecision.Second, false));
        Assert.Equal("M/d HH:mm:ss", TimeInterval.GetTrackingFormat(TimeLabelPrecision.Second, true));
        Assert.Equal("HH:mm", TimeInterval.GetTrackingFormat(TimeLabelPrecision.Minute, false));
        Assert.Equal("M/d HH:mm", TimeInterval.GetTrackingFormat(TimeLabelPrecision.Minute, true));
        Assert.Equal("M/d", TimeInterval.GetTrackingFormat(TimeLabelPrecision.Day, true));
        Assert.Equal("yyyy/M", TimeInterval.GetTrackingFormat(TimeLabelPrecision.Month, true));
    }
}
