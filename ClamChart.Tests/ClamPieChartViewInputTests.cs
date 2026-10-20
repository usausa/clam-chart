namespace ClamChart.Tests;

using SkiaSharp.Views.Maui;

public sealed class ClamPieChartViewInputTests
{
    [Fact]
    public void AutoTouchModeFollowsTheSettings()
    {
        // Arrange
        using var view = Chart(10, 30);
        using var bitmap = view.Paint(340, 340);

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
    public void HitOnlyCapturesOnlyTouchesThatStartOnASlice()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(340, 340);
        var layout = view.ChartLayout;

        // Act
        var onSlice = view.HandleTouch(1, SKTouchAction.Pressed, SlicePoint(view, 1), 0);
        view.HandleTouch(1, SKTouchAction.Cancelled, default, 0);
        var center = view.HandleTouch(1, SKTouchAction.Pressed, new Point(layout.CenterX, layout.CenterY), 0);

        // Assert
        Assert.True(onSlice);
        Assert.False(center);
    }

    [Fact]
    public void TapOnASliceRaisesItemTappedSelectsAndThenRunsTheCommand()
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
        using var bitmap = view.Paint(340, 340);

        // Act
        Tap(view, SlicePoint(view, 1));

        // Assert
        Assert.Equal(["event", "selection", "command"], calls);
        Assert.NotNull(tapped);
        Assert.Null(tapped.Series);
        Assert.Equal(-1, tapped.SeriesIndex);
        Assert.Equal(1, tapped.Index);
        Assert.Same(entries[1], tapped.Item);
        Assert.Equal("C1", tapped.X);
        Assert.Equal(30, tapped.Y);
        Assert.Same(entries[1], view.SelectedItem);
    }

    [Fact]
    public void TappingTheSelectedSliceAgainClearsTheSelection()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(340, 340);
        Tap(view, SlicePoint(view, 0));

        // Act
        Tap(view, SlicePoint(view, 0, 6));

        // Assert
        Assert.Null(view.SelectedItem);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void HandledItemTapKeepsTheSelection()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        view.ItemTapped += static (_, e) => e.Handled = true;
        using var bitmap = view.Paint(340, 340);

        // Act
        Tap(view, SlicePoint(view, 1));

        // Assert
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void TapOnOthersRaisesItemTappedWithoutSelecting()
    {
        // Arrange
        using var view = Chart(1, 2, 30, 40);
        view.SelectionMode = ChartSelectionMode.Item;
        view.OthersThreshold = 0.1;
        ChartItemEventArgs? tapped = null;
        view.ItemTapped += (_, e) => tapped = e;
        using var bitmap = view.Paint(340, 340);

        // Act
        Tap(view, SlicePoint(view, 2));

        // Assert
        Assert.True(view.ChartLayout.Slices[2].IsOthers);
        Assert.NotNull(tapped);
        Assert.Null(tapped.Item);
        Assert.Equal(-1, tapped.Index);
        Assert.Equal("Other", tapped.X);
        Assert.Equal(3, tapped.Y);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void TapInTheCenterRaisesChartTappedAndClearsTheSelection()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        var tapped = 0;
        view.ChartTapped += (_, _) => tapped++;
        using var bitmap = view.Paint(340, 340);
        view.SelectedIndex = 1;

        // Act
        Tap(view, new Point(view.ChartLayout.CenterX, view.ChartLayout.CenterY));

        // Assert
        Assert.Equal(1, tapped);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void HandledChartTapKeepsTheSelection()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        view.ChartTapped += static (_, e) => e.Handled = true;
        using var bitmap = view.Paint(340, 340);
        view.SelectedIndex = 1;

        // Act
        Tap(view, new Point(view.ChartLayout.CenterX, view.ChartLayout.CenterY));

        // Assert
        Assert.Equal(1, view.SelectedIndex);
    }

    [Fact]
    public void LegendTapRaisesTheEventAndSelectsTheSlice()
    {
        // Arrange
        var entries = Entries(10, 30);
        using var view = Chart(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        ChartLegendEventArgs? tapped = null;
        var commands = 0;
        view.LegendItemTapped += (_, e) => tapped = e;
        view.LegendItemTappedCommand = new Command(() => commands++);
        using var bitmap = view.Paint(340, 340);

        // Act
        Tap(view, LegendItem(view, 1));

        // Assert
        Assert.NotNull(tapped);
        Assert.Null(tapped.Series);
        Assert.Same(entries[1], tapped.Item);
        Assert.Equal(1, tapped.Index);
        Assert.Equal(1, commands);
        Assert.Equal(1, view.SelectedIndex);
    }

    [Fact]
    public void HandledLegendTapDoesNotSelect()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        view.LegendItemTapped += static (_, e) => e.Handled = true;
        using var bitmap = view.Paint(340, 340);

        // Act
        Tap(view, LegendItem(view, 0));

        // Assert
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void ReleaseOnAnotherSliceIsNotATap()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(340, 340);
        var layout = view.ChartLayout;
        var boundary = layout.GetAngle(layout.Slices[0].End);
        var radius = (layout.OuterRadius + layout.InnerRadius) / 2;
        var delta = 2 / radius * 180 / Math.PI;

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, Polar(layout, boundary - delta, radius), 0);
        view.HandleTouch(1, SKTouchAction.Released, Polar(layout, boundary + delta, radius), 10);

        // Assert
        Assert.Equal(-1, view.SelectedIndex);
    }

    private static TestClamPieChartView Chart(params double[] values) => Chart(Entries(values));

    private static TestClamPieChartView Chart(List<ChartEntry> entries) => new() { ItemsSource = entries, LegendVisible = true };

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static Point SlicePoint(TestClamPieChartView view, int sliceIndex, double offset = 0)
    {
        var layout = view.ChartLayout;
        var slice = layout.Slices[sliceIndex];
        return Polar(layout, layout.GetAngle((slice.Start + slice.End) / 2), ((layout.OuterRadius + layout.InnerRadius) / 2) + offset);
    }

    private static Point Polar(PieLayout layout, double angle, double radius)
    {
        var (x, y) = PieLayout.GetDirection(angle);
        return new Point(layout.CenterX + (x * radius), layout.CenterY + (y * radius));
    }

    private static Point LegendItem(TestClamPieChartView view, int index)
    {
        var bounds = view.ChartLayout.Legend.Items[index].Bounds;
        return new Point(bounds.CenterX, bounds.CenterY);
    }

    private static void Tap(TestClamPieChartView view, Point point)
    {
        view.HandleTouch(1, SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SKTouchAction.Released, point, 10);
    }
}
