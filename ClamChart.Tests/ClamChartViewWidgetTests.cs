namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamChartViewWidgetTests
{
    private const float Scale = 2;

    [Fact]
    public void TrackingIsSharedWithTheSameGroup()
    {
        // Arrange
        using var leader = Chart("ops", ["A", "B", "C"]);
        using var follower = Chart("ops", ["A", "B", "C"]);
        using var other = Chart("db", ["A", "B", "C"]);
        Paint(leader, follower, other);

        // Act
        leader.HandleTouch(1, SKTouchAction.Pressed, new Point(leader.ChartLayout.XAxis.ToPixel(1), 100), 0);
        using var followed = follower.Paint(360, 240);
        using var ignored = other.Paint(360, 240);

        // Assert
        var line = ChartStyles.Light.TrackballLineColor.ToSKColor();
        var x = follower.ChartLayout.XAxis.ToPixel(1);
        var y = follower.ChartLayout.PlotBounds.Y + 3;
        Assert.Equal(line, GetPixel(followed, x, y));
        Assert.NotEqual(line, GetPixel(ignored, x, y));
        Assert.Null(follower.TrackedItem);
        Assert.False(follower.IsTracking);

        // Act
        leader.HandleTouch(1, SKTouchAction.Released, new Point(leader.ChartLayout.XAxis.ToPixel(1), 100), 10);
        using var released = follower.Paint(360, 240);

        // Assert
        Assert.NotEqual(line, GetPixel(released, x, y));
    }

    [Fact]
    public void CategoriesAreMatchedByName()
    {
        // Arrange
        using var leader = Chart("ops", ["A", "B", "C"]);
        using var follower = Chart("ops", ["B", "C"]);
        Paint(leader, follower);

        // Act
        leader.HandleTouch(1, SKTouchAction.Pressed, new Point(leader.ChartLayout.XAxis.ToPixel(1), 100), 0);
        using var followed = follower.Paint(360, 240);

        // Assert
        var line = ChartStyles.Light.TrackballLineColor.ToSKColor();
        Assert.Equal(line, GetPixel(followed, follower.ChartLayout.XAxis.ToPixel(0), follower.ChartLayout.PlotBounds.Y + 3));
    }

    [Fact]
    public void LeavingTheGroupStopsFollowing()
    {
        // Arrange
        using var leader = Chart("ops", ["A", "B"]);
        using var follower = Chart("ops", ["A", "B"]);
        Paint(leader, follower);
        leader.HandleTouch(1, SKTouchAction.Pressed, new Point(leader.ChartLayout.XAxis.ToPixel(1), 100), 0);

        // Act
        follower.TrackingGroup = null;
        using var bitmap = follower.Paint(360, 240);

        // Assert
        var line = ChartStyles.Light.TrackballLineColor.ToSKColor();
        Assert.NotEqual(line, GetPixel(bitmap, follower.ChartLayout.XAxis.ToPixel(1), follower.ChartLayout.PlotBounds.Y + 3));
    }

    [Fact]
    public void UnsnappedLeaderSharesTheFingerPosition()
    {
        // Arrange
        using var leader = NumericChart("ops");
        leader.TrackballSnapEnabled = false;
        using var snapping = NumericChart("ops");
        using var free = NumericChart("ops");
        free.TrackballSnapEnabled = false;
        Paint(leader, snapping, free);
        var plot = leader.ChartLayout.PlotBounds;
        var y = plot.Y + (plot.Height * 0.6);

        // Act
        leader.HandleTouch(1, SKTouchAction.Pressed, new Point(leader.ChartLayout.XAxis.ToPixel(7), y), 0);
        using var snapped = snapping.Paint(360, 240);
        using var followed = free.Paint(360, 240);

        // Assert
        var line = ChartStyles.Light.TrackballLineColor.ToSKColor();
        var top = plot.Y + 3;
        Assert.Equal(line, GetPixel(snapped, snapping.ChartLayout.XAxis.ToPixel(10), top));
        Assert.NotEqual(line, GetPixel(snapped, snapping.ChartLayout.XAxis.ToPixel(7), top));
        Assert.Equal(line, GetPixel(followed, free.ChartLayout.XAxis.ToPixel(7), top));
        Assert.NotEqual(line, GetPixel(followed, free.ChartLayout.XAxis.ToPixel(10), top));
        Assert.NotEqual(line, GetPixel(followed, plot.X + 3, y));
        Assert.Null(free.TrackedItem);
    }

    [Fact]
    public void UnsnappedCategoryFollowerKeepsThePlaceWithinTheCategory()
    {
        // Arrange
        using var leader = Chart("ops", ["A", "B", "C"]);
        leader.TrackballSnapEnabled = false;
        using var follower = Chart("ops", ["B", "C"]);
        follower.TrackballSnapEnabled = false;
        Paint(leader, follower);

        // Act
        leader.HandleTouch(1, SKTouchAction.Pressed, new Point(leader.ChartLayout.XAxis.ToPixel(1.3), 100), 0);
        using var followed = follower.Paint(360, 240);

        // Assert
        var line = ChartStyles.Light.TrackballLineColor.ToSKColor();
        var top = follower.ChartLayout.PlotBounds.Y + 3;
        Assert.Equal(line, GetPixel(followed, follower.ChartLayout.XAxis.ToPixel(0.3), top));
        Assert.NotEqual(line, GetPixel(followed, follower.ChartLayout.XAxis.ToPixel(0), top));
    }

    [Fact]
    public void TooltipModeShowsTheTooltipOfTheFollowerAtTheSameHeight()
    {
        // Arrange
        using var leader = Chart("ops", ["A", "B", "C"]);
        using var tooltip = Chart("ops", ["A", "B", "C"]);
        tooltip.TrackingGroupMode = ChartTrackingGroupMode.Tooltip;
        using var crosshair = Chart("ops", ["A", "B", "C"]);
        Paint(leader, tooltip, crosshair);
        var plot = leader.ChartLayout.PlotBounds;

        // Act
        leader.HandleTouch(1, SKTouchAction.Pressed, new Point(leader.ChartLayout.XAxis.ToPixel(1), plot.Y + (plot.Height * 0.75)), 0);

        // Assert
        Assert.Equal("B", tooltip.TooltipHeader);
        var anchor = tooltip.TooltipPoint;
        var follower = tooltip.ChartLayout.PlotBounds;
        Assert.Equal(tooltip.ChartLayout.XAxis.ToPixel(1), anchor.X, 6);
        Assert.Equal(follower.Y + (follower.Height * 0.75), anchor.Y, 6);
        Assert.Equal("2", tooltip.GetTrackingText(0, 1));
        Assert.Null(tooltip.TrackedItem);
        Assert.Equal(-1, tooltip.TrackedIndex);
        Assert.Null(crosshair.TooltipHeader);

        // Act
        leader.HandleTouch(1, SKTouchAction.Released, new Point(leader.ChartLayout.XAxis.ToPixel(1), 100), 10);

        // Assert
        Assert.Null(tooltip.TooltipHeader);
    }

    [Fact]
    public void TooltipModeFollowerDrawsTheTooltip()
    {
        // Arrange
        using var leader = Chart("ops", ["A", "B", "C"]);
        using var follower = Chart("ops", ["A", "B", "C"]);
        Paint(leader, follower);
        var plot = leader.ChartLayout.PlotBounds;
        leader.HandleTouch(1, SKTouchAction.Pressed, new Point(leader.ChartLayout.XAxis.ToPixel(1), plot.Y + (plot.Height * 0.75)), 0);
        using var crosshair = follower.Paint(360, 240);

        // Act
        follower.TrackingGroupMode = ChartTrackingGroupMode.Tooltip;
        using var tooltip = follower.Paint(360, 240);

        // Assert
        var anchor = follower.TooltipPoint;
        var background = ChartStyles.Light.TooltipBackground.ToSKColor();
        Assert.NotEqual(background, GetPixel(crosshair, anchor.X + 4, anchor.Y - 50));
        Assert.Equal(background, GetPixel(tooltip, anchor.X + 4, anchor.Y - 50));
    }

    [Fact]
    public void CategoryWidthRequestsTheLengthOfTheCategories()
    {
        // Arrange
        using var view = new TestClamChartView { XAxis = new CategoryAxis { CategoryWidth = 40 } };
        view.Series.Add(new BarSeries { ItemsSource = Enumerable.Range(0, 12).Select(static i => new ChartEntry($"M{i}", i)).ToList() });
        using var bitmap = view.Paint(360, 240);
        var extent = 360 - view.ChartLayout.PlotBounds.Width;

        // Act
        var size = ((IView)view).Measure(Double.PositiveInfinity, 240);

        // Assert
        Assert.Equal((12 * 40) + extent, size.Width, 6);
    }

    [Fact]
    public void CategoryWidthFollowsTheOrientationAndYieldsToARequest()
    {
        // Arrange
        using var ranking = new TestClamChartView { Orientation = ChartOrientation.Horizontal, XAxis = new CategoryAxis { CategoryWidth = 30 } };
        ranking.Series.Add(new BarSeries { ItemsSource = Enumerable.Range(0, 5).Select(static i => new ChartEntry($"P{i}", i)).ToList() });
        using var requested = new TestClamChartView { WidthRequest = 300, XAxis = new CategoryAxis { CategoryWidth = 40 } };
        requested.Series.Add(new BarSeries { ItemsSource = Enumerable.Range(0, 12).Select(static i => new ChartEntry($"M{i}", i)).ToList() });
        Paint(ranking, requested);
        var extent = 240 - ranking.ChartLayout.PlotBounds.Height;

        // Act
        var size = ((IView)ranking).Measure(360, Double.PositiveInfinity);
        var fixedSize = ((IView)requested).Measure(Double.PositiveInfinity, 240);

        // Assert
        Assert.Equal((5 * 30) + extent, size.Height, 6);
        Assert.NotEqual((12 * 40) + 0d, fixedSize.Width);
    }

    [Fact]
    public void CategoryWidthMustBePositive()
    {
        // Arrange
        var axis = new CategoryAxis();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.CategoryWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.CategoryWidth = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.CategoryWidth = Double.NaN);
        axis.CategoryWidth = null;
        Assert.Null(axis.CategoryWidth);
    }

    [Fact]
    public void DownsamplingIsAutomaticByDefault()
    {
        // Arrange
        using var view = new TestClamChartView();

        // Act & Assert
        Assert.Equal(ChartDownsampling.Auto, view.Downsampling);
    }

    [Fact]
    public void ManyPointsAreStillDrawnAfterDownsampling()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.YAxis = new NumericAxis { Minimum = 0, Maximum = 10 };
        view.Series.Add(new LineSeries { ItemsSource = Enumerable.Range(0, 20000).Select(static i => i == 10000 ? 9d : 1d).ToList() });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var line = new SKColor(0x2A, 0x78, 0xD6);
        Assert.Equal(line, GetPixel(bitmap, layout.XAxis.ToPixel(2000), layout.YAxis.ToPixel(1)));
        Assert.Equal(line, GetPixel(bitmap, layout.XAxis.ToPixel(10000), layout.YAxis.ToPixel(8)));
    }

    private static TestClamChartView Chart(string group, string[] categories)
    {
        var view = new TestClamChartView { TrackballMode = ChartTrackballMode.Touch, TrackingGroup = group };
        view.Series.Add(new LineSeries { ItemsSource = categories.Select(static (x, i) => new ChartEntry(x, i + 1)).ToList() });
        return view;
    }

    private static TestClamChartView NumericChart(string group)
    {
        var view = new TestClamChartView { TrackballMode = ChartTrackballMode.Touch, TrackingGroup = group };
        view.Series.Add(new LineSeries { ItemsSource = new List<ChartPoint> { new(0, 1), new(10, 2), new(20, 3) } });
        return view;
    }

    private static void Paint(params TestClamChartView[] views)
    {
        foreach (var view in views)
        {
            using var bitmap = view.Paint(360, 240);
        }
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));
}
