namespace ClamChart.Tests;

using SkiaSharp;

public sealed class ClamSparklineViewRenderingTests
{
    private const float Scale = 2;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    private static readonly SKColor Critical = new(0xE0, 0x2F, 0x44);

    [Fact]
    public void LineIsDrawnWithTheFirstPaletteColorOnATransparentBackground()
    {
        // Arrange
        using var view = Spark([0, 10]);
        view.Markers = ChartSparklineMarkers.None;

        // Act
        using var bitmap = view.Paint(100, 40);

        // Assert
        var layout = view.ChartLayout;
        var (x, y) = Point(layout, 0.5, 5);
        Assert.Equal(Blue, GetPixel(bitmap, x, y));
        Assert.Equal(0, GetPixel(bitmap, x, y - 10).Alpha);
    }

    [Fact]
    public void AreaIsFilledFaintlyBelowTheLine()
    {
        // Arrange
        using var view = Spark([0, 10, 5, 5]);
        view.Kind = ChartSparklineKind.Area;
        view.Markers = ChartSparklineMarkers.None;

        // Act
        using var bitmap = view.Paint(100, 40);

        // Assert
        var layout = view.ChartLayout;
        var (x, y) = Point(layout, 2.5, 5);
        Assert.InRange(GetPixel(bitmap, x, y + 8).Alpha, 22, 30);
        Assert.Equal(0, GetPixel(bitmap, x, y - 6).Alpha);
    }

    [Fact]
    public void BarsUseTheNegativeColorBelowZero()
    {
        // Arrange
        using var view = Spark([4, -4]);
        view.Kind = ChartSparklineKind.Bar;

        // Act
        using var bitmap = view.Paint(100, 40);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Blue, GetPixel(bitmap, layout.ToX(0, layout.Scale), 10));
        Assert.Equal(Critical, GetPixel(bitmap, layout.ToX(1, layout.Scale), 30));
        Assert.Equal(0, GetPixel(bitmap, layout.ToX(0, layout.Scale), 30).Alpha);
        Assert.Equal(0, GetPixel(bitmap, layout.ToX(1, layout.Scale), 10).Alpha);
    }

    [Fact]
    public void WinLossSplitsTheHeightBySign()
    {
        // Arrange
        using var view = Spark([0.2, -300, 0]);
        view.Kind = ChartSparklineKind.WinLoss;
        view.NegativeColor = Colors.Lime;

        // Act
        using var bitmap = view.Paint(90, 40);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Blue, GetPixel(bitmap, layout.ToX(0, layout.Scale), 2));
        Assert.Equal(new SKColor(0, 255, 0), GetPixel(bitmap, layout.ToX(1, layout.Scale), 38));
        Assert.Equal(0, GetPixel(bitmap, layout.ToX(1, layout.Scale), 2).Alpha);
        Assert.Equal(0, GetPixel(bitmap, layout.ToX(2, layout.Scale), 2).Alpha);
        Assert.Equal(0, GetPixel(bitmap, layout.ToX(2, layout.Scale), 38).Alpha);
    }

    [Fact]
    public void LastMarkerHasATransparentRing()
    {
        // Arrange
        using var view = Spark([0, 10, 5]);
        view.Thickness = 2;

        // Act
        using var bitmap = view.Paint(100, 40);

        // Assert
        var layout = view.ChartLayout;
        var (x, y) = Point(layout, 2, 5);
        var (cutX, cutY) = Toward(Point(layout, 2, 5), Point(layout, 1, 10), 3);
        var (lineX, lineY) = Point(layout, 1.5, 7.5);
        Assert.Equal(Blue, GetPixel(bitmap, x, y));
        Assert.Equal(0, GetPixel(bitmap, cutX, cutY).Alpha);
        Assert.Equal(Blue, GetPixel(bitmap, lineX, lineY));
    }

    [Fact]
    public void MarkersAreDrawnAtTheExtremes()
    {
        // Arrange
        using var view = Spark([3, 10, 0, 6]);
        view.Thickness = 4;
        view.Markers = ChartSparklineMarkers.High | ChartSparklineMarkers.Low;

        // Act
        using var bitmap = view.Paint(160, 60);

        // Assert
        var layout = view.ChartLayout;
        var (highX, highY) = Toward(Point(layout, 1, 10), Point(layout, 0, 3), 6);
        var (lowX, lowY) = Toward(Point(layout, 2, 0), Point(layout, 3, 6), 6);
        var (lineX, lineY) = Toward(Point(layout, 1, 10), Point(layout, 0, 3), 14);
        Assert.Equal(0, GetPixel(bitmap, highX, highY).Alpha);
        Assert.Equal(0, GetPixel(bitmap, lowX, lowY).Alpha);
        Assert.Equal(Blue, GetPixel(bitmap, lineX, lineY));
    }

    [Fact]
    public void ReferenceValueIsADashedHairline()
    {
        // Arrange
        using var view = Spark([0, 0.5, 0]);
        view.Markers = ChartSparklineMarkers.None;
        view.ReferenceValue = 2;

        // Act
        using var bitmap = view.Paint(100, 40);

        // Assert
        var layout = view.ChartLayout;
        var row = (int)(layout.ToY(2, layout.Scale) * Scale);
        var inked = 0;
        var empty = 0;
        for (var x = (int)(layout.Plot.X * Scale); x < (int)(layout.Plot.Right * Scale); x++)
        {
            if (bitmap.GetPixel(x, row).Alpha > 0)
            {
                inked++;
            }
            else
            {
                empty++;
            }
        }

        Assert.True(inked > 20);
        Assert.True(empty > 20);
    }

    [Fact]
    public void SameBoundsGiveTheSameScale()
    {
        // Arrange
        using var low = Spark([10, 20]);
        using var high = Spark([60, 80]);
        foreach (var view in new[] { low, high })
        {
            view.Minimum = 0;
            view.Maximum = 100;
        }

        // Act
        using var first = low.Paint(100, 40);
        using var second = high.Paint(100, 40);

        // Assert
        Assert.Equal(low.ChartLayout.Scale, high.ChartLayout.Scale);
        Assert.Equal(low.ChartLayout.ToY(50, low.ChartLayout.Scale), high.ChartLayout.ToY(50, high.ChartLayout.Scale));
    }

    [Fact]
    public void EmptyValuesShowTheEmptyText()
    {
        // Arrange
        using var view = Spark([]);
        view.EmptyText = "No data";
        using var silent = Spark([Double.NaN]);

        // Act
        using var first = view.Paint(120, 40);
        using var second = silent.Paint(120, 40);

        // Assert
        Assert.True(CountInk(first) > 10);
        Assert.Equal(0, CountInk(second));
    }

    [Theory]
    [InlineData(ChartSparklineKind.Bar)]
    [InlineData(ChartSparklineKind.WinLoss)]
    public void BarFramesDoNotAllocate(ChartSparklineKind kind)
    {
        // Arrange
        using var view = Spark([3, -1, 4, 1, -5, 9, 2, 6]);
        view.Kind = kind;
        using var bitmap = view.Paint(120, 28);
        using var surface = SKSurface.Create(new SKImageInfo(240, 56));

        // Act
        var allocated = MeasureFrame(view, surface.Canvas);

        // Assert
        Assert.Equal(0, allocated);
    }

    [Theory]
    [InlineData(ChartSparklineKind.Line, 1)]
    [InlineData(ChartSparklineKind.Area, 2)]
    public void LineFramesAllocateOnlyTheSkiaPaths(ChartSparklineKind kind, int paths)
    {
        // Arrange
        using var view = Spark([3, -1, 4, 1, -5, 9, 2, 6]);
        view.Kind = kind;
        view.Smooth = true;
        view.ReferenceValue = 2;
        view.Markers = ChartSparklineMarkers.First | ChartSparklineMarkers.Last | ChartSparklineMarkers.High | ChartSparklineMarkers.Low;
        using var bitmap = view.Paint(120, 28);
        using var surface = SKSurface.Create(new SKImageInfo(240, 56));
        var path = MeasurePath();

        // Act
        var allocated = MeasureFrame(view, surface.Canvas);

        // Assert
        Assert.True(path > 0);
        Assert.True(allocated <= path * paths, $"allocated={allocated} path={path}");
    }

    private static TestClamSparklineView Spark(double[] values) => new() { Values = values };

    private static (double X, double Y) Point(SparklineLayout layout, double position, double value) =>
        (layout.ToX(position, layout.Scale), layout.ToY(value, layout.Scale));

    private static (double X, double Y) Toward((double X, double Y) from, (double X, double Y) to, double distance)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        return (from.X + (dx / length * distance), from.Y + (dy / length * distance));
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static int CountInk(SKBitmap bitmap)
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

    private static long MeasurePath()
    {
        using var builder = new SKPathBuilder();
        Build(builder);
        Build(builder);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Build(builder);
        return GC.GetAllocatedBytesForCurrentThread() - before;

        static void Build(SKPathBuilder builder)
        {
            builder.MoveTo(0, 0);
            builder.LineTo(10, 10);
            using var path = builder.Detach();
        }
    }

    private static long MeasureFrame(TestClamSparklineView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        view.Render(canvas, 120, 28);
        view.Render(canvas, 120, 28);
        var before = GC.GetAllocatedBytesForCurrentThread();
        view.Render(canvas, 120, 28);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        canvas.Restore();
        return allocated;
    }
}
