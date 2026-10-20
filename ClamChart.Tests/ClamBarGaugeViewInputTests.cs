namespace ClamChart.Tests;

using SkiaSharp.Views.Maui;

public sealed class ClamBarGaugeViewInputTests
{
    [Fact]
    public void AutoTouchModeFollowsTheHandlers()
    {
        // Arrange
        using var view = Gauge(10, 30);
        using var bitmap = view.Paint(360, 120);

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.ItemTapped += static (_, _) => { };
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.ChartTapped += static (_, _) => { };
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void NarrowGaugesOnlyReceiveChartTaps()
    {
        // Arrange
        using var view = Gauge(10, 30);
        view.ItemTapped += static (_, _) => { };
        using var bitmap = view.Paint(150, 400);

        // Act & Assert
        Assert.Equal(ChartDensity.Minimal, view.EffectiveDensity);
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.ChartTappedCommand = new Command(static () => { });
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void HitOnlyCapturesOnlyTouchesThatStartOnARow()
    {
        // Arrange
        using var view = Gauge(10, 30);
        view.ItemTapped += static (_, _) => { };
        using var bitmap = view.Paint(360, 120);

        // Act
        var onRow = view.HandleTouch(1, SKTouchAction.Pressed, RowPoint(view, 1), 0);
        view.HandleTouch(1, SKTouchAction.Cancelled, default, 0);
        var between = view.HandleTouch(1, SKTouchAction.Pressed, GapPoint(view), 0);

        // Assert
        Assert.True(onRow);
        Assert.False(between);
    }

    [Fact]
    public void TapOnARowRaisesItemTappedAndThenRunsTheCommand()
    {
        // Arrange
        var entries = Entries(10, 30);
        using var view = Gauge(entries);
        var calls = new List<string>();
        ChartItemEventArgs? tapped = null;
        view.ItemTapped += (_, e) =>
        {
            tapped = e;
            calls.Add("event");
        };
        view.ItemTappedCommand = new Command(() => calls.Add("command"));
        using var bitmap = view.Paint(360, 120);

        // Act
        Tap(view, RowPoint(view, 1));

        // Assert
        Assert.Equal(["event", "command"], calls);
        Assert.NotNull(tapped);
        Assert.Null(tapped.Series);
        Assert.Equal(-1, tapped.SeriesIndex);
        Assert.Equal(1, tapped.Index);
        Assert.Same(entries[1], tapped.Item);
        Assert.Equal("C1", tapped.X);
        Assert.Equal(30, tapped.Y);
    }

    [Fact]
    public void SortedRowsReportTheIndexOfTheData()
    {
        // Arrange
        var entries = Entries(10, 30);
        using var view = Gauge(entries);
        view.SortMode = ChartSortMode.Descending;
        ChartItemEventArgs? tapped = null;
        view.ItemTapped += (_, e) => tapped = e;
        using var bitmap = view.Paint(360, 120);

        // Act
        Tap(view, RowPoint(view, 0));

        // Assert
        Assert.NotNull(tapped);
        Assert.Equal(1, tapped.Index);
        Assert.Same(entries[1], tapped.Item);
    }

    [Fact]
    public void HandledItemTapSkipsTheCommand()
    {
        // Arrange
        using var view = Gauge(10, 30);
        var executed = false;
        view.ItemTapped += static (_, e) => e.Handled = true;
        view.ItemTappedCommand = new Command(() => executed = true);
        using var bitmap = view.Paint(360, 120);

        // Act
        Tap(view, RowPoint(view, 0));

        // Assert
        Assert.False(executed);
    }

    [Fact]
    public void TapBetweenRowsRaisesChartTapped()
    {
        // Arrange
        using var view = Gauge(10, 30);
        var items = 0;
        ChartTappedEventArgs? tapped = null;
        view.ItemTapped += (_, _) => items++;
        view.ChartTapped += (_, e) => tapped = e;
        using var bitmap = view.Paint(360, 120);

        // Act
        Tap(view, GapPoint(view));

        // Assert
        Assert.NotNull(tapped);
        Assert.Equal(0, items);
    }

    [Fact]
    public void ReleaseOnAnotherRowIsNotATap()
    {
        // Arrange
        using var view = Gauge(10, 30);
        var calls = 0;
        view.ItemTapped += (_, _) => calls++;
        view.ChartTapped += (_, _) => calls++;
        using var bitmap = view.Paint(360, 120);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, RowPoint(view, 0), 0);
        view.HandleTouch(1, SKTouchAction.Released, RowPoint(view, 1), 10);

        // Assert
        Assert.Equal(0, calls);
    }

    private static TestClamBarGaugeView Gauge(params double[] values) => Gauge(Entries(values));

    private static TestClamBarGaugeView Gauge(List<ChartEntry> entries) => new() { ItemsSource = entries, ItemSpacing = 24 };

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static Point RowPoint(TestClamBarGaugeView view, int slot)
    {
        var bounds = view.ChartLayout.GetTrackBounds(slot);
        return new Point(bounds.CenterX, bounds.CenterY);
    }

    private static Point GapPoint(TestClamBarGaugeView view)
    {
        var layout = view.ChartLayout;
        return new Point(layout.Content.CenterX, layout.Content.Y + layout.Pitch - (layout.Spacing / 2));
    }

    private static void Tap(TestClamBarGaugeView view, Point point)
    {
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
    }
}
