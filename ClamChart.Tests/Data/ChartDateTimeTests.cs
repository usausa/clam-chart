namespace ClamChart.Tests.Data;

public sealed class ChartDateTimeTests
{
    [Theory]
    [InlineData(Double.NaN, 0)]
    [InlineData(-5, 0)]
    [InlineData(1e30, 3155378975999999999)]
    [InlineData(12345, 12345)]
    public void TicksAreLimitedToTheCalendar(double ticks, long expected)
    {
        // Act & Assert
        Assert.Equal(expected, ChartDateTime.ClampTicks(ticks));
    }

    [Fact]
    public void TheLastTickIsTheLastTime()
    {
        // Act & Assert
        Assert.Equal(DateTime.MaxValue, ChartDateTime.FromTicks(DateTime.MaxValue.Ticks));
    }
}
