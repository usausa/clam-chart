namespace ClamChart.Tests.Layout;

public sealed class BarGaugeLayoutTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void TopLabelsPutTheNameAboveTheBar()
    {
        // Act
        var layout = Layout([Entry(0, "Tokyo", 82), Entry(1, "Osaka", 58)], new BarGaugeLayoutOptions(), 300, 200);

        // Assert
        Assert.Equal(new ChartRect(0, 0, 300, 14), layout.LabelTemplate);
        Assert.Equal(new ChartRect(0, 21, 274, 8), layout.TrackTemplate);
        Assert.Equal(new ChartRect(282, 18, 18, 14), layout.ValueTemplate);
        Assert.Equal(44, layout.Pitch);
        Assert.Equal(76, layout.DesiredHeight);
        Assert.Equal(new ChartRect(0, 65, 274, 8), layout.GetTrackBounds(1));
        Assert.Equal(["82%", "58%"], layout.Rows.Select(static x => x.ValueText));
        Assert.Equal(["Tokyo", "Osaka"], layout.Rows.Select(static x => x.Label));
    }

    [Fact]
    public void LeftLabelsTakeAtMostFortyPercentOfTheWidth()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50), Entry(1, "Very long store name", 25)], new BarGaugeLayoutOptions { LabelPosition = ChartBarGaugeLabelPosition.Left }, 200, 200);

        // Assert
        Assert.Equal(new ChartRect(0, 0, 80, 14), layout.LabelTemplate);
        Assert.Equal(new ChartRect(88, 3, 86, 8), layout.TrackTemplate);
        Assert.Equal(new ChartRect(182, 0, 18, 14), layout.ValueTemplate);
        Assert.Equal(26, layout.Pitch);
        Assert.Equal(40, layout.DesiredHeight);
    }

    [Fact]
    public void WithoutLabelsOnlyTheBarLineIsLeft()
    {
        // Act
        var unnamed = Layout([Entry(0, null, 50), Entry(1, null, 25)], new BarGaugeLayoutOptions(), 200, 200);
        var hidden = Layout([Entry(0, "A", 50)], new BarGaugeLayoutOptions { LabelPosition = ChartBarGaugeLabelPosition.None, ValueVisible = false }, 200, 200);

        // Assert
        Assert.True(unnamed.LabelTemplate.IsEmpty);
        Assert.Equal(new ChartRect(0, 3, 174, 8), unnamed.TrackTemplate);
        Assert.Equal(26, unnamed.Pitch);
        Assert.True(hidden.ValueTemplate.IsEmpty);
        Assert.Equal(new ChartRect(0, 0, 200, 8), hidden.TrackTemplate);
        Assert.Equal(8, hidden.DesiredHeight);
        Assert.Null(hidden.Rows[0].ValueText);
    }

    [Fact]
    public void RatiosUseTheItemMaximumBeforeTheCommonOne()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50), Entry(1, "B", 50, 200), Entry(2, "C", 150)], new BarGaugeLayoutOptions(), 300, 200);
        var shifted = Layout([Entry(0, "A", 50)], new BarGaugeLayoutOptions { Minimum = 20 }, 300, 200);

        // Assert
        Assert.Equal([0.5, 0.25, 1], layout.Rows.Select(static x => x.Ratio));
        Assert.Equal([50d, 25, 150], layout.Rows.Select(static x => Math.Round(x.Percent, Precision)));
        Assert.Equal(["50%", "25%", "150%"], layout.Rows.Select(static x => x.ValueText));
        Assert.Equal(0.375, shifted.Rows[0].Ratio, Precision);
    }

    [Fact]
    public void InvalidValuesHaveNoBarAndNoText()
    {
        // Act
        var layout = Layout([Entry(0, "A", Double.NaN), Entry(1, "B", 10, 0)], new BarGaugeLayoutOptions(), 300, 200);

        // Assert
        Assert.All(layout.Rows, static x => Assert.Equal(0, x.Ratio));
        Assert.All(layout.Rows, static x => Assert.True(Double.IsNaN(x.Percent)));
        Assert.All(layout.Rows, static x => Assert.Null(x.ValueText));
        Assert.True(layout.ValueTemplate.IsEmpty);
    }

    [Fact]
    public void ValueModesChooseTheText()
    {
        // Arrange
        var formatting = new ChartValueFormatting(null, null, ChartTextFormat.Parse("N1"), Culture);

        // Act
        var value = Layout([Entry(0, "A", 72)], new BarGaugeLayoutOptions { ValueMode = ChartBarGaugeValueMode.Value }, 300, 200);
        var formatted = Layout([Entry(0, "A", 72)], new BarGaugeLayoutOptions { ValueMode = ChartBarGaugeValueMode.Value, Formatting = formatting }, 300, 200);
        var ofMaximum = Layout([Entry(0, "A", 72), Entry(1, "B", 72, 120)], new BarGaugeLayoutOptions { ValueMode = ChartBarGaugeValueMode.ValueOfMaximum }, 300, 200);

        // Assert
        Assert.Equal("72", value.Rows[0].ValueText);
        Assert.Equal("72.0", formatted.Rows[0].ValueText);
        Assert.Equal(["72 / 100", "72 / 120"], ofMaximum.Rows.Select(static x => x.ValueText));
    }

    [Fact]
    public void SortingAndMaxItemsChooseTheRows()
    {
        // Arrange
        BarGaugeEntry[] entries = [Entry(0, "A", 30), Entry(1, "B", Double.NaN), Entry(2, "C", 80), Entry(3, "D", 50)];

        // Act
        var descending = Layout(entries, new BarGaugeLayoutOptions { SortMode = ChartSortMode.Descending }, 300, 200);
        var ascending = Layout(entries, new BarGaugeLayoutOptions { SortMode = ChartSortMode.Ascending }, 300, 200);
        var top = Layout(entries, new BarGaugeLayoutOptions { SortMode = ChartSortMode.Descending, MaxItems = 2 }, 300, 200);

        // Assert
        Assert.Equal([2, 3, 0, 1], descending.Rows.Select(static x => x.DataIndex));
        Assert.Equal([0, 3, 2, 1], ascending.Rows.Select(static x => x.DataIndex));
        Assert.Equal([2, 3], top.Rows.Select(static x => x.DataIndex));
        Assert.Equal([0, 1], top.Rows.Select(static x => x.Slot));
    }

    [Fact]
    public void TargetsAreClampedToTheTrack()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50, target: 150), Entry(1, "B", 50, target: 40), Entry(2, "C", 50)], new BarGaugeLayoutOptions(), 300, 200);

        // Assert
        Assert.Equal(1, layout.Rows[0].TargetRatio);
        Assert.Equal(0.4, layout.Rows[1].TargetRatio, Precision);
        Assert.True(Double.IsNaN(layout.Rows[2].TargetRatio));
    }

    [Theory]
    [InlineData(ChartBarGaugeLabelPosition.Top, true, null)]
    [InlineData(ChartBarGaugeLabelPosition.Top, false, 1)]
    [InlineData(ChartBarGaugeLabelPosition.Left, true, null)]
    [InlineData(ChartBarGaugeLabelPosition.Left, false, null)]
    [InlineData(ChartBarGaugeLabelPosition.None, true, 2)]
    [InlineData(ChartBarGaugeLabelPosition.None, false, 0)]
    public void MeasuredHeightMatchesTheLayout(ChartBarGaugeLabelPosition position, bool valueVisible, int? maxItems)
    {
        // Arrange
        BarGaugeEntry[] entries = [Entry(0, "Tokyo", 82), Entry(1, null, 58), Entry(2, "Nagoya", 31)];
        var options = new BarGaugeLayoutOptions { LabelPosition = position, ValueVisible = valueVisible, MaxItems = maxItems, BarThickness = 10, ItemSpacing = 6 };

        // Act
        var layout = Layout(entries, options, 300, 200);
        var height = BarGaugeLayout.MeasureHeight(entries, new ChartStyle(), position, valueVisible, 10, 6, maxItems, Measurer);

        // Assert
        Assert.Equal(layout.DesiredHeight, height);
    }

    [Fact]
    public void VerticalColumnsShareTheWidth()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50), Entry(1, "B", 25)], new BarGaugeLayoutOptions { Orientation = ChartOrientation.Vertical }, 200, 150);

        // Assert
        Assert.Equal(new ChartRect(43, 18, 8, 114), layout.TrackTemplate);
        Assert.Equal(new ChartRect(0, 0, 94, 14), layout.ValueTemplate);
        Assert.Equal(new ChartRect(0, 136, 94, 14), layout.LabelTemplate);
        Assert.Equal(106, layout.Pitch);
        Assert.Equal(new ChartRect(149, 18, 8, 114), layout.GetTrackBounds(1));
        Assert.Equal(114, layout.GetTrackLength());
    }

    [Fact]
    public void SegmentedCellsFillTheTrack()
    {
        // Arrange
        var options = new BarGaugeLayoutOptions { DisplayMode = ChartBarGaugeMode.Segmented, ValueVisible = false };

        // Act
        var layout = Layout([Entry(0, null, 50)], options, 200, 200);
        var basic = Layout([Entry(0, null, 50)], options with { DisplayMode = ChartBarGaugeMode.Basic }, 200, 200);

        // Assert
        Assert.Equal(16, layout.CellCount);
        Assert.Equal(10.625, layout.CellSize, Precision);
        Assert.Equal(2, layout.CellGap);
        Assert.Equal(0, basic.CellCount);
    }

    [Fact]
    public void FindRowUsesTheRowBand()
    {
        // Arrange
        var layout = Layout([Entry(0, "Tokyo", 82), Entry(1, "Osaka", 58)], new BarGaugeLayoutOptions(), 300, 200);

        // Act & Assert
        Assert.Equal(0, layout.FindRow(100, 10));
        Assert.Equal(-1, layout.FindRow(100, 38));
        Assert.Equal(1, layout.FindRow(100, 50));
        Assert.Equal(-1, layout.FindRow(310, 10));
    }

    [Fact]
    public void PaddingMovesTheContentAndAddsToTheHeight()
    {
        // Arrange
        var layout = new BarGaugeLayout();

        // Act
        layout.Update(300, 200, new Thickness(8), new ChartStyle(), Culture, new BarGaugeLayoutOptions(), [Entry(0, "Tokyo", 82)], Measurer);

        // Assert
        Assert.Equal(new ChartRect(8, 8, 284, 184), layout.Bounds);
        Assert.Equal(new ChartRect(8, 29, 258, 8), layout.TrackTemplate);
        Assert.Equal(48, layout.DesiredHeight);
    }

    private static BarGaugeEntry Entry(int index, string? label, double value, double maximum = Double.NaN, double target = Double.NaN, Color? color = null) =>
        new(index, label, value, maximum, target, color);

    private static BarGaugeLayout Layout(BarGaugeEntry[] entries, BarGaugeLayoutOptions options, double width, double height)
    {
        var layout = new BarGaugeLayout();
        layout.Update(width, height, new Thickness(0), new ChartStyle(), Culture, options, entries, Measurer);
        return layout;
    }
}
