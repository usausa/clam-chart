namespace ClamChart.Tests.Layout;

public sealed class StackCalculatorTests
{
    [Fact]
    public void SeriesInAGroupAreStackedInOrder()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Bar, [1, 2], "total");
        set.AddValues(ChartSeriesKind.Bar, [3, 4], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.True(geometries[0].IsStacked);
        Assert.Equal([0d, 0], geometries[0].Start.ToArray());
        Assert.Equal([1d, 2], geometries[0].End.ToArray());
        Assert.Equal([1d, 2], geometries[1].Start.ToArray());
        Assert.Equal([4d, 6], geometries[1].End.ToArray());
        Assert.Equal([false, false], geometries[0].Outer.ToArray());
        Assert.Equal([true, true], geometries[1].Outer.ToArray());
    }

    [Fact]
    public void PositiveAndNegativeValuesAreStackedSeparately()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Bar, [2, -1], "total");
        set.AddValues(ChartSeriesKind.Bar, [-3, 4], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.Equal([0d, 0], geometries[0].Start.ToArray());
        Assert.Equal([2d, -1], geometries[0].End.ToArray());
        Assert.Equal([0d, 0], geometries[1].Start.ToArray());
        Assert.Equal([-3d, 4], geometries[1].End.ToArray());
        Assert.Equal([true, true], geometries[0].Outer.ToArray());
        Assert.Equal([true, true], geometries[1].Outer.ToArray());
    }

    [Fact]
    public void PercentStackFillsEachSign()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Bar, [1, 2], "total");
        set.AddValues(ChartSeriesKind.Bar, [3, -1], "total");
        set.AddValues(ChartSeriesKind.Bar, [0, 2], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Percent);

        // Assert
        Assert.Equal([25d, 50], geometries[0].End.ToArray());
        Assert.Equal([25d, 0], geometries[1].Start.ToArray());
        Assert.Equal([100d, -100], geometries[1].End.ToArray());
        Assert.Equal([100d, 50], geometries[2].Start.ToArray());
        Assert.Equal([100d, 100], geometries[2].End.ToArray());
    }

    [Fact]
    public void ZeroIsNotTheOuterSegment()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Bar, [1], "total");
        set.AddValues(ChartSeriesKind.Bar, [0], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.True(geometries[0].Outer[0]);
        Assert.False(geometries[1].Outer[0]);
    }

    [Fact]
    public void HiddenSeriesAreNotStacked()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Bar, [1], "total");
        set.AddValues(ChartSeriesKind.Bar, [5], "total", visible: false);
        set.AddValues(ChartSeriesKind.Bar, [2], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.False(geometries[1].IsStacked);
        Assert.Equal(1, geometries[2].Start[0]);
        Assert.Equal(3, geometries[2].End[0]);
    }

    [Fact]
    public void StacksAreSeparatedByAxisAndKind()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Bar, [1], "total");
        set.AddValues(ChartSeriesKind.Bar, [2], "total", ChartAxisTarget.Secondary);
        set.AddValues(ChartSeriesKind.Area, [3], "total");
        set.AddValues(ChartSeriesKind.Area, [4], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.Equal(0, geometries[1].Start[0]);
        Assert.Equal(0, geometries[2].Start[0]);
        Assert.Equal(3, geometries[3].Start[0]);
        Assert.Equal(7, geometries[3].End[0]);
    }

    [Fact]
    public void PointsAreStackedByTheirXValue()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Area, [(0, 1), (1, 2)], "total");
        set.AddPoints(ChartSeriesKind.Area, [(1, 3), (2, 4)], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.Equal([2d, 0], geometries[1].Start.ToArray());
        Assert.Equal([5d, 4], geometries[1].End.ToArray());
    }

    [Fact]
    public void MissingValuesAreGapsAndDoNotAccumulate()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Area, [1, Double.NaN], "total");
        set.AddValues(ChartSeriesKind.Area, [2, 3], "total");

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.True(Double.IsNaN(geometries[0].Start[1]));
        Assert.True(Double.IsNaN(geometries[0].End[1]));
        Assert.Equal([1d, 0], geometries[1].Start.ToArray());
    }

    [Fact]
    public void UnstackedSeriesStartFromTheirBaseline()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [3]);
        var area = set.AddValues(ChartSeriesKind.Area, [4]);
        set.Replace(1, area with { BaselineValue = 2 });
        set.AddValues(ChartSeriesKind.Bar, [5]);

        // Act
        var geometries = Calculate(set, ChartStackMode.Normal);

        // Assert
        Assert.Equal(3, geometries[0].Start[0]);
        Assert.Equal(2, geometries[1].Start[0]);
        Assert.Equal(0, geometries[2].Start[0]);
        Assert.Equal(5, geometries[2].End[0]);
        Assert.False(geometries[2].IsStacked);
    }

    [Fact]
    public void BarSlotsAreSharedByAStack()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Bar, [1]);
        set.AddValues(ChartSeriesKind.Bar, [1], "total");
        set.AddValues(ChartSeriesKind.Line, [1]);
        set.AddValues(ChartSeriesKind.Bar, [1], "total");
        set.AddValues(ChartSeriesKind.Bar, [1]);
        set.AddValues(ChartSeriesKind.Bar, [1], visible: false);
        var geometries = set.CreateGeometries();
        using var stacks = new StackCalculator();

        // Act
        stacks.Calculate(set.Series, geometries, ChartStackMode.Normal);

        // Assert
        Assert.Equal(3, stacks.BarSlotCount);
        Assert.Equal([0, 1, -1, 1, 2, -1], geometries.Select(static x => x.BarSlot).ToArray());
    }

    private static IReadOnlyList<SeriesGeometry> Calculate(TestSeriesSet set, ChartStackMode mode)
    {
        var geometries = set.CreateGeometries();
        using var stacks = new StackCalculator();
        stacks.Calculate(set.Series, geometries, mode);
        return geometries;
    }
}
