namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class StatRenderer : IDisposable
{
    private const int BufferSize = 64;

    private const double BackgroundOpacity = 0.15;

    private const double TrendFillOpacity = 0.2;

    private const double OnColorFillOpacity = 0.4;

    private const int OnColorLineBrighten = 40;

    private const double GradientAngle = 120;

    private const float GradientHueShift = 8;

    private const float GradientStartLightness = 15;

    private const float GradientEndLightness = 5;

    private const float LightThemeFactor = -0.7f;

    private const double BrightTextThreshold = 180;

    private const double TransparentThreshold = 0.3;

    private const float CapHeightRatio = 0.7f;

    private const float CheckStrokeRatio = 0.18f;

    private const float ArrowHeightRatio = 0.8f;

    private static readonly SKColor DarkInk = new(32, 34, 38);

    private static readonly SKColor LightInk = new(247, 248, 250);

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly ChartSpanText spans = new();
    private readonly SparklineRenderer trend;
    private readonly SKPaint shaderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint maskPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.DstIn };
    private SKShader? gradient;
    private (SKColor Color, float Width, float Height, bool Dark) gradientKey;
    private SKShader? fade;
    private SKRect fadeKey;
    private SKPath? iconPath;
    private (ChartThresholdLevel Level, SKPoint Center, float Size) iconKey;
    private SKPath? arrowPath;
    private (bool Up, SKPoint Center, float Width) arrowKey;

    public IChartTextMeasurer Measurer => text;

    public int SpanElementCount => spans.ElementCount;

    public StatRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
        trend = new SparklineRenderer(text);
    }

    public void Dispose()
    {
        trend.Dispose();
        paints.Dispose();
        text.Dispose();
        shaderPaint.Dispose();
        maskPaint.Dispose();
        gradient?.Dispose();
        fade?.Dispose();
        iconPath?.Dispose();
        arrowPath?.Dispose();
    }

    public void Render(SKCanvas canvas, StatLayout layout, StatRenderState state, SparklineCore core, ChartStyle style)
    {
        var accent = (state.Fill ?? state.StateColor ?? state.SeriesColor).ToSKColor();
        var gradientMode = state.ColorMode == ChartStatColorMode.BackgroundGradient;
        var ink = GetInk(accent, style.IsDark);
        if (gradientMode)
        {
            DrawGradient(canvas, layout.Bounds, accent, style.IsDark, state.Pulse);
        }
        else if ((state.ColorMode == ChartStatColorMode.Background) && (state.StateColor is { } tint))
        {
            canvas.DrawRect(ToRect(layout.Bounds), paints.Fill(ChartColor.Fade(tint.ToSKColor(), BackgroundOpacity * state.Pulse)));
        }

        var line = gradientMode ? Brighten(accent, OnColorLineBrighten) : accent;
        var trendPaint = gradientMode ? new SparklinePaint(line, line, ChartColor.Fade(SKColors.White, OnColorFillOpacity), 1) : new SparklinePaint(line, style.CriticalColor.ToSKColor(), ChartColor.Fade(line, TrendFillOpacity), 1);
        if (layout.HasTrend && (layout.TrendPlacement == ChartTrendPlacement.Background))
        {
            DrawFadedTrend(canvas, layout.TrendBounds, core, trendPaint, style);
        }

        var indicator = state.ColorMode == ChartStatColorMode.Indicator ? state.Pulse : 1;
        if (!layout.BandBounds.IsEmpty && (state.StateColor is { } band))
        {
            canvas.DrawRect(ToRect(layout.BandBounds), paints.Fill(ChartColor.Fade(band.ToSKColor(), indicator)));
        }

        if (layout.HasTrend && (layout.TrendPlacement != ChartTrendPlacement.Background))
        {
            trend.Render(canvas, core.Layout, core.Interpolator, core.Values, trendPaint, style, false);
        }

        var overlay = state.CountUp.OverlayAlpha;
        var valueMode = (state.ColorMode == ChartStatColorMode.Value) && !state.IsEmptyValue;
        var valueColor = valueMode ? ChartColor.Fade(accent, state.Pulse) : accent;
        var primary = gradientMode ? ink : valueMode ? valueColor : (state.IsEmptyValue ? style.MutedTextColor : style.PrimaryTextColor).ToSKColor();
        var secondary = gradientMode ? ink : style.SecondaryTextColor.ToSKColor();
        if (state.StateColor is { } icon)
        {
            DrawIcon(canvas, layout, ChartColor.Fade(gradientMode ? ink : icon.ToSKColor(), overlay * indicator));
        }

        if (layout.Title is { } title)
        {
            text.DrawText(canvas, title, layout.TitleBounds, layout.TitleFontSize, layout.TitleBounds.Width, secondary, style);
        }

        DrawValue(canvas, layout, state, primary, valueMode ? valueColor : secondary, style);
        DrawDelta(canvas, layout, gradientMode ? ink : GetToneColor(layout.Delta.Tone, style), secondary, overlay, style);
    }

    private static SKRect ToRect(ChartRect bounds) => new((float)bounds.X, (float)bounds.Y, (float)bounds.Right, (float)bounds.Bottom);

    private static SKColor GetInk(SKColor color, bool dark)
    {
        if (color.Alpha < TransparentThreshold * 255)
        {
            return dark ? LightInk : DarkInk;
        }

        var brightness = ((color.Red * 299) + (color.Green * 587) + (color.Blue * 114)) / 1000d;
        return brightness > BrightTextThreshold ? DarkInk : LightInk;
    }

    private static SKColor Brighten(SKColor color, int amount)
    {
        var delta = (int)Math.Round(255 * amount / 100d);
        return new SKColor((byte)Math.Min(255, color.Red + delta), (byte)Math.Min(255, color.Green + delta), (byte)Math.Min(255, color.Blue + delta), color.Alpha);
    }

    private static (SKPoint Start, SKPoint End) GetGradientLine(ChartRect bounds)
    {
        var radians = GradientAngle * Math.PI / 180;
        var dx = Math.Sin(radians);
        var dy = -Math.Cos(radians);
        var half = ((bounds.Width * Math.Abs(dx)) + (bounds.Height * Math.Abs(dy))) / 2;
        return (new SKPoint((float)(bounds.CenterX - (dx * half)), (float)(bounds.CenterY - (dy * half))), new SKPoint((float)(bounds.CenterX + (dx * half)), (float)(bounds.CenterY + (dy * half))));
    }

    private static SKColor Shift(SKColor color, float hue, float lightness)
    {
        color.ToHsl(out var h, out var s, out var l);
        return SKColor.FromHsl((h + hue + 360) % 360, s, Math.Clamp(l + lightness, 0, 100), color.Alpha);
    }

    private static SKColor GetToneColor(StatDeltaTone tone, ChartStyle style) =>
        (tone switch
        {
            StatDeltaTone.Good => style.PositiveTextColor,
            StatDeltaTone.Bad => style.NegativeTextColor,
            _ => style.SecondaryTextColor
        }).ToSKColor();

    private float GetBaseline(ChartRect bounds, float size)
    {
        var metrics = text.GetMetrics(size);
        return (float)bounds.Y + (((float)bounds.Height - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
    }

    private float GetMiddle(ChartRect bounds, float size)
    {
        var metrics = text.GetMetrics(size);
        var cap = metrics.CapHeight > 0 ? metrics.CapHeight : size * CapHeightRatio;
        return GetBaseline(bounds, size) - (cap / 2);
    }

    private void DrawGradient(SKCanvas canvas, ChartRect bounds, SKColor color, bool dark, double opacity)
    {
        var key = (color, (float)bounds.Width, (float)bounds.Height, dark);
        if ((gradient is null) || (gradientKey != key))
        {
            var factor = dark ? 1 : LightThemeFactor;
            var start = Shift(color, GradientHueShift, -GradientStartLightness * factor);
            var end = Shift(color, -GradientHueShift, -GradientEndLightness * factor);
            var (from, to) = GetGradientLine(bounds);
            gradient?.Dispose();
            gradient = SKShader.CreateLinearGradient(from, to, [start, end], null, SKShaderTileMode.Clamp);
            gradientKey = key;
        }

        shaderPaint.Shader = gradient;
        shaderPaint.Color = ChartColor.Fade(SKColors.Black, opacity);
        canvas.DrawRect(ToRect(bounds), shaderPaint);
    }

    private void DrawFadedTrend(SKCanvas canvas, ChartRect bounds, SparklineCore core, SparklinePaint paint, ChartStyle style)
    {
        var rect = ToRect(bounds);
        if ((fade is null) || (fadeKey != rect))
        {
            fade?.Dispose();
            fade = SKShader.CreateLinearGradient(new SKPoint(0, rect.Top), new SKPoint(0, rect.MidY), [SKColors.Transparent, SKColors.Black], null, SKShaderTileMode.Clamp);
            fadeKey = rect;
        }

        canvas.SaveLayer(rect, null);
        try
        {
            trend.Render(canvas, core.Layout, core.Interpolator, core.Values, paint, style, false);
            maskPaint.Shader = fade;
            canvas.DrawRect(rect, maskPaint);
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void DrawIcon(SKCanvas canvas, StatLayout layout, SKColor color)
    {
        var level = layout.IconLevel;
        if ((level == ChartThresholdLevel.None) || layout.IconBounds.IsEmpty || (color.Alpha == 0))
        {
            return;
        }

        var size = (float)layout.IconBounds.Width;
        var center = new SKPoint((float)layout.IconBounds.CenterX, GetMiddle(layout.IconBounds, (float)layout.TitleFontSize));
        if (level == ChartThresholdLevel.Critical)
        {
            canvas.DrawCircle(center, size * 0.42f, paints.Fill(color));
            return;
        }

        var key = (level, center, size);
        if ((iconPath is null) || (iconKey != key))
        {
            iconPath?.Dispose();
            iconPath = BuildIcon(level, center, size);
            iconKey = key;
        }

        canvas.DrawPath(iconPath, level == ChartThresholdLevel.Good ? paints.Stroke(color, size * CheckStrokeRatio) : paints.Fill(color));
    }

    private SKPath BuildIcon(ChartThresholdLevel level, SKPoint center, float size)
    {
        var builder = paints.Path;
        var (x, y) = (center.X, center.Y);
        switch (level)
        {
            case ChartThresholdLevel.Good:
                builder.MoveTo(x - (size * 0.38f), y + (size * 0.02f));
                builder.LineTo(x - (size * 0.12f), y + (size * 0.28f));
                builder.LineTo(x + (size * 0.38f), y - (size * 0.28f));
                break;
            case ChartThresholdLevel.Warning:
                builder.MoveTo(x, y - (size * 0.46f));
                builder.LineTo(x + (size * 0.5f), y + (size * 0.4f));
                builder.LineTo(x - (size * 0.5f), y + (size * 0.4f));
                builder.Close();
                break;
            default:
                builder.MoveTo(x, y - (size * 0.48f));
                builder.LineTo(x + (size * 0.48f), y);
                builder.LineTo(x, y + (size * 0.48f));
                builder.LineTo(x - (size * 0.48f), y);
                builder.Close();
                break;
        }

        return builder.Detach();
    }

    private void DrawValue(SKCanvas canvas, StatLayout layout, StatRenderState state, SKColor primary, SKColor secondary, ChartStyle style)
    {
        if (layout.ValueText is not { } value)
        {
            return;
        }

        var size = (float)layout.ValueFontSize;
        var baseline = GetBaseline(layout.ValueBounds, size);
        var halo = style.TextHaloColor.ToSKColor();
        var haloWidth = (float)style.TextHaloWidth;
        float right;
        if (state.CountUp.IsRunning)
        {
            right = DrawCounting(canvas, layout, state, size, baseline, primary, halo, haloWidth);
        }
        else
        {
            var drawable = text.Create(value, size);
            var x = (float)layout.ValueBounds.X;
            text.Draw(canvas, drawable, x, baseline, SKTextAlign.Left, primary, halo, haloWidth);
            right = x + drawable.Width;
        }

        if (layout.Unit is { } unit)
        {
            var x = layout.Mode == ChartStatLayoutMode.Horizontal ? (float)layout.UnitBounds.X : right + (float)layout.UnitGap;
            text.Draw(canvas, text.Create(unit, (float)layout.UnitFontSize), x, baseline, SKTextAlign.Left, secondary, halo, haloWidth);
        }
    }

    private float DrawCounting(SKCanvas canvas, StatLayout layout, StatRenderState state, float size, float baseline, SKColor color, SKColor halo, float haloWidth)
    {
        Span<char> buffer = stackalloc char[BufferSize];
        var number = state.CountUp.Current;
        var chars = state.Formatting.TryFormat(number, buffer, out var written) ? buffer[..written] : state.Formatting.Format(number).AsSpan();
        var width = spans.Measure(text, chars, size);
        var scale = (width > 0) && (width > layout.ValueMaxWidth) ? (float)(layout.ValueMaxWidth / width) : 1f;
        var drawn = width * scale;
        var x = layout.Mode == ChartStatLayoutMode.Horizontal ? (float)layout.ValueBounds.Right - drawn : (float)layout.ValueBounds.X;
        if (scale < 1)
        {
            canvas.Save();
            canvas.Scale(scale, scale, x, baseline);
        }

        spans.Draw(canvas, text, chars, size, x, baseline, color, halo, haloWidth);
        if (scale < 1)
        {
            canvas.Restore();
        }

        return x + drawn;
    }

    private void DrawDelta(SKCanvas canvas, StatLayout layout, SKColor color, SKColor secondary, double overlay, ChartStyle style)
    {
        if ((layout.Delta.Text is not { } delta) || (overlay <= 0))
        {
            return;
        }

        var size = (float)layout.DeltaFontSize;
        var tint = ChartColor.Fade(color, overlay);
        if ((layout.Delta.Direction != 0) && !layout.ArrowBounds.IsEmpty)
        {
            DrawArrow(canvas, layout.ArrowBounds, size, layout.Delta.Direction > 0, tint);
        }

        text.DrawText(canvas, delta, layout.DeltaBounds, size, Double.PositiveInfinity, tint, style);
        if (layout.Caption is { } caption)
        {
            text.DrawText(canvas, caption, layout.CaptionBounds, size, layout.CaptionBounds.Width, ChartColor.Fade(secondary, overlay), style);
        }
    }

    private void DrawArrow(SKCanvas canvas, ChartRect bounds, float size, bool up, SKColor color)
    {
        var width = (float)bounds.Width;
        var center = new SKPoint((float)bounds.CenterX, GetMiddle(bounds, size));
        var key = (up, center, width);
        if ((arrowPath is null) || (arrowKey != key))
        {
            var half = width / 2;
            var height = width * ArrowHeightRatio / 2;
            var tip = up ? -height : height;
            var builder = paints.Path;
            builder.MoveTo(center.X, center.Y + tip);
            builder.LineTo(center.X + half, center.Y - tip);
            builder.LineTo(center.X - half, center.Y - tip);
            builder.Close();
            arrowPath?.Dispose();
            arrowPath = builder.Detach();
            arrowKey = key;
        }

        canvas.DrawPath(arrowPath, paints.Fill(color));
    }
}
