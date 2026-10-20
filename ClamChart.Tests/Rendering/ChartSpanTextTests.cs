namespace ClamChart.Tests.Rendering;

using SkiaSharp;

public sealed class ChartSpanTextTests
{
    [Fact]
    public void WidthMatchesTheWholeText()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");
        var spans = new ChartSpanText();

        // Act
        var width = spans.Measure(painter, "12,345.6K", 24);

        // Assert
        Assert.Equal(painter.Create("12,345.6K", 24).Width, width, 2);
        Assert.Equal(9, spans.ElementCount);
    }

    [Fact]
    public void DrawingMatchesTheWholeText()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");
        var spans = new ChartSpanText();
        using var whole = new SKBitmap(new SKImageInfo(240, 60));
        using var pieces = new SKBitmap(new SKImageInfo(240, 60));

        // Act
        using (var canvas = new SKCanvas(whole))
        {
            painter.Draw(canvas, painter.Create("98,765", 32), 8, 44, SKTextAlign.Left, SKColors.Black, SKColors.Transparent, 0);
        }

        using (var canvas = new SKCanvas(pieces))
        {
            spans.Draw(canvas, painter, "98,765", 32, 8, 44, SKColors.Black, SKColors.Transparent, 0);
        }

        // Assert
        Assert.True(CountDifferences(whole, pieces) <= 4);
        Assert.True(CountInk(whole) > 100);
    }

    [Fact]
    public void CombinedCharactersStayTogether()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");
        var spans = new ChartSpanText();

        // Act
        var width = spans.Measure(painter, "e\u0301", 24);

        // Assert
        Assert.Equal(1, spans.ElementCount);
        Assert.Equal(painter.Create("e\u0301", 24).Width, width, 2);
    }

    [Fact]
    public void DrawingKnownCharactersDoesNotAllocate()
    {
        // Arrange
        using var painter = new ChartTextPainter("sans-serif");
        var spans = new ChartSpanText();
        using var bitmap = new SKBitmap(new SKImageInfo(240, 60));
        using var canvas = new SKCanvas(bitmap);
        Draw(spans, painter, canvas);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        Draw(spans, painter, canvas);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.Equal(0, allocated);
    }

    private static void Draw(ChartSpanText spans, ChartTextPainter painter, SKCanvas canvas)
    {
        Span<char> buffer = stackalloc char[16];
        for (var i = 0; i < 20; i++)
        {
            (1000 + (i * 37.5)).TryFormat(buffer, out var written, "#,0.#", CultureInfo.InvariantCulture);
            spans.Measure(painter, buffer[..written], 24);
            spans.Draw(canvas, painter, buffer[..written], 24, 8, 40, SKColors.Black, SKColors.White, 1);
        }
    }

    private static int CountDifferences(SKBitmap first, SKBitmap second)
    {
        var count = 0;
        for (var y = 0; y < first.Height; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                if (Math.Abs(first.GetPixel(x, y).Alpha - second.GetPixel(x, y).Alpha) > 8)
                {
                    count++;
                }
            }
        }

        return count;
    }

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
}
