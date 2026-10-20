namespace ClamChart.Tests.Layout;

public sealed class LabelThinningTests
{
    private const double Gap = 4;

    [Fact]
    public void AllLabelsAreKeptWhenTheyFit()
    {
        // Act & Assert
        Assert.Equal([0, 1, 2, 3, 4], Select(Centered(10, 0, 20, 40, 60, 80), 10));
    }

    [Fact]
    public void OverlappingLabelsAreThinnedEvenly()
    {
        // Act & Assert
        Assert.Equal([0, 2, 4], Select(Centered(30, 0, 20, 40, 60, 80), 10));
    }

    [Fact]
    public void LastLabelIsKeptInsteadOfTheOneBeforeIt()
    {
        // Act & Assert
        Assert.Equal([0, 2, 5], Select(Centered(30, 0, 20, 40, 60, 80, 100), 10));
    }

    [Fact]
    public void StrideThatReachesTheLastLabelIsPreferred()
    {
        // Act & Assert
        Assert.Equal([0, 5, 10], Select(Centered(25, 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100), 20));
    }

    [Theory]
    [InlineData(3, new[] { 0, 2, 4 })]
    [InlineData(2, new[] { 0, 4 })]
    [InlineData(1, new[] { 0 })]
    [InlineData(0, new int[0])]
    public void MaxCountLimitsTheLabels(int maxCount, int[] expected)
    {
        // Act & Assert
        Assert.Equal(expected, Select(Centered(10, 0, 20, 40, 60, 80), maxCount));
    }

    [Fact]
    public void OnlyTheFirstLabelIsKeptWhenTheEndsOverlap()
    {
        // Act & Assert
        Assert.Equal([0], Select(Centered(30, 0, 10), 10));
    }

    [Fact]
    public void SingleAndNoLabels()
    {
        // Act & Assert
        Assert.Equal([0], Select(Centered(30, 0), 10));
        Assert.Empty(Select([], 10));
    }

    [Fact]
    public void DescendingPositionsAreThinnedTheSameWay()
    {
        // Act & Assert
        Assert.Equal([0, 2, 4], Select(Centered(30, 80, 60, 40, 20, 0), 10));
    }

    private static LabelExtent[] Centered(double width, params double[] positions) =>
        positions.Select(x => new LabelExtent(x - (width / 2), x + (width / 2))).ToArray();

    private static List<int> Select(LabelExtent[] labels, int maxCount)
    {
        var selected = new List<int>();
        LabelThinning.Select(labels, Gap, maxCount, selected);
        return selected;
    }
}
