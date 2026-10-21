namespace ClamChart.Tests.Layout;

public sealed class LegendLayoutTests
{
    private const double FontSize = 12;

    private const double MarkerSize = 8;

    private static readonly TestTextMeasurer Measurer = new();

    [Fact]
    public void RowsWrapAndTheRestIsShownAsACount()
    {
        // Arrange
        var legend = new LegendLayout();
        var entries = Entries(LegendMarkerKind.Dot, "AAAA", "AAAA", "AAAA", "AAAA", "AAAA");

        // Act
        var height = legend.MeasureRows(entries, 100, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(32, height);
        Assert.Equal(3, legend.Items.Count);
        Assert.Equal(new ChartRect(0, 0, 36, 14), legend.Items[0].Bounds);
        Assert.Equal(new ChartRect(48, 0, 36, 14), legend.Items[1].Bounds);
        Assert.Equal(new ChartRect(0, 18, 36, 14), legend.Items[2].Bounds);
        Assert.Equal(2, legend.OverflowCount);
        Assert.Equal("+2", legend.OverflowText);
        Assert.Equal(new ChartRect(48, 18, 12, 14), legend.OverflowBounds);
        Assert.True(legend.IsVisible);
    }

    [Fact]
    public void MarkerAndTextAreCenteredInTheRow()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        legend.MeasureRows(Entries(LegendMarkerKind.Line, "AB"), 100, Measurer, FontSize, MarkerSize);

        // Assert
        var item = legend.Items[0];
        Assert.Equal(new ChartRect(0, 3, 12, 8), item.Marker);
        Assert.Equal(new ChartRect(16, 0, 12, 14), item.Text);
        Assert.Equal(new ChartRect(0, 0, 28, 14), item.Bounds);
    }

    [Fact]
    public void LongTextIsTruncatedToTheRow()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        legend.MeasureRows(Entries(LegendMarkerKind.Dot, "ABCDEFGHIJ"), 50, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(38, legend.Items[0].Text.Width);
        Assert.Equal(38, legend.Items[0].TextMaxWidth);
        Assert.Equal(50, legend.Items[0].Bounds.Width);
    }

    [Fact]
    public void ColumnFitsTheHeightAndShowsTheRestAsACount()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var width = legend.MeasureColumn(Entries(LegendMarkerKind.Dot, "AAAA", "AAAA", "AAAA", "AAAA"), 50, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(36, width);
        Assert.Equal(2, legend.Items.Count);
        Assert.Equal(new ChartRect(0, 18, 36, 14), legend.Items[1].Bounds);
        Assert.Equal(2, legend.OverflowCount);
        Assert.Equal(new ChartRect(0, 36, 12, 14), legend.OverflowBounds);
        Assert.Equal(50, legend.Bounds.Height);
    }

    [Fact]
    public void ColumnWidthIsLimited()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var width = legend.MeasureColumn(Entries(LegendMarkerKind.Dot, new string('A', 40)), 100, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(LegendLayout.MaxSideWidth, width);
        Assert.Equal(LegendLayout.MaxSideWidth - 12, legend.Items[0].TextMaxWidth);
    }

    [Fact]
    public void OffsetMovesEveryRectangle()
    {
        // Arrange
        var legend = new LegendLayout();
        legend.MeasureRows(Entries(LegendMarkerKind.Dot, "AAAA", "AAAA", "AAAA", "AAAA", "AAAA"), 100, Measurer, FontSize, MarkerSize);

        // Act
        legend.Offset(10, 20);

        // Assert
        Assert.Equal(new ChartRect(10, 20, 36, 14), legend.Items[0].Bounds);
        Assert.Equal(new ChartRect(10, 23, 8, 8), legend.Items[0].Marker);
        Assert.Equal(new ChartRect(58, 38, 12, 14), legend.OverflowBounds);
        Assert.Equal(new ChartRect(10, 20, 100, 32), legend.Bounds);
    }

    [Fact]
    public void NoEntriesIsHidden()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var height = legend.MeasureRows([], 100, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(0, height);
        Assert.False(legend.IsVisible);
        Assert.Empty(legend.Items);
    }

    [Fact]
    public void ValueFollowsTheNameInARow()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        legend.MeasureRows([new LegendEntry(0, "AB", LegendMarkerKind.Dot, "26%"), new LegendEntry(1, "CD", LegendMarkerKind.Dot)], 200, Measurer, FontSize, MarkerSize);

        // Assert
        var first = legend.Items[0];
        Assert.Equal(new ChartRect(0, 0, 48, 14), first.Bounds);
        Assert.Equal(new ChartRect(30, 0, 18, 14), first.Value);
        Assert.Equal(new ChartRect(60, 0, 24, 14), legend.Items[1].Bounds);
        Assert.True(legend.Items[1].Value.IsEmpty);
    }

    [Fact]
    public void ValuesAreRightAlignedInAColumn()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var width = legend.MeasureColumn([new LegendEntry(0, "AB", LegendMarkerKind.Dot, "5%"), new LegendEntry(1, "ABCD", LegendMarkerKind.Dot, "26%")], 100, Measurer, FontSize, MarkerSize);
        legend.Offset(10, 0);

        // Assert
        Assert.Equal(60, width);
        Assert.Equal(new ChartRect(10, 0, 60, 14), legend.Items[0].Bounds);
        Assert.Equal(new ChartRect(58, 0, 12, 14), legend.Items[0].Value);
        Assert.Equal(new ChartRect(52, 18, 18, 14), legend.Items[1].Value);
        Assert.Equal(24, legend.Items[1].TextMaxWidth);
    }

    [Fact]
    public void TableHasAHeaderAndRightAlignedValues()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var height = legend.MeasureTable(Entries(LegendMarkerKind.Line, "cpu0", "cpu1"), ["Name", "Last *", "Max"], ["12", "40", "7", "35"], 200, 100, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(57, height);
        Assert.True(legend.IsTable);
        Assert.Equal(new ChartRect(0, 0, 200, 19), legend.HeaderBounds);
        Assert.Equal(new ChartRect(0, 0, 200, 57), legend.Bounds);
        Assert.Equal(
            [
                new LegendCell(-1, "Name", new ChartRect(36, 2, 24, 14), 70),
                new LegendCell(-1, "Last *", new ChartRect(122, 2, 36, 14), Double.PositiveInfinity),
                new LegendCell(-1, "Max", new ChartRect(174, 2, 18, 14), Double.PositiveInfinity),
                new LegendCell(0, "12", new ChartRect(146, 21, 12, 14), Double.PositiveInfinity),
                new LegendCell(0, "40", new ChartRect(180, 21, 12, 14), Double.PositiveInfinity),
                new LegendCell(1, "7", new ChartRect(152, 40, 6, 14), Double.PositiveInfinity),
                new LegendCell(1, "35", new ChartRect(180, 40, 12, 14), Double.PositiveInfinity)
            ],
            legend.Cells);
        var first = legend.Items[0];
        Assert.Equal(new ChartRect(0, 19, 200, 19), first.Bounds);
        Assert.Equal(new ChartRect(8, 24, 12, 8), first.Marker);
        Assert.Equal(new ChartRect(36, 21, 24, 14), first.Text);
        Assert.Equal(70, first.TextMaxWidth);
        Assert.Equal(new ChartRect(0, 38, 200, 19), legend.Items[1].Bounds);
    }

    [Fact]
    public void TableRowsThatDoNotFitAreShownAsACount()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var height = legend.MeasureTable(Entries(LegendMarkerKind.Dot, "A", "B", "C", "D", "E"), ["Name"], [], 200, 80, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(76, height);
        Assert.Equal(2, legend.Items.Count);
        Assert.Equal(3, legend.OverflowCount);
        Assert.Equal("+3", legend.OverflowText);
        Assert.Equal(new ChartRect(8, 59, 12, 14), legend.OverflowBounds);
    }

    [Fact]
    public void TableWithoutRoomForARowIsHidden()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var height = legend.MeasureTable(Entries(LegendMarkerKind.Dot, "A", "B"), ["Name"], [], 200, 50, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(0, height);
        Assert.False(legend.IsVisible);
        Assert.False(legend.IsTable);
        Assert.Empty(legend.Cells);
    }

    [Fact]
    public void TableColumnsThatDoNotFitAreLeftOut()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        legend.MeasureTable(Entries(LegendMarkerKind.Line, "cpu0"), ["Name", "Last *", "Max"], ["12", "40"], 150, 100, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal(["Name", "Last *", "12"], legend.Cells.Select(static x => x.Text));
        Assert.Equal(new ChartRect(106, 2, 36, 14), legend.Cells[1].Bounds);
        Assert.Equal(new ChartRect(130, 21, 12, 14), legend.Cells[2].Bounds);
        Assert.Equal(54, legend.Items[0].TextMaxWidth);
    }

    [Fact]
    public void TableLeavesEmptyValuesOut()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        legend.MeasureTable(Entries(LegendMarkerKind.Dot, "A", "B"), ["Name", "Last *"], ["1", String.Empty], 200, 100, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.Equal([-1, -1, 0], legend.Cells.Select(static x => x.Row));
    }

    [Fact]
    public void OffsetMovesTheTable()
    {
        // Arrange
        var legend = new LegendLayout();
        legend.MeasureTable(Entries(LegendMarkerKind.Line, "cpu0"), ["Name", "Last *"], ["12"], 200, 100, Measurer, FontSize, MarkerSize);

        // Act
        legend.Offset(10, 20);

        // Assert
        Assert.Equal(new ChartRect(10, 20, 200, 19), legend.HeaderBounds);
        Assert.Equal(new ChartRect(46, 22, 24, 14), legend.Cells[0].Bounds);
        Assert.Equal(new ChartRect(190, 41, 12, 14), legend.Cells[2].Bounds);
        Assert.Equal(new ChartRect(10, 39, 200, 19), legend.Items[0].Bounds);
    }

    [Fact]
    public void ListAfterATableHasNoCells()
    {
        // Arrange
        var legend = new LegendLayout();
        legend.MeasureTable(Entries(LegendMarkerKind.Dot, "A"), ["Name", "Last *"], ["1"], 200, 100, Measurer, FontSize, MarkerSize);

        // Act
        legend.MeasureRows(Entries(LegendMarkerKind.Dot, "A"), 200, Measurer, FontSize, MarkerSize);

        // Assert
        Assert.False(legend.IsTable);
        Assert.Empty(legend.Cells);
        Assert.True(legend.HeaderBounds.IsEmpty);
    }

    [Fact]
    public void ColumnWidthCanBeLimitedByTheCaller()
    {
        // Arrange
        var legend = new LegendLayout();

        // Act
        var width = legend.MeasureColumn(Entries(LegendMarkerKind.Dot, new string('A', 40)), 100, Measurer, FontSize, MarkerSize, 120);

        // Assert
        Assert.Equal(120, width);
        Assert.Equal(108, legend.Items[0].TextMaxWidth);
    }

    private static LegendEntry[] Entries(LegendMarkerKind marker, params string[] texts) =>
        texts.Select((x, i) => new LegendEntry(i, x, marker)).ToArray();
}
