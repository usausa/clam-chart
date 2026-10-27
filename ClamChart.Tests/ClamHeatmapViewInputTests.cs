namespace ClamChart.Tests;

using ClamChart.Tests.Data;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamHeatmapViewInputTests
{
    [Fact]
    public void AutoTouchModeFollowsTheSettings()
    {
        // Arrange
        using var view = Matrix();
        using var bitmap = view.Paint(300, 200);

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.SelectionMode = ChartSelectionMode.Item;
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.TrackballMode = ChartTrackballMode.LongPress;
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void HitOnlyTakesOnlyTouchesOnCellsWithValues()
    {
        // Arrange
        using var view = Matrix();
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(300, 200);

        // Act
        var onValue = view.HandleTouch(1, SKTouchAction.Pressed, Center(view, 0), 0);
        view.HandleTouch(1, SKTouchAction.Cancelled, default, 0);
        var onEmpty = view.HandleTouch(1, SKTouchAction.Pressed, Center(view, 1), 0);

        // Assert
        Assert.True(onValue);
        Assert.False(onEmpty);
    }

    [Fact]
    public void TappingACellReportsTheItem()
    {
        // Arrange
        using var view = Matrix();
        ChartItemEventArgs? args = null;
        view.ItemTapped += (_, e) => args = e;
        using var bitmap = view.Paint(300, 200);

        // Act
        Tap(view, Center(view, 3));

        // Assert
        Assert.NotNull(args);
        Assert.Null(args.Series);
        Assert.Equal(-1, args.SeriesIndex);
        Assert.Equal(2, args.Index);
        Assert.Equal("10", args.X);
        Assert.Equal(5, args.Y);
    }

    [Fact]
    public void SelectionTogglesTheTappedCell()
    {
        // Arrange
        using var view = Matrix();
        view.SelectionMode = ChartSelectionMode.Item;
        var changes = 0;
        view.SelectionChanged += (_, _) => changes++;
        using var bitmap = view.Paint(300, 200);
        var point = Center(view, 2);

        // Act
        Tap(view, point);

        // Assert
        Assert.Equal(1, view.SelectedIndex);
        Assert.Same(((IList<TestHeatItem>)view.ItemsSource!)[1], view.SelectedItem);

        // Act
        Tap(view, point);

        // Assert
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Null(view.SelectedItem);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void TappingAnEmptyCellClearsTheSelection()
    {
        // Arrange
        using var view = Matrix();
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 0;
        var taps = 0;
        view.ChartTapped += (_, _) => taps++;
        using var bitmap = view.Paint(300, 200);

        // Act
        Tap(view, Center(view, 1));

        // Assert
        Assert.Equal(1, taps);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void TrackballFollowsTheCellUnderTheFinger()
    {
        // Arrange
        using var view = Matrix();
        view.TrackballMode = ChartTrackballMode.Touch;
        var events = new List<ChartTrackingEventArgs>();
        view.TrackingStarted += (_, e) => events.Add(e);
        view.TrackingChanged += (_, e) => events.Add(e);
        view.TrackingEnded += (_, e) => events.Add(e);
        using var bitmap = view.Paint(300, 200);
        var first = Center(view, 0);
        var empty = Center(view, 1);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, first, 0);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(first.X + 3, first.Y + 1), 10);
        view.HandleTouch(1, SKTouchAction.Moved, empty, 20);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal(0, events[0].Index);
        Assert.Equal("9", events[0].X);
        Assert.Empty(events[0].Points);
        Assert.Equal(-1, events[1].Index);
        Assert.Equal("10", events[1].X);
        Assert.Null(view.TrackedItem);
        Assert.Equal(-1, view.TrackedIndex);

        // Act
        view.HandleTouch(1, SKTouchAction.Moved, Center(view, 2), 30);

        // Assert
        Assert.Same(((IList<TestHeatItem>)view.ItemsSource!)[1], view.TrackedItem);
        Assert.Equal(1, view.TrackedIndex);

        // Act
        view.HandleTouch(1, SKTouchAction.Released, Center(view, 2), 40);

        // Assert
        Assert.Equal(4, events.Count);
        Assert.Equal(-1, events[3].Index);
        Assert.Null(view.TrackedItem);
    }

    [Fact]
    public void TrackingWithinACellDoesNotAllocate()
    {
        // Arrange
        using var view = Matrix();
        view.TrackballMode = ChartTrackballMode.Touch;
        view.TrackingChanged += static (_, _) => { };
        using var bitmap = view.Paint(300, 200);
        using var surface = SKSurface.Create(new SKImageInfo(600, 400));
        var center = Center(view, 0);
        view.HandleTouch(1, SKTouchAction.Pressed, center, 0);
        view.HandleTouch(1, SKTouchAction.Moved, new Point(center.X + 10, center.Y), 10);
        Render(view, surface.Canvas);

        // Act
        var time = 20;
        var allocated = ChartAllocation.Measure(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                view.HandleTouch(1, SKTouchAction.Moved, new Point(center.X + i, center.Y + (i % 3)), time + i);
                Render(view, surface.Canvas);
            }

            time += 10;
        });

        // Assert
        Assert.Equal(0, allocated);
    }

    private static TestClamHeatmapView Matrix() =>
        new()
        {
            ValueAccessors = TestHeatItem.Accessors,
            RowKey = "day",
            ColumnKey = "slot",
            ValueKey = "value",
            ItemsSource = new List<TestHeatItem> { new("Mon", "9", 0, 1), new("Tue", "9", 0, 3), new("Tue", "10", 0, 5) }
        };

    private static Point Center(TestClamHeatmapView view, int cell)
    {
        var layout = view.ChartLayout;
        var bounds = layout.GetCellBounds(cell);
        return new Point(bounds.X + ((bounds.Width - layout.Gap) / 2), bounds.Y + ((bounds.Height - layout.Gap) / 2));
    }

    private static void Tap(TestClamHeatmapView view, Point point)
    {
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
    }

    private static void Render(TestClamHeatmapView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(2);
        view.Render(canvas, 300, 200);
        canvas.Restore();
    }
}
