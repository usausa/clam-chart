namespace ClamChart.Rendering;

using System.Runtime.InteropServices;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class RadialBarRenderer : IDisposable
{
    public const double Tolerance = 1e-6;

    private const byte LightSegmentAlpha = 77;

    private const byte DarkSegmentAlpha = 51;

    private const float WrapLightness = -15;

    private const int BufferSize = 64;

    private static readonly SKColor LightSegmentColor = new(36, 41, 46, LightSegmentAlpha);

    private static readonly SKColor DarkSegmentColor = new(204, 204, 220, DarkSegmentAlpha);

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly ChartSpanText spans = new();
    private readonly SKPaint shaderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private readonly SKPaint glowPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private readonly Dictionary<(SKColor Color, double Ratio), SKShader> gradients = [];
    private readonly Dictionary<(SKColor Color, double Ratio), GradientStops> gradientStops = [];
    private readonly List<ChartDrawingThreshold> steps = [];
    private IReadOnlyList<Color>? palette;
    private SKMaskFilter? glowFilter;
    private float glowSigma;
    private int version = -1;

    public IChartTextMeasurer Measurer => text;

    public RadialBarRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        ClearShaders();
        paints.Dispose();
        text.Dispose();
        shaderPaint.Dispose();
        glowPaint.Dispose();
        glowFilter?.Dispose();
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, RadialBarLayout layout, GaugeInterpolator<RadialBarRow> display, PieInteraction interaction, ChartCountUp centerCount, int countedKey, RadialBarRenderState state, ChartStyle style)
    {
        if ((version != layout.Version) || !ReferenceEquals(palette, state.Palette))
        {
            ClearShaders();
            version = layout.Version;
            palette = state.Palette;
        }

        var progress = interaction.SelectionProgress < 1 ? display.Ease(interaction.SelectionProgress) : 1;
        var current = display.FindKey(interaction.SelectedKey) >= 0 ? interaction.SelectedKey : -1;
        var previous = (progress < 1) && (display.FindKey(interaction.PreviousKey) >= 0) ? interaction.PreviousKey : -1;
        var selection = new PieSelection(previous, current, progress, style.DimmedOpacity, 0);
        DrawRings(canvas, layout, display, selection, state, style);
        DrawCenter(canvas, layout, display, style, selection, centerCount, countedKey);
        DrawLegend(canvas, layout, state, style);
    }

    private static SKColor GetBaseColor(RadialBarRow row, RadialBarRenderState state, ChartStyle style) =>
        (row.Color ?? (row.Slot < state.Palette.Count ? state.Palette[row.Slot] : style.OtherColor)).ToSKColor();

    private static SKColor GetColor(RadialBarRenderState state, double percent, SKColor baseColor, ChartStyle style)
    {
        if (Double.IsNaN(percent) || (state.Thresholds.Count == 0))
        {
            return baseColor;
        }

        return ThresholdResolver.GetColor(ThresholdResolver.Resolve(state.Thresholds, percent), style)?.ToSKColor() ?? baseColor;
    }

    public static SKColor Darken(SKColor color)
    {
        color.ToHsl(out var h, out var s, out var l);
        return SKColor.FromHsl(h, s, Math.Clamp(l + WrapLightness, 0, 100), color.Alpha);
    }

    public static SKColor GetUnlitColor(SKColor? trackColor, bool dark) => trackColor ?? (dark ? DarkSegmentColor : LightSegmentColor);

    private static SKRect GetOval(float radius) => new(-radius, -radius, radius, radius);

    private static SKPoint GetPoint(float radius, double clockAngle)
    {
        var (x, y) = PieLayout.GetDirection(clockAngle);
        return new SKPoint((float)(x * radius), (float)(y * radius));
    }

    private void DrawRings(SKCanvas canvas, RadialBarLayout layout, GaugeInterpolator<RadialBarRow> display, PieSelection selection, RadialBarRenderState state, ChartStyle style)
    {
        var thickness = (float)layout.Thickness;
        if ((layout.OuterRadius <= 0) || (thickness <= 0))
        {
            return;
        }

        var track = ChartColor.GetTrack(style);
        var entrance = display.EntranceAlpha;
        canvas.Save();
        try
        {
            canvas.Translate((float)layout.CenterX, (float)layout.CenterY);
            if (layout.BarGlow)
            {
                DrawGlows(canvas, layout, display, selection, state, style, entrance);
            }

            for (var i = 0; i < display.Count; i++)
            {
                var row = display.GetRow(i);
                var alpha = display.GetAlpha(i) * entrance * selection.Alpha(display.GetKey(i));
                var radius = (float)layout.GetRingRadius(display.GetSlot(i));
                if ((alpha <= 0) || (radius <= 0))
                {
                    continue;
                }

                var ratio = Double.IsNaN(row.Percent) ? 0 : display.GetRatio(i);
                var baseColor = GetBaseColor(row, state, style);
                if (layout.DisplayMode == ChartRadialBarMode.Segmented)
                {
                    DrawSegments(canvas, layout, radius, ratio, baseColor, state, style, alpha, false);
                    continue;
                }

                if (state.TrackVisible && (ratio < 1))
                {
                    DrawArc(canvas, layout, radius, ratio, 1, ChartColor.Fade(track, alpha), layout.CapStyle);
                }

                if (ratio <= Tolerance)
                {
                    continue;
                }

                if (layout.DisplayMode == ChartRadialBarMode.Gradient)
                {
                    DrawGradientBar(canvas, layout, row, radius, ratio, baseColor, state, style, alpha);
                }
                else
                {
                    DrawBasicBar(canvas, layout, radius, ratio, GetColor(state, row.Percent, baseColor, style), alpha);
                }
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void DrawGlows(SKCanvas canvas, RadialBarLayout layout, GaugeInterpolator<RadialBarRow> display, PieSelection selection, RadialBarRenderState state, ChartStyle style, double entrance)
    {
        var segmented = layout.DisplayMode == ChartRadialBarMode.Segmented;
        var gradient = layout.DisplayMode == ChartRadialBarMode.Gradient;
        var round = layout.CapStyle == ChartCapStyle.Round;
        glowPaint.MaskFilter = GetGlowFilter((float)RadialBarLayout.GetGlowSigma(layout.Thickness));
        glowPaint.StrokeWidth = (float)layout.Thickness;
        glowPaint.StrokeCap = round && !gradient && !segmented ? SKStrokeCap.Round : SKStrokeCap.Butt;
        for (var i = 0; i < display.Count; i++)
        {
            var row = display.GetRow(i);
            var alpha = display.GetAlpha(i) * entrance * selection.Alpha(display.GetKey(i));
            var radius = (float)layout.GetRingRadius(display.GetSlot(i));
            if ((alpha <= 0) || (radius <= 0))
            {
                continue;
            }

            var ratio = Double.IsNaN(row.Percent) ? 0 : display.GetRatio(i);
            var baseColor = GetBaseColor(row, state, style);
            if (segmented)
            {
                DrawSegments(canvas, layout, radius, ratio, baseColor, state, style, alpha, true);
                continue;
            }

            if (ratio <= Tolerance)
            {
                continue;
            }

            var drawn = Math.Min(ratio, 1);
            if (gradient)
            {
                glowPaint.Shader = GetGradient(layout, row, baseColor, state, style, out var stops);
                glowPaint.Color = SKColors.Black.WithAlpha((byte)Math.Round(255 * alpha));
                DrawSweep(canvas, layout, radius, 0, drawn, glowPaint);
                glowPaint.Shader = null;
                if (round)
                {
                    DrawGlowCap(canvas, GetPoint(radius, layout.GetAngle(0)), (float)layout.Thickness / 2, ChartColor.Fade(stops.Colors[0], alpha));
                    DrawGlowCap(canvas, GetPoint(radius, layout.GetAngle(drawn)), (float)layout.Thickness / 2, ChartColor.Fade(RadialBarGradient.GetColorAt(stops.Colors, stops.Positions, drawn), alpha));
                }
            }
            else
            {
                glowPaint.Color = ChartColor.Fade(GetColor(state, row.Percent, baseColor, style), alpha);
                DrawSweep(canvas, layout, radius, 0, drawn, glowPaint);
            }
        }
    }

    private void DrawGlowCap(SKCanvas canvas, SKPoint center, float radius, SKColor color)
    {
        glowPaint.Style = SKPaintStyle.Fill;
        glowPaint.Color = color;
        canvas.DrawCircle(center, radius, glowPaint);
        glowPaint.Style = SKPaintStyle.Stroke;
    }

    private SKMaskFilter GetGlowFilter(float sigma)
    {
        if ((glowFilter is null) || !glowSigma.Equals(sigma))
        {
            glowFilter?.Dispose();
            glowFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
            glowSigma = sigma;
        }

        return glowFilter;
    }

    private void DrawBasicBar(SKCanvas canvas, RadialBarLayout layout, float radius, double ratio, SKColor color, double alpha)
    {
        var laps = Math.Max(0, Math.Ceiling(ratio) - 1);
        DrawArc(canvas, layout, radius, 0, Math.Min(ratio, 1), ChartColor.Fade(color, alpha), layout.CapStyle);
        if (laps > 0)
        {
            DrawArc(canvas, layout, radius, 0, ratio - laps, ChartColor.Fade(Darken(color), alpha), layout.CapStyle);
        }
    }

    private void DrawGradientBar(SKCanvas canvas, RadialBarLayout layout, RadialBarRow row, float radius, double ratio, SKColor baseColor, RadialBarRenderState state, ChartStyle style, double alpha)
    {
        var shader = GetGradient(layout, row, baseColor, state, style, out var stops);
        var drawn = Math.Min(ratio, 1);
        var paintAlpha = (byte)Math.Round(255 * alpha);
        shaderPaint.Shader = shader;
        shaderPaint.Color = SKColors.Black.WithAlpha(paintAlpha);
        shaderPaint.StrokeWidth = (float)layout.Thickness;
        shaderPaint.StrokeCap = SKStrokeCap.Butt;
        DrawSweep(canvas, layout, radius, 0, drawn, shaderPaint);
        var round = layout.CapStyle == ChartCapStyle.Round;
        if (round)
        {
            var cap = (float)layout.Thickness / 2;
            var start = stops.Colors[0];
            var end = RadialBarGradient.GetColorAt(stops.Colors, stops.Positions, drawn);
            var startPoint = GetPoint(radius, layout.GetAngle(0));
            var endPoint = GetPoint(radius, layout.GetAngle(drawn));
            canvas.DrawCircle(startPoint, cap, paints.Fill(ChartColor.Fade(start, alpha)));
            canvas.DrawCircle(endPoint, cap, paints.Fill(ChartColor.Fade(end, alpha)));
        }

        var laps = Math.Max(0, Math.Ceiling(ratio) - 1);
        if (laps > 0)
        {
            DrawArc(canvas, layout, radius, 0, ratio - laps, ChartColor.Fade(Darken(stops.Colors[^1]), alpha), layout.CapStyle);
        }
    }

    private SKShader GetGradient(RadialBarLayout layout, RadialBarRow row, SKColor baseColor, RadialBarRenderState state, ChartStyle style, out GradientStops stops)
    {
        var thresholds = state.Thresholds.Count > 0;
        var key = (baseColor, thresholds ? 0 : row.Ratio);
        if (gradients.TryGetValue(key, out var shader))
        {
            stops = gradientStops[key];
            return shader;
        }

        if (thresholds)
        {
            ThresholdSteps.Resolve(state.Thresholds, baseColor, style, steps);
        }

        var (colors, positions) = thresholds
            ? RadialBarGradient.GetThresholdStops(CollectionsMarshal.AsSpan(steps), baseColor)
            : RadialBarGradient.GetColorStops(baseColor, row.Ratio, style.IsDark);
        stops = new GradientStops(colors, positions);
        gradientStops[key] = stops;
        shader = CreateGradient(colors, positions, layout.StartAngle, layout.SweepAngle);
        gradients[key] = shader;
        return shader;
    }

    public static SKShader CreateGradient(SKColor[] colors, float[] positions, double startAngle, double sweepAngle) =>
        SKShader.CreateSweepGradient(SKPoint.Empty, colors, positions, SKShaderTileMode.Clamp, 0, (float)Math.Min(sweepAngle, 360), SKMatrix.CreateRotationDegrees((float)(startAngle - 90)));

    private void DrawSegments(SKCanvas canvas, RadialBarLayout layout, float radius, double ratio, SKColor baseColor, RadialBarRenderState state, ChartStyle style, double alpha, bool glow)
    {
        var thickness = layout.Thickness;
        var gapAngle = layout.SegmentGap / radius * 180 / Math.PI;
        var count = RadialBarLayout.GetSegmentCount(radius, thickness, layout.SweepAngle, layout.SegmentCount, layout.SegmentGap);
        var sweep = Math.Min(layout.SweepAngle, 360);
        var step = sweep / count;
        var unlit = GetUnlitColor(style.TrackColor?.ToSKColor(), style.IsDark);
        var paint = glow ? glowPaint : paints.Stroke(SKColors.Black, (float)thickness, SKStrokeCap.Butt, SKStrokeJoin.Miter);
        var oval = GetOval(radius);
        for (var c = 0; c < count; c++)
        {
            var fraction = (double)c / count;
            var lit = fraction < ratio - Tolerance;
            if (!lit && !state.TrackVisible)
            {
                continue;
            }

            paint.Color = ChartColor.Fade(lit ? GetColor(state, fraction * 100, baseColor, style) : unlit, alpha);
            var start = layout.StartAngle + (c * step) + (gapAngle / 2);
            var arc = Math.Max(0, step - gapAngle);
            canvas.DrawArc(oval, (float)(start - 90), (float)arc, false, paint);
        }
    }

    private void DrawArc(SKCanvas canvas, RadialBarLayout layout, float radius, double from, double to, SKColor color, ChartCapStyle capStyle)
    {
        var paint = paints.Stroke(color, (float)layout.Thickness, capStyle == ChartCapStyle.Round ? SKStrokeCap.Round : SKStrokeCap.Butt, SKStrokeJoin.Miter);
        DrawSweep(canvas, layout, radius, from, to, paint);
    }

    private static void DrawSweep(SKCanvas canvas, RadialBarLayout layout, float radius, double from, double to, SKPaint paint)
    {
        var sweep = Math.Min(layout.SweepAngle, 360) * (to - from);
        if (sweep <= Tolerance)
        {
            return;
        }

        if (layout.IsFullCircle && (from <= Tolerance) && (to >= 1 - Tolerance))
        {
            canvas.DrawCircle(0, 0, radius, paint);
            return;
        }

        canvas.DrawArc(GetOval(radius), (float)(layout.GetAngle(from) - 90), (float)sweep, false, paint);
    }

    private void DrawCenter(SKCanvas canvas, RadialBarLayout layout, GaugeInterpolator<RadialBarRow> display, ChartStyle style, PieSelection selection, ChartCountUp centerCount, int countedKey)
    {
        var alpha = display.EntranceAlpha;
        if (alpha <= 0)
        {
            return;
        }

        var current = layout.GetCenter(selection.Current);
        var counting = centerCount.IsRunning && (layout.GetCenterKey(selection.Current) == countedKey);
        if (selection.Progress < 1)
        {
            var previous = layout.GetCenter(selection.Previous);
            if (previous != current)
            {
                DrawCenterText(canvas, layout, previous, alpha * (1 - selection.Progress), style, null);
                DrawCenterText(canvas, layout, current, alpha * selection.Progress, style, counting ? centerCount : null);
                return;
            }
        }

        DrawCenterText(canvas, layout, current, alpha, style, counting ? centerCount : null);
    }

    private void DrawCenterText(SKCanvas canvas, RadialBarLayout layout, RadialBarCenter center, double alpha, ChartStyle style, ChartCountUp? count)
    {
        if ((center.Value is not { } value) || (alpha <= 0))
        {
            return;
        }

        var color = ChartColor.Fade(style.PrimaryTextColor.ToSKColor(), alpha);
        if ((count is null) || Double.IsNaN(center.Number))
        {
            text.DrawText(canvas, value, center.ValueBounds, center.ValueFontSize, Double.PositiveInfinity, color, style);
        }
        else
        {
            DrawCounting(canvas, layout, center, count.Current, color, style);
        }

        if (center.Caption is { } caption)
        {
            text.DrawText(canvas, caption, center.CaptionBounds, center.CaptionFontSize, Double.PositiveInfinity, ChartColor.Fade(style.SecondaryTextColor.ToSKColor(), alpha), style);
        }
    }

    private void DrawCounting(SKCanvas canvas, RadialBarLayout layout, RadialBarCenter center, double number, SKColor color, ChartStyle style)
    {
        Span<char> buffer = stackalloc char[BufferSize];
        var formatting = center.IsPercent ? layout.PercentFormatting : layout.ValueFormatting;
        var chars = formatting.TryFormat(number, buffer, out var written) ? buffer[..written] : formatting.Format(number).AsSpan();
        var size = (float)center.ValueFontSize;
        var width = spans.Measure(text, chars, size);
        var limit = (float)center.ValueBounds.Width;
        var scale = (width > 0) && (width > limit) ? limit / width : 1f;
        var metrics = text.GetMetrics(size);
        var bounds = center.ValueBounds;
        var baseline = (float)bounds.Y + (((float)bounds.Height - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
        var x = (float)bounds.CenterX - (width * scale / 2);
        if (scale < 1)
        {
            canvas.Save();
            canvas.Scale(scale, scale, x, baseline);
        }

        spans.Draw(canvas, text, chars, size, x, baseline, color, style.TextHaloColor.ToSKColor(), (float)style.TextHaloWidth);
        if (scale < 1)
        {
            canvas.Restore();
        }
    }

    private void DrawLegend(SKCanvas canvas, RadialBarLayout layout, RadialBarRenderState state, ChartStyle style)
    {
        var legend = layout.Legend;
        if (!legend.IsVisible)
        {
            return;
        }

        var color = style.SecondaryTextColor.ToSKColor();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < legend.Items.Count; i++)
        {
            var item = legend.Items[i];
            var row = layout.Rows[item.SeriesIndex];
            var marker = item.Marker;
            var markerColor = GetColor(state, row.Percent, GetBaseColor(row, state, style), style);
            canvas.DrawCircle((float)marker.CenterX, (float)marker.CenterY, (float)marker.Height / 2, paints.Fill(markerColor));
            text.DrawText(canvas, row.Label ?? String.Empty, item.Text, style.LegendFontSize, item.TextMaxWidth, color, style);
            if (!item.Value.IsEmpty)
            {
                var value = layout.LegendValueMode == ChartLegendValueMode.Percent ? row.PercentText : row.ValueText;
                text.DrawText(canvas, value ?? String.Empty, item.Value, style.LegendFontSize, Double.PositiveInfinity, color, style);
            }
        }

        if (legend.OverflowCount > 0)
        {
            text.DrawText(canvas, legend.OverflowText, legend.OverflowBounds, style.LegendFontSize, Double.PositiveInfinity, style.MutedTextColor.ToSKColor(), style);
        }
    }

    private void ClearShaders()
    {
        foreach (var shader in gradients.Values)
        {
            shader.Dispose();
        }

        gradients.Clear();
        gradientStops.Clear();
    }

    private sealed record GradientStops(SKColor[] Colors, float[] Positions);
}
