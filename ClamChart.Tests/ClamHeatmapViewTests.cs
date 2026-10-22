namespace ClamChart.Tests;

using ClamChart.Tests.Data;

public sealed class ClamHeatmapViewTests
{
    [Fact]
    public void DefaultsFollowTheSpecification()
    {
        // Arrange
        using var view = new ClamHeatmapView();

        // Assert
        Assert.Null(view.ItemsSource);
        Assert.Equal(ChartHeatmapLayoutMode.Matrix, view.LayoutMode);
        Assert.Null(view.Minimum);
        Assert.Null(view.Maximum);
        Assert.Null(view.ColorScale);
        Assert.Equal(1, view.CellGap);
        Assert.Null(view.LegendVisible);
        Assert.Equal(ChartLegendPosition.Bottom, view.LegendPosition);
        Assert.Equal(ChartSelectionMode.None, view.SelectionMode);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Equal(ChartTrackballMode.None, view.TrackballMode);
        Assert.True(view.TooltipVisible);
        Assert.True(view.TrackItemChanges);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new ClamHeatmapView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.CellGap = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.CellGap = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Minimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ColorScale = [Colors.Red]);
        view.ColorScale = [Colors.White, Colors.Red];
        view.ColorScale = null;
        view.CellGap = 0;
        Assert.Equal(0, view.CellGap);
    }

    [Fact]
    public void SummaryNamesTheHighestAndLowestCells()
    {
        // Arrange
        using var view = new TestClamHeatmapView
        {
            Culture = CultureInfo.InvariantCulture,
            ValueAccessors = TestHeatItem.Accessors,
            RowKey = "day",
            ColumnKey = "slot",
            ValueKey = "value",
            ItemsSource = new List<TestHeatItem> { new("Mon", "9", 0, 3), new("Mon", "10", 0, 8), new("Tue", "9", 0, 1) }
        };

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        Assert.Equal("3 items, max 8 (Mon 10), min 1 (Tue 9)", SemanticProperties.GetDescription(view));
    }

    [Fact]
    public void CalendarWithoutDatesIsReportedOnce()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = new TestClamHeatmapView { LayoutMode = ChartHeatmapLayoutMode.Calendar, ItemsSource = new List<ChartEntry> { new("A", 1), new("B", 2) } };
        var message = "The calendar heatmap needs dates";
        var before = capture.Count(message);

        // Act
        using var first = view.Paint(300, 200);
        view.CellGap = 2;
        using var second = view.Paint(300, 200);

        // Assert
        Assert.Equal(before + 1, capture.Count(message));
        Assert.Equal(0, view.ChartLayout.CellCount);
    }

    [Fact]
    public void InvertedBoundsAreReportedOnce()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = new TestClamHeatmapView { Minimum = 10, Maximum = 5, ItemsSource = new List<ChartEntry> { new("A", 1), new("B", 2) } };
        var message = "The heatmap Maximum is not greater than Minimum";
        var before = capture.Count(message);

        // Act
        using var first = view.Paint(300, 200);
        view.CellGap = 2;
        using var second = view.Paint(300, 200);

        // Assert
        Assert.Equal(before + 1, capture.Count(message));
        Assert.Equal((1d, 2d), (view.ChartLayout.Minimum, view.ChartLayout.Maximum));
    }

    [Fact]
    public void ItemChangesUpdateTheCells()
    {
        // Arrange
        var items = new ObservableCollection<ChartEntry>([new("A", 1), new("B", 2)]);
        using var view = new TestClamHeatmapView { ItemsSource = items };
        view.OnAttached();
        using var first = view.Paint(300, 200);

        // Act
        items.Add(new ChartEntry("C", 3));
        using var second = view.Paint(300, 200);

        // Assert
        Assert.Equal(["A", "B", "C"], view.ChartLayout.ColumnNames);
        Assert.Equal(3, view.RenderedPointCount);
    }
}
