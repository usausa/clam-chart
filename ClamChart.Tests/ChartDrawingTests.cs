namespace ClamChart.Tests;

using SkiaSharp;

public sealed class ChartDrawingTests
{
    [Fact]
    public void LineIsDrawnAcrossTheBounds()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawSparkline(canvas, new SKRect(0, 0, 100, 40), [1, 1, 1], new SparklineDrawOptions { Color = SKColors.Red, Markers = ChartSparklineMarkers.None });

        // Assert
        Assert.Equal(SKColors.Red, bitmap.GetPixel(50, 20));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(2, 20));
        Assert.Equal(0, bitmap.GetPixel(50, 5).Alpha);
    }

    [Fact]
    public void AreaIsFilledUnderTheLine()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawSparkline(canvas, new SKRect(0, 0, 100, 40), [0, 2, 2], new SparklineDrawOptions { Kind = ChartSparklineKind.Area, Color = SKColors.Blue, AreaOpacity = 0.5f, Markers = ChartSparklineMarkers.None });

        // Assert
        Assert.InRange(bitmap.GetPixel(80, 30).Alpha, 120, 135);
    }

    [Fact]
    public void BarsUseTheNegativeColorBelowZero()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawSparkline(canvas, new SKRect(0, 0, 100, 40), [2, -2], new SparklineDrawOptions { Kind = ChartSparklineKind.Bar, Color = SKColors.Lime, NegativeColor = SKColors.Red });

        // Assert
        Assert.Equal(SKColors.Lime, bitmap.GetPixel(25, 10));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(75, 30));
        Assert.Equal(0, bitmap.GetPixel(25, 30).Alpha);
    }

    [Fact]
    public void LastMarkerIsDrawn()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawSparkline(canvas, new SKRect(0, 0, 100, 40), [0, 1], new SparklineDrawOptions { Color = SKColors.Blue, Thickness = 4 });

        // Assert
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(95, 6));
    }

    [Fact]
    public void NothingIsDrawnWithoutValues()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawSparkline(canvas, new SKRect(0, 0, 100, 40), [Double.NaN, Double.NaN], new SparklineDrawOptions());
        ChartDrawing.DrawSparkline(canvas, new SKRect(0, 0, 100, 40), [], new SparklineDrawOptions());

        // Assert
        Assert.All(bitmap.Pixels, static x => Assert.Equal(0, x.Alpha));
    }
}
