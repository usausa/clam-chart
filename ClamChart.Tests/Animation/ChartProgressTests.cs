namespace ClamChart.Tests.Animation;

using ClamChart.Animation;

public sealed class ChartProgressTests
{
    [Fact]
    public void ElementsStartWithinThirtyPercentOfTheWhole()
    {
        // Act & Assert
        Assert.Equal(0, ChartProgress.ForElement(0, 0, 5));
        Assert.Equal(1, ChartProgress.ForElement(0.7, 0, 5), 9);
        Assert.Equal(0, ChartProgress.ForElement(0.3, 4, 5), 9);
        Assert.Equal(0.5, ChartProgress.ForElement(0.65, 4, 5), 9);
        Assert.Equal(1, ChartProgress.ForElement(1, 4, 5));
        Assert.Equal(0.5, ChartProgress.ForElement(0.35, 0, 1), 9);
    }

    [Fact]
    public void ElementsCanUseAShorterStagger()
    {
        // Act & Assert
        Assert.Equal(0.5, ChartProgress.ForElement(0.4, 0, 3, 0.2), 9);
        Assert.Equal(0.375, ChartProgress.ForElement(0.4, 1, 3, 0.2), 9);
        Assert.Equal(0.25, ChartProgress.ForElement(0.4, 2, 3, 0.2), 9);
        Assert.Equal(0.4, ChartProgress.ForElement(0.4, 0, 1, 0), 9);
    }

    [Fact]
    public void StaggerFollowsTheDelayBetweenElements()
    {
        // Act & Assert
        Assert.Equal(80d / 450, ChartProgress.GetStagger(3, 40, 450), 9);
        Assert.Equal(0.3, ChartProgress.GetStagger(20, 40, 450), 9);
        Assert.Equal(0, ChartProgress.GetStagger(1, 40, 450));
        Assert.Equal(0, ChartProgress.GetStagger(3, 40, 0));
    }

    [Fact]
    public void DelayedProgressStartsLate()
    {
        // Act & Assert
        Assert.Equal(0, ChartProgress.Delayed(0.5, ChartProgress.OverlayDelay));
        Assert.Equal(0.5, ChartProgress.Delayed(0.85, ChartProgress.OverlayDelay), 9);
        Assert.Equal(1, ChartProgress.Delayed(1, ChartProgress.OverlayDelay));
        Assert.Equal(15, ChartProgress.Lerp(10, 20, 0.5));
    }
}
