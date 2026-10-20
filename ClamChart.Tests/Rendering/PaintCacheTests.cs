namespace ClamChart.Tests.Rendering;

using SkiaSharp;

public sealed class PaintCacheTests
{
    [Fact]
    public void PaintsAreReusedWithTheRequestedSettings()
    {
        // Arrange
        using var cache = new PaintCache();

        // Act
        var first = cache.Stroke(SKColors.Red, 2);
        var second = cache.Stroke(SKColors.Blue, 0, SKStrokeCap.Butt, SKStrokeJoin.Bevel);
        var fill = cache.Fill(SKColors.Green);

        // Assert
        Assert.Same(first, second);
        Assert.Equal(SKPaintStyle.Stroke, second.Style);
        Assert.Equal(SKColors.Blue, second.Color);
        Assert.Equal(0f, second.StrokeWidth);
        Assert.Equal(SKStrokeCap.Butt, second.StrokeCap);
        Assert.Equal(SKStrokeJoin.Bevel, second.StrokeJoin);
        Assert.Same(fill, cache.Fill(SKColors.Yellow));
        Assert.Equal(SKPaintStyle.Fill, fill.Style);
        Assert.Equal(SKColors.Yellow, fill.Color);
    }

    [Fact]
    public void ClearPaintMakesPixelsTransparent()
    {
        // Arrange
        using var cache = new PaintCache();
        using var bitmap = new SKBitmap(new SKImageInfo(20, 20, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);

        // Act
        canvas.DrawCircle(10, 10, 5, cache.Clear);
        canvas.Flush();

        // Assert
        Assert.Equal(0, bitmap.GetPixel(10, 10).Alpha);
        Assert.Equal(SKColors.Red, bitmap.GetPixel(1, 1));
    }

    [Fact]
    public void PathBuilderIsShared()
    {
        // Arrange
        using var cache = new PaintCache();

        // Act
        cache.Path.MoveTo(0, 0);
        cache.Path.LineTo(10, 10);
        using var path = cache.Path.Detach();

        // Assert
        Assert.Equal(2, path.PointCount);
        Assert.Same(cache.Path, cache.Path);
    }
}
