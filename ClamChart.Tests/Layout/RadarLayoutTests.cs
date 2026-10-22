namespace ClamChart.Tests.Layout;

public sealed class RadarLayoutTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void AxesFollowTheFirstAppearanceAcrossSeries()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("Speed", 3), ("Power", 4)]);
        set.AddEntries(ChartSeriesKind.Radar, [("Power", 5), ("Range", 2), ("Speed", 1)]);

        // Act
        var layout = Layout(set);

        // Assert
        Assert.Equal(["Speed", "Power", "Range"], layout.Axes.Select(static x => x.Text));
        Assert.Equal([3, 4, Double.NaN], Enumerable.Range(0, 3).Select(k => layout.GetValue(0, k)));
        Assert.Equal([1d, 5, 2], Enumerable.Range(0, 3).Select(k => layout.GetValue(1, k)));
        Assert.Equal([0, 1, -1], Enumerable.Range(0, 3).Select(k => layout.GetDataIndex(0, k)));
        Assert.Equal([2, 0, 1], Enumerable.Range(0, 3).Select(k => layout.GetDataIndex(1, k)));
        Assert.Equal(2, layout.FindAxis(1, 1));
        Assert.Equal(-1, layout.FindAxis(0, 5));
    }

    [Fact]
    public void RepeatedCategoryKeepsTheFirstValue()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 1), ("B", 2), ("A", 9)]);

        // Act
        var layout = Layout(set);

        // Assert
        Assert.Equal(2, layout.AxisCount);
        Assert.Equal(1, layout.GetValue(0, 0));
        Assert.Equal(0, layout.GetDataIndex(0, 0));
    }

    [Fact]
    public void PlainValuesMakeUnnamedAxesInTheirOrder()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Radar, [1, 2, 3]);

        // Act
        var layout = Layout(set);

        // Assert
        Assert.Equal(3, layout.AxisCount);
        Assert.All(layout.Axes, static x => Assert.Null(x.Text));
        Assert.Equal("1", layout.GetAxisText(0));
        Assert.Equal("3", layout.GetAxisText(2));
        Assert.False(layout.ShowsLabels);
        Assert.Empty(layout.Levels);
    }

    [Fact]
    public void ScaleStartsAtZeroAndPlacesTheLevels()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 3), ("B", 87), ("C", 40)]);

        // Act
        var layout = Layout(set, width: 400, height: 400);

        // Assert
        Assert.Equal(new AxisScale(0, 100, 20), layout.Scale);
        Assert.Equal([20d, 40, 60, 80, 100], layout.Levels.Select(static x => x.Value));
        Assert.Equal(["20", "40", "60", "80", "100"], layout.Levels.Select(static x => x.Text));
        Assert.Equal(16.5, layout.Levels[^1].Width, Precision);
    }

    [Fact]
    public void FixedBoundsAndLevelCountShapeTheScale()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 3), ("B", 12), ("C", 4)]);

        // Act
        var layout = Layout(set, new RadarLayoutOptions { Minimum = 0, Maximum = 10, LevelCount = 2 }, 400, 400);

        // Assert
        Assert.Equal(new AxisScale(0, 10, 5), layout.Scale);
        Assert.Equal([5d, 10], layout.Levels.Select(static x => x.Value));
        Assert.Equal(1, RadarLayout.GetRatio(layout.GetValue(0, 1), layout.Scale));
    }

    [Fact]
    public void HiddenSeriesAreLeftOutOfTheScale()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 1), ("B", 2), ("C", 3)]);
        set.AddEntries(ChartSeriesKind.Radar, [("A", 50), ("B", 60), ("D", 70)], visible: false);

        // Act
        var layout = Layout(set);

        // Assert
        Assert.Equal(4, layout.AxisCount);
        Assert.Equal(0, layout.Scale.Minimum);
        Assert.Equal(3, layout.Scale.Maximum);
    }

    [Fact]
    public void LabelsSitJustOutsideTheRingAndFillTheHeight()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("Top", 1), ("Right", 2), ("Bottom", 3), ("Left", 4)]);

        // Act
        var layout = Layout(set, width: 300, height: 200);

        // Assert
        var gap = RadarLayout.LabelGap;
        var axes = layout.Axes;
        Assert.True(layout.ShowsLabels);
        Assert.Equal(79, layout.Radius, Precision);
        Assert.Equal(100, layout.CenterY, Precision);
        Assert.Equal(0, axes[0].LabelBounds.Y, Precision);
        Assert.Equal(layout.CenterX, axes[0].LabelBounds.CenterX, Precision);
        Assert.Equal(layout.CenterX + layout.Radius + gap, axes[1].LabelBounds.X, Precision);
        Assert.Equal(layout.CenterY, axes[1].LabelBounds.CenterY, Precision);
        Assert.Equal(200, axes[2].LabelBounds.Bottom, Precision);
        Assert.Equal(layout.CenterX - layout.Radius - gap, axes[3].LabelBounds.Right, Precision);
        Assert.Equal(150 - ((27.5 - 22) / 2), layout.CenterX, Precision);
    }

    [Fact]
    public void LabelsAreDroppedWhenTheyLeaveTooLittleRoom()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("Acceleration", 1), ("Durability", 2), ("Efficiency", 3), ("Comfortable", 4)]);

        // Act
        var layout = Layout(set, width: 80, height: 80);

        // Assert
        Assert.False(layout.ShowsLabels);
        Assert.Equal(40, layout.Radius, Precision);
        Assert.Empty(layout.Levels);
    }

    [Fact]
    public void LabelsAndLevelsAreHiddenWhenTurnedOff()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 1), ("B", 2), ("C", 3)]);

        // Act
        var layout = Layout(set, new RadarLayoutOptions { GridShape = ChartRadarGridShape.Circle, LabelsVisible = false, RingMargin = 1 }, 200, 100);

        // Assert
        Assert.False(layout.ShowsLabels);
        Assert.Empty(layout.Levels);
        Assert.Equal(49, layout.Radius, Precision);
    }

    [Fact]
    public void LevelLabelsAreThinnedWhenTheRingsAreClose()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 10), ("B", 2), ("C", 3)]);

        // Act
        var layout = Layout(set, new RadarLayoutOptions { LevelCount = 10 }, 200, 120);

        // Assert
        Assert.Equal(10, layout.Scale.Maximum);
        Assert.Equal(10, layout.Ticks.Count - 1);
        Assert.InRange(layout.Levels.Count, 2, 9);
        Assert.Equal(10, layout.Levels[^1].Value);
        var half = (layout.LabelHeight / 2) + RadarLayout.LevelLabelPadding;
        for (var i = 1; i < layout.Levels.Count; i++)
        {
            var spacing = layout.Radius * (layout.Levels[i].Value - layout.Levels[i - 1].Value) / 10;
            Assert.True(spacing >= half * 2);
        }
    }

    [Fact]
    public void FewerThanThreeAxesUseCircles()
    {
        // Arrange
        using var two = new TestSeriesSet();
        two.AddEntries(ChartSeriesKind.Radar, [("A", 1), ("B", 2)]);
        using var three = new TestSeriesSet();
        three.AddEntries(ChartSeriesKind.Radar, [("A", 1), ("B", 2), ("C", 3)]);

        // Act
        var polygon = Layout(two);
        var triangle = Layout(three);
        var circle = Layout(three, new RadarLayoutOptions { GridShape = ChartRadarGridShape.Circle });

        // Assert
        Assert.True(polygon.IsCircle);
        Assert.False(triangle.IsCircle);
        Assert.True(circle.IsCircle);
    }

    [Fact]
    public void LegendListsTitledSeriesBelowTheRadar()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 1), ("B", 2), ("C", 3)], title: "First");
        set.AddEntries(ChartSeriesKind.Radar, [("A", 3), ("B", 2), ("C", 1)]);
        set.AddEntries(ChartSeriesKind.Radar, [("A", 2), ("B", 2), ("C", 2)], title: "Third");

        // Act
        var layout = Layout(set, new RadarLayoutOptions { LegendVisible = true }, 300, 300);

        // Assert
        var legend = layout.Legend;
        Assert.True(legend.IsVisible);
        Assert.Equal([0, 2], legend.Items.Select(static x => x.SeriesIndex));
        Assert.Equal(300, legend.Bounds.Bottom, Precision);
        Assert.Equal(legend.Bounds.Y - RadarLayout.LegendGap, layout.PlotBounds.Bottom, Precision);
    }

    [Fact]
    public void HitTestFindsTheNearestVisibleVertexInReach()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 10), ("B", 5), ("C", 5)], title: "First");
        set.AddEntries(ChartSeriesKind.Radar, [("A", 5), ("B", 10), ("C", 5)], title: "Second");
        set.AddEntries(ChartSeriesKind.Radar, [("A", 9), ("B", 9), ("C", 9)], visible: false);
        var layout = Layout(set, new RadarLayoutOptions { LegendVisible = true }, 300, 300);
        var (x, y) = layout.GetPoint(1, 1);
        var legend = layout.Legend.Items[1].Bounds;

        // Act
        var vertex = layout.HitTest(x - 10, y + 5, set.Series);
        var plot = layout.HitTest(layout.CenterX + 3, layout.CenterY + 40, set.Series);
        var outside = layout.HitTest(1, 1, set.Series);
        var entry = layout.HitTest(legend.CenterX, legend.CenterY, set.Series);

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 1, 1), vertex);
        Assert.Equal(ChartHitKind.Plot, plot.Kind);
        Assert.Equal(ChartHitKind.None, outside.Kind);
        Assert.Equal(new ChartHit(ChartHitKind.Legend, 1), entry);
    }

    [Fact]
    public void FindAxisPicksTheNearestDirection()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Radar, [("A", 1), ("B", 2), ("C", 3), ("D", 4)]);
        var layout = Layout(set);

        // Act & Assert
        Assert.Equal(0, layout.FindAxis(layout.CenterX + 5, layout.CenterY - 40));
        Assert.Equal(1, layout.FindAxis(layout.CenterX + 40, layout.CenterY - 10));
        Assert.Equal(2, layout.FindAxis(layout.CenterX - 5, layout.CenterY + 40));
        Assert.Equal(3, layout.FindAxis(layout.CenterX - 40, layout.CenterY + 10));
        Assert.Equal(0, layout.FindAxis(layout.CenterX - 5, layout.CenterY - 40));
        Assert.Equal(-1, layout.FindAxis(layout.CenterX, layout.CenterY));
    }

    [Fact]
    public void SeriesFormatComesBeforeTheChartFormat()
    {
        // Arrange
        var plain = new CartesianSeries { Data = DataSnapshot.Empty };
        var formatted = plain with { ValueFormat = ChartTextFormat.Parse("0.0") };
        var chart = new ChartValueFormatting(null, null, ChartTextFormat.Parse("0 pt"), Culture);

        // Act & Assert
        Assert.Equal(CompactNumberFormat.Format(12500, Culture), RadarLayout.FormatValue(12500, plain, ChartValueFormatting.Default with { Culture = Culture }));
        Assert.Equal("3 pt", RadarLayout.FormatValue(3, plain, chart));
        Assert.Equal("3.0", RadarLayout.FormatValue(3, formatted, chart));
    }

    private static RadarLayout Layout(TestSeriesSet set, RadarLayoutOptions? options = null, double width = 200, double height = 200)
    {
        var layout = new RadarLayout();
        layout.Update(width, height, new Thickness(0), new ChartStyle(), Culture, options ?? new RadarLayoutOptions(), set.Series, Measurer);
        return layout;
    }
}
