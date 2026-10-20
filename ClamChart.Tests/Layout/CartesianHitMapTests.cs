namespace ClamChart.Tests.Layout;

public sealed class CartesianHitMapTests
{
    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CartesianLayoutOptions Categories = new()
    {
        XAxis = new AxisLayoutOptions { Kind = ChartAxisKind.Category },
        YAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { IncludeZero = true } }
    };

    [Fact]
    public void BarIsHitAnywhereInItsColumn()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 20), ("Mar", 30)]);
        using var layout = Update(set, Categories);

        // Act
        var hit = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(1), layout.PlotBounds.Y + 5);

        // Assert
        Assert.Equal(ChartHitKind.Item, hit.Kind);
        Assert.Equal(0, hit.SeriesIndex);
        Assert.Equal(1, hit.Index);
    }

    [Fact]
    public void ClusteredBarsSplitTheColumn()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 20)]);
        using var layout = Update(set, Categories);
        var y = layout.YAxis.ToPixel(5);

        // Act
        var left = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(-0.25), y);
        var right = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(0.25), y);

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, 0), left);
        Assert.Equal(new ChartHit(ChartHitKind.Item, 1, 0), right);
    }

    [Fact]
    public void PackedClusterIsHitOnTheDrawnBar()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 20)]);
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 30)]);
        using var layout = Update(set, Categories);
        var (start, end) = layout.Geometries[0].GetBarRange(0);

        // Act
        var hit = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel((start + end) / 2), layout.YAxis.ToPixel(5));

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, 0), hit);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(20, 1)]
    [InlineData(28, 1)]
    public void StackedBarsPickTheSegment(double value, int series)
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)], "total");
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 15)], "total");
        using var layout = Update(set, Categories);

        // Act
        var hit = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(0), layout.YAxis.ToPixel(value));

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, series, 0), hit);
    }

    [Fact]
    public void LinePointIsHitOnlyNearIt()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10), ("Feb", 20), ("Mar", 30)]);
        using var layout = Update(set, Categories);
        var x = layout.XAxis.ToPixel(1.3);
        var y = layout.YAxis.ToPixel(20);

        // Act
        var near = CartesianHitMap.HitTest(layout, set.Series, x, y + CartesianHitMap.PointRadius);
        var far = CartesianHitMap.HitTest(layout, set.Series, x, y + CartesianHitMap.PointRadius + 1);

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, 1), near);
        Assert.Equal(new ChartHit(ChartHitKind.Plot), far);
    }

    [Fact]
    public void NearestPointWinsAmongSeries()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10), ("Feb", 20)]);
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 12), ("Feb", 22)]);
        using var layout = Update(set, Categories);

        // Act
        var lower = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(1), layout.YAxis.ToPixel(20.5));
        var upper = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(1), layout.YAxis.ToPixel(21.5));

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, 1), lower);
        Assert.Equal(new ChartHit(ChartHitKind.Item, 1, 1), upper);
    }

    [Fact]
    public void AreaIsHitInsideItsFill()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Area, [("Jan", 10), ("Feb", 20), ("Mar", 30)]);
        using var layout = Update(set, Categories);

        // Act
        var inside = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(1.4), layout.YAxis.ToPixel(2));
        var above = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(0), layout.PlotBounds.Y + 1);

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, 1), inside);
        Assert.Equal(new ChartHit(ChartHitKind.Plot), above);
    }

    [Fact]
    public void PointNearTheEdgeIsHitFromOutsideThePlot()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Line, [(0, 1), (10, 2), (20, 3)]);
        var options = Categories with { XAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { NiceRange = false } } };
        using var layout = Update(set, options);

        // Act
        var hit = CartesianHitMap.HitTest(layout, set.Series, layout.PlotBounds.Right + 10, layout.YAxis.ToPixel(3));

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, 2), hit);
    }

    [Theory]
    [InlineData(12, 1)]
    [InlineData(16, 2)]
    [InlineData(3, 0)]
    public void NumericPointsAreFoundByTheNearestX(double x, int index)
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Line, [(0, 10), (10, 10), (20, 10)]);
        var options = Categories with { XAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { NiceRange = false } } };
        using var layout = Update(set, options);

        // Act
        var hit = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(x), layout.YAxis.ToPixel(10));

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, index), hit);
        Assert.Equal(index, CartesianHitMap.FindNearestIndex(layout, 0, layout.XAxis.ToPixel(x)));
    }

    [Fact]
    public void CategoryPointIsNotFoundBeyondHalfACategory()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 20), ("Mar", 30)]);
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10)]);
        using var layout = Update(set, Categories);

        // Act & Assert
        Assert.Equal(0, CartesianHitMap.FindNearestIndex(layout, 1, layout.XAxis.ToPixel(0.4)));
        Assert.Equal(-1, CartesianHitMap.FindNearestIndex(layout, 1, layout.XAxis.ToPixel(0.6)));
    }

    [Fact]
    public void LegendItemIsHit()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10)], title: "Sales");
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 20)], title: "Cost");
        using var layout = Update(set, Categories with { LegendVisible = true });
        var item = layout.Legend.Items[1].Bounds;

        // Act
        var hit = CartesianHitMap.HitTest(layout, set.Series, item.CenterX, item.CenterY);

        // Assert
        Assert.Equal(ChartHitKind.Legend, hit.Kind);
        Assert.Equal(1, hit.SeriesIndex);
        Assert.Equal(-1, hit.Index);
    }

    [Fact]
    public void HiddenSeriesAndGapsAreNotHit()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10), ("Feb", Double.NaN)]);
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 5), ("Feb", 5)], visible: false);
        using var layout = Update(set, Categories);

        // Act
        var gap = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(1), layout.YAxis.ToPixel(10));
        var hidden = CartesianHitMap.HitTest(layout, set.Series, layout.XAxis.ToPixel(0), layout.YAxis.ToPixel(5));

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Plot), gap);
        Assert.Equal(new ChartHit(ChartHitKind.Plot), hidden);
    }

    [Fact]
    public void OutsideThePlotIsNothing()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        using var layout = Update(set, Categories);

        // Act & Assert
        Assert.Equal(ChartHit.None, CartesianHitMap.HitTest(layout, set.Series, 1, 1));
    }

    [Fact]
    public void HorizontalBarsAreHitAlongTheVerticalAxis()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Tokyo", 10), ("Osaka", 20)]);
        using var layout = Update(set, Categories with { Orientation = ChartOrientation.Horizontal });

        // Act
        var hit = CartesianHitMap.HitTest(layout, set.Series, layout.PlotBounds.Right - 2, layout.XAxis.ToPixel(1));

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, 0, 1), hit);
    }

    private static CartesianLayout Update(TestSeriesSet set, CartesianLayoutOptions options)
    {
        var layout = new CartesianLayout();
        layout.Update(360, 240, new Thickness(8), ChartStyles.Light, CultureInfo.InvariantCulture, options, set.Series, Measurer);
        return layout;
    }
}
