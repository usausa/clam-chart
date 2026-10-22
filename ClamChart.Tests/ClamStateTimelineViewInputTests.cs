namespace ClamChart.Tests;

using ClamChart.Tests.Data;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamStateTimelineViewInputTests
{
    private static readonly DateTime Origin = new(2026, 10, 22, 0, 0, 0);

    [Fact]
    public void AutoTouchModeFollowsTheSettings()
    {
        // Arrange
        using var view = Servers();
        using var bitmap = view.Paint(300, 200);

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.ItemTapped += static (_, _) => { };
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.TrackballMode = ChartTrackballMode.LongPress;
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void TappingAStateReportsItsFirstItem()
    {
        // Arrange
        using var view = Servers();
        ChartItemEventArgs? args = null;
        view.ItemTapped += (_, e) => args = e;
        using var bitmap = view.Paint(300, 200);

        // Act
        Tap(view, Point(view, 0, 1.5));

        // Assert
        Assert.NotNull(args);
        Assert.Null(args.Series);
        Assert.Equal(0, args.Index);
        Assert.Same(((IList<TestStateItem>)view.ItemsSource!)[0], args.Item);
        Assert.Equal(Origin, args.X);
        Assert.True(Double.IsNaN(args.Y));
    }

    [Fact]
    public void TappingAnEmptyPlaceReportsTheChart()
    {
        // Arrange
        using var view = Servers();
        var items = 0;
        var charts = 0;
        view.ItemTapped += (_, _) => items++;
        view.ChartTapped += (_, _) => charts++;
        using var bitmap = view.Paint(300, 200);

        // Act
        Tap(view, Point(view, 1, 0.5));

        // Assert
        Assert.Equal(0, items);
        Assert.Equal(1, charts);
    }

    [Fact]
    public void TrackingFollowsTheStateUnderTheFinger()
    {
        // Arrange
        using var view = Servers();
        view.TrackballMode = ChartTrackballMode.Touch;
        var events = new List<ChartTrackingEventArgs>();
        view.TrackingStarted += (_, e) => events.Add(e);
        view.TrackingChanged += (_, e) => events.Add(e);
        view.TrackingEnded += (_, e) => events.Add(e);
        using var bitmap = view.Paint(300, 200);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, Point(view, 0, 1), 0);
        view.HandleTouch(1, SKTouchAction.Moved, Point(view, 0, 1.2), 10);
        view.HandleTouch(1, SKTouchAction.Moved, Point(view, 0, 2.5), 20);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal(0, events[0].Index);
        Assert.Equal(Origin.AddHours(1), (DateTime)events[0].X!, TimeSpan.FromMinutes(5));
        Assert.Empty(events[0].Points);
        Assert.Equal(1, events[1].Index);
        Assert.Same(((IList<TestStateItem>)view.ItemsSource!)[1], view.TrackedItem);
        Assert.Equal(1, view.TrackedIndex);

        // Act
        view.HandleTouch(1, SKTouchAction.Moved, Point(view, 1, 0.5), 30);

        // Assert
        Assert.Equal(-1, events[^1].Index);
        Assert.Null(view.TrackedItem);

        // Act
        view.HandleTouch(1, SKTouchAction.Released, Point(view, 1, 0.5), 40);

        // Assert
        Assert.Equal(-1, view.TrackedIndex);
    }

    [Fact]
    public void TrackingWithinAStateDoesNotAllocate()
    {
        // Arrange
        using var view = Servers();
        view.TrackballMode = ChartTrackballMode.Touch;
        view.TrackingChanged += static (_, _) => { };
        using var bitmap = view.Paint(300, 200);
        using var surface = SKSurface.Create(new SKImageInfo(600, 400));
        var point = Point(view, 0, 1);
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(point.X + 2, point.Y), 10);
        Render(view, surface.Canvas);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10; i++)
        {
            view.HandleTouch(1, SKTouchAction.Moved, new Point(point.X + i, point.Y + (i % 3)), 20 + i);
            Render(view, surface.Canvas);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.Equal(0, allocated);
    }

    private static TestClamStateTimelineView Servers() =>
        new()
        {
            ContentPadding = new Thickness(0),
            ValueAccessors = TestStateItem.Accessors,
            RowKey = "server",
            StartKey = "start",
            StateKey = "state",
            Maximum = Origin.AddHours(4),
            ItemsSource = new List<TestStateItem>
            {
                new("web", Origin, "Running"),
                new("web", Origin.AddHours(2), "Stopped"),
                new("db", Origin.AddHours(1), "Running")
            }
        };

    private static Point Point(TestClamStateTimelineView view, int row, double hours)
    {
        var layout = view.ChartLayout;
        var scale = layout.Axis.Scale;
        var x = layout.Plot.X + ((Origin.AddHours(hours).Ticks - scale.Minimum) / scale.Span * layout.Plot.Width);
        return new Point(x, layout.GetRowTop(row) + (layout.RowHeight / 2));
    }

    private static void Tap(TestClamStateTimelineView view, Point point)
    {
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
    }

    private static void Render(TestClamStateTimelineView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(2);
        view.Render(canvas, 300, 200);
        canvas.Restore();
    }
}
