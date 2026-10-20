namespace ClamChart.Tests.Rendering;

public sealed class CompactNumberFormatTests
{
    [Theory]
    [InlineData(0d, "0")]
    [InlineData(0.004, "0")]
    [InlineData(-0.004, "0")]
    [InlineData(0.25, "0.25")]
    [InlineData(0.125, "0.13")]
    [InlineData(-0.125, "-0.13")]
    [InlineData(-12.5, "-12.5")]
    [InlineData(1284d, "1,284")]
    [InlineData(9999d, "9,999")]
    [InlineData(10000d, "10K")]
    [InlineData(12900d, "12.9K")]
    [InlineData(-12900d, "-12.9K")]
    [InlineData(99999d, "100K")]
    [InlineData(125000d, "125K")]
    [InlineData(999950d, "1M")]
    [InlineData(4200000d, "4.2M")]
    [InlineData(1250000000d, "1.25B")]
    [InlineData(3.5e12, "3.5T")]
    [InlineData(1e15, "1,000T")]
    public void LargeValuesAreShortenedWithThreeSignificantDigits(double value, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, CompactNumberFormat.Format(value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void SeparatorsFollowTheCulture()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("de-DE");

        // Act & Assert
        Assert.Equal("1.284", CompactNumberFormat.Format(1284, culture));
        Assert.Equal("0,25", CompactNumberFormat.Format(0.25, culture));
        Assert.Equal("12,9K", CompactNumberFormat.Format(12900, culture));
    }

    [Fact]
    public void NonFiniteValuesUseTheCultureText()
    {
        // Act & Assert
        Assert.Equal(Double.NaN.ToString(CultureInfo.InvariantCulture), CompactNumberFormat.Format(Double.NaN, CultureInfo.InvariantCulture));
        Assert.Equal(Double.PositiveInfinity.ToString(CultureInfo.InvariantCulture), CompactNumberFormat.Format(Double.PositiveInfinity, CultureInfo.InvariantCulture));
    }
}
