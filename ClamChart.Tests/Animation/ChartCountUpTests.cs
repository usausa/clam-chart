namespace ClamChart.Tests.Animation;

using ClamChart.Animation;

public sealed class ChartCountUpTests
{
    [Theory]
    [InlineData(0d, 0)]
    [InlineData(1284d, 0)]
    [InlineData(-12900d, 0)]
    [InlineData(98.6, 1)]
    [InlineData(0.30000000000000004, 1)]
    [InlineData(0.25, 2)]
    [InlineData(0.125, 2)]
    [InlineData(Double.NaN, 0)]
    public void DigitsFollowTheShownDecimals(double value, int expected)
    {
        // Act & Assert
        Assert.Equal(expected, ChartCountUp.GetDigits(value));
    }

    [Fact]
    public void IntermediateValuesAreRoundedOnlyWithDigits()
    {
        // Act & Assert
        Assert.Equal(617, ChartCountUp.Interpolate(0, 1234, 0.5, 0));
        Assert.Equal(49.3, ChartCountUp.Interpolate(0, 98.6, 0.5, 1), 9);
        Assert.Equal(0.123456, ChartCountUp.Interpolate(0, 0.246912, 0.5, -1), 9);
        Assert.Equal(-50, ChartCountUp.Interpolate(0, -100, 0.5, 0));
    }
}
