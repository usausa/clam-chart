namespace ClamChart.Tests;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamSparklineViewAnimationTests
{
    private const string Data = "ClamSparklineViewData";

    private const float Scale = 2;

    [Fact]
    public void FirstDataIsShownWithoutAnEntrance()
    {
        // Arrange
        using var view = Spark(new ObservableCollection<double> { 1, 2, 3 });

        // Act
        using var bitmap = view.Paint(100, 28);

        // Assert
        Assert.Empty(Driver(view).Starts);
        Assert.False(view.IsAnimating);
    }

    [Fact]
    public void ReplacingTheValuesDoesNotAnimate()
    {
        // Arrange
        using var view = Spark(new ObservableCollection<double> { 1, 2, 3 });
        using var first = view.Paint(100, 28);

        // Act
        view.Values = new ObservableCollection<double> { 3, 2, 1 };
        using var second = view.Paint(100, 28);
        view.Values = [5, 6, 7];
        using var third = view.Paint(100, 28);

        // Assert
        Assert.Empty(Driver(view).Starts);
        Assert.Equal(7, view.ChartLayout.Scale.Maximum);
    }

    [Fact]
    public void StreamingFlowsToTheLeft()
    {
        // Arrange
        var values = new ObservableCollection<double> { 1, 2, 3, 4 };
        using var view = Spark(values);
        var driver = Driver(view);
        using var first = view.Paint(100, 28);

        // Act
        values.Add(5);
        values.RemoveAt(0);
        using var second = view.Paint(100, 28);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.True(view.Interpolator.IsMorphing);
        var display = view.Interpolator.Display;
        Assert.Equal(5, display.Count);
        Assert.Equal(-0.5, display.X[0], 9);
        Assert.Equal(1, display.End[0], 9);
        Assert.Equal(2.5, display.X[3], 9);
        Assert.Equal(3, display.X[4], 9);
        Assert.Equal(4.5, display.End[4], 9);

        // Act
        driver.Finish(Data);

        // Assert
        Assert.False(view.Interpolator.IsMorphing);
        Assert.Equal(new SparklineScale(2, 5, 3), view.Interpolator.Scale);
    }

    [Fact]
    public void ItemChangesMorphTheValue()
    {
        // Arrange
        var values = new ObservableCollection<double> { 1, 2, 3 };
        using var view = Spark(values);
        var driver = Driver(view);
        using var first = view.Paint(100, 28);

        // Act
        values[1] = 4;
        using var second = view.Paint(100, 28);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal(3, view.Interpolator.Display.End[1], 9);
        Assert.Equal(3.5, view.Interpolator.Scale.Maximum, 9);
    }

    [Fact]
    public void UpdateAnimationCanBeTurnedOff()
    {
        // Arrange
        var values = new ObservableCollection<double> { 1, 2, 3 };
        using var view = Spark(values);
        view.UpdateAnimationEnabled = false;
        using var first = view.Paint(100, 28);

        // Act
        values.Add(4);
        using var second = view.Paint(100, 28);

        // Assert
        Assert.Empty(Driver(view).Starts);
        Assert.Equal(4, view.ChartLayout.Count);
    }

    [Fact]
    public void ArraysAreNeverAnimated()
    {
        // Arrange
        using var view = Spark(new[] { 1d, 2, 3 });
        using var first = view.Paint(100, 28);

        // Act
        view.Maximum = 10;
        using var second = view.Paint(100, 28);

        // Assert
        Assert.Empty(Driver(view).Starts);
        Assert.False(view.Interpolator.HasState);
    }

    [Fact]
    public void RevealIsPlayedWhenItIsChosen()
    {
        // Arrange
        using var view = Spark(new[] { 5d, 5, 5, 5 });
        view.EntranceAnimation = ChartEntranceAnimation.Reveal;
        view.AnimationEasing = Easing.Linear;
        view.Markers = ChartSparklineMarkers.None;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(100, 28);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(100, 28);

        // Assert
        var layout = view.ChartLayout;
        var y = layout.ToY(5, layout.Scale);
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(ChartEntranceAnimation.Reveal, view.Interpolator.Entrance);
        Assert.True(middle.GetPixel((int)(layout.ToX(0.5, layout.Scale) * Scale), (int)(y * Scale)).Alpha > 0);
        Assert.Equal(0, middle.GetPixel((int)(layout.ToX(2.5, layout.Scale) * Scale), (int)(y * Scale)).Alpha);
    }

    [Fact]
    public void PlayEntranceAnimationGrowsBars()
    {
        // Arrange
        using var view = Spark(new[] { 2d, 4 });
        view.Kind = ChartSparklineKind.Bar;
        view.EntranceAnimation = ChartEntranceAnimation.Grow;
        view.AnimationEnabled = false;
        var driver = Driver(view);
        using var first = view.Paint(100, 28);
        view.AnimationEnabled = true;

        // Act
        view.PlayEntranceAnimation();

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.True(view.IsAnimating);
        view.CompleteAnimations();
        Assert.False(view.IsAnimating);
        Assert.Equal([Data], driver.Aborts);
    }

    [Fact]
    public void AnimationFramesOfBarsDoNotAllocate()
    {
        // Arrange
        var values = new ObservableCollection<double> { 3, -1, 4, 1, -5, 9 };
        using var view = Spark(values);
        view.Kind = ChartSparklineKind.Bar;
        var driver = Driver(view);
        using var bitmap = view.Paint(120, 28);
        using var surface = SKSurface.Create(new SKImageInfo(240, 56));
        values.Add(2);
        values.RemoveAt(0);
        using var changed = view.Paint(120, 28);
        MeasureFrames(view, driver, surface.Canvas);

        // Act
        var allocated = MeasureFrames(view, driver, surface.Canvas);

        // Assert
        Assert.True(view.Interpolator.IsMorphing);
        Assert.Equal(0, allocated);
    }

    private static TestClamSparklineView Spark(IEnumerable<double> values)
    {
        var view = new TestClamSparklineView { AnimationDriver = new TestAnimationDriver(), Values = values, AnimationEasing = Easing.Linear };
        view.OnAttached();
        return view;
    }

    private static TestAnimationDriver Driver(TestClamSparklineView view) => (TestAnimationDriver)view.AnimationDriver;

    private static long MeasureFrames(TestClamSparklineView view, TestAnimationDriver driver, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        var allocated = ChartAllocation.Measure(() =>
        {
            for (var i = 1; i <= 4; i++)
            {
                driver.Frame(Data, i * 0.2);
                view.Render(canvas, 120, 28);
            }
        });
        canvas.Restore();
        return allocated;
    }
}
