namespace ClamChart.Tests.Layout;

public sealed class CategoryMergerTests
{
    [Fact]
    public void CategoriesAreMergedInOrderOfAppearance()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 1), ("Feb", 2), ("Mar", 3)]);
        set.AddEntries(ChartSeriesKind.Line, [("Feb", 4), ("Apr", 5)]);
        var geometries = set.CreateGeometries();
        var merger = new CategoryMerger();

        // Act
        merger.Merge(set.Series, geometries);

        // Assert
        Assert.Equal(["Jan", "Feb", "Mar", "Apr"], merger.Categories);
        Assert.Equal([0d, 1, 2], geometries[0].X.ToArray());
        Assert.Equal([1d, 3], geometries[1].X.ToArray());
    }

    [Fact]
    public void NullCategoriesShareOneCategory()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [(null, 1), ("A", 2), (null, 3)]);
        var geometries = set.CreateGeometries();
        var merger = new CategoryMerger();

        // Act
        merger.Merge(set.Series, geometries);

        // Assert
        Assert.Equal([null, "A"], merger.Categories);
        Assert.Equal([0d, 1, 0], geometries[0].X.ToArray());
    }

    [Fact]
    public void SeriesWithoutCategoriesUseTheirPositions()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("A", 1), ("B", 2)]);
        set.AddValues(ChartSeriesKind.Line, [5, 6, 7]);
        var geometries = set.CreateGeometries();
        var merger = new CategoryMerger();

        // Act
        merger.Merge(set.Series, geometries);

        // Assert
        Assert.Equal(["A", "B", null], merger.Categories);
        Assert.Equal([0d, 1, 2], geometries[1].X.ToArray());
    }

    [Fact]
    public void HiddenSeriesKeepTheirCategories()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("A", 1)]);
        set.AddEntries(ChartSeriesKind.Bar, [("B", 1)], visible: false);
        var geometries = set.CreateGeometries();
        var merger = new CategoryMerger();

        // Act
        merger.Merge(set.Series, geometries);

        // Assert
        Assert.Equal(["A", "B"], merger.Categories);
    }

    [Fact]
    public void MergeStartsOver()
    {
        // Arrange
        using var first = new TestSeriesSet();
        first.AddEntries(ChartSeriesKind.Bar, [("A", 1), ("B", 2)]);
        using var second = new TestSeriesSet();
        second.AddEntries(ChartSeriesKind.Bar, [("B", 1)]);
        var merger = new CategoryMerger();
        merger.Merge(first.Series, first.CreateGeometries());

        // Act
        var geometries = second.CreateGeometries();
        merger.Merge(second.Series, geometries);

        // Assert
        Assert.Equal(["B"], merger.Categories);
        Assert.Equal([0d], geometries[0].X.ToArray());
    }
}
