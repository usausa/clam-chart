namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamRadarChartViewInputTests
{
    [Fact]
    public void AutoTouchModeFollowsTheSettings()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 8, 6, 4));
        using var bitmap = view.Paint(300, 300);

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.SelectionMode = ChartSelectionMode.Item;
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.SelectionMode = ChartSelectionMode.None;
        view.LegendToggleEnabled = true;
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.TrackballMode = ChartTrackballMode.LongPress;
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void MinimalDensityOnlyReceivesChartTaps()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 8, 6, 4));
        view.SelectionMode = ChartSelectionMode.Item;
        view.TrackballMode = ChartTrackballMode.Touch;
        var taps = 0;
        view.ChartTapped += (_, _) => taps++;
        using var bitmap = view.Paint(150, 90);

        // Act
        Tap(view, Vertex(view, 0, 0));

        // Assert
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
        Assert.Equal(1, taps);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.False(view.IsTracking);
    }

    [Fact]
    public void TappingAVertexReportsTheItem()
    {
        // Arrange
        var second = Series("Second", 5, 5, 5, 5);
        using var view = Chart(Series("First", 10, 8, 6, 4), second);
        ChartItemEventArgs? args = null;
        view.ItemTapped += (_, e) => args = e;
        using var bitmap = view.Paint(300, 300);

        // Act
        Tap(view, Vertex(view, 1, 2));

        // Assert
        Assert.NotNull(args);
        Assert.Same(second, args.Series);
        Assert.Equal(1, args.SeriesIndex);
        Assert.Equal(2, args.Index);
        Assert.Equal("C", args.X);
        Assert.Equal(5, args.Y);
    }

    [Fact]
    public void ItemSelectionTogglesTheVertexOfTheSeries()
    {
        // Arrange
        var second = Series("Second", 5, 5, 5, 5);
        using var view = Chart(Series("First", 10, 8, 6, 4), second);
        view.SelectionMode = ChartSelectionMode.Item;
        var changes = 0;
        view.SelectionChanged += (_, _) => changes++;
        using var bitmap = view.Paint(300, 300);
        var point = Vertex(view, 1, 3);

        // Act
        Tap(view, point);

        // Assert
        Assert.Same(second, view.SelectedSeries);
        Assert.Equal(3, view.SelectedIndex);
        Assert.Same(((IList<ChartEntry>)second.ItemsSource!)[3], view.SelectedItem);

        // Act
        Tap(view, point);

        // Assert
        Assert.Null(view.SelectedSeries);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Null(view.SelectedItem);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void CategorySelectionUsesTheFirstVisibleSeriesOfTheAxis()
    {
        // Arrange
        var first = Series("First", 10, 8, 6, 4);
        using var view = Chart(first, Series("Second", 5, 5, 5, 5));
        view.SelectionMode = ChartSelectionMode.Category;
        using var bitmap = view.Paint(300, 300);
        var layout = view.ChartLayout;

        // Act
        Tap(view, Vertex(view, 1, 1));

        // Assert
        Assert.Null(view.SelectedSeries);
        Assert.Equal(1, view.SelectedIndex);
        Assert.Same(((IList<ChartEntry>)first.ItemsSource!)[1], view.SelectedItem);

        // Act
        Tap(view, new Point(layout.CenterX - 30, layout.CenterY + (layout.Radius * 0.9)));

        // Assert
        Assert.Equal(2, view.SelectedIndex);
    }

    [Fact]
    public void TappingAwayClearsTheSelection()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 8, 6, 4));
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 0;
        var taps = 0;
        view.ChartTapped += (_, _) => taps++;
        using var bitmap = view.Paint(300, 300);

        // Act
        Tap(view, new Point(3, 3));

        // Assert
        Assert.Equal(1, taps);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void LegendTogglesTheSeriesButKeepsOneVisible()
    {
        // Arrange
        var first = Series("First", 10, 8, 6, 4);
        var second = Series("Second", 5, 5, 5, 5);
        using var view = Chart(first, second);
        view.LegendVisible = true;
        view.LegendToggleEnabled = true;
        ChartLegendEventArgs? args = null;
        view.LegendItemTapped += (_, e) => args = e;
        using var bitmap = view.Paint(300, 300);

        // Act
        Tap(view, LegendItem(view, 0));
        using var hidden = view.Paint(300, 300);
        Tap(view, LegendItem(view, 1));

        // Assert
        Assert.False(first.IsVisible);
        Assert.True(second.IsVisible);
        Assert.NotNull(args);
        Assert.Same(second, args.Series);
        Assert.Equal(1, args.Index);
    }

    [Fact]
    public void TrackballFollowsTheNearestAxis()
    {
        // Arrange
        var first = Series("First", 10, 8, 6, 4);
        using var view = Chart(first, Series("Second", 5, 5, Double.NaN, 5));
        view.TrackballMode = ChartTrackballMode.Touch;
        var events = new List<ChartTrackingEventArgs>();
        view.TrackingStarted += (_, e) => events.Add(e);
        view.TrackingChanged += (_, e) => events.Add(e);
        view.TrackingEnded += (_, e) => events.Add(e);
        using var bitmap = view.Paint(300, 300);
        var layout = view.ChartLayout;
        var right = new Point(layout.CenterX + 60, layout.CenterY - 5);
        var along = new Point(layout.CenterX + 80, layout.CenterY - 3);
        var bottom = new Point(layout.CenterX - 5, layout.CenterY + 70);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, right, 0);
        view.HandleTouch(1, SKTouchAction.Moved, along, 10);
        view.HandleTouch(1, SKTouchAction.Moved, bottom, 20);

        // Assert
        Assert.True(view.IsTracking);
        Assert.Equal(2, events.Count);
        Assert.Equal("B", events[0].X);
        Assert.Equal(1, events[0].Index);
        Assert.Equal([8d, 5], events[0].Points.Select(static x => x.Y));
        Assert.Equal("C", events[1].X);
        Assert.Single(events[1].Points);
        Assert.Same(((IList<ChartEntry>)first.ItemsSource!)[2], view.TrackedItem);
        Assert.Equal(2, view.TrackedIndex);

        // Act
        view.HandleTouch(1, SKTouchAction.Released, bottom, 30);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Empty(events[2].Points);
        Assert.Null(view.TrackedItem);
        Assert.Equal(-1, view.TrackedIndex);
    }

    [Fact]
    public void TooltipNamesTheSeriesAndFormatsTheValue()
    {
        // Arrange
        var first = Series("First", 10, 8, 6, 4);
        first.ValueFormat = "0.0";
        using var view = Chart(first, Series(String.Empty, 5, 5, 5, 5));
        view.ValueFormat = "0 pt";
        using var bitmap = view.Paint(300, 300);

        // Act
        var named = view.GetTrackingText(0, 1);
        var plain = view.GetTrackingText(1, 1);
        view.TooltipFormatter = static info => info.X + "=" + info.Y.ToString(CultureInfo.InvariantCulture);
        var custom = view.GetTrackingText(0, 1);

        // Assert
        Assert.Equal("First " + 8.ToString("0.0", CultureInfo.CurrentCulture), named);
        Assert.Equal("5 pt", plain);
        Assert.Equal("B=8", custom);
    }

    [Fact]
    public void TrackingMovesWithinAnAxisWithoutAllocating()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 8, 6, 4), Series("Second", 5, 5, 5, 5));
        view.TrackballMode = ChartTrackballMode.Touch;
        view.TrackingChanged += static (_, _) => { };
        using var bitmap = view.Paint(300, 300);
        using var surface = SKSurface.Create(new SKImageInfo(600, 600));
        var layout = view.ChartLayout;
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(layout.CenterX + 60, layout.CenterY - 5), 0);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(layout.CenterX + 70, layout.CenterY - 2), 10);
        Render(view, surface.Canvas);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10; i++)
        {
            view.HandleTouch(1, SKTouchAction.Moved, new Point(layout.CenterX + 60 + i, layout.CenterY + (i % 3)), 20 + i);
            Render(view, surface.Canvas);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.Equal(0, allocated);
    }

    private static TestClamRadarChartView Chart(params RadarSeries[] series)
    {
        var view = new TestClamRadarChartView();
        foreach (var item in series)
        {
            view.Series.Add(item);
        }

        return view;
    }

    private static RadarSeries Series(string title, params double[] values) =>
        new() { Title = title, ItemsSource = values.Select(static (value, index) => new ChartEntry(((char)('A' + index)).ToString(), value)).ToList(), EmptyPointMode = ChartEmptyPointMode.Gap };

    private static Point Vertex(TestClamRadarChartView view, int seriesIndex, int axis)
    {
        var layout = view.ChartLayout;
        var (x, y) = layout.GetPoint(axis, RadarLayout.GetRatio(layout.GetValue(seriesIndex, axis), layout.Scale));
        return new Point(x, y);
    }

    private static Point LegendItem(TestClamRadarChartView view, int index)
    {
        var bounds = view.ChartLayout.Legend.Items[index].Bounds;
        return new Point(bounds.CenterX, bounds.CenterY);
    }

    private static void Tap(TestClamRadarChartView view, Point point)
    {
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
    }

    private static void Render(TestClamRadarChartView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(2);
        view.Render(canvas, 300, 300);
        canvas.Restore();
    }
}
