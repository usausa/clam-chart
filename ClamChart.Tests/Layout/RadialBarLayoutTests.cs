namespace ClamChart.Tests.Layout;

public sealed class RadialBarLayoutTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void RingsStartAtTheOutsideWithTheThicknessOfTheGrafanaGauge()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50), Entry(1, "B", 25), Entry(2, "C", 75)]);

        // Assert
        Assert.Equal(150, layout.CenterX);
        Assert.Equal(150, layout.CenterY);
        Assert.Equal(150, layout.OuterRadius);
        Assert.Equal(25, layout.Thickness, Precision);
        Assert.Equal(137.5, layout.GetRingRadius(0), Precision);
        Assert.Equal(108.5, layout.GetRingRadius(1), Precision);
        Assert.Equal(79.5, layout.GetRingRadius(2), Precision);
        Assert.Equal(67, layout.GetInnerEdge(), Precision);
        Assert.Equal([0, 1, 2], layout.Rows.Select(static x => x.Slot));
    }

    [Fact]
    public void ManyRingsShareTheSpaceOutsideTheInnerRadius()
    {
        // Act
        var layout = Layout([.. Enumerable.Range(0, 6).Select(static x => Entry(x, null, 50))]);

        // Assert
        Assert.Equal((90d - (4 * 5)) / 6, layout.Thickness, Precision);
        Assert.Equal(60, layout.GetInnerEdge(), Precision);
    }

    [Fact]
    public void ThicknessAndSpacingCanBeGiven()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50), Entry(1, "B", 25)], new RadialBarLayoutOptions { RingThickness = 10, RingSpacing = 2 });

        // Assert
        Assert.Equal(10, layout.Thickness);
        Assert.Equal(145, layout.GetRingRadius(0), Precision);
        Assert.Equal(133, layout.GetRingRadius(1), Precision);
    }

    [Fact]
    public void RatiosUseTheItemMaximumAndClampUnlessTheyWrap()
    {
        // Arrange
        GaugeEntry[] entries = [Entry(0, "A", 50), Entry(1, "B", 50, 200), Entry(2, "C", 150)];

        // Act
        var clamp = Layout(entries);
        var wrap = Layout(entries, new RadialBarLayoutOptions { OverflowMode = ChartOverflowMode.Wrap });
        var segmented = Layout(entries, new RadialBarLayoutOptions { OverflowMode = ChartOverflowMode.Wrap, DisplayMode = ChartRadialBarMode.Segmented });

        // Assert
        Assert.Equal([0.5, 0.25, 1], clamp.Rows.Select(static x => x.Ratio));
        Assert.Equal([0.5, 0.25, 1.5], wrap.Rows.Select(static x => x.Ratio));
        Assert.Equal(1, segmented.Rows[2].Ratio);
        Assert.Equal(["50%", "25%", "150%"], clamp.Rows.Select(static x => x.PercentText));
        Assert.Equal(["50", "50", "150"], clamp.Rows.Select(static x => x.ValueText));
        Assert.Equal(150, clamp.Rows[2].Percent, Precision);
    }

    [Fact]
    public void InvalidValuesDrawOnlyTheTrack()
    {
        // Act
        var layout = Layout([Entry(0, "A", Double.NaN), Entry(1, "B", 10, 0)]);

        // Assert
        Assert.All(layout.Rows, static x => Assert.Equal(0, x.Ratio));
        Assert.All(layout.Rows, static x => Assert.True(Double.IsNaN(x.Percent)));
        Assert.All(layout.Rows, static x => Assert.Null(x.PercentText));
        Assert.False(layout.Center.IsVisible);
    }

    [Fact]
    public void AutoCenterShowsTheOutermostRing()
    {
        // Act
        var layout = Layout([Entry(0, "C0", 50), Entry(1, "C1", 25)], new RadialBarLayoutOptions { CenterValueMode = ChartCenterValueMode.Auto });

        // Assert
        var center = layout.Center;
        Assert.Equal("50%", center.Value);
        Assert.Equal("C0", center.Caption);
        Assert.Equal(32, center.ValueFontSize);
        Assert.Equal(12, center.CaptionFontSize);
        Assert.Equal(new ChartRect(126, 125, 48, 34), center.ValueBounds);
        Assert.Equal(new ChartRect(144, 161, 12, 14), center.CaptionBounds);
        Assert.Equal(0.5, center.Number, Precision);
        Assert.True(center.IsPercent);
        Assert.Equal(0, layout.GetCenterKey(-1));
    }

    [Fact]
    public void CenterTextAndLabelReplaceTheDefault()
    {
        // Act
        var text = Layout([Entry(0, "C0", 50)], new RadialBarLayoutOptions { CenterValueMode = ChartCenterValueMode.Auto, CenterText = "3/4", CenterLabel = "Done" });
        var label = Layout([Entry(0, "C0", 50)], new RadialBarLayoutOptions { CenterValueMode = ChartCenterValueMode.Auto, CenterLabel = "Goal" });

        // Assert
        Assert.Equal("3/4", text.Center.Value);
        Assert.Equal("Done", text.Center.Caption);
        Assert.True(Double.IsNaN(text.Center.Number));
        Assert.Equal(RadialBarLayout.DefaultCenterKey, text.GetCenterKey(-1));
        Assert.Equal("50%", label.Center.Value);
        Assert.Equal("Goal", label.Center.Caption);
        Assert.Equal(RadialBarLayout.DefaultCenterKey, label.GetCenterKey(-1));
    }

    [Fact]
    public void OtherCenterModesFollowTheMode()
    {
        // Arrange
        GaugeEntry[] entries = [Entry(0, "C0", 50), Entry(1, "C1", 25)];

        // Act
        var total = Layout(entries, new RadialBarLayoutOptions { CenterValueMode = ChartCenterValueMode.Total, CenterLabel = "Sum" });
        var selected = Layout(entries, new RadialBarLayoutOptions { CenterValueMode = ChartCenterValueMode.Selected, Selectable = true });
        var none = Layout(entries, new RadialBarLayoutOptions { CenterValueMode = ChartCenterValueMode.None, Selectable = true });

        // Assert
        Assert.Equal("75", total.Center.Value);
        Assert.Equal("Sum", total.Center.Caption);
        Assert.False(total.Center.IsPercent);
        Assert.False(selected.Center.IsVisible);
        Assert.Equal("25%", selected.GetCenter(1).Value);
        Assert.Equal("C1", selected.GetCenter(1).Caption);
        Assert.Equal(1, selected.GetCenterKey(1));
        Assert.False(none.Center.IsVisible);
        Assert.False(none.GetCenter(1).IsVisible);
    }

    [Fact]
    public void LegendTakesTheRightSideByDefault()
    {
        // Act
        var layout = Layout([Entry(0, "Move", 50), Entry(1, "Stand", 25)], new RadialBarLayoutOptions { LegendVisible = true }, 400, 200);

        // Assert
        Assert.True(layout.Legend.IsVisible);
        Assert.Equal(2, layout.Legend.Items.Count);
        Assert.True(layout.RingBounds.Right <= layout.Legend.Bounds.X - RadialBarLayout.LegendGap + Precision);
        Assert.Equal(100, layout.OuterRadius, Precision);
    }

    [Fact]
    public void LegendValuesFollowTheMode()
    {
        // Act
        var layout = Layout([Entry(0, "Move", 50)], new RadialBarLayoutOptions { LegendVisible = true, LegendPosition = ChartLegendPosition.Bottom, LegendValueMode = ChartLegendValueMode.Percent }, 300, 300);

        // Assert
        Assert.False(layout.Legend.Items[0].Value.IsEmpty);
        Assert.True(layout.RingBounds.Bottom < 300);
    }

    [Fact]
    public void HitsFindTheRingUnderThePoint()
    {
        // Arrange
        var layout = Layout([Entry(0, "A", 50), Entry(1, "B", 25)]);

        // Act & Assert
        Assert.Equal(0, layout.FindRingAt(150, 150 - 137.5));
        Assert.Equal(1, layout.FindRingAt(150 + 108.5, 150));
        Assert.Equal(0, layout.FindRingAt(150, 150 - 123));
        Assert.Equal(1, layout.FindRingAt(150, 150 - 122));
        Assert.Equal(-1, layout.FindRingAt(150, 150));
        Assert.Equal(new ChartHit(ChartHitKind.Item, -1, 1), layout.HitTest(150 - 108.5, 150));
    }

    [Fact]
    public void PartialSweepsHitOnlyInsideTheSweep()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50)], new RadialBarLayoutOptions { StartAngle = 270, SweepAngle = 180 });

        // Assert
        Assert.Equal(0, layout.FindRingAt(layout.CenterX, layout.CenterY - layout.GetRingRadius(0)));
        Assert.Equal(-1, layout.FindRingAt(layout.CenterX, layout.CenterY + layout.GetRingRadius(0)));
    }

    [Fact]
    public void RoundCapsOnAPartialSweepLeaveRoomForTheCaps()
    {
        // Act
        var flat = Layout([Entry(0, "A", 50)], new RadialBarLayoutOptions { StartAngle = 270, SweepAngle = 180 });
        var round = Layout([Entry(0, "A", 50)], new RadialBarLayoutOptions { StartAngle = 270, SweepAngle = 180, CapStyle = ChartCapStyle.Round });
        var circle = Layout([Entry(0, "A", 50)], new RadialBarLayoutOptions { CapStyle = ChartCapStyle.Round });

        // Assert
        Assert.True(round.OuterRadius < flat.OuterRadius);
        Assert.Equal(flat.OuterRadius - (flat.Thickness / 2), round.OuterRadius, Precision);
        Assert.Equal(150, circle.OuterRadius);
    }

    [Fact]
    public void GlowLeavesRoomAroundTheRings()
    {
        // Act
        var layout = Layout([Entry(0, "A", 50)], new RadialBarLayoutOptions { BarGlow = true });

        // Assert
        Assert.Equal(150, layout.CenterX);
        Assert.Equal(150, layout.CenterY);
        Assert.Equal(150 - (300 * 0.02), layout.OuterRadius, Precision);
        Assert.Equal(layout.OuterRadius / 6, layout.Thickness, Precision);
        Assert.Equal(0.75 + (layout.Thickness * 0.08), RadialBarLayout.GetGlowSigma(layout.Thickness), Precision);
    }

    [Fact]
    public void ThresholdMarkersTakeAThinRingOutsideTheBars()
    {
        // Act
        var plain = Layout([Entry(0, "A", 50)]);
        var marked = Layout([Entry(0, "A", 50)], new RadialBarLayoutOptions { ThresholdMarkers = true });

        // Assert
        var (width, spacing) = RadialBarLayout.GetMarkerSize(plain.Thickness);
        Assert.Equal(Math.Clamp(0.2 * Math.Pow(25, 0.92), 4, 12), width, Precision);
        Assert.Equal(Math.Clamp(width / 2, 2, 12), spacing, Precision);
        Assert.Equal(0, plain.MarkerWidth);
        Assert.Equal(width, marked.MarkerWidth, Precision);
        Assert.Equal(150 - (width / 2), marked.MarkerRadius, Precision);
        Assert.Equal(150 - width - spacing, marked.OuterRadius, Precision);
        Assert.Equal(marked.OuterRadius / 6, marked.Thickness, Precision);
        Assert.Equal(150, marked.CenterX);
        Assert.Equal(150, marked.CenterY);
    }

    [Fact]
    public void ThresholdMarkersGrowWithTheBarsUpToTwelve()
    {
        // Act & Assert
        Assert.Equal((4d, 2d), RadialBarLayout.GetMarkerSize(8));
        Assert.Equal(0.2 * Math.Pow(60, 0.92), RadialBarLayout.GetMarkerSize(60).Width, Precision);
        Assert.Equal((12d, 6d), RadialBarLayout.GetMarkerSize(400));
    }

    private static GaugeEntry Entry(int index, string? label, double value, double maximum = Double.NaN) =>
        new(index, label, value, maximum, Double.NaN, null);

    private static RadialBarLayout Layout(GaugeEntry[] entries, RadialBarLayoutOptions? options = null, double width = 300, double height = 300)
    {
        var layout = new RadialBarLayout();
        layout.Update(width, height, new Thickness(0), new ChartStyle(), Culture, options ?? new RadialBarLayoutOptions(), entries, Measurer);
        return layout;
    }
}
