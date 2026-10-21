namespace ClamChart.Tests;

using System.Runtime.CompilerServices;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamChartViewInputTests
{
    [Fact]
    public void TouchesPassThroughWhenNothingIsConfigured()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        using var bitmap = view.Paint(360, 240);

        // Act
        var handled = view.HandleTouch(1, SKTouchAction.Pressed, BarCenter(view, 0, 1), 0);

        // Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        Assert.False(handled);
    }

    [Fact]
    public void AutoTouchModeFollowsTheSettings()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        using var bitmap = view.Paint(360, 240);

        // Act & Assert
        view.SelectionMode = ChartSelectionMode.Item;
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.SelectionMode = ChartSelectionMode.None;
        view.LegendToggleEnabled = true;
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.LegendToggleEnabled = false;
        view.ItemTapped += static (_, _) => { };
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.TrackballMode = ChartTrackballMode.LongPress;
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
        view.TouchMode = ChartTouchMode.PassThrough;
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
    }

    [Fact]
    public void MinimalDensityOnlyReceivesChartTaps()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        view.SelectionMode = ChartSelectionMode.Item;
        view.TrackballMode = ChartTrackballMode.Touch;
        using var bitmap = view.Paint(150, 90);

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.ChartTappedCommand = new Command(static () => { });
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void HitOnlyCapturesOnlyTouchesThatStartOnAnItem()
    {
        // Arrange
        using var view = Chart(new LineSeries { ItemsSource = Entries(10, 20, 30) });
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(360, 240);
        var layout = view.ChartLayout;

        // Act
        var onPoint = view.HandleTouch(1, SKTouchAction.Pressed, new Point(layout.XAxis.ToPixel(1), layout.YAxis.ToPixel(20)), 0);
        view.HandleTouch(1, SKTouchAction.Cancelled, default, 0);
        var away = view.HandleTouch(1, SKTouchAction.Pressed, new Point(layout.XAxis.ToPixel(1), layout.PlotBounds.Bottom - 1), 0);

        // Assert
        Assert.True(onPoint);
        Assert.False(away);
    }

    [Fact]
    public void TapOnABarRaisesItemTappedSelectsAndThenRunsTheCommand()
    {
        // Arrange
        var entries = Entries(10, 20, 30);
        var series = new BarSeries { ItemsSource = entries };
        using var view = Chart(series);
        view.SelectionMode = ChartSelectionMode.Item;
        var calls = new List<string>();
        ChartItemEventArgs? tapped = null;
        ChartSelectionChangedEventArgs? changed = null;
        view.ItemTapped += (_, e) =>
        {
            tapped = e;
            calls.Add("event");
        };
        view.SelectionChanged += (_, e) =>
        {
            changed = e;
            calls.Add("selection");
        };
        view.ItemTappedCommand = new Command(() => calls.Add("command"));
        using var bitmap = view.Paint(360, 240);
        var point = BarCenter(view, 0, 1);

        // Act
        Tap(view, point);

        // Assert
        Assert.Equal(["event", "selection", "command"], calls);
        Assert.NotNull(tapped);
        Assert.Same(series, tapped.Series);
        Assert.Equal(0, tapped.SeriesIndex);
        Assert.Equal(1, tapped.Index);
        Assert.Same(entries[1], tapped.Item);
        Assert.Equal("C1", tapped.X);
        Assert.Equal(20, tapped.Y);
        Assert.Equal(point, tapped.Position);
        Assert.Same(entries[1], view.SelectedItem);
        Assert.Equal(1, view.SelectedIndex);
        Assert.Same(series, view.SelectedSeries);
        Assert.NotNull(changed);
        Assert.Null(changed.OldItem);
        Assert.Same(entries[1], changed.NewItem);
        Assert.Equal(-1, changed.OldIndex);
        Assert.Equal(1, changed.NewIndex);

        // Act
        Tap(view, point);

        // Assert
        Assert.Null(view.SelectedItem);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Null(view.SelectedSeries);
    }

    [Fact]
    public void HandledItemTapSkipsTheSelectionAndTheCommand()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        view.SelectionMode = ChartSelectionMode.Item;
        var executed = false;
        view.ItemTapped += static (_, e) => e.Handled = true;
        view.ItemTappedCommand = new Command(() => executed = true);
        using var bitmap = view.Paint(360, 240);

        // Act
        Tap(view, BarCenter(view, 0, 1));

        // Assert
        Assert.Null(view.SelectedItem);
        Assert.False(executed);
    }

    [Fact]
    public void CategorySelectionUsesThePointOfTheFirstVisibleSeries()
    {
        // Arrange
        var first = Entries(10, 20, 30);
        var second = Entries(5, 6, 7);
        using var view = Chart(new BarSeries { ItemsSource = first }, new BarSeries { ItemsSource = second });
        view.SelectionMode = ChartSelectionMode.Category;
        using var bitmap = view.Paint(360, 240);

        // Act
        Tap(view, BarCenter(view, 1, 2));

        // Assert
        Assert.Same(first[2], view.SelectedItem);
        Assert.Equal(2, view.SelectedIndex);
        Assert.Null(view.SelectedSeries);
    }

    [Fact]
    public void TapOutsideTheElementsClearsTheSelectionUnlessHandled()
    {
        // Arrange
        var entries = Entries(10, 20, 30);
        using var view = Chart(new LineSeries { ItemsSource = entries });
        view.SelectionMode = ChartSelectionMode.Item;
        view.TouchMode = ChartTouchMode.Interactive;
        var taps = 0;
        var handle = new StrongBox<bool>(true);
        view.ChartTapped += (_, e) =>
        {
            taps++;
            e.Handled = handle.Value;
        };
        using var bitmap = view.Paint(360, 240);
        var layout = view.ChartLayout;
        var empty = new Point(layout.XAxis.ToPixel(1), layout.PlotBounds.Bottom - 1);
        Tap(view, new Point(layout.XAxis.ToPixel(2), layout.YAxis.ToPixel(30)));

        // Act
        Tap(view, empty);

        // Assert
        Assert.Equal(1, taps);
        Assert.Same(entries[2], view.SelectedItem);

        // Act
        handle.Value = false;
        Tap(view, empty);

        // Assert
        Assert.Equal(2, taps);
        Assert.Null(view.SelectedItem);
    }

    [Fact]
    public void SelectedItemFromTheViewModelWaitsForItsData()
    {
        // Arrange
        var entries = Entries(10, 20, 30);
        var series = new LineSeries();
        using var view = Chart(series);
        view.SelectionMode = ChartSelectionMode.Item;
        TestDispatcher.Current.RunPending();

        // Act
        view.SelectedItem = entries[2];

        // Assert
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Same(entries[2], view.SelectedItem);

        // Act
        series.ItemsSource = entries;
        TestDispatcher.Current.RunPending();

        // Assert
        Assert.Equal(2, view.SelectedIndex);
        Assert.Same(series, view.SelectedSeries);
    }

    [Fact]
    public void RemovingTheSelectedItemClearsTheSelection()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(10, 20, 30));
        using var view = Chart(new LineSeries { ItemsSource = entries });
        view.SelectionMode = ChartSelectionMode.Item;
        view.OnAttached();
        TestDispatcher.Current.RunPending();
        var selected = entries[1];
        view.SelectedItem = selected;
        ChartSelectionChangedEventArgs? changed = null;
        view.SelectionChanged += (_, e) => changed = e;

        // Act
        entries.RemoveAt(0);
        TestDispatcher.Current.RunPending();

        // Assert
        Assert.Equal(0, view.SelectedIndex);
        Assert.Null(changed);

        // Act
        entries.Remove(selected);
        TestDispatcher.Current.RunPending();

        // Assert
        Assert.Null(view.SelectedItem);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.NotNull(changed);
        Assert.Same(selected, changed.OldItem);
    }

    [Fact]
    public void SelectedIndexPicksTheItemOfTheSeries()
    {
        // Arrange
        var first = Entries(10, 20, 30);
        var second = Entries(1, 2, 3);
        var other = new LineSeries { ItemsSource = second };
        using var view = Chart(new LineSeries { ItemsSource = first }, other);
        view.SelectionMode = ChartSelectionMode.Item;
        TestDispatcher.Current.RunPending();

        // Act
        view.SelectedIndex = 2;

        // Assert
        Assert.Same(first[2], view.SelectedItem);

        // Act
        view.SelectedSeries = other;

        // Assert
        Assert.Same(second[2], view.SelectedItem);
        Assert.Equal(2, view.SelectedIndex);

        // Act
        view.SelectionMode = ChartSelectionMode.None;

        // Assert
        Assert.Null(view.SelectedItem);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void LegendTapTogglesTheSeriesButKeepsTheLastOne()
    {
        // Arrange
        var first = new LineSeries { Title = "A", ItemsSource = Entries(1, 2) };
        var second = new LineSeries { Title = "B", ItemsSource = Entries(3, 4) };
        using var view = Chart(first, second);
        view.LegendToggleEnabled = true;
        using var bitmap = view.Paint(360, 240);

        // Act
        Tap(view, LegendItem(view, 0));
        using var hidden = view.Paint(360, 240);
        Tap(view, LegendItem(view, 1));

        // Assert
        Assert.False(first.IsVisible);
        Assert.True(second.IsVisible);

        // Act
        Tap(view, LegendItem(view, 0));

        // Assert
        Assert.True(first.IsVisible);
    }

    [Fact]
    public void LegendTableRowTapTogglesTheSeries()
    {
        // Arrange
        var first = new LineSeries { Title = "A", ItemsSource = Entries(1, 2) };
        var second = new LineSeries { Title = "B", ItemsSource = Entries(3, 4) };
        using var view = Chart(first, second);
        view.LegendMode = ChartLegendMode.Table;
        view.LegendToggleEnabled = true;
        using var bitmap = view.Paint(360, 240);
        var legend = view.ChartLayout.Legend;

        // Act
        Tap(view, new Point(legend.Items[1].Bounds.Right - 4, legend.Items[1].Bounds.Y + 1));

        // Assert
        Assert.True(first.IsVisible);
        Assert.False(second.IsVisible);

        // Act
        Tap(view, new Point(legend.HeaderBounds.CenterX, legend.HeaderBounds.CenterY));

        // Assert
        Assert.True(first.IsVisible);
        Assert.False(second.IsVisible);
    }

    [Fact]
    public void HandledLegendTapKeepsTheSeries()
    {
        // Arrange
        var first = new LineSeries { Title = "A", ItemsSource = Entries(1, 2) };
        using var view = Chart(first, new LineSeries { Title = "B", ItemsSource = Entries(3, 4) });
        view.LegendToggleEnabled = true;
        ChartLegendEventArgs? tapped = null;
        view.LegendItemTapped += (_, e) =>
        {
            tapped = e;
            e.Handled = true;
        };
        using var bitmap = view.Paint(360, 240);

        // Act
        Tap(view, LegendItem(view, 0));

        // Assert
        Assert.True(first.IsVisible);
        Assert.NotNull(tapped);
        Assert.Same(first, tapped.Series);
        Assert.Equal(0, tapped.Index);
    }

    [Fact]
    public void LongPressTracksTheNearestPointUntilRelease()
    {
        // Arrange
        var entries = Entries(10, 20, 30, 40);
        using var view = Chart(new LineSeries { ItemsSource = entries });
        view.TrackballMode = ChartTrackballMode.LongPress;
        var events = new List<string>();
        view.TrackingStarted += (_, e) => events.Add($"started {e.Index}");
        view.TrackingChanged += (_, e) => events.Add($"changed {e.Index}");
        view.TrackingEnded += (_, e) => events.Add($"ended {e.Index}");
        var commands = 0;
        view.TrackingChangedCommand = new Command(() => commands++);
        using var bitmap = view.Paint(360, 240);
        var layout = view.ChartLayout;
        var y = layout.YAxis.ToPixel(20);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(layout.XAxis.ToPixel(1), y), 1000);
        view.HandleInputTick(1499);

        // Assert
        Assert.False(view.IsTracking);
        Assert.True(TestDispatcher.Current.LastTimer?.IsRunning);

        // Act
        view.HandleInputTick(1500);

        // Assert
        Assert.True(view.IsTracking);
        Assert.False(TestDispatcher.Current.LastTimer?.IsRunning);
        Assert.Same(entries[1], view.TrackedItem);
        Assert.Equal(1, view.TrackedIndex);

        // Act
        view.HandleTouch(1, SKTouchAction.Moved, new Point(layout.XAxis.ToPixel(1.2), y), 1600);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(layout.XAxis.ToPixel(2.9), y), 1700);
        view.HandleTouch(1, SKTouchAction.Released, new Point(layout.XAxis.ToPixel(2.9), y), 1800);

        // Assert
        Assert.Equal(["started 1", "changed 3", "ended -1"], events);
        Assert.Equal(3, commands);
        Assert.False(view.IsTracking);
        Assert.Null(view.TrackedItem);
        Assert.Equal(-1, view.TrackedIndex);
    }

    [Fact]
    public void TouchTrackballGivesWayToVerticalScrolling()
    {
        // Arrange
        using var view = Chart(new LineSeries { ItemsSource = Entries(10, 20, 30) });
        view.TrackballMode = ChartTrackballMode.Touch;
        var ended = 0;
        view.TrackingEnded += (_, _) => ended++;
        using var bitmap = view.Paint(360, 240);
        var start = new Point(view.ChartLayout.XAxis.ToPixel(1), 100);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, start, 0);

        // Assert
        Assert.True(view.IsTracking);

        // Act
        view.HandleTouch(1, SKTouchAction.Moved, new Point(start.X + 2, start.Y + 20), 10);

        // Assert
        Assert.False(view.IsTracking);
        Assert.Equal(1, ended);
    }

    [Fact]
    public void SecondFingerAndCancelInteractionEndTheTracking()
    {
        // Arrange
        using var view = Chart(new LineSeries { ItemsSource = Entries(10, 20, 30) });
        view.TrackballMode = ChartTrackballMode.Touch;
        using var bitmap = view.Paint(360, 240);
        var point = new Point(view.ChartLayout.XAxis.ToPixel(1), 100);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(2, SKTouchAction.Pressed, point, 10);

        // Assert
        Assert.False(view.IsTracking);

        // Act
        view.HandleTouch(1, SKTouchAction.Released, point, 20);
        view.HandleTouch(2, SKTouchAction.Released, point, 30);
        view.HandleTouch(1, SKTouchAction.Pressed, point, 40);
        view.CancelInteraction();

        // Assert
        Assert.False(view.IsTracking);
        Assert.Equal(-1, view.TrackedIndex);
    }

    [Fact]
    public void TrackballSnapsToTheNearestXOfAllSeries()
    {
        // Arrange
        var first = new List<ChartPoint> { new(0, 1), new(10, 2), new(20, 3) };
        var second = new List<ChartPoint> { new(5, 4), new(15, 5) };
        using var view = Chart(new LineSeries { ItemsSource = first }, new LineSeries { ItemsSource = second });
        view.TrackballMode = ChartTrackballMode.Touch;
        ChartTrackingEventArgs? started = null;
        view.TrackingStarted += (_, e) => started = e;
        using var bitmap = view.Paint(360, 240);
        var layout = view.ChartLayout;

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(layout.XAxis.ToPixel(6), 100), 0);

        // Assert
        Assert.Same(second[0], view.TrackedItem);
        Assert.Equal(0, view.TrackedIndex);
        Assert.NotNull(started);
        Assert.Equal(5d, started.X);
        Assert.Equal([0, 1], started.Points.Select(static x => x.SeriesIndex));
        Assert.Equal([0, 0], started.Points.Select(static x => x.Index));

        // Act
        view.HandleTouch(1, SKTouchAction.Moved, new Point(layout.XAxis.ToPixel(9), 100), 10);

        // Assert
        Assert.Same(first[1], view.TrackedItem);
        Assert.Equal(1, view.TrackedIndex);
    }

    [Fact]
    public void TooltipFormatterReceivesTheTrackedPoint()
    {
        // Arrange
        var entries = Entries(10, 20, 30);
        using var view = Chart(new LineSeries { Title = "Sales", ItemsSource = entries });
        view.TrackballMode = ChartTrackballMode.Touch;
        var infos = new List<ChartTrackingInfo>();
        view.TooltipFormatter = info =>
        {
            infos.Add(info);
            return "row";
        };
        using var bitmap = view.Paint(360, 240);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(view.ChartLayout.XAxis.ToPixel(1), 100), 0);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(view.ChartLayout.XAxis.ToPixel(2), 100), 10);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(view.ChartLayout.XAxis.ToPixel(1), 100), 20);

        // Assert
        Assert.Equal([1, 2], infos.Select(static x => x.Index));
        Assert.Same(entries[1], infos[0].Item);
        Assert.Equal("C1", infos[0].X);
        Assert.Equal(20, infos[0].Y);
    }

    [Fact]
    public void MinimalChartIsOneButtonWithAPressedOverlay()
    {
        // Arrange
        using var view = Chart(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        view.SelectionMode = ChartSelectionMode.Item;
        var items = 0;
        var taps = 0;
        view.ItemTapped += (_, _) => items++;
        view.ChartTapped += (_, _) => taps++;
        using var bitmap = view.Paint(150, 90);
        var point = BarCenter(view, 0, 1);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);

        // Assert
        Assert.True(view.IsPressed);

        // Act
        view.HandleTouch(1, SKTouchAction.Released, point, 10);

        // Assert
        Assert.False(view.IsPressed);
        Assert.Equal(0, items);
        Assert.Equal(1, taps);
        Assert.Null(view.SelectedItem);
    }

    [Fact]
    public void TrackingMovesWithinOnePointDoNotAllocate()
    {
        // Arrange
        using var view = Chart(new LineSeries { Title = "Sales", ItemsSource = Entries(10, 20, 30, 40) });
        view.TrackballMode = ChartTrackballMode.Touch;
        using var bitmap = view.Paint(360, 240);
        var x = view.ChartLayout.XAxis.ToPixel(1);
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(x - 20, 100), 0);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(x + 10, 100), 1);
        Move(view, x, 2);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        Move(view, x, 100);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.True(view.IsTracking);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void TrackballAndTooltipAddNoAllocationToAFrame()
    {
        // Arrange
        using var view = Chart(new LineSeries { Title = "Sales", ItemsSource = Entries(10, 20, 30, 40) }, new BarSeries { Title = "Cost", ItemsSource = Entries(5, 6, 7, 8) });
        view.TrackballMode = ChartTrackballMode.Touch;
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(360, 240);
        using var surface = SKSurface.Create(new SKImageInfo(720, 480));
        var idle = MeasureFrame(view, surface.Canvas);
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(view.ChartLayout.XAxis.ToPixel(1), 100), 0);
        view.SelectedIndex = 1;

        // Act
        var tracking = MeasureFrame(view, surface.Canvas);

        // Assert
        Assert.True(view.IsTracking);
        Assert.Equal(idle, tracking);
    }

    [Fact]
    public void TapSelectsTheScatterPointByTheDistanceInBothDirections()
    {
        // Arrange
        var points = new List<ChartPoint> { new(0, 0), new(5, 5), new(10, 10) };
        using var view = Chart(new ScatterSeries { ItemsSource = points });
        view.SelectionMode = ChartSelectionMode.Item;
        view.TouchMode = ChartTouchMode.Interactive;
        using var bitmap = view.Paint(360, 240);
        var layout = view.ChartLayout;
        var x = layout.XAxis.ToPixel(5);
        var y = layout.YAxis.ToPixel(5);
        var diagonal = CartesianHitMap.PointRadius * 0.8;

        // Act
        Tap(view, new Point(x + 10, y - 10));

        // Assert
        Assert.Same(points[1], view.SelectedItem);

        // Act
        Tap(view, new Point(x + diagonal, y - diagonal));

        // Assert
        Assert.Null(view.SelectedItem);
    }

    [Fact]
    public void ScatterTrackballFollowsTheNearestPointWithACrosshair()
    {
        // Arrange
        var points = new List<ChartPoint> { new(0, 10), new(10, 0) };
        using var view = Chart(new ScatterSeries { Title = "Points", ItemsSource = points });
        view.TrackballMode = ChartTrackballMode.Touch;
        using var first = view.Paint(360, 240);
        var layout = view.ChartLayout;
        var left = layout.XAxis.ToPixel(0);
        var right = layout.XAxis.ToPixel(10);
        var bottom = layout.YAxis.ToPixel(0);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(left + ((right - left) * 0.4), bottom - 5), 0);
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Same(points[1], view.TrackedItem);
        Assert.Equal(1, view.TrackedIndex);
        var line = ChartStyles.Light.TrackballLineColor.ToSKColor();
        Assert.Equal(line, bitmap.GetPixel((int)((layout.PlotBounds.X + 3) * 2), (int)Math.Floor(bottom * 2)));
        Assert.Equal(line, bitmap.GetPixel((int)Math.Floor(right * 2), (int)((layout.PlotBounds.Y + 3) * 2)));
    }

    private static TestClamChartView Chart(params ChartSeries[] series)
    {
        var view = new TestClamChartView();
        foreach (var item in series)
        {
            view.Series.Add(item);
        }

        return view;
    }

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static Point BarCenter(TestClamChartView view, int seriesIndex, int index)
    {
        var layout = view.ChartLayout;
        var geometry = layout.Geometries[seriesIndex];
        var (from, to) = geometry.GetBarRange(geometry.X[index]);
        var along = layout.XAxis.ToPixel((from + to) / 2);
        var across = layout.YAxis.ToPixel((geometry.Start[index] + geometry.End[index]) / 2);
        return layout.Orientation == ChartOrientation.Vertical ? new Point(along, across) : new Point(across, along);
    }

    private static Point LegendItem(TestClamChartView view, int index)
    {
        var bounds = view.ChartLayout.Legend.Items[index].Bounds;
        return new Point(bounds.CenterX, bounds.CenterY);
    }

    private static void Tap(TestClamChartView view, Point point)
    {
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
    }

    private static void Move(TestClamChartView view, double x, double time)
    {
        for (var i = 0; i < 20; i++)
        {
            view.HandleTouch(1, SKTouchAction.Moved, new Point(x + ((i % 5) - 2), 100 + (i % 3)), time + i);
        }
    }

    private static long MeasureFrame(TestClamChartView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(2);
        view.Render(canvas, 360, 240);
        view.Render(canvas, 360, 240);
        var before = GC.GetAllocatedBytesForCurrentThread();
        view.Render(canvas, 360, 240);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }
}
