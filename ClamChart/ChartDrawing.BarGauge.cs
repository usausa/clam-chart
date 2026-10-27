namespace ClamChart;

using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;

public static partial class ChartDrawing
{
    private static readonly SKColor LightTargetColor = new(0x0B, 0x0B, 0x0B);

    private static readonly SKColor DarkTargetColor = new(0xF0, 0xEF, 0xEC);

    public static void DrawBarGauge(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in BarGaugeDrawOptions options) =>
        DrawBarGauge(canvas, bounds, values, default, options, null);

    public static void DrawBarGauge(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in BarGaugeDrawOptions options, ChartDrawingCache? cache) =>
        DrawBarGauge(canvas, bounds, values, default, options, cache);

    public static void DrawBarGauge(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, ReadOnlySpan<double> targets, in BarGaugeDrawOptions options) =>
        DrawBarGauge(canvas, bounds, values, targets, options, null);

    public static void DrawBarGauge(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, ReadOnlySpan<double> targets, in BarGaugeDrawOptions options, ChartDrawingCache? cache)
    {
        var count = values.Length;
        var horizontal = options.Orientation == ChartOrientation.Horizontal;
        var spacing = GetLength(options.Spacing);
        var size = ((horizontal ? bounds.Height : bounds.Width) - (spacing * (count - 1))) / count;
        if ((count == 0) || (bounds.Width <= 0) || (bounds.Height <= 0) || !(size > 0))
        {
            return;
        }

        var length = horizontal ? bounds.Width : bounds.Height;
        var range = options.Maximum - options.Minimum;
        var gauge = new Gauge(horizontal, length, options.Minimum, Double.IsFinite(range) ? range : 0);
        var baseColor = options.Color ?? (options.IsDark ? DarkPalette[0] : LightPalette[0]);
        var track = ChartColor.GetTrack(options.TrackColor, options.IsDark);
        var targetColor = options.IsDark ? DarkTargetColor : LightTargetColor;
        var steps = RentSteps(options.Thresholds, out var stepCount);
        var owned = cache is null ? new SKPaint { IsAntialias = true } : null;
        var paint = owned ?? cache!.RentPaint();
        paint.Style = SKPaintStyle.Fill;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var offset = i * (size + spacing);
                var bar = horizontal
                    ? new SKRect(bounds.Left, bounds.Top + offset, bounds.Right, bounds.Top + offset + size)
                    : new SKRect(bounds.Left + offset, bounds.Top, bounds.Left + offset + size, bounds.Bottom);
                var (ratio, percent) = gauge.GetRatio(values[i]);
                if (options.DisplayMode == ChartBarGaugeMode.Segmented)
                {
                    DrawCells(canvas, gauge, bar, ratio, baseColor, steps.AsSpan(0, stepCount), options, paint);
                }
                else
                {
                    DrawTrack(canvas, gauge, bar, ratio, options.TrackVisible ? track : SKColors.Transparent, paint);
                    if (options.FillMode == ChartFillMode.Gradient)
                    {
                        DrawGradientBar(canvas, gauge, bar, ratio, percent, baseColor, steps.AsSpan(0, stepCount), paint);
                    }
                    else
                    {
                        DrawBasicBar(canvas, gauge, bar, ratio, ThresholdSteps.GetColor(steps.AsSpan(0, stepCount), percent, baseColor), paint);
                    }
                }

                if ((i < targets.Length) && Double.IsFinite(targets[i]) && (gauge.Range > 0))
                {
                    DrawTarget(canvas, gauge, bounds, bar, Math.Clamp((targets[i] - gauge.Minimum) / gauge.Range, 0, 1), targetColor, paint);
                }
            }
        }
        finally
        {
            ReturnSteps(steps);
            owned?.Dispose();
        }
    }

    private static void DrawTrack(SKCanvas canvas, in Gauge gauge, SKRect bar, double ratio, SKColor color, SKPaint paint)
    {
        if (color.Alpha == 0)
        {
            return;
        }

        var filled = gauge.GetFilledLength(ratio);
        var rest = gauge.Horizontal
            ? new SKRect((float)(bar.Left + filled), bar.Top, bar.Right, bar.Bottom)
            : new SKRect(bar.Left, bar.Top, bar.Right, (float)(bar.Bottom - filled));
        if ((rest.Width > 0) && (rest.Height > 0))
        {
            paint.Color = color;
            canvas.DrawRect(rest, paint);
        }
    }

    private static void DrawBasicBar(SKCanvas canvas, in Gauge gauge, SKRect bar, double ratio, SKColor color, SKPaint paint)
    {
        if (Double.IsNaN(ratio))
        {
            return;
        }

        var filled = gauge.GetBar(bar, gauge.GetFilledLength(ratio));
        paint.Color = ChartColor.Fade(color, BarGaugeRenderer.BasicFillOpacity);
        canvas.DrawRect(filled, paint);
        var edge = gauge.Horizontal
            ? new SKRect(Math.Max(filled.Left, filled.Right - BarGaugeRenderer.BasicEdgeWidth), filled.Top, filled.Right, filled.Bottom)
            : new SKRect(filled.Left, filled.Top, filled.Right, Math.Min(filled.Bottom, filled.Top + BarGaugeRenderer.BasicEdgeWidth));
        paint.Color = color;
        canvas.DrawRect(edge, paint);
    }

    private static void DrawGradientBar(SKCanvas canvas, in Gauge gauge, SKRect bar, double ratio, double percent, SKColor baseColor, ReadOnlySpan<ChartDrawingThreshold> steps, SKPaint paint)
    {
        if (Double.IsNaN(ratio))
        {
            return;
        }

        var (colors, positions) = BarGaugeGradient.GetStops(steps, percent, ratio, baseColor);
        var length = (float)gauge.Length;
        var drawn = (float)gauge.GetFilledLength(ratio);
        using var shader = SKShader.CreateLinearGradient(SKPoint.Empty, gauge.Horizontal ? new SKPoint(length, 0) : new SKPoint(0, -length), colors, positions, SKShaderTileMode.Clamp);
        canvas.Save();
        try
        {
            paint.Shader = shader;
            paint.Color = SKColors.Black;
            if (gauge.Horizontal)
            {
                canvas.Translate(bar.Left, bar.Top);
                canvas.DrawRect(0, 0, drawn, bar.Height, paint);
            }
            else
            {
                canvas.Translate(bar.Left, bar.Bottom);
                canvas.DrawRect(0, -drawn, bar.Width, drawn, paint);
            }
        }
        finally
        {
            paint.Shader = null;
            canvas.Restore();
        }
    }

    private static void DrawCells(SKCanvas canvas, in Gauge gauge, SKRect bar, double ratio, SKColor baseColor, ReadOnlySpan<ChartDrawingThreshold> steps, in BarGaugeDrawOptions options, SKPaint paint)
    {
        var gap = GetLength(options.SegmentGap);
        var (count, cellSize) = BarGaugeLayout.GetCells(gauge.Length, options.SegmentWidth, gap);
        var size = (float)cellSize;
        if ((count <= 0) || (size <= 0))
        {
            return;
        }

        var pitch = size + gap;
        var across = gauge.Horizontal ? bar.Height : bar.Width;
        var width = gauge.Horizontal ? size : across;
        var height = gauge.Horizontal ? across : size;
        SKShader? shader = null;
        var shaderColor = default(SKColor);
        try
        {
            for (var c = 0; c < count; c++)
            {
                var fraction = (double)c / count;
                var color = ThresholdSteps.GetColor(steps, fraction * 100, baseColor);
                var start = c * pitch;
                var cell = gauge.Horizontal
                    ? new SKRect(bar.Left + start, bar.Top, bar.Left + start + size, bar.Bottom)
                    : new SKRect(bar.Left, bar.Bottom - start - size, bar.Right, bar.Bottom - start);
                if (!Double.IsNaN(ratio) && (fraction <= ratio + BarGaugeRenderer.Tolerance))
                {
                    if ((shader is null) || (shaderColor != color))
                    {
                        shader?.Dispose();
                        shader = BarGaugeRenderer.CreateLitCell(color, width, height);
                        shaderColor = color;
                    }

                    DrawLitCell(canvas, cell, shader, width, height, paint);
                }
                else
                {
                    paint.Color = ChartColor.Fade(color, BarGaugeRenderer.UnlitOpacity);
                    canvas.DrawRect(cell, paint);
                }
            }
        }
        finally
        {
            shader?.Dispose();
        }
    }

    private static void DrawLitCell(SKCanvas canvas, SKRect cell, SKShader shader, float width, float height, SKPaint paint)
    {
        canvas.Save();
        try
        {
            canvas.Translate(cell.MidX, cell.MidY);
            paint.Shader = shader;
            paint.Color = SKColors.Black;
            canvas.DrawRect(-width / 2, -height / 2, width, height, paint);
        }
        finally
        {
            paint.Shader = null;
            canvas.Restore();
        }
    }

    private static void DrawTarget(SKCanvas canvas, in Gauge gauge, SKRect bounds, SKRect bar, double ratio, SKColor color, SKPaint paint)
    {
        var position = gauge.Length * ratio;
        var overhang = (float)BarGaugeLayout.TargetOverhang;
        var half = BarGaugeRenderer.TargetWidth / 2;
        var line = gauge.Horizontal
            ? new SKRect((float)(bar.Left + position) - half, Math.Max(bounds.Top, bar.Top - overhang), (float)(bar.Left + position) + half, Math.Min(bounds.Bottom, bar.Bottom + overhang))
            : new SKRect(Math.Max(bounds.Left, bar.Left - overhang), (float)(bar.Bottom - position) - half, Math.Min(bounds.Right, bar.Right + overhang), (float)(bar.Bottom - position) + half);
        paint.Color = color;
        canvas.DrawRect(line, paint);
    }

    private readonly record struct Gauge(bool Horizontal, double Length, double Minimum, double Range)
    {
        public (double Ratio, double Percent) GetRatio(double value)
        {
            if (!Double.IsFinite(value) || !(Range > 0))
            {
                return (Double.NaN, Double.NaN);
            }

            var raw = (value - Minimum) / Range;
            return (Math.Clamp(raw, 0, 1), raw * 100);
        }

        public double GetFilledLength(double ratio) => Double.IsNaN(ratio) ? 0 : Math.Max(Length * ratio, 1);

        public SKRect GetBar(SKRect bar, double length) =>
            Horizontal
                ? new SKRect(bar.Left, bar.Top, (float)(bar.Left + length), bar.Bottom)
                : new SKRect(bar.Left, (float)(bar.Bottom - length), bar.Right, bar.Bottom);
    }
}
