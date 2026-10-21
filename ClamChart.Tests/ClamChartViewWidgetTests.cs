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

    private static void Paint(params TestClamChartView[] views)
    {
        foreach (var view in views)
        {
            using var bitmap = view.Paint(360, 240);
        }
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));
}
