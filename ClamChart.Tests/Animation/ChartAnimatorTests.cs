namespace ClamChart.Tests.Animation;

using ClamChart.Animation;

public sealed class ChartAnimatorTests
{
    private const string Name = "Test";

    [Fact]
    public void LoopKeepsRunningAcrossCycles()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = new TestChartView { AnimationDriver = driver };
        var progress = new List<double>();
        var animator = new ChartAnimator(view, Name, progress.Add);

        // Act
        animator.Start(1000, true);
        driver.Finish(Name);
        driver.Finish(Name);
        driver.Frame(Name, 0.25);

        // Assert
        Assert.True(animator.IsRunning);
        Assert.True(driver.IsRunning(Name));
        Assert.Equal(0.25, animator.Progress);
        Assert.Equal([1, 1, 0.25], progress);
        Assert.Single(driver.Starts);
    }

    [Fact]
    public void StopEndsTheLoop()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = new TestChartView { AnimationDriver = driver };
        var animator = new ChartAnimator(view, Name, static _ => { });
        animator.Start(1000, true);

        // Act
        animator.Stop();

        // Assert
        Assert.False(animator.IsRunning);
        Assert.False(driver.IsRunning(Name));
        Assert.Equal([Name], driver.Aborts);
    }

    [Fact]
    public void LoopEndsWhenTheSystemCancelsIt()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = new TestChartView { AnimationDriver = driver };
        var animator = new ChartAnimator(view, Name, static _ => { });
        animator.Start(1000, true);

        // Act
        driver.Cancel(Name);

        // Assert
        Assert.False(animator.IsRunning);
        Assert.Equal(1, animator.Progress);
    }

    [Fact]
    public void SingleRunEndsAtTheFirstFinish()
    {
        // Arrange
        var driver = new TestAnimationDriver();
        using var view = new TestChartView { AnimationDriver = driver };
        var animator = new ChartAnimator(view, Name, static _ => { });
        animator.Start(1000);

        // Act
        driver.Finish(Name);

        // Assert
        Assert.False(animator.IsRunning);
        Assert.False(driver.IsRunning(Name));
    }
}
