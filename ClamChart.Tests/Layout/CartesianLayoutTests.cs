namespace ClamChart.Tests.Layout;

public sealed class CartesianLayoutTests
{
    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CartesianLayoutOptions CategoryBars = new()
    {
        XAxis = new AxisLayoutOptions { Kind = ChartAxisKind.Category },
        YAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { IncludeZero = true } }
    };

    private static readonly CartesianLayoutOptions Exact = new()
    {
        XAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { NiceRange = false } },
        YAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { NiceRange = false } }
    };

    [Fact]
    public void PlotLeavesRoomForTheAxisLabels()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 20), ("Mar", 30)]);

        // Act
        using var layout = Update(set, CategoryBars);

        // Assert
        Assert.Equal(new ChartRect(8, 8, 344, 224), layout.Bounds);
        Assert.Equal(new ChartRect(23, 14.5, 329, 198), layout.PlotBounds);
        Assert.Equal(new AxisScale(0, 30, 10), layout.YAxis.Scale);
        Assert.Equal(["0", "10", "20", "30"], layout.YAxis.Labels.Select(static x => x.Text));
        Assert.Equal(new ChartRect(13.5, 206, 5.5, 13), layout.YAxis.Labels[0].Bounds);
        Assert.Equal(new ChartRect(8, 8, 11, 13), layout.YAxis.Labels[3].Bounds);
        Assert.Equal(["Jan", "Feb", "Mar"], layout.XAxis.Labels.Select(static x => x.Text));
        Assert.Equal(new ChartRect(179.25, 219, 16.5, 13), layout.XAxis.Labels[1].Bounds);
    }

    [Fact]
    public void LegendRowsAreReservedAtTheBottom()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10), ("Feb", 20)], title: "Sales");
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 5), ("Feb", 6)], title: "Cost");

        // Act
        using var layout = Update(set, CategoryBars with { LegendVisible = true });

        // Assert
        Assert.Equal(14.5, layout.PlotBounds.Y);
        Assert.Equal(176, layout.PlotBounds.Height);
        Assert.True(layout.Legend.IsVisible);
        Assert.Equal(new ChartRect(8, 218, 46, 14), layout.Legend.Items[0].Bounds);
        Assert.Equal(new ChartRect(66, 218, 36, 14), layout.Legend.Items[1].Bounds);
    }

    [Fact]
    public void LegendAtTheTopPushesThePlotDown()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10)], title: "Sales");

        // Act
        using var layout = Update(set, CategoryBars with { LegendVisible = true, LegendPosition = ChartLegendPosition.Top });

        // Assert
        Assert.Equal(8, layout.Legend.Items[0].Bounds.Y);
        Assert.Equal(36.5, layout.PlotBounds.Y);
    }

    [Fact]
    public void SeriesWithoutTitleAreNotInTheLegend()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10)]);

        // Act
        using var layout = Update(set, CategoryBars with { LegendVisible = true });

        // Assert
        Assert.False(layout.Legend.IsVisible);
        Assert.Equal(198, layout.PlotBounds.Height);
    }

    [Fact]
    public void TitlesUseARowAboveThePlotAndBelowTheLabels()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 20), ("Mar", 30)]);
        var options = CategoryBars with
        {
            XAxis = CategoryBars.XAxis with { Title = "Month" },
            YAxis = CategoryBars.YAxis with { Title = "Sales" }
        };

        // Act
        using var layout = Update(set, options);

        // Assert
        Assert.Equal(new ChartRect(23, 31.5, 329, 164), layout.PlotBounds);
        Assert.Equal(new ChartRect(8, 8, 27.5, 13), layout.YAxis.TitleBounds);
        Assert.Equal(new ChartRect(173.75, 219, 27.5, 13), layout.XAxis.TitleBounds);
        Assert.Empty(layout.YAxis.TitleMarkers);
    }

    [Fact]
    public void SecondaryAxisAlignsToThePrimaryAndShowsTheColorsOfItsSeries()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [0, 50, 100], title: "Temp");
        set.AddValues(ChartSeriesKind.Area, [10, 37, 20], axis: ChartAxisTarget.Secondary, title: "Humidity");
        var options = new CartesianLayoutOptions
        {
            YAxis = new AxisLayoutOptions { Title = "Temp" },
            SecondaryYAxis = new AxisLayoutOptions { Title = "Humidity" }
        };

        // Act
        using var layout = Update(set, options);

        // Assert
        Assert.Equal(new ChartRect(28.5, 31.5, 308.5, 181), layout.PlotBounds);
        Assert.Equal(new AxisScale(0, 100, 25), layout.YAxis.Scale);
        Assert.Equal(new AxisScale(10, 50, 10), layout.SecondaryYAxis.Scale);
        Assert.Equal([10d, 20, 30, 40, 50], layout.SecondaryYAxis.Ticks);
        Assert.Equal(AxisSide.Right, layout.SecondaryYAxis.Side);
        Assert.Equal(new ChartRect(20, 8, 22, 13), layout.YAxis.TitleBounds);
        Assert.Equal([new ChartRect(8, 10.5, 8, 8)], layout.YAxis.TitleMarkers);
        Assert.Equal(new ChartRect(296, 8, 44, 13), layout.SecondaryYAxis.TitleBounds);
        Assert.Equal([new ChartRect(344, 10.5, 8, 8)], layout.SecondaryYAxis.TitleMarkers);
    }

    [Fact]
    public void SeriesOfTheSecondaryAxisUseThePrimaryWithoutIt()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [0, 50]);
        set.AddValues(ChartSeriesKind.Line, [0, 100], axis: ChartAxisTarget.Secondary);

        // Act
        using var layout = Update(set, new CartesianLayoutOptions());

        // Assert
        Assert.False(layout.HasSecondaryAxis);
        Assert.Same(layout.YAxis, layout.GetValueAxis(ChartAxisTarget.Secondary));
        Assert.Equal(100, layout.YAxis.Scale.Maximum);
    }

    [Fact]
    public void StackedSeriesExtendTheRange()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 20)], "total");
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 30), ("Feb", 40)], "total");

        // Act
        using var layout = Update(set, CategoryBars);

        // Assert
        Assert.Equal(60, layout.YAxis.Scale.Maximum);
        Assert.Equal(1, layout.BarSlotCount);
    }

    [Fact]
    public void PercentStackUsesPercentValues()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 1), ("Feb", 3)], "total");
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 3), ("Feb", 1)], "total");

        // Act
        using var layout = Update(set, CategoryBars with { StackMode = ChartStackMode.Percent });

        // Assert
        Assert.Equal(new AxisScale(0, 100, 25), layout.YAxis.Scale);
        Assert.Equal([25d, 75], layout.Geometries[0].End.ToArray());
    }

    [Fact]
    public void HorizontalOrientationPutsTheCategoriesOnTheLeftAndTruncatesThem()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Short", 5), (new string('N', 30), 10)]);

        // Act
        using var layout = Update(set, CategoryBars with { Orientation = ChartOrientation.Horizontal });

        // Assert
        var maxWidth = 344 * CartesianLayout.VerticalLabelRatio;
        Assert.Equal(AxisSide.Left, layout.XAxis.Side);
        Assert.Equal(AxisSide.Bottom, layout.YAxis.Side);
        Assert.Equal(AxisSide.Top, layout.SecondaryYAxis.Side);
        Assert.Equal(maxWidth, layout.XAxis.Labels[1].Width);
        Assert.Equal(maxWidth, layout.XAxis.Labels[1].MaxWidth);
        Assert.Equal(8 + maxWidth + CartesianLayout.LabelGap, layout.PlotBounds.X, 9);
        Assert.Equal(layout.PlotBounds.Bottom + 6.5, layout.YAxis.Labels[0].Bounds.Y);
    }

    [Fact]
    public void OppositePositionsMoveTheAxes()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        var options = CategoryBars with
        {
            XAxis = CategoryBars.XAxis with { Position = ChartAxisPosition.Opposite },
            YAxis = CategoryBars.YAxis with { Position = ChartAxisPosition.Opposite }
        };

        // Act
        using var layout = Update(set, options);

        // Assert
        Assert.Equal(AxisSide.Top, layout.XAxis.Side);
        Assert.Equal(AxisSide.Right, layout.YAxis.Side);
        Assert.Equal(8, layout.PlotBounds.X);
        Assert.Equal(layout.PlotBounds.Y - 6.5 - 13, layout.XAxis.Labels[0].Bounds.Y);
        Assert.Equal(layout.PlotBounds.Right + CartesianLayout.LabelGap, layout.YAxis.Labels[0].Bounds.X);
    }

    [Fact]
    public void HiddenAxisTakesNoRoomButKeepsItsScale()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 20)]);
        var options = CategoryBars with
        {
            XAxis = CategoryBars.XAxis with { IsVisible = false },
            YAxis = CategoryBars.YAxis with { IsVisible = false }
        };

        // Act
        using var layout = Update(set, options);

        // Assert
        Assert.Equal(layout.Bounds, layout.PlotBounds);
        Assert.Empty(layout.YAxis.Labels);
        Assert.Equal(new AxisScale(0, 20, 5), layout.YAxis.Scale);
    }

    [Fact]
    public void SideLegendIsHiddenWhenThePlotWouldBeTooNarrow()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 10)], title: new string('A', 24));
        var options = CategoryBars with { LegendVisible = true, LegendPosition = ChartLegendPosition.Right };

        // Act
        using var narrow = Update(set, options);
        using var wide = Update(set, options, 800);

        // Assert
        Assert.False(narrow.Legend.IsVisible);
        Assert.True(wide.Legend.IsVisible);
        Assert.Equal(792 - 160, wide.Legend.Bounds.X);
        Assert.Equal(792 - 160 - CartesianLayout.LegendGap, wide.PlotBounds.Right);
        Assert.Equal(wide.PlotBounds.Y, wide.Legend.Bounds.Y);
    }

    [Fact]
    public void TimeAxisChoosesTheIntervalFromItsWidth()
    {
        // Arrange
        using var set = new TestSeriesSet();
        var start = new DateTime(2026, 10, 19, 10, 0, 0);
        set.AddTimes(ChartSeriesKind.Line, [(start, 1), (start.AddMinutes(30), 2), (start.AddHours(1), 3)]);
        var options = new CartesianLayoutOptions { XAxis = new AxisLayoutOptions { Kind = ChartAxisKind.DateTime } };

        // Act
        using var layout = Update(set, options);

        // Assert
        Assert.Equal(new TimeInterval(ChartDateTimeIntervalType.Minutes, 15), layout.XAxis.TimeInterval);
        Assert.Equal(["10:00", "10:15", "10:30", "10:45", "11:00"], layout.XAxis.Labels.Select(static x => x.Text));
    }

    [Fact]
    public void BarsOnANumericAxisArePaddedByHalfAColumn()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Bar, [(0, 1), (10, 2), (20, 3)]);
        var options = CategoryBars with { XAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { NiceRange = false } } };

        // Act
        using var layout = Update(set, options);

        // Assert
        var (start, end) = layout.Geometries[0].GetBarHitRange(0);
        Assert.Equal(-5, layout.XAxis.Scale.Minimum);
        Assert.Equal(25, layout.XAxis.Scale.Maximum);
        Assert.Equal(10, layout.BarColumnWidth);
        Assert.Equal(-5, start, 9);
        Assert.Equal(5, end, 9);
    }

    [Fact]
    public void ClusteredBarsArePackedAtTheCenterOfTheColumn()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 20)]);

        // Act
        using var layout = Update(set, CategoryBars);

        // Assert
        var center = layout.XAxis.ToPixel(0);
        var (firstStart, firstEnd) = layout.Geometries[0].GetBarRange(0);
        var (secondStart, secondEnd) = layout.Geometries[1].GetBarRange(0);
        Assert.Equal(2, layout.BarSlotCount);
        Assert.Equal(center - 25, layout.XAxis.ToPixel(firstStart), 9);
        Assert.Equal(center - 1, layout.XAxis.ToPixel(firstEnd), 9);
        Assert.Equal(center + 1, layout.XAxis.ToPixel(secondStart), 9);
        Assert.Equal(center + 25, layout.XAxis.ToPixel(secondEnd), 9);
        Assert.Equal(-0.5, layout.Geometries[0].GetBarHitRange(0).Start, 9);
        Assert.Equal(0, layout.Geometries[0].GetBarHitRange(0).End, 9);
        Assert.Equal(0, layout.Geometries[1].GetBarHitRange(0).Start, 9);
        Assert.Equal(0.5, layout.Geometries[1].GetBarHitRange(0).End, 9);
    }

    [Fact]
    public void BarWidthFollowsTheRatioInNarrowColumns()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [.. Enumerable.Range(0, 30).Select(static i => ((string?)$"C{i}", (double)i))]);

        // Act
        using var layout = Update(set, CategoryBars);

        // Assert
        Assert.Equal(CartesianSeries.DefaultBarWidthRatio, layout.Geometries[0].BarWidth, 9);
        Assert.Equal(0, layout.Geometries[0].BarOffset, 9);
    }

    [Fact]
    public void RatioOfTheFirstVisibleBarSeriesSetsTheGroupWidth()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 5)]);
        set.Replace(0, set[0] with { BarWidthRatio = 1 });
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        set.Replace(1, set[1] with { BarWidthRatio = 0.2, MaxBarWidth = 1000 });
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 20)]);
        set.Replace(2, set[2] with { BarWidthRatio = 1, MaxBarWidth = 1000 });

        // Act
        using var layout = Update(set, CategoryBars);

        // Assert
        var group = layout.PlotBounds.Width * 0.2;
        Assert.Equal(layout.XAxis.ToPixel(-0.1), layout.XAxis.ToPixel(layout.Geometries[1].GetBarRange(0).Start), 9);
        Assert.Equal(layout.XAxis.ToPixel(0.1), layout.XAxis.ToPixel(layout.Geometries[2].GetBarRange(0).End), 9);
        Assert.Equal((group - 2) / 2, layout.XAxis.ToPixel(layout.Geometries[1].GetBarRange(0).End) - layout.XAxis.ToPixel(layout.Geometries[1].GetBarRange(0).Start), 9);
    }

    [Fact]
    public void MaxBarWidthOfTheSeriesOverridesTheStyle()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        set.Replace(0, set[0] with { MaxBarWidth = 40 });

        // Act
        using var layout = Update(set, CategoryBars);

        // Assert
        var (start, end) = layout.Geometries[0].GetBarRange(0);
        Assert.Equal(40, layout.XAxis.ToPixel(end) - layout.XAxis.ToPixel(start), 9);
    }

    [Fact]
    public void DataLabelsArePickedByTheModeInPriorityOrder()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [10, 40, 5, 20, 30]);

        // Act
        using var last = Update(set, new CartesianLayoutOptions { DataLabelsMode = ChartDataLabelsMode.Last });
        using var extremes = Update(set, new CartesianLayoutOptions { DataLabelsMode = ChartDataLabelsMode.MinMax });
        using var all = Update(set, new CartesianLayoutOptions { DataLabelsMode = ChartDataLabelsMode.All });
        using var none = Update(set, new CartesianLayoutOptions());

        // Assert
        Assert.Equal([4], last.DataLabels.Select(static x => x.Index));
        Assert.Equal([1, 2], extremes.DataLabels.Select(static x => x.Index));
        Assert.Equal([4, 1, 2, 0, 3], all.DataLabels.Select(static x => x.Index));
        Assert.Equal(["30", "40", "5", "10", "20"], all.DataLabels.Select(static x => x.Text));
        Assert.Equal(Measurer.Measure("40", ChartStyles.Light.DataLabelFontSize, Double.PositiveInfinity), all.DataLabels[1].Width);
        Assert.Equal(Measurer.GetLineHeight(ChartStyles.Light.DataLabelFontSize), all.DataLabelHeight);
        Assert.Empty(none.DataLabels);
    }

    [Fact]
    public void DataLabelsSkipHiddenSeriesAndGaps()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [1, 2], visible: false);
        set.AddValues(ChartSeriesKind.Line, [3, Double.NaN]);

        // Act
        using var layout = Update(set, new CartesianLayoutOptions { DataLabelsMode = ChartDataLabelsMode.All });

        // Assert
        Assert.Equal([new DataLabel(1, 0, "3", Measurer.Measure("3", ChartStyles.Light.DataLabelFontSize, Double.PositiveInfinity))], layout.DataLabels);
    }

    [Fact]
    public void DataLabelTextUsesTheSeriesFormatThenTheAxisFormat()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [12]);
        set.AddValues(ChartSeriesKind.Line, [12]);
        set.Replace(0, set[0] with { ValueFormat = ChartTextFormat.Parse("{0:0.0}") });
        var options = new CartesianLayoutOptions
        {
            YAxis = new AxisLayoutOptions { LabelFormat = ChartTextFormat.Parse("{0}%") },
            DataLabelsMode = ChartDataLabelsMode.Last
        };

        // Act
        using var layout = Update(set, options);

        // Assert
        Assert.Equal(["12.0", "12%"], layout.DataLabels.Select(static x => x.Text));
    }

    [Fact]
    public void PercentStackLabelsShowTheShare()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 1)], "total");
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 3)], "total");
        set.AddEntries(ChartSeriesKind.Line, [("Jan", 50)]);

        // Act
        using var percent = Update(set, CategoryBars with { StackMode = ChartStackMode.Percent, DataLabelsMode = ChartDataLabelsMode.Last });
        using var normal = Update(set, CategoryBars with { DataLabelsMode = ChartDataLabelsMode.Last });

        // Assert
        var culture = CultureInfo.InvariantCulture;
        Assert.Equal([0.25.ToString("P0", culture), 0.75.ToString("P0", culture), "50"], percent.DataLabels.Select(static x => x.Text));
        Assert.Equal(["1", "3", "50"], normal.DataLabels.Select(static x => x.Text));
    }

    [Fact]
    public void OneLegendRowCanBeRequested()
    {
        // Arrange
        using var set = new TestSeriesSet();
        for (var i = 0; i < 12; i++)
        {
            set.AddValues(ChartSeriesKind.Line, [i], title: $"Series {i}");
        }

        // Act
        using var layout = Update(set, new CartesianLayoutOptions { LegendVisible = true, LegendMaxRows = 1 });

        // Assert
        Assert.All(layout.Legend.Items, item => Assert.Equal(layout.Legend.Items[0].Bounds.Y, item.Bounds.Y));
        Assert.True(layout.Legend.OverflowCount > 0);
        Assert.Equal(layout.Legend.Items[0].Bounds.Height, layout.Legend.Bounds.Height);
    }

    [Fact]
    public void InversedAxisMapsTheOtherWay()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 30)]);

        // Act
        using var layout = Update(set, CategoryBars with { YAxis = CategoryBars.YAxis with { IsInversed = true } });

        // Assert
        Assert.Equal(layout.PlotBounds.Bottom, layout.YAxis.ToPixel(30));
        Assert.Equal(layout.PlotBounds.Y, layout.YAxis.ToPixel(0));
        Assert.Equal(30, layout.YAxis.ToValue(layout.PlotBounds.Bottom));
    }

    [Fact]
    public void UpdateWithFewerSeriesReleasesTheRest()
    {
        // Arrange
        using var first = new TestSeriesSet();
        first.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        first.AddEntries(ChartSeriesKind.Bar, [("Jan", 20)]);
        using var second = new TestSeriesSet();
        second.AddEntries(ChartSeriesKind.Bar, [("Jan", 10)]);
        using var layout = Update(first, CategoryBars);

        // Act
        layout.Update(360, 240, new Thickness(8), ChartStyles.Light, CultureInfo.InvariantCulture, CategoryBars, second.Series, Measurer);

        // Assert
        Assert.Single(layout.Geometries);
        Assert.Equal(1, layout.BarSlotCount);
    }

    [Fact]
    public void ValueRangeFollowsThePointsInTheFixedXRange()
    {
        // Arrange
        using var points = new TestSeriesSet();
        points.AddPoints(ChartSeriesKind.Line, [(0, 100), (10, 20), (20, 30), (30, 25), (40, 200)]);
        using var entries = new TestSeriesSet();
        entries.AddEntries(ChartSeriesKind.Line, [("A", 100), ("B", 20), ("C", 30)]);
        var categoryRange = FixedXRange(1, 2) with { XAxis = new AxisLayoutOptions { Kind = ChartAxisKind.Category, Range = new AxisRangeOptions { Minimum = 1, Maximum = 2 } } };

        // Act
        using var numeric = Update(points, FixedXRange(10, 30));
        using var category = Update(entries, categoryRange);

        // Assert
        Assert.Equal(20, numeric.YAxis.Scale.Minimum);
        Assert.Equal(30, numeric.YAxis.Scale.Maximum);
        Assert.Equal(20, category.YAxis.Scale.Minimum);
        Assert.Equal(100, category.YAxis.Scale.Maximum);
    }

    [Fact]
    public void LinesCrossingTheEdgesOfTheXRangeAddTheirValuesAtTheEdges()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Line, [(0, 0), (10, 10), (20, 0)]);
        using var linear = Update(set, FixedXRange(5, 15));
        set.Replace(0, set[0] with { Interpolation = ChartLineInterpolation.Step });

        // Act
        using var step = Update(set, FixedXRange(5, 15));

        // Assert
        Assert.Equal(5, linear.YAxis.Scale.Minimum);
        Assert.Equal(10, linear.YAxis.Scale.Maximum);
        Assert.Equal(0, step.YAxis.Scale.Minimum);
        Assert.Equal(10, step.YAxis.Scale.Maximum);
    }

    [Fact]
    public void BarsOutsideTheFixedXRangeDoNotWidenTheValueRange()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Bar, [(0, 100), (10, 20), (20, 30)]);

        // Act
        using var layout = Update(set, FixedXRange(5, 25));

        // Assert
        Assert.Equal(20, layout.YAxis.Scale.Minimum);
        Assert.Equal(30, layout.YAxis.Scale.Maximum);
    }

    [Fact]
    public void TrackingLabelOfATimeAxisShowsTheTimeOfThePoints()
    {
        // Arrange
        var start = new DateTime(2026, 9, 20, 11, 0, 0);
        using var month = new TestSeriesSet();
        month.AddTimes(ChartSeriesKind.Line, [.. Enumerable.Range(0, 721).Select(i => (start.AddHours(i), 1d))]);
        using var day = new TestSeriesSet();
        day.AddTimes(ChartSeriesKind.Line, [(start, 1), (start.AddHours(3), 2), (start.AddHours(5), 3)]);
        var time = new CartesianLayoutOptions { XAxis = new AxisLayoutOptions { Kind = ChartAxisKind.DateTime } };
        var formatted = time with { XAxis = time.XAxis with { LabelFormat = ChartTextFormat.Parse("M/d") } };
        var x = new DateTime(2026, 10, 9, 14, 0, 0).Ticks;

        // Act
        using var monthLayout = Update(month, time);
        using var formattedLayout = Update(month, formatted);
        using var dayLayout = Update(day, time);

        // Assert
        Assert.Equal(TimeLabelPrecision.Day, monthLayout.XAxis.TimeInterval.LabelPrecision);
        Assert.Equal("10/9 14:00", monthLayout.GetTrackingLabel(x, month.Series));
        Assert.Equal("10/9", formattedLayout.GetTrackingLabel(x, month.Series));
        Assert.Equal("14:00", dayLayout.GetTrackingLabel(start.AddHours(3).Ticks, day.Series));
    }

    [Fact]
    public void PercentStackedAxisIsLabelledWithPercentages()
    {
        // Arrange
        using var stacked = new TestSeriesSet();
        stacked.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 30)], stackGroup: "a");
        stacked.AddEntries(ChartSeriesKind.Bar, [("Jan", 30), ("Feb", 10)], stackGroup: "a");
        using var mixed = new TestSeriesSet();
        mixed.AddEntries(ChartSeriesKind.Bar, [("Jan", 10), ("Feb", 30)], stackGroup: "a");
        mixed.AddEntries(ChartSeriesKind.Line, [("Jan", 30), ("Feb", 10)]);
        var percent = CategoryBars with { StackMode = ChartStackMode.Percent };
        var formatted = percent with { YAxis = percent.YAxis with { LabelFormat = ChartTextFormat.Parse("N0") } };
        var zoomed = percent with { YAxis = percent.YAxis with { Range = new AxisRangeOptions { Minimum = 0, Maximum = 10, Interval = 2.5 } } };

        // Act
        using var stackedLayout = Update(stacked, percent);
        using var mixedLayout = Update(mixed, percent);
        using var formattedLayout = Update(stacked, formatted);
        using var zoomedLayout = Update(stacked, zoomed);

        // Assert
        Assert.Equal(1d.ToString("P0", CultureInfo.InvariantCulture), stackedLayout.YAxis.Labels[^1].Text);
        Assert.All(stackedLayout.YAxis.Labels, static x => Assert.EndsWith("%", x.Text, StringComparison.Ordinal));
        Assert.DoesNotContain(mixedLayout.YAxis.Labels, static x => x.Text.Contains('%', StringComparison.Ordinal));
        Assert.Equal("100", formattedLayout.YAxis.Labels[^1].Text);
        Assert.Contains(zoomedLayout.YAxis.Labels, static x => x.Text == 0.025.ToString("P1", CultureInfo.InvariantCulture));
        Assert.Contains(zoomedLayout.YAxis.Labels, static x => x.Text == 0.05.ToString("P0", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void YAnnotationsWidenTheValueRangeButXAnnotationsAndThresholdsDoNot()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Line, [(0, 10), (10, 30)]);
        var options = Exact with
        {
            Annotations =
            [
                new CartesianAnnotation(0, false, ChartAnnotationAxis.Y, ChartAxisTarget.Primary, 50, 50, Double.NaN, Double.NaN),
                new CartesianAnnotation(1, true, ChartAnnotationAxis.X, ChartAxisTarget.Primary, 15, -5, Double.NaN, Double.NaN)
            ],
            Thresholds = [new ChartThreshold { Value = 80, Level = ChartThresholdLevel.Critical }]
        };

        // Act
        using var layout = Update(set, options);

        // Assert
        Assert.Equal(10, layout.YAxis.Scale.Minimum);
        Assert.Equal(50, layout.YAxis.Scale.Maximum);
        Assert.Equal(0, layout.XAxis.Scale.Minimum);
        Assert.Equal(10, layout.XAxis.Scale.Maximum);
        Assert.Equal(-5, layout.Annotations[1].From);
        Assert.Equal(15, layout.Annotations[1].To);
    }

    [Fact]
    public void AnnotationOfTheSecondaryAxisWidensOnlyThatAxis()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [10, 30]);
        set.AddValues(ChartSeriesKind.Line, [10, 30], axis: ChartAxisTarget.Secondary);
        var options = Exact with { Annotations = [new CartesianAnnotation(0, false, ChartAnnotationAxis.Y, ChartAxisTarget.Secondary, 50, 50, Double.NaN, Double.NaN)] };

        // Act
        using var single = Update(set, options);
        using var dual = Update(set, options with { SecondaryYAxis = Exact.YAxis });

        // Assert
        Assert.Equal(50, single.YAxis.Scale.Maximum);
        Assert.Equal(30, dual.YAxis.Scale.Maximum);
        Assert.Equal(50, dual.SecondaryYAxis.Scale.Maximum);
    }

    [Fact]
    public void AnnotationsOnATimeAxisUseTheirTimes()
    {
        // Arrange
        var start = new DateTime(2026, 10, 1, 9, 0, 0);
        var ticks = (double)start.AddHours(1).Ticks;
        using var times = new TestSeriesSet();
        times.AddTimes(ChartSeriesKind.Line, [(start, 1), (start.AddHours(2), 2)]);
        using var numbers = new TestSeriesSet();
        numbers.AddPoints(ChartSeriesKind.Line, [(0, 1), (10, 2)]);
        List<CartesianAnnotation> annotations =
        [
            new(0, false, ChartAnnotationAxis.X, ChartAxisTarget.Primary, 5, 5, ticks, ticks),
            new(1, false, ChartAnnotationAxis.X, ChartAxisTarget.Primary, 5, 5, Double.NaN, Double.NaN),
            new(2, false, ChartAnnotationAxis.Y, ChartAxisTarget.Primary, 1.5, 1.5, Double.NaN, Double.NaN)
        ];

        // Act
        using var time = Update(times, Exact with { XAxis = Exact.XAxis with { Kind = ChartAxisKind.DateTime }, Annotations = annotations });
        using var number = Update(numbers, Exact with { Annotations = annotations });

        // Assert
        Assert.Equal([0, 2], time.Annotations.Select(static x => x.Index));
        Assert.Equal(ticks, time.Annotations[0].From);
        Assert.Equal(1.5, time.Annotations[1].From);
        Assert.Equal([0, 1, 2], number.Annotations.Select(static x => x.Index));
        Assert.Equal(5, number.Annotations[0].From);
    }

    [Fact]
    public void ThresholdsAreSortedByValueKeepingTheOrderOfTies()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddValues(ChartSeriesKind.Line, [10, 30]);
        var good = new ChartThreshold { Level = ChartThresholdLevel.Good };
        var warning = new ChartThreshold { Value = 20, Level = ChartThresholdLevel.Warning };
        var serious = new ChartThreshold { Value = 20, Level = ChartThresholdLevel.Serious };
        var critical = new ChartThreshold { Value = 25, Level = ChartThresholdLevel.Critical };

        // Act
        using var layout = Update(set, Exact with { Thresholds = [critical, warning, good, serious] });

        // Assert
        Assert.Equal([good, warning, serious, critical], layout.Thresholds);
    }

    [Fact]
    public void ScatterPointsOutsideTheFixedXRangeDoNotWidenTheValueRange()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Scatter, [(0, 100), (10, 20), (20, 30), (30, 200)]);

        // Act
        using var layout = Update(set, FixedXRange(5, 25));

        // Assert
        Assert.Equal(20, layout.YAxis.Scale.Minimum);
        Assert.Equal(30, layout.YAxis.Scale.Maximum);
    }

    [Fact]
    public void ScatterIsShownAsADotInTheLegend()
    {
        // Arrange
        using var set = new TestSeriesSet();
        set.AddPoints(ChartSeriesKind.Scatter, [(0, 1), (1, 2)], title: "Points");
        set.AddPoints(ChartSeriesKind.Line, [(0, 1), (1, 2)], title: "Trend");

        // Act
        using var layout = Update(set, Exact with { LegendVisible = true });

        // Assert
        var size = ChartStyles.Light.LegendMarkerSize;
        Assert.Equal(LegendLayout.GetMarkerWidth(LegendMarkerKind.Dot, size), layout.Legend.Items[0].Marker.Width);
        Assert.Equal(LegendLayout.GetMarkerWidth(LegendMarkerKind.Line, size), layout.Legend.Items[1].Marker.Width);
    }

    private static CartesianLayoutOptions FixedXRange(double minimum, double maximum) =>
        new()
        {
            XAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { Minimum = minimum, Maximum = maximum } },
            YAxis = new AxisLayoutOptions { Range = new AxisRangeOptions { NiceRange = false } }
        };

    private static CartesianLayout Update(TestSeriesSet set, CartesianLayoutOptions options, double width = 360, double height = 240)
    {
        var layout = new CartesianLayout();
        layout.Update(width, height, new Thickness(8), ChartStyles.Light, CultureInfo.InvariantCulture, options, set.Series, Measurer);
        return layout;
    }
}
