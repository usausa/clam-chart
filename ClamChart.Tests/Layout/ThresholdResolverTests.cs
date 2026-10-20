namespace ClamChart.Tests.Layout;

public sealed class ThresholdResolverTests
{
    [Fact]
    public void HighestThresholdAtOrBelowTheValueIsUsed()
    {
        // Arrange
        var good = new ChartThreshold { Level = ChartThresholdLevel.Good };
        var warning = new ChartThreshold { Value = 70, Level = ChartThresholdLevel.Warning };
        var critical = new ChartThreshold { Value = 90, Level = ChartThresholdLevel.Critical };
        List<ChartThreshold> thresholds = [critical, good, warning];

        // Act & Assert
        Assert.Same(good, ThresholdResolver.Resolve(thresholds, 10));
        Assert.Same(warning, ThresholdResolver.Resolve(thresholds, 70));
        Assert.Same(warning, ThresholdResolver.Resolve(thresholds, 89.9));
        Assert.Same(critical, ThresholdResolver.Resolve(thresholds, 95));
        Assert.Null(ThresholdResolver.Resolve(thresholds, Double.NaN));
        Assert.Null(ThresholdResolver.Resolve([warning], 10));
    }

    [Fact]
    public void LaterThresholdWinsATie()
    {
        // Arrange
        var first = new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Warning };
        var second = new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Serious };

        // Act & Assert
        Assert.Same(second, ThresholdResolver.Resolve([first, second], 60));
    }

    [Fact]
    public void ColorComesFromTheThresholdOrItsLevel()
    {
        // Arrange
        var style = ChartStyles.Light;
        var custom = new ChartThreshold { Color = Colors.Purple, Level = ChartThresholdLevel.Critical };

        // Act & Assert
        Assert.Equal(Colors.Purple, ThresholdResolver.GetColor(custom, style));
        Assert.Equal(style.GoodColor, ThresholdResolver.GetColor(new ChartThreshold { Level = ChartThresholdLevel.Good }, style));
        Assert.Equal(style.WarningColor, ThresholdResolver.GetColor(new ChartThreshold { Level = ChartThresholdLevel.Warning }, style));
        Assert.Equal(style.SeriousColor, ThresholdResolver.GetColor(new ChartThreshold { Level = ChartThresholdLevel.Serious }, style));
        Assert.Equal(style.CriticalColor, ThresholdResolver.GetColor(new ChartThreshold { Level = ChartThresholdLevel.Critical }, style));
        Assert.Null(ThresholdResolver.GetColor(new ChartThreshold(), style));
        Assert.Null(ThresholdResolver.GetColor(null, style));
    }
}
