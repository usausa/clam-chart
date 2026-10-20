namespace ClamChart.Tests.Layout;

public sealed class NiceScaleTests
{
    [Theory]
    [InlineData(0.7, 1)]
    [InlineData(1, 1)]
    [InlineData(1.2, 2)]
    [InlineData(2, 2)]
    [InlineData(2.2, 2.5)]
    [InlineData(2.5, 2.5)]
    [InlineData(3, 5)]
    [InlineData(5, 5)]
    [InlineData(6, 10)]
    [InlineData(12, 20)]
    [InlineData(0.3, 0.5)]
    [InlineData(0.11, 0.2)]
    [InlineData(0.024, 0.025)]
    [InlineData(3e-12, 5e-12)]
    [InlineData(3e12, 5e12)]
    public void StepIsOneTwoTwoAndAHalfOrFiveTimesAPowerOfTen(double rawStep, double expected)
    {
        // Act & Assert
        Assert.Equal(expected, NiceScale.Step(rawStep));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Double.NaN)]
    [InlineData(Double.PositiveInfinity)]
    public void StepOfAnInvalidValueIsOne(double rawStep)
    {
        // Act & Assert
        Assert.Equal(1, NiceScale.Step(rawStep));
    }

    [Fact]
    public void StepOfAnExtremeValueKeepsItsMagnitude()
    {
        // Act
        var small = NiceScale.Step(3e-300);
        var large = NiceScale.Step(3e300);

        // Assert
        Assert.Equal(5e-300, small, 5e-312);
        Assert.Equal(5e300, large, 5e288);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2.5)]
    [InlineData(2.5, 5)]
    [InlineData(5, 10)]
    [InlineData(10, 20)]
    [InlineData(0.1, 0.2)]
    [InlineData(0.25, 0.5)]
    [InlineData(0.5, 1)]
    public void NextStepIsTheFollowingNiceStep(double step, double expected)
    {
        // Act & Assert
        Assert.Equal(expected, NiceScale.NextStep(step));
    }

    [Theory]
    [InlineData(3, 0.1, 0.3)]
    [InlineData(7, 0.1, 0.7)]
    [InlineData(-3, 0.1, -0.3)]
    [InlineData(4, 2.5e-12, 1e-11)]
    [InlineData(3, 2.5e12, 7.5e12)]
    public void MultipleHasNoRoundingNoise(double index, double step, double expected)
    {
        // Act & Assert
        Assert.Equal(expected, NiceScale.Multiple(index, step));
    }

    [Fact]
    public void MultipleOfAnIrregularStepIsAPlainProduct()
    {
        // Act & Assert
        Assert.Equal(2.0 / 3, NiceScale.Multiple(2, 1.0 / 3));
    }

    [Fact]
    public void MultipleOfZeroIsPositiveZero()
    {
        // Act
        var value = NiceScale.Multiple(-0.0, 25);

        // Assert
        Assert.Equal(0, value);
        Assert.False(Double.IsNegative(value));
    }

    [Theory]
    [InlineData(0.3, 0.1, 0.3, 0.3)]
    [InlineData(-0.25, 0.1, -0.3, -0.2)]
    [InlineData(87, 25, 75, 100)]
    [InlineData(100, 25, 100, 100)]
    public void FloorAndCeilingRoundToMultiples(double value, double step, double floor, double ceiling)
    {
        // Act & Assert
        Assert.Equal(floor, NiceScale.Floor(value, step));
        Assert.Equal(ceiling, NiceScale.Ceiling(value, step));
    }

    [Theory]
    [InlineData(0.30000000000000004, 0.1, 0.3)]
    [InlineData(0.35, 0.1, 0.35)]
    [InlineData(16.599999999999998, 6.8, 16.6)]
    [InlineData(0.123456789, 0.2, 0.123456789)]
    [InlineData(33.33333333333333, 33.333333333333336, 33.33333333333333)]
    public void SnapRemovesTheNoiseOfARegularStep(double value, double step, double expected)
    {
        // Act & Assert
        Assert.Equal(expected, NiceScale.Snap(value, step));
    }

    [Theory]
    [InlineData(0, 87, 4, 0, 100, 25)]
    [InlineData(3, 97, 5, 0, 100, 20)]
    [InlineData(-37, 52, 5, -40, 60, 20)]
    [InlineData(-87, -3, 4, -100, 0, 25)]
    [InlineData(1e-12, 9e-12, 4, 0, 1e-11, 2.5e-12)]
    [InlineData(1e12, 9e12, 4, 0, 1e13, 2.5e12)]
    public void CreateExpandsBothEndsToMultiplesOfTheStep(double minimum, double maximum, int divisions, double low, double high, double step)
    {
        // Act & Assert
        Assert.Equal(new AxisScale(low, high, step), NiceScale.Create(minimum, maximum, divisions, true, true));
    }

    [Fact]
    public void CreateForNegativeValuesEndsAtPositiveZero()
    {
        // Act
        var scale = NiceScale.Create(-87, -3, 4, true, true);

        // Assert
        Assert.Equal(0, scale.Maximum);
        Assert.False(Double.IsNegative(scale.Maximum));
    }

    [Fact]
    public void CreateKeepsTheEndsThatAreNotNice()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(3, 100, 20), NiceScale.Create(3, 97, 5, false, true));
        Assert.Equal(new AxisScale(3, 97, 20), NiceScale.Create(3, 97, 5, false, false));
    }

    [Fact]
    public void CreateAcrossZeroWithOneDivisionUsesTwoSteps()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(-2, 2, 2), NiceScale.Create(-1, 1, 1, true, true));
    }

    [Fact]
    public void CreateWithExtremeValuesStaysFinite()
    {
        // Act
        var scale = NiceScale.Create(-1e300, 1e300, 5, true, true);

        // Assert
        Assert.True(Double.IsFinite(scale.Minimum));
        Assert.True(Double.IsFinite(scale.Maximum));
        Assert.True(scale.Minimum <= -1e300 * (1 - 1e-12));
        Assert.True(scale.Maximum >= 1e300 * (1 - 1e-12));
        Assert.True(scale.Span / scale.Interval <= 5 + 1e-9);
    }
}
