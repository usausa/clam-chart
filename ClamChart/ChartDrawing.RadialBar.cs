namespace ClamChart;

using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;

public static partial class ChartDrawing
{
    private const float MaxInnerRadius = 0.95f;

    private static readonly SKColor LightOtherColor = new(0xB5, 0xB3, 0xAA);

    private static readonly SKColor DarkOtherColor = new(0x5C, 0x5B, 0x56);

    public static SKRect DrawRadialBar(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in RadialBarDrawOptions options)
    {
        var sweepAngle = Math.Min(options.SweepAngle, 360);
        if ((values.Length == 0) || (bounds.Width <= 0) || (bounds.Height <= 0) || !(sweepAngle > 0))
        {
            return SKRect.Empty;
        }

        var startAngle = Single.IsFinite(options.StartAngle) ? options.StartAngle : 0;
        var innerRadius = Single.IsNaN(options.InnerRadius) ? 0 : Math.Clamp(options.InnerRadius, 0, MaxInnerRadius);
        var spacing = GetLength(options.Spacing);
        var glowRadius = GetLength(options.GlowRadius);
        var area = new ChartRect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        var (centerX, centerY, radius, thickness) = RadialBarLayout.FitRings(area, startAngle, sweepAngle, innerRadius, options.Thickness is { } width ? GetLength(width) : null, spacing, values.Length, glowRadius, options.CapStyle);
        if ((radius <= 0) || (thickness <= 0))
        {
            return SKRect.Empty;
        }

        var range = options.Maximum - options.Minimum;
        var segmented = options.DisplayMode == ChartRadialBarMode.Segmented;
        var wrap = (options.OverflowMode == ChartOverflowMode.Wrap) && !segmented;
        var ring = new Ring(startAngle, sweepAngle, radius, thickness, spacing, options.CapStyle, options.DisplayMode, options.SegmentCount, GetLength(options.SegmentGap), options.Minimum, Double.IsFinite(range) ? range : 0, wrap);
        var steps = RentSteps(options.Thresholds, out var stepCount);
        canvas.Save();
        try
        {
            canvas.Translate((float)centerX, (float)centerY);
            if (glowRadius > 0)
            {
                DrawGlows(canvas, values, ring, steps.AsSpan(0, stepCount), glowRadius, options);
            }

            using var paint = new SKPaint();
            paint.IsAntialias = true;
            for (var i = 0; i < values.Length; i++)
            {
                DrawRing(canvas, ring, i, values[i], steps.AsSpan(0, stepCount), options, paint);
            }
        }
        finally
        {
            canvas.Restore();
            ReturnSteps(steps);
        }

        return GetHole(centerX, centerY, ring.GetRadius(values.Length - 1) - (thickness / 2));
    }

    private static SKColor GetRingColor(int index, in RadialBarDrawOptions options)
    {
        var palette = options.Colors ?? (options.IsDark ? DarkPalette : LightPalette);
        return index < palette.Count ? palette[index] : options.IsDark ? DarkOtherColor : LightOtherColor;
    }

    private static (SKColor[] Colors, float[] Positions) GetRingStops(ReadOnlySpan<ChartDrawingThreshold> steps, double ratio, SKColor baseColor, bool dark) =>
        steps.Length > 0 ? RadialBarGradient.GetThresholdStops(steps, baseColor) : RadialBarGradient.GetColorStops(baseColor, ratio, dark);

    private static SKRect GetHole(double centerX, double centerY, double edge)
    {
        if (!(edge > 0))
        {
            return SKRect.Empty;
        }

        var half = edge / Math.Sqrt(2);
        return new SKRect((float)(centerX - half), (float)(centerY - half), (float)(centerX + half), (float)(centerY + half));
    }

    private static SKRect GetOval(float radius) => new(-radius, -radius, radius, radius);

    private static SKPoint GetPoint(float radius, double clockAngle)
    {
        var (x, y) = PieLayout.GetDirection(clockAngle);
        return new SKPoint((float)(x * radius), (float)(y * radius));
    }

    private static void DrawGlows(SKCanvas canvas, ReadOnlySpan<double> values, in Ring ring, ReadOnlySpan<ChartDrawingThreshold> steps, double glowRadius, in RadialBarDrawOptions options)
    {
        using var filter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, ChartGlow.GetSigma(glowRadius));
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Style = SKPaintStyle.Stroke;
        paint.MaskFilter = filter;
        paint.StrokeWidth = (float)ring.Thickness * ChartGlow.WidthRatio;
        paint.StrokeCap = (ring.DisplayMode == ChartRadialBarMode.Basic) && (ring.CapStyle == ChartCapStyle.Round) ? SKStrokeCap.Round : SKStrokeCap.Butt;
        for (var i = 0; i < values.Length; i++)
        {
            var radius = (float)ring.GetRadius(i);
            var (ratio, percent) = ring.GetRatio(values[i]);
            if ((radius <= 0) || (ratio <= RadialBarRenderer.Tolerance))
            {
                continue;
            }

            var baseColor = GetRingColor(i, options);
            if (ring.DisplayMode == ChartRadialBarMode.Segmented)
            {
                DrawSegments(canvas, ring, radius, ratio, baseColor, steps, options, paint, ChartGlow.Opacity, true);
            }
            else if (ring.DisplayMode == ChartRadialBarMode.Gradient)
            {
                var (colors, positions) = GetRingStops(steps, ratio, baseColor, options.IsDark);
                using var shader = RadialBarRenderer.CreateGradient(colors, positions, ring.StartAngle, ring.SweepAngle);
                paint.Shader = shader;
                paint.Color = SKColors.Black.WithAlpha((byte)Math.Round(255 * ChartGlow.Opacity));
                DrawSweep(canvas, ring, radius, 0, Math.Min(ratio, 1), paint);
                paint.Shader = null;
            }
            else
            {
                paint.Color = ChartColor.Fade(ThresholdSteps.GetColor(steps, percent, baseColor), ChartGlow.Opacity);
                DrawSweep(canvas, ring, radius, 0, Math.Min(ratio, 1), paint);
            }
        }
    }

    private static void DrawRing(SKCanvas canvas, in Ring ring, int index, double value, ReadOnlySpan<ChartDrawingThreshold> steps, in RadialBarDrawOptions options, SKPaint paint)
    {
        var radius = (float)ring.GetRadius(index);
        if (radius <= 0)
        {
            return;
        }

        var (ratio, percent) = ring.GetRatio(value);
        var baseColor = GetRingColor(index, options);
        if (ring.DisplayMode == ChartRadialBarMode.Segmented)
        {
            DrawSegments(canvas, ring, radius, ratio, baseColor, steps, options, paint, 1, false);
            return;
        }

        if (options.TrackVisible && (ratio < 1))
        {
            DrawArc(canvas, ring, radius, ratio, 1, ChartColor.GetTrack(options.TrackColor, options.IsDark), paint);
        }

        if (ratio <= RadialBarRenderer.Tolerance)
        {
            return;
        }

        if (ring.DisplayMode == ChartRadialBarMode.Gradient)
        {
            DrawGradientRing(canvas, ring, radius, ratio, baseColor, steps, options.IsDark, paint);
        }
        else
        {
            DrawBasicRing(canvas, ring, radius, ratio, ThresholdSteps.GetColor(steps, percent, baseColor), paint);
        }
    }

    private static void DrawBasicRing(SKCanvas canvas, in Ring ring, float radius, double ratio, SKColor color, SKPaint paint)
    {
        var laps = Math.Max(0, Math.Ceiling(ratio) - 1);
        DrawArc(canvas, ring, radius, 0, Math.Min(ratio, 1), color, paint);
        if (laps > 0)
        {
            DrawArc(canvas, ring, radius, 0, ratio - laps, RadialBarRenderer.Darken(color), paint);
        }
    }

    private static void DrawGradientRing(SKCanvas canvas, in Ring ring, float radius, double ratio, SKColor baseColor, ReadOnlySpan<ChartDrawingThreshold> steps, bool dark, SKPaint paint)
    {
        var (colors, positions) = GetRingStops(steps, ratio, baseColor, dark);
        var drawn = Math.Min(ratio, 1);
        using (var shader = RadialBarRenderer.CreateGradient(colors, positions, ring.StartAngle, ring.SweepAngle))
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.Shader = shader;
            paint.Color = SKColors.Black;
            paint.StrokeWidth = (float)ring.Thickness;
            paint.StrokeCap = SKStrokeCap.Butt;
            DrawSweep(canvas, ring, radius, 0, drawn, paint);
            paint.Shader = null;
        }

        if (ring.CapStyle == ChartCapStyle.Round)
        {
            var cap = (float)ring.Thickness / 2;
            paint.Style = SKPaintStyle.Fill;
            paint.Color = colors[0];
            canvas.DrawCircle(GetPoint(radius, ring.GetAngle(0)), cap, paint);
            paint.Color = RadialBarGradient.GetColorAt(colors, positions, drawn);
            canvas.DrawCircle(GetPoint(radius, ring.GetAngle(drawn)), cap, paint);
        }

        var laps = Math.Max(0, Math.Ceiling(ratio) - 1);
        if (laps > 0)
        {
            DrawArc(canvas, ring, radius, 0, ratio - laps, RadialBarRenderer.Darken(colors[^1]), paint);
        }
    }

    private static void DrawSegments(SKCanvas canvas, in Ring ring, float radius, double ratio, SKColor baseColor, ReadOnlySpan<ChartDrawingThreshold> steps, in RadialBarDrawOptions options, SKPaint paint, double alpha, bool glow)
    {
        var gapAngle = ring.SegmentGap / radius * 180 / Math.PI;
        var count = RadialBarLayout.GetSegmentCount(radius, ring.Thickness, ring.SweepAngle, ring.SegmentCount, ring.SegmentGap);
        var step = ring.SweepAngle / count;
        var unlit = RadialBarRenderer.GetUnlitColor(options.TrackColor, options.IsDark);
        if (!glow)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = (float)ring.Thickness;
            paint.StrokeCap = SKStrokeCap.Butt;
            paint.StrokeJoin = SKStrokeJoin.Miter;
        }

        var oval = GetOval(radius);
        for (var c = 0; c < count; c++)
        {
            var fraction = (double)c / count;
            var lit = fraction < ratio - RadialBarRenderer.Tolerance;
            if (!lit && (glow || !options.TrackVisible))
            {
                continue;
            }

            paint.Color = ChartColor.Fade(lit ? ThresholdSteps.GetColor(steps, fraction * 100, baseColor) : unlit, alpha);
            var start = ring.StartAngle + (c * step) + (gapAngle / 2);
            var arc = Math.Max(0, step - gapAngle);
            canvas.DrawArc(oval, (float)(start - 90), (float)arc, false, paint);
        }
    }

    private static void DrawArc(SKCanvas canvas, in Ring ring, float radius, double from, double to, SKColor color, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = color;
        paint.StrokeWidth = (float)ring.Thickness;
        paint.StrokeCap = ring.CapStyle == ChartCapStyle.Round ? SKStrokeCap.Round : SKStrokeCap.Butt;
        paint.StrokeJoin = SKStrokeJoin.Miter;
        DrawSweep(canvas, ring, radius, from, to, paint);
    }

    private static void DrawSweep(SKCanvas canvas, in Ring ring, float radius, double from, double to, SKPaint paint)
    {
        var sweep = ring.SweepAngle * (to - from);
        if (sweep <= RadialBarRenderer.Tolerance)
        {
            return;
        }

        if (ring.IsFullCircle && (from <= RadialBarRenderer.Tolerance) && (to >= 1 - RadialBarRenderer.Tolerance))
        {
            canvas.DrawCircle(0, 0, radius, paint);
            return;
        }

        canvas.DrawArc(GetOval(radius), (float)(ring.GetAngle(from) - 90), (float)sweep, false, paint);
    }

    private readonly record struct Ring(double StartAngle, double SweepAngle, double Radius, double Thickness, double Spacing, ChartCapStyle CapStyle, ChartRadialBarMode DisplayMode, int SegmentCount, double SegmentGap, double Minimum, double Range, bool Wrap)
    {
        public bool IsFullCircle => SweepAngle >= 360;

        public double GetAngle(double fraction) => StartAngle + (SweepAngle * fraction);

        public double GetRadius(int index) => Radius - (Thickness / 2) - (index * (Thickness + Spacing));

        public (double Ratio, double Percent) GetRatio(double value)
        {
            if (!Double.IsFinite(value) || !(Range > 0))
            {
                return (0, Double.NaN);
            }

            var raw = (value - Minimum) / Range;
            return (Wrap ? Math.Max(0, raw) : Math.Clamp(raw, 0, 1), raw * 100);
        }
    }
}
