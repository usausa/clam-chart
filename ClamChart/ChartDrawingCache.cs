namespace ClamChart;

using SkiaSharp;

public sealed class ChartDrawingCache : IDisposable
{
    private readonly SKPaint paint = new();

    private readonly SKPaint glowPaint = new();

    private readonly SKPathBuilder builder = new();

    private SKMaskFilter? blur;

    private float blurSigma = Single.NaN;

    public void Dispose()
    {
        paint.Dispose();
        glowPaint.Dispose();
        builder.Dispose();
        blur?.Dispose();
        blur = null;
        blurSigma = Single.NaN;
    }

    internal SKPaint RentPaint() => Reset(paint);

    internal SKPaint RentGlowPaint() => Reset(glowPaint);

    internal SKPathBuilder RentBuilder() => builder;

    internal SKMaskFilter RentBlur(float sigma)
    {
        if ((blur is null) || !blurSigma.Equals(sigma))
        {
            blur?.Dispose();
            blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
            blurSigma = sigma;
        }

        return blur;
    }

    private static SKPaint Reset(SKPaint value)
    {
        value.Shader = null;
        value.MaskFilter = null;
        value.PathEffect = null;
        value.IsAntialias = true;
        value.Style = SKPaintStyle.Fill;
        value.Color = SKColors.Black;
        value.StrokeWidth = 0;
        value.StrokeCap = SKStrokeCap.Butt;
        value.StrokeJoin = SKStrokeJoin.Miter;
        return value;
    }
}
