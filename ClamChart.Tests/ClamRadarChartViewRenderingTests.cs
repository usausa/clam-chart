namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamRadarChartViewRenderingTests
{
    private const float Scale = 2;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    private static readonly SKColor Orange = new(0xEB, 0x68, 0x34);

    [Fact]
    public void SeriesFillsItsShapeFaintlyAndStrokesTheEdge()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 10, 10, 10));

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var radius = layout.Radius;
        var fill = GetPixel(bitmap, layout.CenterX + (radius * 0.25), layout.CenterY - (radius * 0.25));
        var edge = GetPixel(bitmap, layout.CenterX + (radius * 0.5), layout.CenterY - (radius * 0.5));
        Assert.True(IsNear(fill, Blue));
        Assert.InRange(fill.Alpha, 36, 40);
        Assert.Equal(Blue, edge);
        Assert.Equal(0, GetPixel(bitmap, 2, 2).Alpha);
    }

    [Fact]
    public void GridShapeChangesTheRings()
    {
        // Arrange
        using var polygon = Chart(Series("First", 1, 1, 1, 1));
        polygon.Maximum = 10;
        using var circle = Chart(Series("First", 1, 1, 1, 1));
        circle.Maximum = 10;
        circle.GridShape = ChartRadarGridShape.Circle;

        // Act
        using var first = polygon.Paint(300, 300);
        using var second = circle.Paint(300, 300);

        // Assert
        var layout = polygon.ChartLayout;
        var offset = layout.Radius * 0.4 * Math.Sqrt(0.5);
        var spoke = layout.CenterX + (layout.Radius * 0.5);
        Assert.Equal(0, GetMaxAlpha(first, layout.CenterX + offset, layout.CenterY - offset));
        Assert.True(GetMaxAlpha(second, layout.CenterX + offset, layout.CenterY - offset) > 0);
        Assert.True(GetMaxAlpha(first, spoke, layout.CenterY) > 0);
        Assert.Equal(0, GetMaxAlpha(first, spoke, layout.CenterY + 4));
    }

    [Fact]
    public void LevelLabelsSitOnTheTopSpokeAndCutTheGrid()
    {
        // Arrange
        using var view = Chart(Series("First", 1, 1, 1, 1));
        view.Maximum = 10;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var height = layout.LabelHeight;
        var padding = RadarLayout.LevelLabelPadding;
        var six = layout.CenterY - (layout.Radius * 0.6);
        var eight = layout.CenterY - (layout.Radius * 0.8);
        var cut = six - (height / 2) - padding;
        var open = eight + (height / 2) + padding;
        Assert.Equal([2d, 4, 6, 8, 10], layout.Levels.Select(static x => x.Value));
        Assert.True(open < cut - 2);
        Assert.Equal(0, GetMaxAlpha(bitmap, layout.CenterX, cut + 1));
        Assert.True(GetMaxAlpha(bitmap, layout.CenterX, (cut + open) / 2) > 0);
        Assert.True(GetMaxAlpha(bitmap, layout.CenterX, six, 3) > 0);
    }

    [Fact]
    public void SelectedSeriesDimsTheOthersAndMarksTheVertex()
    {
        // Arrange
        var second = Series("Second", 5, 5, 5, 5);
        using var view = Chart(Series("First", 9, 9, 9, 9), second);
        view.Maximum = 10;
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedSeries = second;
        view.SelectedIndex = 1;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var radius = layout.Radius;
        var edge = GetPixel(bitmap, layout.CenterX + (radius * 0.45), layout.CenterY - (radius * 0.45));
        var marker = GetPixel(bitmap, layout.CenterX + (radius * 0.5), layout.CenterY);
        Assert.InRange(edge.Alpha, 85, 100);
        Assert.Equal(Orange, marker);
        Assert.Equal(0, GetPixel(bitmap, layout.CenterX + (radius * 0.5) + 5, layout.CenterY).Alpha);
    }

    [Fact]
    public void CategorySelectionMarksTheItemOnEverySeries()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 10, 10, 10), Series("Second", 5, 5, 5, 5));
        view.SelectionMode = ChartSelectionMode.Category;
        view.SelectedIndex = 1;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var layout = view.ChartLayout;
        var radius = layout.Radius;
        Assert.Equal(Blue, GetPixel(bitmap, layout.CenterX + radius, layout.CenterY));
        Assert.Equal(Orange, GetPixel(bitmap, layout.CenterX + (radius * 0.5), layout.CenterY));
        Assert.Equal(Blue, GetPixel(bitmap, layout.CenterX + (radius * 0.5), layout.CenterY - (radius * 0.5)));
    }

    [Fact]
    public void TrackballDrawsTheAxisMarkersAndTheTooltip()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 10, 10, 10), Series("Second", 5, 5, 5, 5));
        view.TrackballMode = ChartTrackballMode.Touch;
        view.TooltipVisible = false;
        using var first = view.Paint(300, 300);
        var layout = view.ChartLayout;
        var finger = new Point(layout.CenterX + layout.Radius + 10, layout.CenterY + (layout.Radius * 0.6));

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, finger, 0);
        using var markers = view.Paint(300, 300);
        view.TooltipVisible = true;
        using var tooltip = view.Paint(300, 300);

        // Assert
        Assert.Equal(Blue, GetPixel(markers, layout.CenterX + layout.Radius, layout.CenterY));
        Assert.Equal(Orange, GetPixel(markers, layout.CenterX + (layout.Radius * 0.5), layout.CenterY));
        Assert.Equal(0, GetPixel(markers, finger.X, finger.Y - 50).Alpha);
        Assert.Equal(ChartStyles.Light.TooltipBackground.ToSKColor().Alpha, GetPixel(tooltip, finger.X, finger.Y - 50).Alpha);
    }

    [Fact]
    public void LegendDimsTheHiddenSeries()
    {
        // Arrange
        var hidden = Series("Second", 5, 5, 5, 5);
        hidden.IsVisible = false;
        using var view = Chart(Series("First", 10, 10, 10, 10), hidden);
        view.LegendVisible = true;

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var items = view.ChartLayout.Legend.Items;
        Assert.Equal(2, items.Count);
        Assert.Equal(Blue, GetPixel(bitmap, items[0].Marker.CenterX, items[0].Marker.CenterY));
        Assert.Equal((byte)Math.Round(255 * ChartStyles.Light.DimmedOpacity), GetPixel(bitmap, items[1].Marker.CenterX, items[1].Marker.CenterY).Alpha);
    }

    [Fact]
    public void MinimalDensityDrawsOnlyTheShape()
    {
        // Arrange
        using var view = Chart(Series("First", 10, 10, 10, 10), Series("Second", 5, 5, 5, 5));
        view.LegendVisible = null;

        // Act
        using var bitmap = view.Paint(150, 90);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(ChartDensity.Minimal, view.EffectiveDensity);
        Assert.False(layout.ShowsLabels);
        Assert.False(layout.Legend.IsVisible);
        Assert.Equal(44, layout.Radius, 6);
    }

    [Fact]
    public void EmptyChartShowsTheEmptyText()
    {
        // Arrange
        using var view = new TestClamRadarChartView { EmptyText = "No data" };

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.True(GetMaxAlpha(bitmap, 150, 150, 20) > 0);
    }

    [Fact]
    public void IdleSelectedAndTrackedFramesDoNotAllocate()
    {
        // Arrange
        var second = Series("Second", 5, 5, 5, 5);
        using var view = Chart(Series("First", 10, 10, 10, 10), second);
        view.LegendVisible = true;
        view.SelectionMode = ChartSelectionMode.Item;
        view.TrackballMode = ChartTrackballMode.Touch;
        using var first = view.Paint(300, 300);
        using var surface = SKSurface.Create(new SKImageInfo(600, 600));
        var layout = view.ChartLayout;

        // Act
        var idle = MeasureFrame(view, surface.Canvas);
        view.SelectedSeries = second;
        view.SelectedIndex = 2;
        var selected = MeasureFrame(view, surface.Canvas);
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(layout.CenterX + 40, layout.CenterY + 5), 0);
        var tracked = MeasureFrame(view, surface.Canvas);

        // Assert
        Assert.Equal(0, idle);
        Assert.Equal(0, selected);
        Assert.Equal(0, tracked);
    }

    private static TestClamRadarChartView Chart(params RadarSeries[] series)
    {
        var view = new TestClamRadarChartView();
        foreach (var item in series)
        {
            view.Series.Add(item);
        }

        return view;
    }

    private static RadarSeries Series(string title, params double[] values) =>
        new() { Title = title, ItemsSource = values.Select(static (value, index) => new ChartEntry(((char)('A' + index)).ToString(), value)).ToList() };

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static bool IsNear(SKColor color, SKColor expected) =>
        (Math.Abs(color.Red - expected.Red) <= 8) && (Math.Abs(color.Green - expected.Green) <= 8) && (Math.Abs(color.Blue - expected.Blue) <= 8);

    private static byte GetMaxAlpha(SKBitmap bitmap, double x, double y, int reach = 1)
    {
        var alpha = (byte)0;
        var cx = (int)(x * Scale);
        var cy = (int)(y * Scale);
        for (var dy = -reach; dy <= reach; dy++)
        {
            for (var dx = -reach; dx <= reach; dx++)
            {
                alpha = Math.Max(alpha, bitmap.GetPixel(cx + dx, cy + dy).Alpha);
            }
        }

        return alpha;
    }

    private static long MeasureFrame(TestClamRadarChartView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        view.Render(canvas, 300, 300);
        view.Render(canvas, 300, 300);
        var before = GC.GetAllocatedBytesForCurrentThread();
        view.Render(canvas, 300, 300);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }
}
