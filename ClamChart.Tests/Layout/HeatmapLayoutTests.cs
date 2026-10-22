namespace ClamChart.Tests.Layout;

using ClamChart.Tests.Data;

public sealed class HeatmapLayoutTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    private static readonly ChartValueOptions MatrixValues = new() { Accessors = TestHeatItem.Accessors, XKey = "slot", YKey = "value", LabelKey = "day" };

    private static readonly HeatmapLayoutOptions Matrix = new() { HasRows = true };

    private static readonly HeatmapLayoutOptions Calendar = new() { LayoutMode = ChartHeatmapLayoutMode.Calendar, FirstDayOfWeek = DayOfWeek.Sunday };

    [Fact]
    public void RowsAndColumnsFollowTheFirstAppearance()
    {
        // Arrange
        TestHeatItem[] items = [new("Mon", "9", 0, 3), new("Tue", "9", 0, 5), new("Mon", "10", 0, 7), new("Mon", "9", 0, 99)];

        // Act
        var layout = Layout(items, MatrixValues, Matrix);

        // Assert
        Assert.Equal(["Mon", "Tue"], layout.RowNames);
        Assert.Equal(["9", "10"], layout.ColumnNames);
        Assert.Equal([3, 7, 5, Double.NaN], Enumerable.Range(0, 4).Select(layout.GetValue));
        Assert.Equal([0, 2, 1, -1], Enumerable.Range(0, 4).Select(layout.GetDataIndex));
        Assert.All(Enumerable.Range(0, 4), x => Assert.True(layout.IsPresent(x)));
        Assert.Equal("Tue 9", layout.GetCellText(2));
        Assert.Equal("10", layout.GetCellX(1));
        Assert.Equal(1, layout.FindCellOfIndex(2));
    }

    [Fact]
    public void NumberColumnsAreSortedAndNamed()
    {
        // Arrange
        TestHeatItem[] items = [new("Mon", null, 3, 1), new("Mon", null, 1, 2), new("Mon", null, 2, 3), new("Tue", null, 1, 4)];

        // Act
        var layout = Layout(items, MatrixValues with { XKey = "hour" }, Matrix);

        // Assert
        Assert.Equal(["1", "2", "3"], layout.ColumnNames);
        Assert.Equal([2, 3, 1, 4, Double.NaN, Double.NaN], Enumerable.Range(0, 6).Select(layout.GetValue));
        Assert.Equal(2d, layout.GetCellX(1));
    }

    [Fact]
    public void ValuesWithoutRowsMakeOneRow()
    {
        // Arrange
        ChartEntry[] items = [new("A", 1), new("B", 2), new("C", 3)];

        // Act
        var layout = Layout(items, new ChartValueOptions());

        // Assert
        Assert.Equal(1, layout.RowCount);
        Assert.Equal(["A", "B", "C"], layout.ColumnNames);
        Assert.Equal("B", layout.GetCellText(1));
        Assert.Empty(layout.RowLabels);
        Assert.Equal(3, layout.ColumnLabels.Count);
    }

    [Fact]
    public void CalendarPlacesTheDaysInWeekColumns()
    {
        // Arrange
        var items = Days(new DateTime(2026, 10, 1), 22, static day => day == 9 ? null : day + 1);

        // Act
        var sunday = Layout(items, new ChartValueOptions(), Calendar, 400, 200);
        var monday = Layout(items, new ChartValueOptions(), Calendar with { FirstDayOfWeek = DayOfWeek.Monday }, 400, 200);

        // Assert
        Assert.Equal(new DateTime(2026, 9, 27), sunday.CalendarStart);
        Assert.Equal(7, sunday.RowCount);
        Assert.Equal(4, sunday.ColumnCount);
        Assert.Equal(["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"], sunday.RowNames);
        var first = Cell(sunday, 0, 4);
        Assert.Equal(new DateTime(2026, 10, 1), sunday.GetDate(first));
        Assert.Equal(1, sunday.GetValue(first));
        Assert.False(sunday.IsPresent(Cell(sunday, 0, 3)));
        Assert.True(sunday.IsPresent(Cell(sunday, 3, 4)));
        Assert.False(sunday.IsPresent(Cell(sunday, 3, 5)));
        Assert.True(Double.IsNaN(sunday.GetValue(Cell(sunday, 1, 6))));
        Assert.Equal("10/1/2026 (Thu)", sunday.GetCellText(first));
        Assert.Equal(new DateTime(2026, 10, 1), sunday.GetCellX(first));
        Assert.Equal(new DateTime(2026, 9, 28), monday.CalendarStart);
        Assert.Equal(new DateTime(2026, 10, 1), monday.GetDate(Cell(monday, 0, 3)));
    }

    [Fact]
    public void CalendarLabelsTheMonthsAndEveryOtherWeekday()
    {
        // Arrange
        var items = Days(new DateTime(2026, 9, 1), 91, static day => day % 5);

        // Act
        var layout = Layout(items, new ChartValueOptions(), Calendar, 400, 200);

        // Assert
        var grid = layout.GridBounds;
        Assert.Equal(14, layout.ColumnCount);
        Assert.Equal(["Sep", "Oct", "Nov"], layout.ColumnLabels.Select(static x => x.Text));
        Assert.Equal([grid.X, grid.X + (5 * layout.CellWidth), grid.X + (9 * layout.CellWidth)], layout.ColumnLabels.Select(static x => x.Bounds.X));
        Assert.Equal(["Mon", "Wed", "Fri"], layout.RowLabels.Select(static x => x.Text));
        Assert.Equal(layout.CellWidth, layout.CellHeight);
        Assert.Equal(grid.X - HeatmapLayout.LabelGap, layout.RowLabels[0].Bounds.Right, Precision);
    }

    [Fact]
    public void CalendarCellsAreSquaresCenteredInTheArea()
    {
        // Arrange
        var items = Days(new DateTime(2026, 10, 4), 14, static day => day);

        // Act
        var layout = Layout(items, new ChartValueOptions(), Calendar with { LabelsVisible = false }, 300, 140);

        // Assert
        Assert.Equal(20, layout.CellWidth, Precision);
        Assert.Equal(new ChartRect(130, 0, 40, 140), layout.GridBounds);
    }

    [Fact]
    public void CalendarScaleStaysNextToTheCenteredGrid()
    {
        // Arrange
        var items = Days(new DateTime(2026, 10, 4), 14, static day => day);

        // Act
        var bottom = Layout(items, new ChartValueOptions(), Calendar with { LabelsVisible = false, LegendVisible = true }, 300, 300);
        var right = Layout(items, new ChartValueOptions(), Calendar with { LabelsVisible = false, LegendVisible = true, LegendPosition = ChartLegendPosition.Right }, 300, 140);

        // Assert
        var grid = bottom.GridBounds;
        var scale = bottom.ScaleBounds;
        var below = 300 - (scale.Bottom + HeatmapLayout.ScaleLabelGap + bottom.LabelHeight);
        Assert.Equal(grid.Bottom + HeatmapLayout.LegendGap, scale.Y, Precision);
        Assert.Equal(grid.Y, below, Precision);
        Assert.Equal(grid.X, scale.X, Precision);
        Assert.Equal(right.GridBounds.Right + HeatmapLayout.LegendGap, right.ScaleBounds.X, Precision);
        Assert.Equal(right.GridBounds.Y, right.ScaleBounds.Y, Precision);
    }

    [Fact]
    public void CalendarKeepsTheLatestWeeks()
    {
        // Arrange
        ChartTimePoint[] items = [new(new DateTime(1990, 1, 1), 5), new(new DateTime(2026, 10, 22), 9)];

        // Act
        var layout = Layout(items, new ChartValueOptions(), Calendar, 400, 200);

        // Assert
        Assert.Equal(HeatmapLayout.MaxWeeks, layout.ColumnCount);
        Assert.Equal(9, layout.GetValue(Cell(layout, layout.ColumnCount - 1, 4)));
        Assert.Equal(Cell(layout, layout.ColumnCount - 1, 4), layout.FindCellOfIndex(1));
        Assert.Equal(-1, layout.FindCellOfIndex(0));
    }

    [Fact]
    public void CalendarNeedsDates()
    {
        // Arrange
        ChartEntry[] items = [new("A", 1), new("B", 2)];

        // Act
        var layout = Layout(items, new ChartValueOptions(), Calendar);

        // Assert
        Assert.False(layout.HasDates);
        Assert.Equal(0, layout.CellCount);
    }

    [Fact]
    public void RangeComesFromTheValuesOrTheFixedBounds()
    {
        // Arrange
        ChartEntry[] items = [new("A", 3), new("B", 12), new("C", 7)];

        // Act
        var automatic = Layout(items, new ChartValueOptions());
        var fixedBounds = Layout(items, new ChartValueOptions(), new HeatmapLayoutOptions { Minimum = 0, Maximum = 10 });
        var inverted = Layout(items, new ChartValueOptions(), new HeatmapLayoutOptions { Minimum = 10, Maximum = 0 });

        // Assert
        Assert.Equal((3d, 12d), (automatic.Minimum, automatic.Maximum));
        Assert.Equal((0d, 10d), (fixedBounds.Minimum, fixedBounds.Maximum));
        Assert.Equal((3d, 12d), (inverted.Minimum, inverted.Maximum));
    }

    [Fact]
    public void MatrixFillsTheAreaBesideTheLabels()
    {
        // Arrange
        TestHeatItem[] items = [new("Mon", "9", 0, 1), new("Tue", "10", 0, 2)];

        // Act
        var layout = Layout(items, MatrixValues, Matrix with { CellGap = 2 }, 300, 200);

        // Assert
        var labelHeight = layout.LabelHeight;
        var grid = layout.GridBounds;
        Assert.Equal(16.5 + HeatmapLayout.LabelGap, grid.X, Precision);
        Assert.Equal(200 - labelHeight - HeatmapLayout.LabelGap, grid.Bottom, Precision);
        Assert.Equal(grid.Width / 2, layout.CellWidth, Precision);
        Assert.Equal(grid.Height / 2, layout.CellHeight, Precision);
        Assert.Equal(grid.Y + ((layout.CellHeight - 2) / 2), layout.RowLabels[0].Bounds.CenterY, Precision);
        Assert.Equal(grid.X + ((layout.CellWidth - 2) / 2), layout.ColumnLabels[0].Bounds.CenterX, Precision);
    }

    [Fact]
    public void CrowdedColumnLabelsAreThinned()
    {
        // Arrange
        var items = Enumerable.Range(0, 24).Select(static hour => new TestHeatItem("Mon", null, hour, hour)).ToList();

        // Act
        var layout = Layout(items, MatrixValues with { XKey = "hour" }, Matrix, 160, 120);

        // Assert
        var labels = layout.ColumnLabels;
        Assert.InRange(labels.Count, 2, 23);
        for (var i = 1; i < labels.Count; i++)
        {
            Assert.True(labels[i].Bounds.X >= labels[i - 1].Bounds.Right + HeatmapLayout.LabelSpacing);
        }
    }

    [Fact]
    public void ScaleRunsUnderTheGridWithTheEndValues()
    {
        // Arrange
        var items = Enumerable.Range(0, 11).Select(static i => new ChartEntry($"C{i}", i * 10)).ToList();

        // Act
        var layout = Layout(items, new ChartValueOptions(), new HeatmapLayoutOptions { LegendVisible = true }, 300, 200);

        // Assert
        var scale = layout.ScaleBounds;
        var labels = layout.ScaleLabels;
        Assert.True(layout.ShowsScale);
        Assert.Equal(layout.GridBounds.X, scale.X, Precision);
        Assert.Equal(layout.GridBounds.Width - layout.Gap, scale.Width, Precision);
        Assert.Equal(HeatmapLayout.ScaleThickness, scale.Height);
        Assert.Equal(200 - HeatmapLayout.ScaleThickness - HeatmapLayout.ScaleLabelGap - layout.LabelHeight, scale.Y, Precision);
        Assert.Equal(scale.Y - HeatmapLayout.LegendGap, layout.PlotBounds.Bottom, Precision);
        Assert.Equal("0", labels[0].Text);
        Assert.Equal("100", labels[^1].Text);
        Assert.Equal(scale.X, labels[0].Bounds.X, Precision);
        Assert.Equal(scale.Right, labels[^1].Bounds.Right, Precision);
        Assert.InRange(labels.Count, 3, 6);
        Assert.Equal(labels.Count, labels.Select(static x => x.Text).Distinct().Count());
    }

    [Fact]
    public void VerticalScaleSitsBesideTheGrid()
    {
        // Arrange
        var items = Enumerable.Range(0, 4).Select(static i => new ChartEntry($"C{i}", i)).ToList();

        // Act
        var layout = Layout(items, new ChartValueOptions(), new HeatmapLayoutOptions { LegendVisible = true, LegendPosition = ChartLegendPosition.Right }, 300, 200);

        // Assert
        var scale = layout.ScaleBounds;
        var labels = layout.ScaleLabels;
        Assert.True(layout.IsScaleVertical);
        Assert.Equal(HeatmapLayout.ScaleThickness, scale.Width);
        Assert.Equal(layout.GridBounds.Y, scale.Y, Precision);
        Assert.Equal(300 - HeatmapLayout.ScaleThickness - HeatmapLayout.LabelGap - 5.5, scale.X, Precision);
        Assert.Equal(scale.Bottom, labels[0].Bounds.Bottom, Precision);
        Assert.Equal(scale.Y, labels[^1].Bounds.Y, Precision);
    }

    [Fact]
    public void CellsAreFoundUnderThePoint()
    {
        // Arrange
        TestHeatItem[] items = [new("Mon", "9", 0, 1), new("Tue", "10", 0, 2)];
        var layout = Layout(items, MatrixValues, Matrix with { LabelsVisible = false }, 200, 100);

        // Act & Assert
        Assert.Equal(0, layout.FindCell(10, 10));
        Assert.Equal(3, layout.FindCell(150, 80));
        Assert.Equal(-1, layout.FindCell(250, 80));
        Assert.Equal(3, layout.FindNearestCell(250, 180));
        Assert.Equal(0, layout.FindNearestCell(-20, -20));
    }

    private static int Cell(HeatmapLayout layout, int column, int row) => (row * layout.ColumnCount) + column;

    private static List<ChartTimePoint> Days(DateTime start, int count, Func<int, double?> value) =>
        [.. Enumerable.Range(0, count).Where(day => value(day) is not null).Select(day => new ChartTimePoint(start.AddDays(day), value(day)!.Value))];

    private static HeatmapLayout Layout(IEnumerable items, ChartValueOptions values, HeatmapLayoutOptions? options = null, double width = 300, double height = 200)
    {
        using var data = DataSnapshotBuilder.Build(items, new ChartValueReader(values), ChartEmptyPointMode.Gap);
        var layout = new HeatmapLayout();
        layout.Update(width, height, new Thickness(0), new ChartStyle(), Culture, options ?? new HeatmapLayoutOptions(), data, Measurer);
        return layout;
    }
}
