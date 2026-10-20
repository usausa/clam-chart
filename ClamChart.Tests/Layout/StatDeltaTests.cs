namespace ClamChart.Tests.Layout;

public sealed class StatDeltaTests
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void PercentIsComputedFromThePreviousValue()
    {
        // Act
        var delta = Create(108.2, 100);

        // Assert
        Assert.Equal("+8.2%", delta.Text);
        Assert.Equal(1, delta.Direction);
        Assert.Equal(StatDeltaTone.Good, delta.Tone);
    }

    [Fact]
    public void DecreaseIsGoodWhenIncreaseIsNot()
    {
        // Act
        var good = Create(90, 100, increaseIsGood: false);
        var bad = Create(110, 100, increaseIsGood: false);

        // Assert
        Assert.Equal("-10.0%", good.Text);
        Assert.Equal(-1, good.Direction);
        Assert.Equal(StatDeltaTone.Good, good.Tone);
        Assert.Equal(StatDeltaTone.Bad, bad.Tone);
    }

    [Fact]
    public void PercentUsesTheSizeOfANegativeBase()
    {
        // Act
        var delta = Create(-50, -100);

        // Assert
        Assert.Equal("+50.0%", delta.Text);
        Assert.Equal(1, delta.Direction);
    }

    [Fact]
    public void NoDeltaWithoutAComparableBase()
    {
        // Act & Assert
        Assert.False(Create(10, 0).IsVisible);
        Assert.False(Create(10, null).IsVisible);
        Assert.False(Create(10, Double.NaN).IsVisible);
        Assert.False(Create(Double.NaN, 10).IsVisible);
        Assert.True(Create(10, 0, mode: ChartDeltaMode.Absolute).IsVisible);
    }

    [Fact]
    public void AbsoluteUsesTheCompactNumber()
    {
        // Act
        var small = Create(1500, 300, mode: ChartDeltaMode.Absolute);
        var large = Create(12345, 0, mode: ChartDeltaMode.Absolute);

        // Assert
        Assert.Equal("+1,200", small.Text);
        Assert.Equal("+12.3K", large.Text);
    }

    [Fact]
    public void ChangesThatRoundToZeroAreNeutral()
    {
        // Act
        var percent = Create(100.04, 100);
        var absolute = Create(10.001, 10, mode: ChartDeltaMode.Absolute);

        // Assert
        Assert.Equal("0.0%", percent.Text);
        Assert.Equal(0, percent.Direction);
        Assert.Equal(StatDeltaTone.Neutral, percent.Tone);
        Assert.Equal("0", absolute.Text);
        Assert.Equal(0, absolute.Direction);
    }

    [Fact]
    public void GivenDeltaWinsOverThePreviousValue()
    {
        // Act
        var percent = Create(10, 100, delta: 0.05);
        var absolute = Create(10, 100, delta: -3, mode: ChartDeltaMode.Absolute);

        // Assert
        Assert.Equal("+5.0%", percent.Text);
        Assert.Equal("-3", absolute.Text);
        Assert.Equal(StatDeltaTone.Bad, absolute.Tone);
    }

    [Fact]
    public void FormatIsUsedAsGivenWithTheRawSign()
    {
        // Act
        var delta = Create(100.04, 100, format: "+0.00%;-0.00%");

        // Assert
        Assert.Equal("+0.04%", delta.Text);
        Assert.Equal(1, delta.Direction);
    }

    private static StatDelta Create(double value, double? previous, double? delta = null, ChartDeltaMode mode = ChartDeltaMode.Percent, string? format = null, bool increaseIsGood = true) =>
        StatDelta.Create(value, previous, delta, mode, ChartTextFormat.Parse(format), increaseIsGood, Culture);
}
