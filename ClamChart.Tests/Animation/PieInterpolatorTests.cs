namespace ClamChart.Tests.Animation;

using ClamChart.Animation;
using ClamChart.Tests.Layout;

public sealed class PieInterpolatorTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly ChartValueReader Reader = new(new ChartValueOptions());

    [Fact]
    public void SameLayoutDoesNotMove()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        var layout = Layout(50, 50);
        interpolator.Complete(layout);
        interpolator.Capture(layout);

        // Act
        var moving = interpolator.Begin(layout, [], true, 2);

        // Assert
        Assert.False(moving);
        Assert.False(interpolator.IsMorphing);
        Assert.Equal(2, interpolator.Count);
    }

    [Fact]
    public void FractionsMorphFromTheShownStateToTheTarget()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        Show(interpolator, Layout(50, 50));

        // Act
        var moving = interpolator.Begin(Layout(25, 75), [], true, 2);
        interpolator.Apply(0.5);

        // Assert
        Assert.True(moving);
        Assert.Equal(0.375, interpolator.GetSlice(0).End, Precision);
        Assert.Equal(0.375, interpolator.GetSlice(1).Start, Precision);
        Assert.Equal(1, interpolator.GetSlice(1).End, Precision);
    }

    [Fact]
    public void AppendedSliceGrowsFromTheEnd()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        Show(interpolator, Layout(50, 50));

        // Act
        interpolator.Begin(Layout(25, 25, 50), [0, 1], false, 3);

        // Assert
        Assert.Equal(3, interpolator.Count);
        Assert.Equal((1d, 1d), interpolator.GetSlice(2));
        Assert.Equal(2, interpolator.GetKey(2));
        interpolator.Apply(1);
        Assert.Equal(0.5, interpolator.GetSlice(2).Start, Precision);
    }

    [Fact]
    public void RemovedSliceShrinksWhereItsNeighborsMeet()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        Show(interpolator, Layout(30, 30, 40));

        // Act
        interpolator.Begin(Layout(30, 40), [0, -1, 1], false, 2);
        interpolator.Apply(1);

        // Assert
        Assert.Equal(3, interpolator.Count);
        Assert.Equal(PieInterpolator.DepartingKey, interpolator.GetKey(1));
        Assert.Equal(-1, interpolator.GetTarget(1));
        var (start, end) = interpolator.GetSlice(1);
        Assert.Equal(3d / 7, start, Precision);
        Assert.Equal(3d / 7, end, Precision);
        Assert.Equal(3d / 7, interpolator.GetSlice(2).Start, Precision);
    }

    [Fact]
    public void RemovedSliceKeepsItsColor()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        Show(interpolator, Layout(30, 30, 40));

        // Act
        interpolator.Begin(Layout(30, 40), [0, -1, 1], false, 2);

        // Assert
        Assert.Equal(1, interpolator.GetColor(1).Rank);
        Assert.False(interpolator.GetColor(1).IsOthers);
    }

    [Fact]
    public void ShiftedColorChangesDuringTheMorph()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        Show(interpolator, Layout(30, 30, 40));

        // Act
        interpolator.Begin(Layout(30, 40), [0, -1, 1], false, 2);
        interpolator.Apply(0.5);

        // Assert
        Assert.Equal(2, interpolator.GetPreviousColor(2).Rank);
        Assert.Equal(1, interpolator.GetColor(2).Rank);
        Assert.Equal(0.5, interpolator.ColorProgress);
        interpolator.Complete(Layout(30, 40));
        Assert.Equal(1, interpolator.ColorProgress);
        Assert.Equal(interpolator.GetColor(1), interpolator.GetPreviousColor(1));
    }

    [Fact]
    public void OthersIsMatchedAcrossUpdates()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        var options = new PieLayoutOptions { MaxSlices = 3 };
        Show(interpolator, Layout(options, 1, 2, 3, 4));

        // Act
        var moving = interpolator.Begin(Layout(options, 2, 2, 3, 4), [], true, 4);

        // Assert
        Assert.True(moving);
        Assert.Equal(3, interpolator.Count);
        Assert.Equal(PieInterpolator.OthersKey, interpolator.GetKey(2));
        Assert.True(interpolator.GetColor(2).IsOthers);
    }

    [Fact]
    public void ItemMovingIntoOthersShrinksAway()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        var options = new PieLayoutOptions { OthersThreshold = 0.1 };
        Show(interpolator, Layout(options, 40, 50, 5, 5));

        // Act
        interpolator.Begin(Layout(options, 5, 85, 5, 5), [], true, 4);
        interpolator.Apply(1);

        // Assert
        Assert.Equal(PieInterpolator.DepartingKey, interpolator.GetKey(0));
        var (start, end) = interpolator.GetSlice(0);
        Assert.Equal(start, end, Precision);
        Assert.Equal(1, interpolator.FindKey(1));
    }

    [Fact]
    public void InterruptedRemovalKeepsShrinkingFromTheShownState()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        Show(interpolator, Layout(30, 30, 40));
        var removed = Layout(30, 40);
        interpolator.Begin(removed, [0, -1, 1], false, 2);
        interpolator.Apply(0.5);
        var (shownStart, shownEnd) = interpolator.GetSlice(1);
        interpolator.Capture(removed);

        // Act
        interpolator.Begin(Layout(20, 50), [], true, 2);

        // Assert
        Assert.Equal(3, interpolator.Count);
        Assert.Equal(PieInterpolator.DepartingKey, interpolator.GetKey(1));
        Assert.Equal(shownStart, interpolator.GetSlice(1).Start, Precision);
        Assert.Equal(shownEnd, interpolator.GetSlice(1).End, Precision);
        interpolator.Apply(1);
        Assert.Equal(2d / 7, interpolator.GetSlice(1).Start, Precision);
        Assert.Equal(2d / 7, interpolator.GetSlice(1).End, Precision);
    }

    [Fact]
    public void CompleteShowsTheTarget()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        Show(interpolator, Layout(30, 30, 40));
        var removed = Layout(30, 40);
        interpolator.Begin(removed, [0, -1, 1], false, 2);

        // Act
        interpolator.Complete(removed);

        // Assert
        Assert.False(interpolator.IsMorphing);
        Assert.Equal(2, interpolator.Count);
        Assert.Equal(1, interpolator.GetTarget(1));
    }

    [Fact]
    public void EntranceGrowsTheAnglesFromTheStart()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        var layout = Layout(50, 50);

        // Act
        interpolator.BeginEntrance(layout, ChartEntranceAnimation.Auto);
        interpolator.Apply(0.5);

        // Assert
        Assert.Equal(ChartEntranceAnimation.Grow, interpolator.Entrance);
        Assert.Equal((0.25, 0.5), interpolator.GetSlice(1));
        Assert.Equal(1, interpolator.RevealLimit);
        Assert.Equal(1, interpolator.EntranceAlpha);
        Assert.Equal(0, interpolator.OverlayAlpha);
        interpolator.Apply(0.85);
        Assert.Equal(0.5, interpolator.OverlayAlpha, Precision);
    }

    [Fact]
    public void RevealAndFadeKeepTheAngles()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        var layout = Layout(50, 50);

        // Act & Assert
        interpolator.BeginEntrance(layout, ChartEntranceAnimation.Reveal);
        interpolator.Apply(0.25);
        Assert.Equal((0.5, 1d), interpolator.GetSlice(1));
        Assert.Equal(0.25, interpolator.RevealLimit);
        interpolator.BeginEntrance(layout, ChartEntranceAnimation.Fade);
        interpolator.Apply(0.25);
        Assert.Equal(0.25, interpolator.EntranceAlpha);
        Assert.Equal(1, interpolator.RevealLimit);
    }

    [Fact]
    public void RetargetDuringTheEntranceShowsTheNewData()
    {
        // Arrange
        using var interpolator = new PieInterpolator();
        interpolator.BeginEntrance(Layout(50, 50), ChartEntranceAnimation.Grow);
        interpolator.Apply(0.5);

        // Act
        interpolator.Retarget(Layout(20, 30, 50));

        // Assert
        Assert.True(interpolator.IsEntrance);
        Assert.Equal(3, interpolator.Count);
        Assert.Equal(0.5, interpolator.Progress);
    }

    private static void Show(PieInterpolator interpolator, PieLayout layout)
    {
        interpolator.Complete(layout);
        interpolator.Capture(layout);
    }

    private static PieLayout Layout(params double[] values) => Layout(new PieLayoutOptions(), values);

    private static PieLayout Layout(PieLayoutOptions options, params double[] values)
    {
        using var data = DataSnapshotBuilder.Build(values.Select(static (value, index) => new ChartEntry($"C{index}", value)).ToList(), Reader, ChartEmptyPointMode.Gap);
        var layout = new PieLayout();
        layout.Update(200, 200, new Thickness(0), new ChartStyle(), CultureInfo.InvariantCulture, options, data, Measurer);
        return layout;
    }
}
