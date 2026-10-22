namespace ClamChart.Tests;

using ClamChart.Tests.Data;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamStateTimelineViewAnimationTests
{
    private const string Data = "ClamStateTimelineViewData";

    private const float Scale = 2;

    private static readonly DateTime Origin = new(2026, 10, 22, 0, 0, 0);

    [Fact]
    public void FirstDataIsRevealedFromTheLeft()
    {
        // Arrange
        using var view = Chart();
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 100);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 100);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(ChartEntranceAnimation.Reveal, view.Interpolator.Entrance);
        Assert.True(GetPixel(middle, Point(view, 0.5)).Alpha > 0);
        Assert.Equal(0, GetPixel(middle, Point(view, 3.5)).Alpha);

        // Act
        driver.Finish(Data);
        using var last = view.Paint(300, 100);

        // Assert
        Assert.True(GetPixel(last, Point(view, 3.5)).Alpha > 0);
        Assert.False(view.IsAnimating);
    }

    [Fact]
    public void FadeShowsEveryStateAtOnce()
    {
        // Arrange
        using var view = Chart();
        view.EntranceAnimation = ChartEntranceAnimation.Fade;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 100);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 100);

        // Assert
        var left = GetPixel(middle, Point(view, 0.5)).Alpha;
        var right = GetPixel(middle, Point(view, 3.5)).Alpha;
        Assert.InRange(left, 1, 178);
        Assert.Equal(left, right);
    }

    [Fact]
    public void ChangingTheRangeSlidesTheStates()
    {
        // Arrange
        using var view = Chart();
        view.EntranceAnimation = ChartEntranceAnimation.None;
        var driver = Driver(view);
        using var first = view.Paint(300, 100);

        // Act
        view.Maximum = Origin.AddHours(8);
        using var start = view.Paint(300, 100);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 100);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        var display = view.Interpolator.DisplayScale;
        Assert.Equal(Origin.AddHours(6).Ticks, display.Maximum, TimeSpan.TicksPerSecond);

        // Act
        driver.Finish(Data);
        using var last = view.Paint(300, 100);

        // Assert
        Assert.Equal(Origin.AddHours(8).Ticks, view.Interpolator.DisplayScale.Maximum);
    }

    private static TestClamStateTimelineView Chart()
    {
        var view = new TestClamStateTimelineView
        {
            AnimationDriver = new TestAnimationDriver(),
            AnimationEasing = Easing.Linear,
            ContentPadding = new Thickness(0),
            ChartStyle = new ChartStyle { GridLineColor = Colors.Transparent },
            ShowValue = ChartVisibilityMode.Never,
            ValueAccessors = TestStateItem.Accessors,
            StartKey = "start",
            StateKey = "state",
            Maximum = Origin.AddHours(4),
            ItemsSource = new List<TestStateItem> { new(null, Origin, "Running"), new(null, Origin.AddHours(2), "Stopped") }
        };
        view.OnAttached();
        return view;
    }

    private static TestAnimationDriver Driver(TestClamStateTimelineView view) => (TestAnimationDriver)view.AnimationDriver;

    private static Point Point(TestClamStateTimelineView view, double hours)
    {
        var layout = view.ChartLayout;
        var scale = layout.Axis.Scale;
        var x = layout.Plot.X + ((Origin.AddHours(hours).Ticks - scale.Minimum) / scale.Span * layout.Plot.Width);
        return new Point(x, layout.GetRowTop(0) + (layout.RowHeight / 2));
    }

    private static SKColor GetPixel(SKBitmap bitmap, Point point) => bitmap.GetPixel((int)(point.X * Scale), (int)(point.Y * Scale));
}
