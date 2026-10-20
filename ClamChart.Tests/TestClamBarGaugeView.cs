namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class TestClamBarGaugeView : ClamBarGaugeView
{
    public SKBitmap Paint(double width, double height, float scale = 2)
    {
        TestDispatcher.Current.RunPending();
        Frame = new Rect(0, 0, width, height);
        var info = new SKImageInfo((int)Math.Ceiling(width * scale), (int)Math.Ceiling(height * scale), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        OnPaintSurface(new SKPaintSurfaceEventArgs(surface, info));
        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    public Size MeasureFor(double width)
    {
        TestDispatcher.Current.RunPending();
        return ((IView)this).Measure(width, Double.PositiveInfinity);
    }
}
