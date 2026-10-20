namespace ClamChart.Tests.Layout;

public sealed class AxisRangeResolverTests
{
    private static readonly AxisRangeOptions Default = new();

    [Fact]
    public void DataRangeIsExpandedToNiceValues()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 100, 20), AxisRangeResolver.Resolve(3, 97, Default, 5));
    }

    [Fact]
    public void EmptyDataIsZeroToOne()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 1, 0.2), AxisRangeResolver.Resolve(Double.NaN, Double.NaN, Default, 5));
    }

    [Theory]
    [InlineData(5, 4.5, 5.5)]
    [InlineData(-5, -5.5, -4.5)]
    [InlineData(0, 0, 1)]
    public void SingleValueIsPadded(double value, double low, double high)
    {
        // Act & Assert
        Assert.Equal(new AxisScale(low, high, 0.25), AxisRangeResolver.Resolve(value, value, Default, 4));
    }

    [Fact]
    public void SpanBelowThePrecisionOfTheValuesIsTreatedAsASingleValue()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(9e14, 1.1e15, 5e13), AxisRangeResolver.Resolve(1e15, 1e15 + 1, Default, 5));
    }

    [Fact]
    public void MinimumIsFixedAndMaximumIsAutomatic()
    {
        // Arrange
        var options = new AxisRangeOptions { Minimum = 10 };

        // Act & Assert
        Assert.Equal(new AxisScale(10, 100, 20), AxisRangeResolver.Resolve(12, 87, options, 5));
    }

    [Fact]
    public void MaximumIsFixedAndMinimumIsAutomatic()
    {
        // Arrange
        var options = new AxisRangeOptions { Maximum = 50 };

        // Act & Assert
        Assert.Equal(new AxisScale(10, 50, 10), AxisRangeResolver.Resolve(12, 87, options, 5));
    }

    [Fact]
    public void BothEndsAreFixed()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 35, 10), AxisRangeResolver.Resolve(12, 87, new AxisRangeOptions { Minimum = 0, Maximum = 35 }, 5));
        Assert.Equal(new AxisScale(0, 35, 5), AxisRangeResolver.Resolve(12, 87, new AxisRangeOptions { Minimum = 0, Maximum = 35, Interval = 5 }, 5));
    }

    [Theory]
    [InlineData(50, 10)]
    [InlineData(10, 10)]
    public void InvertedBoundsFallBackToTheAutomaticRange(double minimum, double maximum)
    {
        // Arrange
        var options = new AxisRangeOptions { Minimum = minimum, Maximum = maximum };

        // Act & Assert
        Assert.True(AxisRangeResolver.HasInvertedBounds(options));
        Assert.Equal(new AxisScale(0, 100, 20), AxisRangeResolver.Resolve(3, 97, options, 5));
    }

    [Fact]
    public void ValidOrOneSidedBoundsAreNotInverted()
    {
        // Act & Assert
        Assert.False(AxisRangeResolver.HasInvertedBounds(new AxisRangeOptions { Minimum = 0, Maximum = 100 }));
        Assert.False(AxisRangeResolver.HasInvertedBounds(new AxisRangeOptions { Minimum = 100 }));
        Assert.False(AxisRangeResolver.HasInvertedBounds(Default));
    }

    [Fact]
    public void FixedMinimumAboveTheDataKeepsAPositiveSpan()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(50, 55, 1), AxisRangeResolver.Resolve(3, 20, new AxisRangeOptions { Minimum = 50 }, 5));
        Assert.Equal(new AxisScale(0, 1, 0.2), AxisRangeResolver.Resolve(Double.NaN, Double.NaN, new AxisRangeOptions { Minimum = 0 }, 5));
    }

    [Fact]
    public void FixedMaximumBelowTheDataKeepsAPositiveSpan()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(9, 10, 0.2), AxisRangeResolver.Resolve(20, 30, new AxisRangeOptions { Maximum = 10 }, 5));
    }

    [Theory]
    [InlineData(20, 30, 0, 100, 20)]
    [InlineData(20, 150, 0, 150, 50)]
    [InlineData(-20, 30, -25, 100, 25)]
    public void SoftBoundsAreShownAndExtendedByTheData(double dataMinimum, double dataMaximum, double low, double high, double interval)
    {
        // Arrange
        var options = new AxisRangeOptions { SoftMinimum = 0, SoftMaximum = 100 };

        // Act & Assert
        Assert.Equal(new AxisScale(low, high, interval), AxisRangeResolver.Resolve(dataMinimum, dataMaximum, options, 5));
    }

    [Fact]
    public void SoftBoundAffectsOnlyItsOwnEnd()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(150, 200, 10), AxisRangeResolver.Resolve(150, 200, new AxisRangeOptions { SoftMaximum = 100 }, 5));
        Assert.Equal(new AxisScale(0, 1, 0.2), AxisRangeResolver.Resolve(Double.NaN, Double.NaN, new AxisRangeOptions { SoftMinimum = 0 }, 5));
    }

    [Fact]
    public void InvertedSoftBoundsWithoutDataAreSwapped()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 100, 20), AxisRangeResolver.Resolve(Double.NaN, Double.NaN, new AxisRangeOptions { SoftMinimum = 100, SoftMaximum = 0 }, 5));
    }

    [Theory]
    [InlineData(20, 87, 0, 100)]
    [InlineData(-87, -20, -100, 0)]
    public void IncludeZeroExtendsTheAutomaticEnd(double dataMinimum, double dataMaximum, double low, double high)
    {
        // Arrange
        var options = new AxisRangeOptions { IncludeZero = true };

        // Act & Assert
        Assert.Equal(new AxisScale(low, high, 20), AxisRangeResolver.Resolve(dataMinimum, dataMaximum, options, 5));
    }

    [Fact]
    public void IncludeZeroDoesNotMoveAFixedEnd()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(10, 100, 20), AxisRangeResolver.Resolve(20, 87, new AxisRangeOptions { Minimum = 10, IncludeZero = true }, 5));
    }

    [Fact]
    public void NiceRangeOffKeepsTheDataRange()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(3, 97, 20), AxisRangeResolver.Resolve(3, 97, new AxisRangeOptions { NiceRange = false }, 5));
    }

    [Fact]
    public void IntervalIsUsedForTheStepAndTheExpansion()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 100, 25), AxisRangeResolver.Resolve(3, 97, new AxisRangeOptions { Interval = 25 }, 5));
        Assert.Equal(new AxisScale(3, 97, 25), AxisRangeResolver.Resolve(3, 97, new AxisRangeOptions { Interval = 25, NiceRange = false }, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Double.NaN)]
    public void InvalidIntervalIsAutomatic(double interval)
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 100, 20), AxisRangeResolver.Resolve(3, 97, new AxisRangeOptions { Interval = interval }, 5));
    }

    [Fact]
    public void TinyValuesKeepTheirScale()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 1e-11, 2.5e-12), AxisRangeResolver.Resolve(1e-12, 9e-12, Default, 4));
    }

    [Fact]
    public void ExtremeValuesStayFinite()
    {
        // Act
        var scale = AxisRangeResolver.Resolve(-Double.MaxValue, Double.MaxValue, Default, 5);

        // Assert
        Assert.True(Double.IsFinite(scale.Minimum));
        Assert.True(Double.IsFinite(scale.Maximum));
        Assert.True(Double.IsFinite(scale.Span));
        Assert.True(scale.Interval > 0);
    }

    [Fact]
    public void SubnormalValuesAreTreatedAsZero()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 1, 0.25), AxisRangeResolver.Resolve(1e-310, 2e-310, Default, 4));
    }

    [Fact]
    public void NonFiniteDataIsIgnored()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 1, 0.2), AxisRangeResolver.Resolve(Double.NaN, Double.PositiveInfinity, Default, 5));
        Assert.Equal(new AxisScale(4.5, 5.5, 0.25), AxisRangeResolver.Resolve(5, Double.PositiveInfinity, Default, 4));
    }

    [Theory]
    [InlineData(0, 37, 0, 50, 10)]
    [InlineData(13, 37, 10, 60, 10)]
    public void AlignedAxisUsesTheDivisionsOfThePrimary(double dataMinimum, double dataMaximum, double low, double high, double interval)
    {
        // Act & Assert
        Assert.Equal(new AxisScale(low, high, interval), AxisRangeResolver.ResolveAligned(dataMinimum, dataMaximum, Default, new AxisScale(0, 100, 20)));
    }

    [Fact]
    public void AlignedAxisIncludesZero()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 100, 20), AxisRangeResolver.ResolveAligned(10, 80, new AxisRangeOptions { IncludeZero = true }, new AxisScale(0, 100, 20)));
    }

    [Fact]
    public void AlignedAxisWithFixedBoundsIsDividedEqually()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 100, 100.0 / 3), AxisRangeResolver.ResolveAligned(5, 37, new AxisRangeOptions { Minimum = 0, Maximum = 100 }, new AxisScale(0, 30, 10)));
    }

    [Fact]
    public void AlignedAxisWithOneFixedEndExtendsTheOtherEnd()
    {
        // Arrange
        var primary = new AxisScale(0, 100, 20);

        // Act & Assert
        Assert.Equal(new AxisScale(0, 50, 10), AxisRangeResolver.ResolveAligned(5, 37, new AxisRangeOptions { Minimum = 0 }, primary));
        Assert.Equal(new AxisScale(0, 100, 20), AxisRangeResolver.ResolveAligned(40, 80, new AxisRangeOptions { Maximum = 100 }, primary));
    }

    [Fact]
    public void AlignedAxisWithoutNiceRangeDividesTheDataRange()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(3, 37, 6.8), AxisRangeResolver.ResolveAligned(3, 37, new AxisRangeOptions { NiceRange = false }, new AxisScale(0, 100, 20)));
    }

    [Fact]
    public void AlignedAxisIgnoresItsInterval()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 50, 10), AxisRangeResolver.ResolveAligned(0, 37, new AxisRangeOptions { Interval = 7 }, new AxisScale(0, 100, 20)));
    }

    [Fact]
    public void AlignedAxisFollowsAPrimaryWhoseEndsAreNotTicks()
    {
        // Act
        var scale = AxisRangeResolver.ResolveAligned(0, 37, Default, new AxisScale(3, 97, 20));

        // Assert
        Assert.Equal(-8.5, scale.Minimum, 9);
        Assert.Equal(38.5, scale.Maximum, 9);
        Assert.Equal(10, scale.Interval);
    }

    [Fact]
    public void AlignedAxisWithInvertedBoundsIsAutomatic()
    {
        // Act & Assert
        Assert.Equal(new AxisScale(0, 50, 10), AxisRangeResolver.ResolveAligned(0, 37, new AxisRangeOptions { Minimum = 100, Maximum = 0 }, new AxisScale(0, 100, 20)));
    }

    [Fact]
    public void IndependentAxisUsesItsOwnRangeAndInterval()
    {
        // Arrange
        var primary = AxisRangeResolver.Resolve(3, 97, Default, 5);

        // Act
        var secondary = AxisRangeResolver.Resolve(30, 80, new AxisRangeOptions { Minimum = 0, Maximum = 100, Interval = 25 }, 5);

        // Assert
        Assert.Equal(new AxisScale(0, 100, 20), primary);
        Assert.Equal(new AxisScale(0, 100, 25), secondary);
    }

    [Fact]
    public void TimeRangeIsTheDataOrTheFixedBounds()
    {
        // Arrange
        double start = new DateTime(2026, 10, 19, 10, 0, 0).Ticks;
        var end = start + TimeSpan.TicksPerHour;
        var earlier = start - TimeSpan.TicksPerHour;

        // Act & Assert
        Assert.Equal((start, end), AxisRangeResolver.ResolveTime(start, end, Default));
        Assert.Equal((earlier, end), AxisRangeResolver.ResolveTime(start, end, new AxisRangeOptions { Minimum = earlier }));
        Assert.Equal((start, end), AxisRangeResolver.ResolveTime(start, end, new AxisRangeOptions { Minimum = end, Maximum = start }));
    }

    [Theory]
    [InlineData(0, 0, 0, TimeSpan.TicksPerDay)]
    [InlineData(10, 0, 0, TimeSpan.TicksPerHour)]
    [InlineData(10, 30, 0, TimeSpan.TicksPerMinute)]
    [InlineData(10, 30, 15, TimeSpan.TicksPerMinute)]
    public void TimeRangeWithoutAWidthIsPaddedByThePrecisionOfTheTime(int hour, int minute, int second, long padding)
    {
        // Arrange
        double time = new DateTime(2026, 10, 19, hour, minute, second).Ticks;

        // Act & Assert
        Assert.Equal((time - padding, time + padding), AxisRangeResolver.ResolveTime(time, time, Default));
    }

    [Fact]
    public void FixedTimeBoundBeyondTheDataIsPaddedOnItsOtherSide()
    {
        // Arrange
        double time = new DateTime(2026, 10, 19, 10, 0, 0).Ticks;

        // Act & Assert
        Assert.Equal((time, time + TimeSpan.TicksPerHour), AxisRangeResolver.ResolveTime(time - TimeSpan.TicksPerHour, time - 1, new AxisRangeOptions { Minimum = time }));
        Assert.Equal((time - TimeSpan.TicksPerHour, time), AxisRangeResolver.ResolveTime(time + 1, time + TimeSpan.TicksPerHour, new AxisRangeOptions { Maximum = time }));
        Assert.Equal((0d, TimeSpan.TicksPerMinute * 2d), AxisRangeResolver.ResolveTime(Double.NaN, Double.NaN, Default));
    }

    [Theory]
    [InlineData(5, ChartLabelPlacement.BetweenTicks, -0.5, 4.5)]
    [InlineData(5, ChartLabelPlacement.OnTicks, 0, 4)]
    [InlineData(1, ChartLabelPlacement.OnTicks, -0.5, 0.5)]
    [InlineData(0, ChartLabelPlacement.BetweenTicks, -0.5, 0.5)]
    public void CategoryRangeDependsOnThePlacement(int count, ChartLabelPlacement placement, double low, double high)
    {
        // Act & Assert
        Assert.Equal(new AxisScale(low, high, 1), AxisRangeResolver.ResolveCategory(count, placement));
    }
}
