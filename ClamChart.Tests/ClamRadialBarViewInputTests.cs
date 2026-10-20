namespace ClamChart.Tests;

using SkiaSharp.Views.Maui;

public sealed class ClamRadialBarViewInputTests
{
    [Fact]
    public void AutoTouchModeFollowsTheSettings()
    {
        // Arrange
        using var view = Chart(10, 30);
        using var bitmap = view.Paint(300, 300);

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.SelectionMode = ChartSelectionMode.Item;
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.SelectionMode = ChartSelectionMode.None;
        view.LegendItemTapped += static (_, _) => { };
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        view.ChartTapped += static (_, _) => { };
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void MinimalDensityOnlyReceivesChartTaps()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(150, 90);

        // Act & Assert
        Assert.Equal(ChartTouchMode.PassThrough, view.EffectiveTouchMode);
        view.ChartTappedCommand = new Command(static () => { });
        Assert.Equal(ChartTouchMode.Interactive, view.EffectiveTouchMode);
    }

    [Fact]
    public void TapOnARingRaisesItemTappedSelectsAndThenRunsTheCommand()
    {
        // Arrange
        var entries = Entries(10, 30);
        using var view = Chart(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        var calls = new List<string>();
        ChartItemEventArgs? tapped = null;
        view.ItemTapped += (_, e) =>
        {
            tapped = e;
            calls.Add("event");
        };
        view.SelectionChanged += (_, _) => calls.Add("selection");
        view.ItemTappedCommand = new Command(() => calls.Add("command"));
        using var bitmap = view.Paint(300, 300);

        // Act
        Tap(view, RingPoint(view, 1));

        // Assert
        Assert.Equal(["event", "selection", "command"], calls);
        Assert.NotNull(tapped);
        Assert.Equal(1, tapped.Index);
        Assert.Same(entries[1], tapped.Item);
        Assert.Equal("C1", tapped.X);
        Assert.Equal(30, tapped.Y);
        Assert.Equal(1, view.SelectedIndex);
    }

    [Fact]
    public void TapOnTheSelectedRingClearsTheSelection()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(300, 300);

        // Act
        Tap(view, RingPoint(view, 0));
        var selected = view.SelectedIndex;
        Tap(view, RingPoint(view, 0));

        // Assert
        Assert.Equal(0, selected);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void TapInTheCenterRaisesChartTappedAndClearsTheSelection()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 0;
        ChartTappedEventArgs? tapped = null;
        view.ChartTapped += (_, e) => tapped = e;
        using var bitmap = view.Paint(300, 300);

        // Act
        Tap(view, new Point(view.ChartLayout.CenterX, view.ChartLayout.CenterY));

        // Assert
        Assert.NotNull(tapped);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void LegendTapRaisesTheEventAndSelectsTheRing()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        view.LegendVisible = true;
        ChartLegendEventArgs? tapped = null;
        view.LegendItemTapped += (_, e) => tapped = e;
        using var bitmap = view.Paint(400, 240);

        // Act
        var bounds = view.ChartLayout.Legend.Items[1].Bounds;
        Tap(view, new Point(bounds.CenterX, bounds.CenterY));

        // Assert
        Assert.NotNull(tapped);
        Assert.Equal(1, tapped.Index);
        Assert.Equal(1, view.SelectedIndex);
    }

    [Fact]
    public void ReleaseOnAnotherRingIsNotATap()
    {
        // Arrange
        using var view = Chart(10, 30);
        var calls = 0;
        view.ItemTapped += (_, _) => calls++;
        using var bitmap = view.Paint(300, 300);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, RingPoint(view, 0), 0);
        view.HandleTouch(1, SKTouchAction.Released, RingPoint(view, 1), 10);

        // Assert
        Assert.Equal(0, calls);
    }

    private static TestClamRadialBarView Chart(params double[] values) => Chart(Entries(values));

    private static TestClamRadialBarView Chart(List<ChartEntry> entries) => new() { ItemsSource = entries };

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static Point RingPoint(TestClamRadialBarView view, int slot)
    {
        var layout = view.ChartLayout;
        return new Point(layout.CenterX + layout.GetRingRadius(slot), layout.CenterY);
    }

    private static void Tap(TestClamRadialBarView view, Point point)
    {
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
    }
}
