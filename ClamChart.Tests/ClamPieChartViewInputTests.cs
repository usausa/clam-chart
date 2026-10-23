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
    public void LegendToggleCapturesLegendTouches()
    {
        // Arrange
        using var view = Chart(10, 30);
        using var bitmap = view.Paint(340, 340);

        // Act
        view.LegendToggleEnabled = true;

        // Assert
        Assert.Equal(ChartTouchMode.HitOnly, view.EffectiveTouchMode);
        Assert.True(view.IsInputTarget(LegendItem(view, 0)));
        Assert.False(view.IsInputTarget(SlicePoint(view, 0)));
    }

    [Fact]
    public void LegendToggleHidesTheSliceAndShowsItAgain()
    {
        // Arrange
        using var view = Chart(10, 30, 60);
        view.LegendToggleEnabled = true;
        view.SelectionMode = ChartSelectionMode.Item;
        view.LegendValueMode = ChartLegendValueMode.Percent;
        using var first = view.Paint(340, 340);

        // Act
        Tap(view, LegendItem(view, 1));
        using var hidden = view.Paint(340, 340);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal([0, 2], layout.Slices.Select(static x => x.DataIndex));
        Assert.Equal(70, layout.Total);
        Assert.Equal(1d / 7, layout.Slices[0].End, 9);
        var slice = Assert.Single(layout.HiddenSlices);
        Assert.Equal(1, slice.DataIndex);
        Assert.Equal(PieLayout.HiddenPercentText, slice.PercentText);
        Assert.Equal([0, 2, 1], layout.Legend.Items.Select(static x => x.SeriesIndex));
        Assert.Equal(-1, view.SelectedIndex);

        // Act
        Tap(view, LegendItem(view, 1));
        using var shown = view.Paint(340, 340);

        // Assert
        Assert.Equal([0, 1, 2], view.ChartLayout.Slices.Select(static x => x.DataIndex));
        Assert.Empty(view.ChartLayout.HiddenSlices);
    }

    [Fact]
    public void TheLastVisibleSliceCannotBeHidden()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.LegendToggleEnabled = true;
        using var first = view.Paint(340, 340);
        Tap(view, LegendItem(view, 0));
        using var second = view.Paint(340, 340);

        // Act
        Tap(view, LegendItem(view, 1));
        using var third = view.Paint(340, 340);

        // Assert
        Assert.Equal([1], view.ChartLayout.Slices.Select(static x => x.DataIndex));
        Assert.Equal([0], view.ChartLayout.HiddenSlices.Select(static x => x.DataIndex));
    }

    [Fact]
    public void HidingTheSelectedSliceClearsTheSelection()
    {
        // Arrange
        var entries = Entries(10, 30);
        using var view = Chart(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        view.LegendToggleEnabled = true;
        view.SelectedIndex = 1;
        ChartSelectionChangedEventArgs? changed = null;
        view.SelectionChanged += (_, e) => changed = e;
        using var first = view.Paint(340, 340);

        // Act
        Tap(view, LegendItem(view, 1));

        // Assert
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Null(view.SelectedItem);
        Assert.NotNull(changed);
        Assert.Same(entries[1], changed.OldItem);
        Assert.Equal(-1, changed.NewIndex);
    }

    [Fact]
    public void HiddenSlicesKeepTheirColorsAndTheirPlaceInTheLegend()
    {
        // Arrange
        using var view = Chart(10, 30, 60);
        view.SortMode = ChartSortMode.Descending;
        view.LegendToggleEnabled = true;
        using var first = view.Paint(340, 340);
        var ranks = view.ChartLayout.Slices.Select(static x => (x.DataIndex, x.ColorRank)).ToList();

        // Act
        Tap(view, LegendItem(view, 1));
        using var second = view.Paint(340, 340);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal([(2, 2), (1, 1), (0, 0)], ranks);
        Assert.Equal([(2, 2), (0, 0)], layout.Slices.Select(static x => (x.DataIndex, x.ColorRank)));
        Assert.Equal(1, layout.HiddenSlices[0].ColorRank);
        Assert.Equal([0, 2, 1], layout.Legend.Items.Select(static x => x.SeriesIndex));
    }

    [Fact]
    public void ItemsLeavingTheMergedSliceDoNotShiftTheColors()
    {
        // Arrange
        using var view = Chart(40, 5, 30, 20);
        view.MaxSlices = 3;
        view.LegendToggleEnabled = true;
        using var first = view.Paint(340, 340);
        var before = view.ChartLayout.Slices.Where(static x => !x.IsOthers).Select(static x => (x.DataIndex, x.ColorRank)).ToList();

        // Act
        Tap(view, LegendItem(view, 0));
        using var second = view.Paint(340, 340);

        // Assert
        Assert.Equal([(0, 0), (2, 1)], before);
        Assert.Equal([(1, 2), (2, 1), (3, 3)], view.ChartLayout.Slices.Select(static x => (x.DataIndex, x.ColorRank)));
        Assert.Equal(0, view.ChartLayout.HiddenSlices[0].ColorRank);
    }

    [Fact]
    public void OthersIsNotToggled()
    {
        // Arrange
        using var view = Chart(10, 30, 60);
        view.MaxSlices = 2;
        view.LegendToggleEnabled = true;
        using var first = view.Paint(340, 340);

        // Act
        Tap(view, LegendItem(view, 1));
        using var second = view.Paint(340, 340);

        // Assert
        Assert.Equal(2, view.ChartLayout.Slices.Count);
        Assert.True(view.ChartLayout.Slices[1].IsOthers);
        Assert.Empty(view.ChartLayout.HiddenSlices);
    }

    [Fact]
    public void TurningTheToggleOffShowsEverySlice()
    {
        // Arrange
        using var view = Chart(10, 30);
        view.LegendToggleEnabled = true;
        using var first = view.Paint(340, 340);
        Tap(view, LegendItem(view, 0));
        using var second = view.Paint(340, 340);

        // Act
        view.LegendToggleEnabled = false;
        using var third = view.Paint(340, 340);

        // Assert
        Assert.Equal([0, 1], view.ChartLayout.Slices.Select(static x => x.DataIndex));
        Assert.Empty(view.ChartLayout.HiddenSlices);
    }

    [Fact]
    public void HiddenSlicesFollowTheirNamesThroughDataChanges()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(10, 30, 60));
        using var view = new TestClamPieChartView { ItemsSource = entries, LegendVisible = true, LegendToggleEnabled = true };
        view.OnAttached();
        using var first = view.Paint(340, 340);
        Tap(view, LegendItem(view, 1));
        using var second = view.Paint(340, 340);

        // Act
        entries.Insert(0, new ChartEntry("C9", 5));
        using var third = view.Paint(340, 340);

        // Assert
        Assert.Equal([0, 1, 3], view.ChartLayout.Slices.Select(static x => x.DataIndex));
        Assert.Equal([2], view.ChartLayout.HiddenSlices.Select(static x => x.DataIndex));
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
