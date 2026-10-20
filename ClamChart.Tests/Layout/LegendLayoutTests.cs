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

    private static LegendEntry[] Entries(LegendMarkerKind marker, params string[] texts) =>
        texts.Select((x, i) => new LegendEntry(i, x, marker)).ToArray();
}
