namespace ClamChart.Tests.Rendering;

using SkiaExtension;

using SkiaSharp;

public sealed class ChartTextPainterTests
{
    [Fact]
    public void TextIsMeasuredAndEmptyTextHasNoWidth()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");

        // Act
        var text = painter.Create("Sales", 12);

        // Assert
        Assert.True(text.Width > 0);
        Assert.Same(DrawableText.Empty, painter.Create(String.Empty, 12));
    }

    [Fact]
    public void MeasuringTruncatesToTheWidth()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");

        // Act
        var full = painter.Measure("Sales by month", 12, Double.PositiveInfinity);
        var truncated = painter.Measure("Sales by month", 12, full / 2);

        // Assert
        Assert.True(full > 0);
        Assert.True(truncated <= full / 2);
        Assert.True(painter.GetLineHeight(12) > 0);
    }

    [Fact]
    public void HaloIsDrawnOnlyWhenItHasAWidth()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");
        var text = painter.Create("M", 24);

        // Act
        var plain = CountDarkPixels(painter, text, 0);
        var halo = CountDarkPixels(painter, text, 2);

        // Assert
        Assert.Equal(0, plain);
        Assert.True(halo > 0);
    }

    [Fact]
    public void DrawingTheSameTextAgainDoesNotAllocate()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");
        using var bitmap = new SKBitmap(new SKImageInfo(120, 40));
        using var canvas = new SKCanvas(bitmap);
        DrawRepeatedly(painter, canvas);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        DrawRepeatedly(painter, canvas);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void TextIsDrawnWhereTheAlignmentPutsIt()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");
        var text = painter.Create("M", 24);

        // Act
        var left = FindFirstInkColumn(painter, text, SKTextAlign.Left);
        var right = FindFirstInkColumn(painter, text, SKTextAlign.Right);

        // Assert
        Assert.InRange(left, 30, 40);
        Assert.InRange(right, 30 - (int)Math.Ceiling(text.Width) - 1, 30);
    }

    private static void DrawRepeatedly(ChartTextPainter painter, SKCanvas canvas)
    {
        for (var i = 0; i < 10; i++)
        {
            painter.Draw(canvas, painter.Create("Sales 20", 12), 10, 30, SKTextAlign.Center, SKColors.Black, SKColors.White, 1);
        }
    }

    private static int FindFirstInkColumn(ChartTextPainter painter, DrawableText text, SKTextAlign align)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();
        painter.Draw(canvas, text, 30, 30, align, SKColors.Black, SKColors.Transparent, 0);
        painter.Draw(canvas, text, 30, 30, align, SKColors.Black, SKColors.Transparent, 0);
        canvas.Flush();
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 128)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    private static int CountDarkPixels(ChartTextPainter painter, DrawableText text, float haloWidth)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(60, 40, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();
        painter.Draw(canvas, text, 10, 30, SKTextAlign.Left, SKColors.White, SKColors.Black, haloWidth);
        canvas.Flush();

        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if ((pixel.Alpha > 128) && (pixel.Red < 64))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
