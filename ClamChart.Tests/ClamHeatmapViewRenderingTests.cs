namespace ClamChart.Tests;

using ClamChart.Tests.Data;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamHeatmapViewRenderingTests
{
    private const float Scale = 2;

    [Fact]
    public void CellsTakeThePaletteColorOfTheirValue()
    {
        // Arrange
        using var view = Strip(0, 50, 100);
        var palette = Palette(ChartPalettes.OrangesLight);

        // Act
        using var bitmap = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(palette[0], GetPixel(bitmap, Center(layout, 0)));
        Assert.Equal(palette[32], GetPixel(bitmap, Center(layout, 1)));
        Assert.Equal(palette[63], GetPixel(bitmap, Center(layout, 2)));
    }

    [Fact]
    public void GapsBetweenCellsAreTransparent()
    {
        // Arrange
        using var view = Strip(0, 50, 100);

        // Act
        using var bitmap = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        var cell = layout.GetCellBounds(0);
        Assert.Equal(0, GetPixel(bitmap, cell.Right - 0.5, cell.CenterY).Alpha);
        Assert.NotEqual(0, GetPixel(bitmap, cell.Right - 1.5, cell.CenterY).Alpha);
    }

    [Fact]
    public void MissingCellsUseTheTrack()
    {
        // Arrange
        using var view = Matrix(new TestHeatItem("Mon", "9", 0, 1), new TestHeatItem("Tue", "10", 0, 5));

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        var track = GetPixel(bitmap, Center(view.ChartLayout, 1));
        Assert.Equal(18, track.Alpha);
        Assert.True(track.Red < 10);
    }

    [Fact]
    public void DarkStyleReversesTheScale()
    {
        // Arrange
        using var view = Strip(0, 100);
        view.ChartStyle = ChartStyles.Dark;

        // Act
        using var bitmap = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(new SKColor(0x7F, 0x27, 0x04), GetPixel(bitmap, Center(layout, 0)));
        Assert.Equal(new SKColor(0xFF, 0xF5, 0xEB), GetPixel(bitmap, Center(layout, 1)));
    }

    [Fact]
    public void ColorScaleReplacesTheStyleColors()
    {
        // Arrange
        using var view = Strip(0, 100);
        view.ColorScale = [Colors.Black, Colors.White];

        // Act
        using var bitmap = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(new SKColor(0, 0, 0), GetPixel(bitmap, Center(layout, 0)));
        Assert.Equal(new SKColor(255, 255, 255), GetPixel(bitmap, Center(layout, 1)));
    }

    [Fact]
    public void ScaleLegendShowsTheWholeRamp()
    {
        // Arrange
        using var view = Strip(0, 50, 100);
        view.LegendVisible = true;
        var palette = Palette(ChartPalettes.OrangesLight);

        // Act
        using var bitmap = view.Paint(300, 120);

        // Assert
        var scale = view.ChartLayout.ScaleBounds;
        Assert.Equal(palette[0], GetPixel(bitmap, scale.X + 1, scale.CenterY));
        Assert.Equal(palette[63], GetPixel(bitmap, scale.Right - 1, scale.CenterY));
        Assert.Equal(palette[32], GetPixel(bitmap, scale.CenterX + 1, scale.CenterY));
    }

    [Fact]
    public void TranslucentScaleStepsDoNotStack()
    {
        // Arrange
        using var view = Strip(0, 50, 100);
        view.LegendVisible = true;
        view.LegendPosition = ChartLegendPosition.Right;
        view.ColorScale = [Color.FromArgb("#1A3274D9"), Color.FromArgb("#3274D9")];

        // Act
        using var bitmap = view.Paint(300, 120);

        // Assert
        var scale = view.ChartLayout.ScaleBounds;
        var top = GetPixel(bitmap, scale.CenterX, scale.Y + 1).Alpha;
        var middle = GetPixel(bitmap, scale.CenterX, scale.CenterY).Alpha;
        var bottom = GetPixel(bitmap, scale.CenterX, scale.Bottom - 1).Alpha;
        Assert.InRange(bottom, 20, 40);
        Assert.InRange(middle, 120, 160);
        Assert.InRange(top, 245, 255);
    }

    [Fact]
    public void SelectedCellIsOutlined()
    {
        // Arrange
        using var view = Strip(0, 50, 100);
        view.SelectionMode = ChartSelectionMode.Item;
        view.SelectedIndex = 1;
        var palette = Palette(ChartPalettes.OrangesLight);

        // Act
        using var bitmap = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        var cell = layout.GetCellBounds(1);
        Assert.Equal(ChartStyles.Light.PrimaryTextColor.ToSKColor(), GetPixel(bitmap, cell.X - 0.5, cell.CenterY));
        Assert.Equal(palette[32], GetPixel(bitmap, cell.X + 3, cell.CenterY));
    }

    [Fact]
    public void CalendarDrawsOnlyTheDaysInTheRange()
    {
        // Arrange
        var start = new DateTime(2026, 10, 1);
        using var view = new TestClamHeatmapView
        {
            LayoutMode = ChartHeatmapLayoutMode.Calendar,
            Culture = CultureInfo.GetCultureInfo("en-US"),
            ItemsSource = Enumerable.Range(0, 22).Select(day => new ChartTimePoint(start.AddDays(day), day)).ToList()
        };

        // Act
        using var bitmap = view.Paint(360, 200);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(0, GetPixel(bitmap, Center(layout, 3 * layout.ColumnCount)).Alpha);
        Assert.Equal(255, GetPixel(bitmap, Center(layout, 4 * layout.ColumnCount)).Alpha);
    }

    [Fact]
    public void EmptyHeatmapShowsTheEmptyText()
    {
        // Arrange
        using var view = new TestClamHeatmapView { EmptyText = "No data" };

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        Assert.True(GetMaxAlpha(bitmap, 150, 100, 20) > 0);
    }

    [Fact]
    public void MinimalDensityDrawsOnlyTheCells()
    {
        // Arrange
        using var view = Matrix(new TestHeatItem("Mon", "9", 0, 1), new TestHeatItem("Tue", "10", 0, 5));
        view.LegendVisible = null;

        // Act
        using var bitmap = view.Paint(150, 90);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(ChartDensity.Minimal, view.EffectiveDensity);
        Assert.Empty(layout.RowLabels);
        Assert.Empty(layout.ColumnLabels);
        Assert.False(layout.ShowsScale);
        Assert.Equal(new ChartRect(0, 0, 150, 90), layout.GridBounds);
    }

    [Fact]
    public void IdleSelectedAndTrackedFramesDoNotAllocate()
    {
        // Arrange
        using var view = Strip(0, 50, 100);
        view.LegendVisible = true;
        view.SelectionMode = ChartSelectionMode.Item;
        view.TrackballMode = ChartTrackballMode.Touch;
        using var first = view.Paint(300, 120);
        using var surface = SKSurface.Create(new SKImageInfo(600, 240));
        var layout = view.ChartLayout;

        // Act
        var idle = MeasureFrame(view, surface.Canvas);
        view.SelectedIndex = 2;
        var selected = MeasureFrame(view, surface.Canvas);
        var (x, y) = Center(layout, 1);
        view.HandleTouch(1, SKTouchAction.Pressed, new Point(x, y), 0);
        var tracked = MeasureFrame(view, surface.Canvas);

        // Assert
        Assert.Equal(0, idle);
        Assert.Equal(0, selected);
        Assert.Equal(0, tracked);
    }

    private static TestClamHeatmapView Strip(params double[] values) =>
        new() { ItemsSource = values.Select(static (value, index) => new ChartEntry($"C{index}", value)).ToList() };

    private static TestClamHeatmapView Matrix(params TestHeatItem[] items) =>
        new() { ValueAccessors = TestHeatItem.Accessors, RowKey = "day", ColumnKey = "slot", ValueKey = "value", ItemsSource = items.ToList() };

    private static SKColor[] Palette(IReadOnlyList<Color> colors)
    {
        var palette = new SKColor[HeatmapPalette.Steps];
        HeatmapPalette.Build(colors, palette);
        return palette;
    }

    private static (double X, double Y) Center(HeatmapLayout layout, int cell)
    {
        var bounds = layout.GetCellBounds(cell);
        return (bounds.X + ((bounds.Width - layout.Gap) / 2), bounds.Y + ((bounds.Height - layout.Gap) / 2));
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static SKColor GetPixel(SKBitmap bitmap, (double X, double Y) point) => GetPixel(bitmap, point.X, point.Y);

    private static byte GetMaxAlpha(SKBitmap bitmap, double x, double y, int reach)
    {
        var alpha = (byte)0;
        for (var dy = -reach; dy <= reach; dy++)
        {
            for (var dx = -reach; dx <= reach; dx++)
            {
                alpha = Math.Max(alpha, bitmap.GetPixel((int)(x * Scale) + dx, (int)(y * Scale) + dy).Alpha);
            }
        }

        return alpha;
    }

    private static long MeasureFrame(TestClamHeatmapView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        view.Render(canvas, 300, 120);
        view.Render(canvas, 300, 120);
        var before = GC.GetAllocatedBytesForCurrentThread();
        view.Render(canvas, 300, 120);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }
}
