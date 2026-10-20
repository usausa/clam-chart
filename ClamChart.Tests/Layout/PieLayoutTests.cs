namespace ClamChart.Tests.Layout;

public sealed class PieLayoutTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly ChartValueReader Reader = new(new ChartValueOptions());

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void SlicesFollowTheDataOrderAndFillTheSweep()
    {
        // Act
        var layout = Layout([10, 30, 60]);

        // Assert
        Assert.Equal(100, layout.Total);
        Assert.Equal([0, 1, 2], layout.Slices.Select(static x => x.DataIndex));
        Assert.Equal([0, 0.1, 0.4], layout.Slices.Select(static x => Math.Round(x.Start, Precision)));
        Assert.Equal([0.1, 0.4, 1], layout.Slices.Select(static x => Math.Round(x.End, Precision)));
        Assert.Equal([0, 1, 2], layout.Slices.Select(static x => x.ColorRank));
        Assert.Equal(["C0", "C1", "C2"], layout.Slices.Select(static x => x.Label));
        Assert.Equal("10%", layout.Slices[0].PercentText);
        Assert.Equal("30", layout.Slices[1].ValueText);
    }

    [Fact]
    public void DescendingSortKeepsTheColorsOfTheDataOrder()
    {
        // Act
        var layout = Layout([10, 30, 60], new PieLayoutOptions { SortMode = ChartSortMode.Descending });

        // Assert
        Assert.Equal([2, 1, 0], layout.Slices.Select(static x => x.DataIndex));
        Assert.Equal([2, 1, 0], layout.Slices.Select(static x => x.ColorRank));
        Assert.Equal(0.6, layout.Slices[0].End, Precision);
    }

    [Fact]
    public void AscendingSortPutsTheSmallestSliceFirst()
    {
        // Act
        var layout = Layout([30, 10, 50, 10], new PieLayoutOptions { SortMode = ChartSortMode.Ascending });

        // Assert
        Assert.Equal([1, 3, 0, 2], layout.Slices.Select(static x => x.DataIndex));
        Assert.Equal(0.1, layout.Slices[0].End, Precision);
    }

    [Fact]
    public void ValuesThatCannotBeDrawnAreSkipped()
    {
        // Act
        var layout = Layout([10, Double.NaN, -5, 0, 30]);

        // Assert
        Assert.Equal([0, 4], layout.Slices.Select(static x => x.DataIndex));
        Assert.Equal(40, layout.Total);
        Assert.True(layout.HasNegativeValues);
    }

    [Fact]
    public void NoDrawableValueMakesNoSlice()
    {
        // Act
        var layout = Layout([0, -1]);

        // Assert
        Assert.Empty(layout.Slices);
        Assert.Equal(0, layout.Total);
    }

    [Fact]
    public void MaxSlicesKeepsTheLargestAndGroupsTheRestIntoOthers()
    {
        // Act
        var layout = Layout([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], new PieLayoutOptions { MaxSlices = 4 });

        // Assert
        Assert.Equal([7, 8, 9, -1], layout.Slices.Select(static x => x.DataIndex));
        Assert.Equal([0, 1, 2, -1], layout.Slices.Select(static x => x.ColorRank));
        var others = layout.Slices[3];
        Assert.True(others.IsOthers);
        Assert.Equal(28, others.Value);
        Assert.Equal("Other", others.Label);
        Assert.Null(others.Color);
        Assert.Equal(1, others.End);
    }

    [Fact]
    public void ExactlyMaxSlicesIsNotGrouped()
    {
        // Act
        var layout = Layout([1, 2, 3, 4, 5, 6, 7, 8]);

        // Assert
        Assert.Equal(8, layout.Slices.Count);
        Assert.DoesNotContain(layout.Slices, static x => x.IsOthers);
    }

    [Fact]
    public void ThresholdGroupsSmallSlicesOnlyWhenThereAreTwoOrMore()
    {
        // Arrange
        var options = new PieLayoutOptions { OthersThreshold = 0.05 };

        // Act
        var grouped = Layout([50, 45, 3, 2], options);
        var single = Layout([50, 47, 3], options);

        // Assert
        Assert.Equal([0, 1, -1], grouped.Slices.Select(static x => x.DataIndex));
        Assert.Equal(5, grouped.Slices[2].Value);
        Assert.Equal([0, 1, 2], single.Slices.Select(static x => x.DataIndex));
    }

    [Fact]
    public void ColorRanksSkipTheGroupedItems()
    {
        // Act
        var layout = Layout([1, 50, 2, 40], new PieLayoutOptions { OthersThreshold = 0.05 });

        // Assert
        Assert.Equal([1, 3, -1], layout.Slices.Select(static x => x.DataIndex));
        Assert.Equal([0, 1, -1], layout.Slices.Select(static x => x.ColorRank));
    }

    [Fact]
    public void ExplicitColorsAreKept()
    {
        // Arrange
        using var data = DataSnapshotBuilder.Build(new List<ChartEntry> { new("A", 1, Colors.Lime), new("B", 3) }, Reader, ChartEmptyPointMode.Gap);

        // Act
        var layout = Layout(data);

        // Assert
        Assert.Equal(Colors.Lime, layout.Slices[0].Color);
        Assert.Null(layout.Slices[1].Color);
    }

    [Fact]
    public void FullCircleIsCenteredInTheArea()
    {
        // Act
        var layout = Layout([1, 1], width: 200, height: 100);

        // Assert
        Assert.Equal(100, layout.CenterX, Precision);
        Assert.Equal(50, layout.CenterY, Precision);
        Assert.Equal(50, layout.OuterRadius, Precision);
        Assert.Equal(30, layout.InnerRadius, Precision);
    }

    [Fact]
    public void UpperSemicircleSitsOnTheBottomOfTheArea()
    {
        // Act
        var layout = Layout([1, 1], new PieLayoutOptions { StartAngle = 270, SweepAngle = 180 }, 200, 100);

        // Assert
        Assert.Equal(100, layout.OuterRadius, 6);
        Assert.Equal(100, layout.CenterX, 6);
        Assert.Equal(100, layout.CenterY, 6);
    }

    [Fact]
    public void ExplodeMarginShrinksThePie()
    {
        // Act
        var layout = Layout([1, 1], new PieLayoutOptions { ExplodeMargin = 6 });

        // Assert
        Assert.Equal(94, layout.OuterRadius, Precision);
        Assert.Equal(100, layout.CenterX, Precision);
    }

    [Theory]
    [InlineData(270, 180, -1, -1, 1, 0)]
    [InlineData(0, 90, 0, -1, 1, 0)]
    [InlineData(225, 270, -1, -1, 1, 0.7071)]
    [InlineData(0, 360, -1, -1, 1, 1)]
    public void UnitBoundsCoverTheSweep(double start, double sweep, double left, double top, double right, double bottom)
    {
        // Act
        var (actualLeft, actualTop, actualRight, actualBottom) = PieLayout.GetUnitBounds(start, sweep, 0.6);

        // Assert
        Assert.Equal(left, actualLeft, 4);
        Assert.Equal(top, actualTop, 4);
        Assert.Equal(right, actualRight, 4);
        Assert.Equal(bottom, actualBottom, 4);
    }

    [Fact]
    public void LegendBelowTakesItsRowsFromTheArea()
    {
        // Act
        var layout = Layout([1, 1], new PieLayoutOptions { LegendVisible = true });

        // Assert
        Assert.True(layout.Legend.IsVisible);
        Assert.Equal(2, layout.Legend.Items.Count);
        Assert.Equal(186, layout.Legend.Bounds.Y);
        Assert.Equal(new ChartRect(0, 0, 200, 178), layout.PieBounds);
        Assert.Equal(89, layout.OuterRadius, Precision);
    }

    [Fact]
    public void LegendOnTheRightIsCenteredBesideThePie()
    {
        // Act
        var layout = Layout([1, 1], new PieLayoutOptions { LegendVisible = true, LegendPosition = ChartLegendPosition.Right, LegendValueMode = ChartLegendValueMode.Percent }, 300);

        // Assert
        var items = layout.Legend.Items;
        Assert.Equal(300 - layout.Legend.Bounds.Width, layout.Legend.Bounds.X);
        Assert.Equal((200 - layout.Legend.Bounds.Height) / 2, layout.Legend.Bounds.Y);
        Assert.Equal(items[0].Bounds.Right, items[0].Value.Right);
        Assert.Equal(items[0].Value.Right, items[1].Value.Right);
        Assert.Equal(layout.Legend.Bounds.X - PieLayout.LegendGap, layout.PieBounds.Right);
    }

    [Fact]
    public void LegendOnTheSideIsHiddenWhenThePieWouldBeTooNarrow()
    {
        // Act
        var layout = Layout([1, 1], new PieLayoutOptions { LegendVisible = true, LegendPosition = ChartLegendPosition.Left }, 30);

        // Assert
        Assert.False(layout.Legend.IsVisible);
        Assert.Equal(layout.Bounds, layout.PieBounds);
    }

    [Fact]
    public void SlicesWithoutALabelAreLeftOutOfTheLegend()
    {
        // Arrange
        using var data = DataSnapshotBuilder.Build(new List<ChartEntry> { new(null, 1), new("B", 3) }, Reader, ChartEmptyPointMode.Gap);

        // Act
        var layout = Layout(data, new PieLayoutOptions { LegendVisible = true });

        // Assert
        Assert.Equal([1], layout.Legend.Items.Select(static x => x.SeriesIndex));
    }

    [Fact]
    public void CenterShowsTheTotalWithTheLabel()
    {
        // Act
        var layout = Layout([40, 60], new PieLayoutOptions { CenterLabel = "Total" });

        // Assert
        var center = layout.Center;
        Assert.Equal("100", center.Value);
        Assert.Equal("Total", center.Caption);
        Assert.Equal(30, center.ValueFontSize);
        Assert.Equal(12, center.CaptionFontSize);
        Assert.Equal(new ChartRect(77.5, 76, 45, 32), center.ValueBounds);
        Assert.Equal(new ChartRect(85, 110, 30, 14), center.CaptionBounds);
    }

    [Fact]
    public void CenterTextShrinksToFitTheHole()
    {
        // Act
        var layout = Layout([40, 60], new PieLayoutOptions { CenterValue = 123456789, CenterFormat = ChartTextFormat.Parse("N0") });

        // Assert
        var center = layout.Center;
        Assert.Equal("123,456,789", center.Value);
        Assert.InRange(center.ValueFontSize, PieLayout.MinCenterFontSize, 29);
        var reach = Math.Max(Math.Abs(center.ValueBounds.Y - layout.CenterY), Math.Abs(center.ValueBounds.Bottom - layout.CenterY));
        Assert.True(Math.Sqrt(Math.Pow(center.ValueBounds.Width / 2, 2) + Math.Pow(reach, 2)) <= layout.InnerRadius - PieLayout.CenterPadding);
    }

    [Fact]
    public void CenterOfASemicircleStaysAboveTheFlatEdge()
    {
        // Act
        var layout = Layout([1, 1], new PieLayoutOptions { StartAngle = 270, SweepAngle = 180, CenterLabel = "Total" }, 200, 100);

        // Assert
        var center = layout.Center;
        Assert.True(center.IsVisible);
        Assert.Equal(100, center.CaptionBounds.Bottom, 6);
        Assert.True(center.ValueBounds.Y < center.CaptionBounds.Y);
    }

    [Fact]
    public void SelectableSlicesGetTheirValueWithTheNameAndPercent()
    {
        // Act
        var layout = Layout([40, 60], new PieLayoutOptions { Selectable = true });

        // Assert
        Assert.Equal(["40", "60"], layout.SliceCenters.Select(static x => x.Value));
        Assert.Equal(["C0 40%", "C1 60%"], layout.SliceCenters.Select(static x => x.Caption));
    }

    [Fact]
    public void CenterFollowsTheMode()
    {
        // Act
        var total = Layout([40, 60], new PieLayoutOptions { Selectable = true, CenterValueMode = ChartCenterValueMode.Total });
        var selected = Layout([40, 60], new PieLayoutOptions { Selectable = true, CenterValueMode = ChartCenterValueMode.Selected });
        var none = Layout([40, 60], new PieLayoutOptions { Selectable = true, CenterValueMode = ChartCenterValueMode.None });
        var pie = Layout([40, 60], new PieLayoutOptions { InnerRadius = 0 });

        // Assert
        Assert.True(total.Center.IsVisible);
        Assert.Empty(total.SliceCenters);
        Assert.False(selected.Center.IsVisible);
        Assert.Equal(2, selected.SliceCenters.Count);
        Assert.False(none.Center.IsVisible);
        Assert.Empty(none.SliceCenters);
        Assert.False(pie.Center.IsVisible);
    }

    [Fact]
    public void InsideLabelsFollowTheContent()
    {
        // Arrange
        static PieLayout Inside(ChartPieLabelContent content) =>
            Layout([40, 60], new PieLayoutOptions { LabelsMode = ChartPieLabelsMode.Inside, LabelContent = content });

        // Act & Assert
        Assert.Equal("40%", Inside(ChartPieLabelContent.Percent).Slices[0].InsideText);
        Assert.Equal("40", Inside(ChartPieLabelContent.Value).Slices[0].InsideText);
        Assert.Equal("C0", Inside(ChartPieLabelContent.Label).Slices[0].InsideText);
        Assert.Equal("C0 40%", Inside(ChartPieLabelContent.LabelAndPercent).Slices[0].InsideText);
        Assert.Equal(16.5, Inside(ChartPieLabelContent.Percent).Slices[0].InsideWidth);
        Assert.Null(Layout([40, 60]).Slices[0].InsideText);
    }

    [Fact]
    public void ValueFormatIsUsedForLabelsAndTheLegend()
    {
        // Act
        var layout = Layout([1234.5], new PieLayoutOptions { ValueFormat = ChartTextFormat.Parse("{0:N1} pt") });

        // Assert
        Assert.Equal("1,234.5 pt", layout.Slices[0].ValueText);
    }

    [Fact]
    public void HitTestFindsSlicesByTheirAngle()
    {
        // Arrange
        var layout = Layout([25, 75]);

        // Act & Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, -1, 0), layout.HitTest(Polar(layout, 45, 80).X, Polar(layout, 45, 80).Y, -1, 0));
        Assert.Equal(new ChartHit(ChartHitKind.Item, -1, 1), layout.HitTest(Polar(layout, 180, 80).X, Polar(layout, 180, 80).Y, -1, 0));
        Assert.Equal(new ChartHit(ChartHitKind.Item, -1, 0), layout.HitTest(Polar(layout, 0, 95).X, Polar(layout, 0, 95).Y, -1, 0));
        Assert.Equal(ChartHit.None, layout.HitTest(100, 100, -1, 0));
        Assert.Equal(ChartHit.None, layout.HitTest(199, 199, -1, 0));
    }

    [Fact]
    public void HitTestIgnoresTheOpenPartOfTheSweep()
    {
        // Arrange
        var layout = Layout([1, 1], new PieLayoutOptions { StartAngle = 270, SweepAngle = 180, InnerRadius = 0 });
        var (belowX, belowY) = Polar(layout, 180, layout.OuterRadius / 2);
        var (aboveX, aboveY) = Polar(layout, 300, layout.OuterRadius / 2);

        // Act & Assert
        Assert.Equal(ChartHit.None, layout.HitTest(belowX, belowY, -1, 0));
        Assert.Equal(new ChartHit(ChartHitKind.Item, -1, 0), layout.HitTest(aboveX, aboveY, -1, 0));
    }

    [Fact]
    public void ExplodedSliceIsHitWhereItMoved()
    {
        // Arrange
        var layout = Layout([25, 75]);
        var (movedX, movedY) = Polar(layout, 45, 103);
        var (vacatedX, vacatedY) = Polar(layout, 45, 61);

        // Act & Assert
        Assert.Equal(new ChartHit(ChartHitKind.Item, -1, 0), layout.HitTest(movedX, movedY, 0, 6));
        Assert.Equal(ChartHit.None, layout.HitTest(movedX, movedY, -1, 0));
        Assert.Equal(ChartHit.None, layout.HitTest(vacatedX, vacatedY, 0, 6));
    }

    [Fact]
    public void LegendIsHitBeforeTheSlices()
    {
        // Arrange
        var layout = Layout([1, 1], new PieLayoutOptions { LegendVisible = true });
        var item = layout.Legend.Items[1].Bounds;

        // Act
        var hit = layout.HitTest(item.CenterX, item.CenterY, -1, 0);

        // Assert
        Assert.Equal(new ChartHit(ChartHitKind.Legend, -1, 1), hit);
    }

    [Fact]
    public void FindSliceReturnsThePositionOfTheDataIndex()
    {
        // Arrange
        var layout = Layout([10, 30, 60], new PieLayoutOptions { SortMode = ChartSortMode.Descending });

        // Act & Assert
        Assert.Equal(2, layout.FindSlice(0));
        Assert.Equal(0, layout.FindSlice(2));
        Assert.Equal(-1, layout.FindSlice(5));
        Assert.Equal(-1, layout.FindSlice(-1));
    }

    private static (double X, double Y) Polar(PieLayout layout, double angle, double radius)
    {
        var (x, y) = PieLayout.GetDirection(angle);
        return (layout.CenterX + (x * radius), layout.CenterY + (y * radius));
    }

    private static PieLayout Layout(double[] values, PieLayoutOptions? options = null, double width = 200, double height = 200)
    {
        using var data = DataSnapshotBuilder.Build(values.Select(static (value, index) => new ChartEntry($"C{index}", value)).ToList(), Reader, ChartEmptyPointMode.Gap);
        return Layout(data, options, width, height);
    }

    private static PieLayout Layout(DataSnapshot data, PieLayoutOptions? options = null, double width = 200, double height = 200)
    {
        var layout = new PieLayout();
        layout.Update(width, height, new Thickness(0), new ChartStyle(), Culture, options ?? new PieLayoutOptions(), data, Measurer);
        return layout;
    }
}
