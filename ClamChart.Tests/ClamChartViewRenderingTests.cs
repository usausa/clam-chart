namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamChartViewRenderingTests
{
    private const float Scale = 2;

    private static readonly SKColor Lime = new(0, 255, 0);

    private static readonly SKColor Red = new(255, 0, 0);

    [Fact]
    public void BackgroundStaysTransparent()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new AreaSeries { ItemsSource = new List<double> { 3, 5, 4 } });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        Assert.Equal(0, bitmap.GetPixel(bitmap.Width - 1, 0).Alpha);
    }

    [Fact]
    public void LineIsDrawnWithTheColorOfItsPosition()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 2, 2 } });
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 8, 8 } });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var first = GetPixel(bitmap, layout.XAxis.ToPixel(0.5), layout.YAxis.ToPixel(2));
        var second = GetPixel(bitmap, layout.XAxis.ToPixel(0.5), layout.YAxis.ToPixel(8));
        Assert.Equal(new SKColor(0x2A, 0x78, 0xD6), first);
        Assert.Equal(new SKColor(0xEB, 0x68, 0x34), second);
    }

    [Fact]
    public void ExplicitColorWinsOverThePalette()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 2, 2 }, Color = Colors.Lime });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(new SKColor(0, 255, 0), GetPixel(bitmap, layout.XAxis.ToPixel(0.5), layout.YAxis.ToPixel(2)));
    }

    [Fact]
    public void MarkerKnockoutIsTransparent()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new AreaSeries { ItemsSource = new List<double> { 2, 4, 6, 8 }, FillOpacity = 1 });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var x = layout.XAxis.ToPixel(3);
        var y = layout.YAxis.ToPixel(8);
        Assert.Equal(255, GetPixelAlpha(bitmap, x, y));
        Assert.Equal(0, GetPixelAlpha(bitmap, x, y + 5));
        Assert.Equal(255, GetPixelAlpha(bitmap, x - 3, y + 12));
    }

    [Fact]
    public void GridLineIsDrawnAtEachTick()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 0, 100 } });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var tick = layout.YAxis.Ticks[^1];
        var y = (int)Math.Floor(layout.YAxis.ToPixel(tick) * Scale);
        Assert.Equal(new SKColor(0xE1, 0xE0, 0xD9), bitmap.GetPixel((int)(layout.PlotBounds.X * Scale) + 2, y));
    }

    [Fact]
    public void BarRoundsOnlyTheDataSide()
    {
        // Arrange
        using var up = Bars(new BarSeries { ItemsSource = Entries(10), CornerRadius = 8 });
        using var down = Bars(new BarSeries { ItemsSource = Entries(-10), CornerRadius = 8 });
        using var right = Bars(new BarSeries { ItemsSource = Entries(10), CornerRadius = 8 });
        right.Orientation = ChartOrientation.Horizontal;

        // Act
        using var upBitmap = up.Paint(360, 240);
        using var downBitmap = down.Paint(360, 240);
        using var rightBitmap = right.Paint(360, 240);

        // Assert
        var bar = GetBarBounds(up, 0, 0);
        Assert.Equal(0, GetPixelAlpha(upBitmap, bar.Left + 1, bar.Top + 1));
        Assert.Equal(0, GetPixelAlpha(upBitmap, bar.Right - 1, bar.Top + 1));
        Assert.Equal(255, GetPixelAlpha(upBitmap, bar.Left + 2, bar.Bottom - 2));
        bar = GetBarBounds(down, 0, 0);
        Assert.Equal(0, GetPixelAlpha(downBitmap, bar.Left + 1, bar.Bottom - 1));
        Assert.Equal(255, GetPixelAlpha(downBitmap, bar.Left + 2, bar.Top + 2));
        bar = GetBarBounds(right, 0, 0);
        Assert.Equal(0, GetPixelAlpha(rightBitmap, bar.Right - 1, bar.Top + 1));
        Assert.Equal(0, GetPixelAlpha(rightBitmap, bar.Right - 1, bar.Bottom - 1));
        Assert.Equal(255, GetPixelAlpha(rightBitmap, bar.Left + 2, bar.Top + 2));
    }

    [Fact]
    public void ClippedBarEndStaysSquare()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(20), CornerRadius = 8 });
        view.YAxis = new NumericAxis { Maximum = 10 };

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var bar = GetBarBounds(view, 0, 0);
        var plot = view.ChartLayout.PlotBounds;
        Assert.Equal(255, GetPixelAlpha(bitmap, bar.Left + 1, plot.Y + 1));
        Assert.Equal(0, GetPixelAlpha(bitmap, bar.Left + 1, plot.Y - 1));
    }

    [Fact]
    public void StackedSegmentsLeaveATransparentGapAndOnlyTheTopIsRounded()
    {
        // Arrange
        using var view = Bars(
            new BarSeries { ItemsSource = Entries(3), StackGroup = "total", CornerRadius = 8 },
            new BarSeries { ItemsSource = Entries(7), StackGroup = "total", CornerRadius = 8 });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var axis = view.ChartLayout.YAxis;
        var lower = GetBarBounds(view, 0, 0);
        var upper = GetBarBounds(view, 1, 0);
        Assert.Equal(0, GetPixelAlpha(bitmap, lower.MidX, axis.ToPixel(3)));
        Assert.Equal(255, GetPixelAlpha(bitmap, lower.MidX, axis.ToPixel(1.5)));
        Assert.Equal(255, GetPixelAlpha(bitmap, lower.MidX, axis.ToPixel(6)));
        Assert.Equal(255, GetPixelAlpha(bitmap, lower.Left + 1, lower.Top + 2));
        Assert.Equal(0, GetPixelAlpha(bitmap, upper.Left + 1, upper.Top + 1));
    }

    [Fact]
    public void NegativeValuesUseTheNegativeColor()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(5, -5), Color = Colors.Blue, NegativeColor = Colors.Lime });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var positive = GetBarBounds(view, 0, 0);
        var negative = GetBarBounds(view, 0, 1);
        Assert.Equal(new SKColor(0, 0, 255), GetPixel(bitmap, positive.MidX, positive.MidY));
        Assert.Equal(new SKColor(0, 255, 0), GetPixel(bitmap, negative.MidX, negative.MidY));
    }

    [Fact]
    public void BarLabelGoesInsideTheEndWhenItDoesNotFitOutside()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(5, 10), Color = Colors.Navy });
        view.DataLabelsMode = ChartDataLabelsMode.All;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var height = (float)view.ChartLayout.DataLabelHeight;
        var low = GetBarBounds(view, 0, 0);
        var high = GetBarBounds(view, 0, 1);
        Assert.True(CountPixels(bitmap, SKRect.Create(low.Left, low.Top - 4 - height, low.Width, height), IsInk) > 0);
        Assert.Equal(0, CountPixels(bitmap, SKRect.Create(low.Left, low.Top + 2, low.Width, height + 4), IsWhite));
        Assert.True(CountPixels(bitmap, SKRect.Create(high.Left, high.Top + 4, high.Width, height), IsWhite) > 0);
    }

    [Fact]
    public void StackedLabelsAreCenteredWithAContrastingColor()
    {
        // Arrange
        using var view = Bars(
            new BarSeries { ItemsSource = Entries(5), StackGroup = "total", Color = Colors.Navy },
            new BarSeries { ItemsSource = Entries(5), StackGroup = "total", Color = Colors.Yellow });
        view.DataLabelsMode = ChartDataLabelsMode.All;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var height = (float)view.ChartLayout.DataLabelHeight;
        var lower = GetBarBounds(view, 0, 0);
        var upper = GetBarBounds(view, 1, 0);
        Assert.True(CountPixels(bitmap, SKRect.Create(lower.Left, lower.MidY - (height / 2), lower.Width, height), IsWhite) > 0);
        Assert.True(CountPixels(bitmap, SKRect.Create(upper.Left, upper.MidY - (height / 2), upper.Width, height), IsDark) > 0);
    }

    [Fact]
    public void LabelsInsideADimmedBarFadeWithIt()
    {
        // Arrange
        using var view = Bars(
            new BarSeries { ItemsSource = Entries(5), StackGroup = "total", Color = Colors.Navy },
            new BarSeries { ItemsSource = Entries(5), StackGroup = "total", Color = Colors.Yellow });
        view.DataLabelsMode = ChartDataLabelsMode.All;
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedSeries = view.Series[0];
        view.SelectedIndex = 0;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var height = (float)view.ChartLayout.DataLabelHeight;
        var lower = GetBarBounds(view, 0, 0);
        var upper = GetBarBounds(view, 1, 0);
        Assert.True(CountPixels(bitmap, SKRect.Create(lower.Left, lower.MidY - (height / 2), lower.Width, height), IsWhite) > 0);
        Assert.Equal(0, CountPixels(bitmap, SKRect.Create(upper.Left, upper.MidY - (height / 2), upper.Width, height), static color => (color.Alpha > 200) && IsDark(color)));
    }

    [Fact]
    public void LineLabelsMoveBelowWhenTheyWouldOverlap()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.DataLabelsMode = ChartDataLabelsMode.Last;
        view.YAxis = new NumericAxis { Minimum = 0, Maximum = 10 };
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 5, 5 } });
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 5, 5 } });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var plot = layout.PlotBounds;
        var y = (float)layout.YAxis.ToPixel(5);
        var height = (float)layout.DataLabelHeight;
        var offset = 4 + 2 + 4;
        Assert.True(CountPixels(bitmap, SKRect.Create((float)plot.Right - 40, y - offset - height, 40, height), IsInk) > 0);
        Assert.True(CountPixels(bitmap, SKRect.Create((float)plot.Right - 40, y + offset, 40, height), IsInk) > 0);
    }

    [Fact]
    public void SelectionDimsTheOtherBars()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 1;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var other = GetBarBounds(view, 0, 0);
        var selected = GetBarBounds(view, 0, 1);
        Assert.Equal(255, GetPixelAlpha(bitmap, selected.MidX, selected.MidY));
        Assert.Equal((byte)Math.Round(255 * ChartStyles.Light.DimmedOpacity), GetPixelAlpha(bitmap, other.MidX, other.MidY));
    }

    [Fact]
    public void SelectedPointOfALineGetsAMarker()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = Entries(10, 20, 30, 40) });
        view.Series.Add(new LineSeries { ItemsSource = Entries(40, 30, 20, 10), Color = Colors.Lime });
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 1;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var marker = GetPixel(bitmap, layout.XAxis.ToPixel(1), layout.YAxis.ToPixel(20) - 3);
        var dimmed = GetPixel(bitmap, layout.XAxis.ToPixel(0.5), layout.YAxis.ToPixel(35));
        Assert.Equal(new SKColor(0x2A, 0x78, 0xD6), marker);
        Assert.Equal((byte)Math.Round(255 * ChartStyles.Light.DimmedOpacity), dimmed.Alpha);
    }

    [Fact]
    public void TrackballDrawsTheLineAndTheTooltip()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.TrackballMode = ChartTrackballMode.Touch;
        view.Series.Add(new LineSeries { Title = "Sales", ItemsSource = Entries(10, 20, 30, 40) });
        using var first = view.Paint(360, 240);
        var layout = view.ChartLayout;
        var x = layout.XAxis.ToPixel(2);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(x, 150), 0);
        using var bitmap = view.Paint(360, 240);

        // Assert
        var style = ChartStyles.Light;
        var tooltip = GetPixel(bitmap, x + 10, 150 - 48 - 3);
        Assert.Equal(style.TrackballLineColor.ToSKColor(), bitmap.GetPixel((int)Math.Floor(x * Scale), (int)((layout.PlotBounds.Y + 5) * Scale)));
        Assert.Equal(style.TooltipBackground.ToSKColor().Alpha, tooltip.Alpha);
        Assert.True(tooltip.Red >= 250);
        Assert.Equal(0, GetPixelAlpha(first, x + 10, 150 - 48 - 3));
    }

    [Fact]
    public void PressedOverlayCoversAMinimalChart()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(10, 20, 30) });
        view.ChartTapped += static (_, _) => { };
        using var idle = view.Paint(150, 90);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(5, 5), 0);
        using var pressed = view.Paint(150, 90);

        // Assert
        Assert.Equal(0, GetPixelAlpha(idle, 2, 2));
        Assert.Equal(ChartStyles.Light.PressedOverlayColor.ToSKColor().Alpha, GetPixelAlpha(pressed, 2, 2));
    }

    [Fact]
    public void EmptyChartDrawsNothingWithoutText()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<double>() });
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 1, 2 }, IsVisible = false });

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.Equal(0, CountVisiblePixels(bitmap));
    }

    [Fact]
    public void EmptyTextIsDrawn()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.EmptyText = "No data";

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.True(CountVisiblePixels(bitmap) > 0);
    }

    [Fact]
    public void ThresholdLinesAreDashedAndTranslucent()
    {
        // Arrange
        using var view = ThresholdChart(null);

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var x = layout.PlotBounds.X;
        var y = layout.YAxis.ToPixel(33);
        Assert.Equal(Red.WithAlpha(178), GetPixel(bitmap, x + 5, y));
        Assert.Equal(Red.WithAlpha(178), GetPixel(bitmap, x + 5, y - 0.5));
        Assert.Equal(Red.WithAlpha(178), GetPixel(bitmap, x + 5, y + 0.5));
        Assert.Equal(0, GetPixelAlpha(bitmap, x + 15, y));
        Assert.Equal(0, GetPixelAlpha(bitmap, x + 5, y + 1.5));
    }

    [Fact]
    public void ThresholdLabelIsDrawnAboveTheRightEndOfItsLine()
    {
        // Arrange
        using var labeled = ThresholdChart("Limit");
        using var plain = ThresholdChart(null);

        // Act
        using var labeledBitmap = labeled.Paint(360, 240);
        using var plainBitmap = plain.Paint(360, 240);

        // Assert
        var layout = labeled.ChartLayout;
        var y = (float)layout.YAxis.ToPixel(33);
        var area = SKRect.Create((float)layout.PlotBounds.Right - 60, y - 16, 60, 14);
        Assert.True(CountPixels(labeledBitmap, area, IsRed) > 10);
        Assert.Equal(0, CountPixels(plainBitmap, area, IsRed));
    }

    [Fact]
    public void ThresholdBandsTintEachRegionBehindTheSeries()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(80), Color = Colors.Blue });
        view.YAxis = new NumericAxis { GridLinesVisible = false };
        view.Thresholds = Steps(50);
        view.ThresholdsMode = ChartThresholdsMode.Bands;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var x = layout.PlotBounds.X + 2;
        var bar = GetBarBounds(view, 0, 0);
        Assert.Equal(Lime.WithAlpha(38), GetPixel(bitmap, x, layout.YAxis.ToPixel(25)));
        Assert.Equal(Red.WithAlpha(38), GetPixel(bitmap, x, layout.YAxis.ToPixel(75)));
        Assert.Equal(new SKColor(0, 0, 255), GetPixel(bitmap, bar.MidX, layout.YAxis.ToPixel(75)));
    }

    [Fact]
    public void ColorSeriesPaintsTheLineByRegionButNotTheSecondaryAxis()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 0, 100 } });
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 100, 0 }, YAxis = ChartAxisTarget.Secondary, Color = Colors.Blue });
        view.Thresholds = [.. Steps(50), new ChartThreshold { Value = 500, Color = Colors.Yellow }];
        view.ThresholdsMode = ChartThresholdsMode.ColorSeries;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Lime, GetPixel(bitmap, layout.XAxis.ToPixel(0.25), layout.YAxis.ToPixel(25)));
        Assert.Equal(Red, GetPixel(bitmap, layout.XAxis.ToPixel(0.75), layout.YAxis.ToPixel(75)));
        Assert.Equal(new SKColor(0, 0, 255), GetPixel(bitmap, layout.XAxis.ToPixel(0.25), layout.SecondaryYAxis.ToPixel(75)));
        Assert.Equal(100, layout.YAxis.Scale.Maximum);
    }

    [Fact]
    public void ColorSeriesPaintsBarsByTheirValueAndAreasByRegion()
    {
        // Arrange
        using var bars = Bars(new BarSeries { ItemsSource = Entries(20, 80) });
        bars.Thresholds = Steps(50);
        bars.ThresholdsMode = ChartThresholdsMode.ColorSeries;
        using var area = new TestClamChartView();
        area.Series.Add(new AreaSeries { ItemsSource = new List<double> { 0, 100 }, FillOpacity = 1 });
        area.Thresholds = Steps(50);
        area.ThresholdsMode = ChartThresholdsMode.ColorSeries;

        // Act
        using var barBitmap = bars.Paint(360, 240);
        using var areaBitmap = area.Paint(360, 240);

        // Assert
        var low = GetBarBounds(bars, 0, 0);
        var high = GetBarBounds(bars, 0, 1);
        Assert.Equal(Lime, GetPixel(barBitmap, low.MidX, low.MidY));
        Assert.Equal(Red, GetPixel(barBitmap, high.MidX, high.MidY));
        Assert.Equal(Red, GetPixel(barBitmap, high.MidX, high.Bottom - 2));
        var layout = area.ChartLayout;
        var x = layout.XAxis.ToPixel(0.75);
        Assert.Equal(Lime, GetPixel(areaBitmap, x, layout.YAxis.ToPixel(30)));
        Assert.Equal(Red, GetPixel(areaBitmap, x, layout.YAxis.ToPixel(60)));
    }

    [Fact]
    public void ReferenceLineIsDashedAndDrawnOverTheSeries()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(80), Color = Colors.Blue });
        view.YAxis = new NumericAxis { GridLinesVisible = false };
        view.Annotations.Add(new ChartReferenceLine { Value = 33 });
        view.Annotations.Add(new ChartReferenceLine { Value = 66, DashPattern = [] });
        view.Annotations.Add(new ChartReferenceLine { Value = 15, DashPattern = [2, 6], Color = Colors.Red });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var color = ChartStyles.Light.SecondaryTextColor.ToSKColor();
        var x = layout.PlotBounds.X;
        var dashed = layout.YAxis.ToPixel(33);
        var solid = layout.YAxis.ToPixel(66);
        var custom = layout.YAxis.ToPixel(15);
        var bar = GetBarBounds(view, 0, 0);
        Assert.Equal(color, GetPixel(bitmap, x + 2, dashed));
        Assert.Equal(0, GetPixelAlpha(bitmap, x + 6, dashed));
        Assert.Equal(color, GetPixel(bitmap, x + 6, solid));
        Assert.Equal(color, GetPixel(bitmap, bar.MidX, solid));
        Assert.Equal(new SKColor(0, 0, 255), GetPixel(bitmap, bar.MidX, solid + 2));
        Assert.Equal(Red, GetPixel(bitmap, x + 1, custom));
        Assert.Equal(0, GetPixelAlpha(bitmap, x + 4, custom));
        Assert.Equal(Red, GetPixel(bitmap, x + 9, custom));
    }

    [Fact]
    public void ReferenceBandTintsItsRangeUnderTheSeriesWithItsLabel()
    {
        // Arrange
        using var view = Bars(new BarSeries { ItemsSource = Entries(80), Color = Colors.Blue });
        view.YAxis = new NumericAxis { GridLinesVisible = false };
        view.Annotations.Add(new ChartReferenceBand { Start = 40, End = 20, Color = Colors.Lime, Label = "Normal" });
        view.Annotations.Add(new ChartReferenceBand { Start = 60, End = 70 });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var x = layout.PlotBounds.X + 100;
        var bar = GetBarBounds(view, 0, 0);
        var muted = GetPixel(bitmap, x, layout.YAxis.ToPixel(65));
        var label = SKRect.Create((float)layout.PlotBounds.X, (float)layout.YAxis.ToPixel(40), 60, 16);
        Assert.Equal(Lime.WithAlpha(38), GetPixel(bitmap, x, layout.YAxis.ToPixel(25)));
        Assert.Equal(new SKColor(0, 0, 255), GetPixel(bitmap, bar.MidX, layout.YAxis.ToPixel(30)));
        Assert.Equal(0, GetPixelAlpha(bitmap, x, layout.YAxis.ToPixel(50)));
        Assert.Equal(38, muted.Alpha);
        Assert.InRange(muted.Red, 0x85, 0x8D);
        Assert.True(CountPixels(bitmap, label, static color => (color.Alpha > 128) && (color.Green > 200) && (color.Red < 100)) > 10);
    }

    [Fact]
    public void ReferenceLineOnATimeAxisIsDrawnAtItsTime()
    {
        // Arrange
        var start = new DateTime(2026, 10, 1, 9, 0, 0);
        using var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<ChartTimePoint> { new(start, 10), new(start.AddHours(4), 10.5) } });
        view.Annotations.Add(new ChartReferenceLine { Axis = ChartAnnotationAxis.X, Time = start.AddHours(1), DashPattern = [] });
        view.Annotations.Add(new ChartReferenceLine { Axis = ChartAnnotationAxis.X, Value = 3 });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        var x = layout.XAxis.ToPixel(start.AddHours(1).Ticks);
        Assert.Equal(ChartStyles.Light.SecondaryTextColor.ToSKColor(), GetPixel(bitmap, x, layout.PlotBounds.Y + 3));
        Assert.Single(layout.Annotations);
    }

    [Fact]
    public void ScatterPointIsAHalfFilledShapeWithAnOutline()
    {
        // Arrange
        using var circle = Scatter(ChartMarkerShape.Circle);
        using var square = Scatter(ChartMarkerShape.Square);

        // Act
        using var circleBitmap = circle.Paint(360, 240);
        using var squareBitmap = square.Paint(360, 240);

        // Assert
        var layout = circle.ChartLayout;
        var x = layout.XAxis.ToPixel(5);
        var y = layout.YAxis.ToPixel(4.3);
        var blue = new SKColor(0, 0, 255);
        Assert.Equal(blue.WithAlpha(128), GetPixel(circleBitmap, x, y));
        Assert.Equal(blue, GetPixel(circleBitmap, x + 10, y));
        Assert.Equal(0, GetPixelAlpha(circleBitmap, x + 8, y + 8));
        Assert.Equal(blue.WithAlpha(128), GetPixel(squareBitmap, x + 8, y + 8));
        Assert.Equal(blue, GetPixel(squareBitmap, x + 10, y + 5));
    }

    [Fact]
    public void ColorSeriesPaintsEachScatterPointByItsValue()
    {
        // Arrange
        using var view = Scatter(ChartMarkerShape.Circle);
        view.Thresholds = Steps(5);
        view.ThresholdsMode = ChartThresholdsMode.ColorSeries;

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Lime.WithAlpha(128), GetPixel(bitmap, layout.XAxis.ToPixel(5), layout.YAxis.ToPixel(4.3)));
        Assert.Equal(Red.WithAlpha(128), GetPixel(bitmap, layout.XAxis.ToPixel(10) - 4, layout.YAxis.ToPixel(10) + 4));
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static byte GetPixelAlpha(SKBitmap bitmap, double x, double y) => GetPixel(bitmap, x, y).Alpha;

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static List<ChartThreshold> Steps(double value) =>
        [new ChartThreshold { Color = Colors.Lime }, new ChartThreshold { Value = value, Color = Colors.Red }];

    private static TestClamChartView ThresholdChart(string? label)
    {
        var view = new TestClamChartView();
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 0, 100 } });
        view.Thresholds = [new ChartThreshold { Color = Colors.Lime }, new ChartThreshold { Value = 33, Color = Colors.Red, Label = label }];
        view.ThresholdsMode = ChartThresholdsMode.Lines;
        return view;
    }

    private static TestClamChartView Scatter(ChartMarkerShape shape)
    {
        var view = new TestClamChartView { YAxis = new NumericAxis { GridLinesVisible = false } };
        view.Series.Add(new ScatterSeries { ItemsSource = new List<ChartPoint> { new(0, 0), new(10, 10), new(5, 4.3) }, MarkerSize = 20, MarkerShape = shape, Color = Colors.Blue });
        return view;
    }

    private static TestClamChartView Bars(params BarSeries[] series)
    {
        var view = new TestClamChartView();
        foreach (var item in series)
        {
            view.Series.Add(item);
        }

        return view;
    }

    private static SKRect GetBarBounds(TestClamChartView view, int seriesIndex, int index)
    {
        var layout = view.ChartLayout;
        var geometry = layout.Geometries[seriesIndex];
        var (from, to) = geometry.GetBarRange(geometry.X[index]);
        var along1 = (float)layout.XAxis.ToPixel(from);
        var along2 = (float)layout.XAxis.ToPixel(to);
        var across1 = (float)layout.YAxis.ToPixel(geometry.Start[index]);
        var across2 = (float)layout.YAxis.ToPixel(geometry.End[index]);
        return layout.Orientation == ChartOrientation.Vertical
            ? new SKRect(Math.Min(along1, along2), Math.Min(across1, across2), Math.Max(along1, along2), Math.Max(across1, across2))
            : new SKRect(Math.Min(across1, across2), Math.Min(along1, along2), Math.Max(across1, across2), Math.Max(along1, along2));
    }

    private static bool IsRed(SKColor color) => (color.Alpha > 128) && (color.Red > 200) && (color.Green < 100) && (color.Blue < 100);

    private static bool IsWhite(SKColor color) => (color.Alpha > 0) && (color.Red > 200) && (color.Green > 200) && (color.Blue > 200);

    private static bool IsDark(SKColor color) => (color.Alpha > 0) && (color.Red < 80) && (color.Green < 80) && (color.Blue < 80);

    private static bool IsInk(SKColor color) =>
        (color.Alpha > 0) && (color.Red < 160) && (Math.Abs(color.Red - color.Green) < 24) && (Math.Abs(color.Green - color.Blue) < 24);

    private static int CountPixels(SKBitmap bitmap, SKRect bounds, Func<SKColor, bool> predicate)
    {
        var count = 0;
        var left = Math.Max(0, (int)(bounds.Left * Scale));
        var right = Math.Min(bitmap.Width, (int)Math.Ceiling(bounds.Right * Scale));
        var top = Math.Max(0, (int)(bounds.Top * Scale));
        var bottom = Math.Min(bitmap.Height, (int)Math.Ceiling(bounds.Bottom * Scale));
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (predicate(bitmap.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountVisiblePixels(SKBitmap bitmap)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
