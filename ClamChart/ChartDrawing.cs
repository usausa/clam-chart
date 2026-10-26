namespace ClamChart;

using System.Buffers;

using ClamChart.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public static partial class ChartDrawing
{
    private const float MarkerRatio = 1.25f;

    private const float BarGap = 2;

    private const float BarGapRatio = 0.25f;

    private const float MaxBarWidth = 24;

    private const float WinLossGap = 1;

    private const double FlatRange = 1;

    private static readonly SKColor[] LightPalette = ToColors(ChartPalettes.DefaultLight);

    private static readonly SKColor[] DarkPalette = ToColors(ChartPalettes.DefaultDark);

    public static void DrawSparkline(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in SparklineDrawOptions options) =>
        DrawSparkline(canvas, bounds, values, options, null);

    public static void DrawSparkline(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in SparklineDrawOptions options, ChartDrawingCache? cache)
    {
        var (first, last, high, low) = Scan(values);
        if ((last < 0) || (bounds.Width <= 0) || (bounds.Height <= 0))
        {
            return;
        }

        var kind = options.Kind;
        var bars = kind is ChartSparklineKind.Bar or ChartSparklineKind.WinLoss;
        var (minimum, maximum) = ResolveRange(kind, values[low], values[high], options.Minimum, options.Maximum);
        var thickness = Math.Max(0, options.Thickness);
        var markers = bars ? ChartSparklineMarkers.None : options.Markers;
        var radius = thickness * MarkerRatio;
        var inset = bars ? 0 : markers != ChartSparklineMarkers.None ? radius : thickness / 2;
        var plot = new SKRect(bounds.Left + inset, bounds.Top + inset, Math.Max(bounds.Left + inset, bounds.Right - inset), Math.Max(bounds.Top + inset, bounds.Bottom - inset));
        var scale = new Scale(plot, minimum, maximum, values.Length, bars);
        var owned = cache is null ? new SKPaint { IsAntialias = true } : null;
        try
        {
            var paint = owned ?? cache!.RentPaint();
            if (bars)
            {
                DrawBars(canvas, values, scale, kind == ChartSparklineKind.WinLoss, options, paint);
                return;
            }

            DrawLine(canvas, values, scale, thickness, options, paint, cache);
            paint.Style = SKPaintStyle.Fill;
            paint.Color = options.Color;
            DrawMarker(canvas, values, scale, (markers & ChartSparklineMarkers.First) != 0 ? first : -1, radius, paint);
            DrawMarker(canvas, values, scale, (markers & ChartSparklineMarkers.Last) != 0 ? last : -1, radius, paint);
            DrawMarker(canvas, values, scale, (markers & ChartSparklineMarkers.High) != 0 ? high : -1, radius, paint);
            DrawMarker(canvas, values, scale, (markers & ChartSparklineMarkers.Low) != 0 ? low : -1, radius, paint);
        }
        finally
        {
            owned?.Dispose();
        }
    }

    private static (int First, int Last, int High, int Low) Scan(ReadOnlySpan<double> values)
    {
        var first = -1;
        var last = -1;
        var high = -1;
        var low = -1;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (!Double.IsFinite(value))
            {
                continue;
            }

            first = first < 0 ? i : first;
            last = i;
            if ((high < 0) || (value > values[high]))
            {
                high = i;
            }

            if ((low < 0) || (value < values[low]))
            {
                low = i;
            }
        }

        return (first, last, high, low);
    }

    private static (double Minimum, double Maximum) ResolveRange(ChartSparklineKind kind, double low, double high, double? fixedMinimum, double? fixedMaximum)
    {
        if (kind == ChartSparklineKind.WinLoss)
        {
            return (-1, 1);
        }

        if (kind == ChartSparklineKind.Bar)
        {
            low = Math.Min(low, 0);
            high = Math.Max(high, 0);
        }

        var inverted = (fixedMinimum is { } lower) && (fixedMaximum is { } upper) && (lower >= upper);
        var useMinimum = inverted ? null : fixedMinimum;
        var useMaximum = inverted ? null : fixedMaximum;
        var minimum = useMinimum ?? low;
        var maximum = useMaximum ?? high;
        if (maximum > minimum)
        {
            return (minimum, maximum);
        }

        if ((useMinimum is null) && (useMaximum is null))
        {
            return (minimum - FlatRange, maximum + FlatRange);
        }

        return useMaximum is null ? (minimum, minimum + FlatRange) : (maximum - FlatRange, maximum);
    }

    private static void DrawLine(SKCanvas canvas, ReadOnlySpan<double> values, Scale scale, float thickness, in SparklineDrawOptions options, SKPaint paint, ChartDrawingCache? cache)
    {
        var count = values.Length;
        var along = ArrayPool<float>.Shared.Rent(count);
        var across = ArrayPool<float>.Shared.Rent(count);
        var tangents = ArrayPool<float>.Shared.Rent(count);
        var owned = cache is null ? new SKPathBuilder() : null;
        var builder = owned ?? cache!.RentBuilder();
        try
        {
            for (var i = 0; i < count; i++)
            {
                along[i] = scale.ToX(i);
                across[i] = Double.IsFinite(values[i]) ? scale.ToY(values[i]) : Single.NaN;
            }

            var interpolation = options.Smooth ? ChartLineInterpolation.Smooth : ChartLineInterpolation.Linear;
            if ((options.Kind == ChartSparklineKind.Area) && (options.AreaOpacity > 0))
            {
                var baseline = scale.ToY(Math.Clamp(0, scale.Minimum, scale.Maximum));
                AddRuns(builder, along, across, tangents, count, interpolation, baseline);
                using var area = builder.Detach();
                paint.Style = SKPaintStyle.Fill;
                paint.Color = options.Color.WithAlpha((byte)Math.Round(options.Color.Alpha * Math.Clamp(options.AreaOpacity, 0, 1)));
                canvas.DrawPath(area, paint);
            }

            if (thickness <= 0)
            {
                return;
            }

            AddRuns(builder, along, across, tangents, count, interpolation, Single.NaN);
            using var line = builder.Detach();
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = options.Color;
            paint.StrokeWidth = thickness;
            paint.StrokeCap = SKStrokeCap.Round;
            paint.StrokeJoin = (count > 1) && (scale.Width / (count - 1) < thickness) ? SKStrokeJoin.Bevel : SKStrokeJoin.Round;
            canvas.DrawPath(line, paint);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(along);
            ArrayPool<float>.Shared.Return(across);
            ArrayPool<float>.Shared.Return(tangents);
            owned?.Dispose();
        }
    }

    private static void AddRuns(SKPathBuilder builder, float[] along, float[] across, float[] tangents, int count, ChartLineInterpolation interpolation, float baseline)
    {
        for (var start = 0; start < count;)
        {
            if (!Single.IsFinite(across[start]))
            {
                start++;
                continue;
            }

            var end = start;
            while ((end < count) && Single.IsFinite(across[end]))
            {
                end++;
            }

            var runAlong = along.AsSpan(start, end - start);
            var runAcross = across.AsSpan(start, end - start);
            var runTangents = tangents.AsSpan(start, end - start);
            if (interpolation == ChartLineInterpolation.Smooth)
            {
                LinePathBuilder.ComputeTangents(runAlong, runAcross, runTangents);
            }

            LinePathBuilder.AddLine(builder, runAlong, runAcross, runTangents, interpolation, false);
            if (!Single.IsNaN(baseline))
            {
                builder.LineTo(runAlong[^1], baseline);
                builder.LineTo(runAlong[0], baseline);
                builder.Close();
            }

            start = end;
        }
    }

    private static void DrawBars(SKCanvas canvas, ReadOnlySpan<double> values, Scale scale, bool winLoss, in SparklineDrawOptions options, SKPaint paint)
    {
        var slot = scale.Width / values.Length;
        var width = Math.Max(0, Math.Min(slot - Math.Min(BarGap, slot * BarGapRatio), MaxBarWidth));
        if (width <= 0)
        {
            return;
        }

        var middle = scale.ToY(Math.Clamp(0, scale.Minimum, scale.Maximum));
        paint.Style = SKPaintStyle.Fill;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (!Double.IsFinite(value) || (value == 0))
            {
                continue;
            }

            float top;
            float bottom;
            if (winLoss)
            {
                top = value > 0 ? scale.Top : middle + (WinLossGap / 2);
                bottom = value > 0 ? middle - (WinLossGap / 2) : scale.Bottom;
            }
            else
            {
                var end = scale.ToY(Math.Clamp(value, scale.Minimum, scale.Maximum));
                top = Math.Min(middle, end);
                bottom = Math.Max(middle, end);
            }

            if (bottom <= top)
            {
                continue;
            }

            var center = scale.ToX(i);
            paint.Color = value < 0 ? options.NegativeColor : options.Color;
            canvas.DrawRect(new SKRect(center - (width / 2), top, center + (width / 2), bottom), paint);
        }
    }

    private static void DrawMarker(SKCanvas canvas, ReadOnlySpan<double> values, Scale scale, int index, float radius, SKPaint paint)
    {
        if ((index >= 0) && (radius > 0))
        {
            canvas.DrawCircle(scale.ToX(index), scale.ToY(values[index]), radius, paint);
        }
    }

    private static SKColor[] ToColors(IReadOnlyList<Color> palette)
    {
        var colors = new SKColor[palette.Count];
        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = palette[i].ToSKColor();
        }

        return colors;
    }

    private static float GetLength(float value) => Single.IsFinite(value) && (value > 0) ? value : 0;

    private static ChartDrawingThreshold[]? RentSteps(IReadOnlyList<ChartDrawingThreshold>? thresholds, out int count)
    {
        count = thresholds?.Count ?? 0;
        if ((thresholds is null) || (count == 0))
        {
            return null;
        }

        var steps = ArrayPool<ChartDrawingThreshold>.Shared.Rent(count);
        for (var i = 0; i < count; i++)
        {
            steps[i] = thresholds[i];
        }

        ThresholdSteps.Sort(steps.AsSpan(0, count));
        return steps;
    }

    private static void ReturnSteps(ChartDrawingThreshold[]? steps)
    {
        if (steps is not null)
        {
            ArrayPool<ChartDrawingThreshold>.Shared.Return(steps);
        }
    }

    private readonly record struct Scale(SKRect Plot, double Minimum, double Maximum, int Count, bool Bars)
    {
        public float Width => Plot.Width;

        public float Top => Plot.Top;

        public float Bottom => Plot.Bottom;

        public float ToX(int index)
        {
            if (Bars)
            {
                return Plot.Left + (Plot.Width / Count * (index + 0.5f));
            }

            return Count > 1 ? Plot.Left + (Plot.Width * index / (Count - 1)) : Plot.MidX;
        }

        public float ToY(double value) => (float)(Plot.Bottom - ((value - Minimum) / (Maximum - Minimum) * Plot.Height));
    }
}
