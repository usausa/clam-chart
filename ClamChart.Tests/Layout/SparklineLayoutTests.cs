namespace ClamChart.Tests.Layout;

public sealed class SparklineLayoutTests
{
    private const int Precision = 9;

    [Fact]
    public void LineRangeFollowsTheValues()
    {
        // Act
        var layout = Layout([3, 5, 4]);

        // Assert
        Assert.Equal(new SparklineScale(3, 5, 2), layout.Scale);
        Assert.Equal(3, layout.Count);
    }

    [Fact]
    public void BarRangeIncludesZero()
    {
        // Act
        var positive = Layout([3, 5, 4], new SparklineLayoutOptions { Kind = ChartSparklineKind.Bar });
        var negative = Layout([-3, -5], new SparklineLayoutOptions { Kind = ChartSparklineKind.Bar });

        // Assert
        Assert.Equal(0, positive.Scale.Minimum);
        Assert.Equal(5, positive.Scale.Maximum);
        Assert.Equal(-5, negative.Scale.Minimum);
        Assert.Equal(0, negative.Scale.Maximum);
    }

    [Fact]
    public void WinLossUsesOnlyTheSign()
    {
        // Act
        var layout = Layout([3, -50, 4], new SparklineLayoutOptions { Kind = ChartSparklineKind.WinLoss, Minimum = 0, Maximum = 100, ReferenceValue = 10 });

        // Assert
        Assert.Equal(-1, layout.Scale.Minimum);
        Assert.Equal(1, layout.Scale.Maximum);
        Assert.Null(layout.Reference);
        Assert.Equal(ChartSparklineMarkers.None, layout.Markers);
    }

    [Fact]
    public void FlatValuesAreCentered()
    {
        // Act
        var layout = Layout([5, 5, 5]);

        // Assert
        Assert.Equal(4, layout.Scale.Minimum);
        Assert.Equal(6, layout.Scale.Maximum);
    }

    [Fact]
    public void FixedBoundsAreKept()
    {
        // Act
        var layout = Layout([3, 5, 4], new SparklineLayoutOptions { Minimum = 0, Maximum = 10 });
        var partial = Layout([3, 5, 4], new SparklineLayoutOptions { Minimum = 0 });

        // Assert
        Assert.Equal(new SparklineScale(0, 10, 2), layout.Scale);
        Assert.Equal(0, partial.Scale.Minimum);
        Assert.Equal(5, partial.Scale.Maximum);
        Assert.False(layout.HasInvertedBounds);
    }

    [Fact]
    public void InvertedBoundsFallBackToTheAutomaticRange()
    {
        // Act
        var layout = Layout([3, 5, 4], new SparklineLayoutOptions { Minimum = 10, Maximum = 0 });

        // Assert
        Assert.True(layout.HasInvertedBounds);
        Assert.Equal(3, layout.Scale.Minimum);
        Assert.Equal(5, layout.Scale.Maximum);
    }

    [Fact]
    public void BoundOutsideTheValuesKeepsAPositiveRange()
    {
        // Act
        var above = Layout([3, 5, 4], new SparklineLayoutOptions { Minimum = 100 });
        var below = Layout([3, 5, 4], new SparklineLayoutOptions { Maximum = -100 });

        // Assert
        Assert.Equal(100, above.Scale.Minimum);
        Assert.Equal(101, above.Scale.Maximum);
        Assert.Equal(-101, below.Scale.Minimum);
        Assert.Equal(-100, below.Scale.Maximum);
    }

    [Fact]
    public void ReferenceValueIsIncludedInTheAutomaticRange()
    {
        // Act
        var layout = Layout([3, 5, 4], new SparklineLayoutOptions { ReferenceValue = 10 });

        // Assert
        Assert.Equal(10, layout.Scale.Maximum);
        Assert.Equal(10, layout.Reference);
    }

    [Fact]
    public void NormalRangeIsIncludedInTheAutomaticRange()
    {
        // Act
        var both = Layout([3, 5, 4], new SparklineLayoutOptions { NormalRangeMinimum = 1, NormalRangeMaximum = 8 });
        var upper = Layout([3, 5, 4], new SparklineLayoutOptions { NormalRangeMaximum = 8 });
        var winLoss = Layout([3, -5, 4], new SparklineLayoutOptions { Kind = ChartSparklineKind.WinLoss, NormalRangeMinimum = 1, NormalRangeMaximum = 8 });

        // Assert
        Assert.Equal(1, both.Scale.Minimum);
        Assert.Equal(8, both.Scale.Maximum);
        Assert.True(both.HasNormalRange);
        Assert.Equal(3, upper.Scale.Minimum);
        Assert.Equal(8, upper.Scale.Maximum);
        Assert.True(Double.IsNaN(upper.NormalLow));
        Assert.True(upper.HasNormalRange);
        Assert.False(winLoss.HasNormalRange);
    }

    [Fact]
    public void ThresholdsAreSortedAndDoNotWidenTheRange()
    {
        // Arrange
        var good = new ChartThreshold { Level = ChartThresholdLevel.Good };
        var warning = new ChartThreshold { Value = 4, Level = ChartThresholdLevel.Warning };
        var critical = new ChartThreshold { Value = 8, Level = ChartThresholdLevel.Critical };

        // Act
        var layout = Layout([3, 5, 4], new SparklineLayoutOptions { Thresholds = [critical, good, warning] });

        // Assert
        Assert.Equal([good, warning, critical], layout.Thresholds);
        Assert.Equal(5, layout.Scale.Maximum);
    }

    [Fact]
    public void MarkerIndicesSkipMissingValues()
    {
        // Act
        var layout = Layout([Double.NaN, 4, 9, 2, 9, 2, Double.NaN]);

        // Assert
        Assert.Equal(1, layout.FirstIndex);
        Assert.Equal(5, layout.LastIndex);
        Assert.Equal(2, layout.HighIndex);
        Assert.Equal(3, layout.LowIndex);
    }

    [Fact]
    public void NoValueKeepsTheIndicesEmpty()
    {
        // Act
        var layout = Layout([Double.NaN]);

        // Assert
        Assert.Equal(-1, layout.FirstIndex);
        Assert.Equal(new SparklineScale(0, 1, 0), layout.Scale);
    }

    [Theory]
    [InlineData(20, null, 1.5)]
    [InlineData(24, null, 2)]
    [InlineData(20, 3d, 3)]
    public void ThicknessFollowsTheHeight(double height, double? thickness, double expected)
    {
        // Act
        var layout = Layout([1, 2], new SparklineLayoutOptions { Thickness = thickness }, 100, height);

        // Assert
        Assert.Equal(expected, layout.Thickness);
    }

    [Fact]
    public void PlotLeavesRoomForMarkersAndLineCaps()
    {
        // Act
        var markers = Layout([1, 2], new SparklineLayoutOptions { Thickness = 2 });
        var plain = Layout([1, 2], new SparklineLayoutOptions { Thickness = 2, Markers = ChartSparklineMarkers.None });
        var bars = Layout([1, 2], new SparklineLayoutOptions { Thickness = 2, Kind = ChartSparklineKind.Bar });

        // Assert
        Assert.Equal(2.5f, markers.MarkerRadius);
        Assert.Equal(1f, markers.MarkerKnockout);
        Assert.Equal(new ChartRect(3.5, 3.5, 93, 33), markers.Plot);
        Assert.Equal(new ChartRect(1, 1, 98, 38), plain.Plot);
        Assert.Equal(new ChartRect(0, 0, 100, 40), bars.Plot);
        Assert.Equal(ChartSparklineMarkers.None, bars.Markers);
    }

    [Fact]
    public void EdgeToEdgeUsesTheWholeBounds()
    {
        // Act
        var layout = Layout([1, 2], new SparklineLayoutOptions { Thickness = 2, EdgeToEdge = true });

        // Assert
        Assert.Equal(new ChartRect(0, 0, 100, 40), layout.Plot);
        Assert.Equal(ChartSparklineMarkers.Last, layout.Markers);
    }

    [Fact]
    public void PositionsSpanThePlot()
    {
        // Arrange
        var line = Layout([1, 2, 3], new SparklineLayoutOptions { Markers = ChartSparklineMarkers.None, Thickness = 0 });
        var bars = Layout([1, 2, 3, 4], new SparklineLayoutOptions { Kind = ChartSparklineKind.Bar });
        var single = Layout([1], new SparklineLayoutOptions { Markers = ChartSparklineMarkers.None, Thickness = 0 });

        // Act & Assert
        Assert.Equal(0, line.ToX(0, line.Scale), Precision);
        Assert.Equal(100, line.ToX(2, line.Scale), Precision);
        Assert.Equal(12.5, bars.ToX(0, bars.Scale), Precision);
        Assert.Equal(25, bars.GetSlot(bars.Scale), Precision);
        Assert.Equal(50, single.ToX(0, single.Scale), Precision);
        Assert.Equal(40, line.ToY(1, line.Scale), Precision);
        Assert.Equal(0, line.ToY(3, line.Scale), Precision);
    }

    private static SparklineLayout Layout(double[] values, SparklineLayoutOptions? options = null, double width = 100, double height = 40)
    {
        var layout = new SparklineLayout();
        layout.Update(width, height, new Thickness(0), new ChartStyle(), options ?? new SparklineLayoutOptions(), new SparklineValues(values, null));
        return layout;
    }
}
