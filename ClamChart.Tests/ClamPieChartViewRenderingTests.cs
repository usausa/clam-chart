namespace ClamChart.Tests;

using SkiaSharp;

public sealed class ClamPieChartViewRenderingTests
{
    private const float Scale = 2;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    private static readonly SKColor Orange = new(0xEB, 0x68, 0x34);

    [Fact]
    public void HoleAndBackgroundStayTransparent()
    {
        // Arrange
        using var view = Chart(1, 1);

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(0, GetPixel(bitmap, layout.CenterX, layout.CenterY).Alpha);
        Assert.Equal(0, GetPixel(bitmap, 1, 1).Alpha);
        Assert.Equal(Blue, GetPixel(bitmap, Ring(layout, 90)));
        Assert.Equal(Orange, GetPixel(bitmap, Ring(layout, 270)));
    }

    [Fact]
    public void GapBetweenSlicesIsTransparent()
    {
        // Arrange
        using var view = Chart(1, 1);

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var (x, y) = Ring(layout, 0);
        Assert.Equal(0, GetPixel(bitmap, x, y).Alpha);
        Assert.Equal(Blue, GetPixel(bitmap, x + 3, y));
        Assert.Equal(Orange, GetPixel(bitmap, x - 3, y));
    }

    [Fact]
    public void PieWithoutAHoleHasGapsThatMeetAtTheCenter()
    {
        // Arrange
        using var view = Chart(1, 1, 1, 1);
        view.InnerRadius = 0;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(0, GetPixel(bitmap, layout.CenterX, layout.CenterY - 40).Alpha);
        Assert.Equal(Blue, GetPixel(bitmap, Polar(layout, 45, 40)));
    }

    [Fact]
    public void SingleSliceIsAFullRingWithoutAGap()
    {
        // Arrange
        using var view = Chart(5);

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(Blue, GetPixel(bitmap, Ring(view.ChartLayout, 0)));
    }

    [Fact]
    public void OthersAndExplicitColorsAreUsed()
    {
        // Arrange
        using var view = new TestClamPieChartView();
        view.ItemsSource = new List<ChartEntry> { new("A", 1), new("B", 2), new("C", 30, Colors.Lime), new("D", 40) };
        view.OthersThreshold = 0.1;
        view.CenterValueMode = ChartCenterValueMode.None;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(new SKColor(0, 255, 0), GetPixel(bitmap, Ring(layout, MidAngle(layout, 0))));
        Assert.Equal(Orange, GetPixel(bitmap, Ring(layout, MidAngle(layout, 1))));
        Assert.Equal(new SKColor(0xB5, 0xB3, 0xAA), GetPixel(bitmap, Ring(layout, MidAngle(layout, 2))));
    }

    [Fact]
    public void SelectedSliceMovesOutAndTheOthersDim()
    {
        // Arrange
        using var view = Chart(1, 1);
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 0;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Blue, GetPixel(bitmap, Polar(layout, 90, layout.OuterRadius + 3)));
        Assert.Equal(0, GetPixel(bitmap, Polar(layout, 270, layout.OuterRadius + 3)).Alpha);
        Assert.InRange(GetPixel(bitmap, Ring(layout, 270)).Alpha, 87, 91);
    }

    [Fact]
    public void InsideLabelsUseTheContrastingInk()
    {
        // Arrange
        using var view = Chart(1, 1);
        view.LabelsMode = ChartPieLabelsMode.Inside;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var (x, y) = Ring(view.ChartLayout, 90);
        Assert.True(CountPixels(bitmap, SKRect.Create((float)x - 12, (float)y - 7, 24, 14), static color => (color.Alpha > 0) && (color.Red < 80) && (color.Green < 80) && (color.Blue < 80)) > 5);
    }

    [Fact]
    public void LabelsInsideADimmedSliceFadeWithIt()
    {
        // Arrange
        using var view = Chart(1, 1);
        view.LabelsMode = ChartPieLabelsMode.Inside;
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 0;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var (selectedX, selectedY) = Ring(layout, 90);
        var (dimmedX, dimmedY) = Ring(layout, 270);
        Assert.True(CountPixels(bitmap, SKRect.Create((float)selectedX - 6, (float)selectedY - 7, 24, 14), IsOpaqueInk) > 5);
        Assert.Equal(0, CountPixels(bitmap, SKRect.Create((float)dimmedX - 12, (float)dimmedY - 7, 24, 14), IsOpaqueInk));
    }

    [Fact]
    public void OutsideLabelsDrawALeaderLineInTheSliceColor()
    {
        // Arrange
        using var view = Chart(1, 1);
        view.LabelsMode = ChartPieLabelsMode.Outside;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var right = layout.OutsideLabels[0];
        var left = layout.OutsideLabels[1];
        Assert.True(IsNear(GetPixel(bitmap, (right.ElbowX + right.EndX) / 2, right.ElbowY), Blue));
        Assert.True(IsNear(GetPixel(bitmap, (left.ElbowX + left.EndX) / 2, left.ElbowY), Orange));
        Assert.True(right.Bounds.X > layout.CenterX + layout.OuterRadius);
        Assert.True(CountPixels(bitmap, SKRect.Create((float)right.Bounds.X, (float)right.Bounds.Y, (float)right.Bounds.Width, (float)right.Bounds.Height), static color => color.Alpha > 0) > 5);
    }

    [Fact]
    public void CenterTextIsDrawnInTheHole()
    {
        // Arrange
        using var shown = Chart(1, 1);
        shown.CenterValueMode = ChartCenterValueMode.Auto;
        shown.CenterLabel = "Total";
        using var hidden = Chart(1, 1);

        // Act
        using var first = shown.Paint(300, 300);
        using var second = hidden.Paint(300, 300);

        // Assert
        Assert.True(CountPixels(first, Hole(shown.ChartLayout), static color => color.Alpha > 0) > 20);
        Assert.Equal(0, CountPixels(second, Hole(hidden.ChartLayout), static color => color.Alpha > 0));
    }

    [Fact]
    public void LegendMarkersUseTheSliceColors()
    {
        // Arrange
        using var view = Chart(1, 1);
        view.LegendVisible = true;
        view.LegendValueMode = ChartLegendValueMode.Percent;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var items = view.ChartLayout.Legend.Items;
        Assert.Equal(Blue, GetPixel(bitmap, items[0].Marker.CenterX, items[0].Marker.CenterY));
        Assert.Equal(Orange, GetPixel(bitmap, items[1].Marker.CenterX, items[1].Marker.CenterY));
        var value = items[0].Value;
        Assert.True(CountPixels(bitmap, SKRect.Create((float)value.X, (float)value.Y, (float)value.Width, (float)value.Height), static color => color.Alpha > 0) > 5);
    }

    [Fact]
    public void UpperSemicircleLeavesTheBottomEmpty()
    {
        // Arrange
        using var view = Chart(1, 1);
        view.StartAngle = 270;
        view.SweepAngle = 180;

        // Act
        using var bitmap = view.Paint(300, 160);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Blue, GetPixel(bitmap, Ring(layout, 300)));
        Assert.Equal(Orange, GetPixel(bitmap, Ring(layout, 60)));
        Assert.Equal(0, GetPixel(bitmap, layout.CenterX - layout.OuterRadius + 4, layout.CenterY + 2).Alpha);
        Assert.True(layout.CenterY > 150);
    }

    [Fact]
    public void EmptyTextIsDrawnWithoutData()
    {
        // Arrange
        using var view = new TestClamPieChartView();
        using var silent = new TestClamPieChartView();
        view.ItemsSource = new List<ChartEntry>();
        view.EmptyText = "No data";
        silent.ItemsSource = new List<ChartEntry>();

        // Act
        using var first = view.Paint(300, 300);
        using var second = silent.Paint(300, 300);

        // Assert
        Assert.True(CountPixels(first, SKRect.Create(100, 140, 100, 20), static color => color.Alpha > 0) > 10);
        Assert.Equal(0, CountPixels(second, SKRect.Create(0, 0, 300, 300), static color => color.Alpha > 0));
    }

    [Theory]
    [InlineData(ChartPieLabelsMode.Inside)]
    [InlineData(ChartPieLabelsMode.Outside)]
    public void FramesDoNotAllocate(ChartPieLabelsMode mode)
    {
        // Arrange
        using var view = Chart(10, 20, 30);
        view.LabelsMode = mode;
        view.LegendVisible = true;
        view.CenterLabel = "Total";
        view.SelectionMode = ChartSelectionMode.Item;
        view.CenterValueMode = ChartCenterValueMode.Auto;
        using var bitmap = view.Paint(340, 340);
        using var surface = SKSurface.Create(new SKImageInfo(680, 680));
        var idle = MeasureFrame(view, surface.Canvas);
        view.SelectedIndex = 1;

        // Act
        var selected = MeasureFrame(view, surface.Canvas);

        // Assert
        Assert.Equal(0, idle);
        Assert.Equal(0, selected);
    }

    [Fact]
    public void HiddenSliceIsFadedInTheLegend()
    {
        // Arrange
        using var view = Chart(1, 1);
        view.LegendVisible = true;
        view.LegendToggleEnabled = true;
        using var first = view.Paint(300, 300);
        var bounds = view.ChartLayout.Legend.Items[0].Bounds;
        var point = new Point(bounds.CenterX, bounds.CenterY);

        // Act
        view.HandleTouch(1, SkiaSharp.Views.Maui.SKTouchAction.Pressed, point, 0);
        view.HandleTouch(1, SkiaSharp.Views.Maui.SKTouchAction.Released, point, 10);
        using var bitmap = view.Paint(300, 300);

        // Assert
        var items = view.ChartLayout.Legend.Items;
        var hidden = GetPixel(bitmap, items[0].Marker.CenterX, items[0].Marker.CenterY);
        var alpha = (int)Math.Round(255 * ChartStyles.Light.DimmedOpacity);
        Assert.InRange(hidden.Alpha, alpha - 2, alpha + 2);
        Assert.Equal(Orange, GetPixel(bitmap, items[1].Marker.CenterX, items[1].Marker.CenterY));
        Assert.Equal(Orange, GetPixel(bitmap, Ring(view.ChartLayout, 90)));
        Assert.Equal(Orange, GetPixel(bitmap, Ring(view.ChartLayout, 270)));
    }

    private static TestClamPieChartView Chart(params double[] values) =>
        new() { ItemsSource = values.Select(static (value, index) => new ChartEntry($"C{index}", value)).ToList(), CenterValueMode = ChartCenterValueMode.None };

    private static bool IsOpaqueInk(SKColor color) => (color.Alpha > 200) && (color.Red < 80) && (color.Green < 80) && (color.Blue < 80);

    private static bool IsNear(SKColor color, SKColor expected) =>
        (color.Alpha > 200) && (Math.Abs(color.Red - expected.Red) < 8) && (Math.Abs(color.Green - expected.Green) < 8) && (Math.Abs(color.Blue - expected.Blue) < 8);

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static SKColor GetPixel(SKBitmap bitmap, (double X, double Y) point) => GetPixel(bitmap, point.X, point.Y);

    private static (double X, double Y) Polar(PieLayout layout, double angle, double radius)
    {
        var (x, y) = PieLayout.GetDirection(angle);
        return (layout.CenterX + (x * radius), layout.CenterY + (y * radius));
    }

    private static (double X, double Y) Ring(PieLayout layout, double angle) => Polar(layout, angle, (layout.OuterRadius + layout.InnerRadius) / 2);

    private static double MidAngle(PieLayout layout, int sliceIndex) =>
        layout.GetAngle((layout.Slices[sliceIndex].Start + layout.Slices[sliceIndex].End) / 2);

    private static SKRect Hole(PieLayout layout)
    {
        var half = (float)(layout.InnerRadius * 0.7);
        return new SKRect((float)layout.CenterX - half, (float)layout.CenterY - half, (float)layout.CenterX + half, (float)layout.CenterY + half);
    }

    private static int CountPixels(SKBitmap bitmap, SKRect bounds, Func<SKColor, bool> predicate)
    {
        var count = 0;
        for (var y = (int)(bounds.Top * Scale); y < (int)(bounds.Bottom * Scale); y++)
        {
            for (var x = (int)(bounds.Left * Scale); x < (int)(bounds.Right * Scale); x++)
            {
                if ((x >= 0) && (y >= 0) && (x < bitmap.Width) && (y < bitmap.Height) && predicate(bitmap.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static long MeasureFrame(TestClamPieChartView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        view.Render(canvas, 340, 340);
        view.Render(canvas, 340, 340);
        var allocated = ChartAllocation.Measure(() =>
        {
            view.Render(canvas, 340, 340);
        });
        canvas.Restore();
        return allocated;
    }
}
