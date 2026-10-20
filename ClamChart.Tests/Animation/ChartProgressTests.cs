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
    public void DelayedProgressStartsLate()
    {
        // Act & Assert
        Assert.Equal(0, ChartProgress.Delayed(0.5, ChartProgress.OverlayDelay));
        Assert.Equal(0.5, ChartProgress.Delayed(0.85, ChartProgress.OverlayDelay), 9);
        Assert.Equal(1, ChartProgress.Delayed(1, ChartProgress.OverlayDelay));
        Assert.Equal(15, ChartProgress.Lerp(10, 20, 0.5));
    }
}
