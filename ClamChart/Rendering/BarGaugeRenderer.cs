namespace ClamChart.Rendering;

using System.Runtime.InteropServices;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class BarGaugeRenderer : IDisposable
{
    public const double BasicFillOpacity = 0.35;

    public const float BasicEdgeWidth = 2;

    public const double UnlitOpacity = 0.18;

    public const float TargetWidth = 2;

    public const double Tolerance = 1e-9;

    private const double LitCenterOpacity = 0.95;

    private const double LitEdgeOpacity = 0.55;

    private const float LitCenterStop = 0.1f;

    private static readonly float CornerScale = MathF.Sqrt(2);

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly SKPaint shaderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly Dictionary<(SKColor Color, double Percent), SKShader> gradients = [];
    private readonly Dictionary<(SKColor Color, float Width, float Height), SKShader> cells = [];
    private readonly List<ChartDrawingThreshold> steps = [];
    private int version = -1;
    private SKColor shaderBase;

    public IChartTextMeasurer Measurer => text;

    public BarGaugeRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        ClearShaders();
        paints.Dispose();
        text.Dispose();
        shaderPaint.Dispose();
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, BarGaugeLayout layout, GaugeInterpolator<BarGaugeRow> display, BarGaugeRenderState state, ChartStyle style)
    {
        var baseKey = state.BaseColor.ToSKColor();
        if ((version != layout.Version) || (shaderBase != baseKey))
        {
            ClearShaders();
            version = layout.Version;
            shaderBase = baseKey;
        }

        var track = ChartColor.GetTrack(style);
        var entrance = display.EntranceAlpha;
        var overlay = display.OverlayAlpha;
        for (var i = 0; i < display.Count; i++)
        {
            var row = display.GetRow(i);
            var alpha = display.GetAlpha(i) * entrance;
            if (alpha <= 0)
            {
                continue;
            }

            var slot = display.GetSlot(i);
            var ratio = Double.IsNaN(row.Percent) ? Double.NaN : display.GetRatio(i);
            var bounds = layout.GetTrackBounds(slot);
            var baseColor = row.Color ?? state.BaseColor;
            if (layout.DisplayMode == ChartBarGaugeMode.Segmented)
            {
                DrawCells(canvas, layout, bounds, ratio, baseColor, state, style, alpha);
            }
            else
            {
                DrawTrack(canvas, layout, state, bounds, ratio, ChartColor.Fade(track, alpha));
                if (style.FillMode == ChartFillMode.Gradient)
                {
                    DrawGradient(canvas, layout, bounds, row, ratio, baseColor, state, style, alpha);
                }
                else
                {
                    DrawBasic(canvas, layout, bounds, ratio, GetColor(state, row.Percent, baseColor, style), alpha);
                }
            }

            if (state.TargetVisible && Double.IsFinite(row.TargetRatio))
            {
                DrawTarget(canvas, layout, bounds, row.TargetRatio, ChartColor.Fade(style.PrimaryTextColor.ToSKColor(), alpha));
            }

            if (!String.IsNullOrEmpty(row.Label) && !layout.LabelTemplate.IsEmpty)
            {
                var align = !layout.IsHorizontal ? 0 : layout.LabelPosition == ChartBarGaugeLabelPosition.Left ? 1 : -1;
                DrawAligned(canvas, row.Label, layout.GetLabelBounds(slot), layout.LabelFontSize, align, ChartColor.Fade(style.SecondaryTextColor.ToSKColor(), alpha), style);
            }

            if ((row.ValueText is { } value) && !layout.ValueTemplate.IsEmpty)
            {
                var color = state.ValueColorMode == ChartBarGaugeValueColorMode.Text ? style.PrimaryTextColor.ToSKColor() : GetColor(state, row.Percent, baseColor, style).ToSKColor();
                DrawAligned(canvas, value, layout.GetValueBounds(slot), layout.ValueFontSize, layout.IsHorizontal ? 1 : 0, ChartColor.Fade(color, alpha * overlay), style);
            }
        }
    }

    private static Color GetColor(BarGaugeRenderState state, double percent, Color baseColor, ChartStyle style)
    {
        if (Double.IsNaN(percent) || (state.Thresholds.Count == 0))
        {
            return baseColor;
        }

        return ThresholdResolver.GetColor(ThresholdResolver.Resolve(state.Thresholds, percent), style) ?? baseColor;
    }

    private static double GetFilledLength(BarGaugeLayout layout, double ratio) =>
        Double.IsNaN(ratio) ? 0 : Math.Max(layout.GetTrackLength() * ratio, 1);

    private static SKRect GetBar(BarGaugeLayout layout, ChartRect bounds, double length) =>
        layout.IsHorizontal
            ? new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + length), (float)bounds.Bottom)
            : new SKRect((float)bounds.X, (float)(bounds.Bottom - length), (float)bounds.Right, (float)bounds.Bottom);

    private void DrawTrack(SKCanvas canvas, BarGaugeLayout layout, BarGaugeRenderState state, ChartRect bounds, double ratio, SKColor color)
    {
        if (!state.TrackVisible || bounds.IsEmpty || (color.Alpha == 0))
        {
            return;
        }

        var filled = GetFilledLength(layout, ratio);
        var rest = layout.IsHorizontal
            ? new SKRect((float)(bounds.X + filled), (float)bounds.Y, (float)bounds.Right, (float)bounds.Bottom)
            : new SKRect((float)bounds.X, (float)bounds.Y, (float)bounds.Right, (float)(bounds.Bottom - filled));
        if ((rest.Width > 0) && (rest.Height > 0))
        {
            canvas.DrawRect(rest, paints.Fill(color));
        }
    }

    private void DrawBasic(SKCanvas canvas, BarGaugeLayout layout, ChartRect bounds, double ratio, Color color, double alpha)
    {
        if (Double.IsNaN(ratio) || bounds.IsEmpty)
        {
            return;
        }

        var tint = color.ToSKColor();
        var bar = GetBar(layout, bounds, GetFilledLength(layout, ratio));
        canvas.DrawRect(bar, paints.Fill(ChartColor.Fade(tint, BasicFillOpacity * alpha)));
        var edge = layout.IsHorizontal
            ? new SKRect(Math.Max(bar.Left, bar.Right - BasicEdgeWidth), bar.Top, bar.Right, bar.Bottom)
            : new SKRect(bar.Left, bar.Top, bar.Right, Math.Min(bar.Bottom, bar.Top + BasicEdgeWidth));
        canvas.DrawRect(edge, paints.Fill(ChartColor.Fade(tint, alpha)));
    }

    private void DrawGradient(SKCanvas canvas, BarGaugeLayout layout, ChartRect bounds, BarGaugeRow row, double ratio, Color baseColor, BarGaugeRenderState state, ChartStyle style, double alpha)
    {
        if (Double.IsNaN(ratio) || bounds.IsEmpty)
        {
            return;
        }

        var shader = GetGradient(layout, row, baseColor, state, style, (float)layout.GetTrackLength());
        var drawn = (float)GetFilledLength(layout, ratio);
        canvas.Save();
        try
        {
            if (layout.IsHorizontal)
            {
                canvas.Translate((float)bounds.X, (float)bounds.Y);
                shaderPaint.Shader = shader;
                shaderPaint.Color = SKColors.Black.WithAlpha((byte)Math.Round(255 * alpha));
                canvas.DrawRect(0, 0, drawn, (float)bounds.Height, shaderPaint);
            }
            else
            {
                canvas.Translate((float)bounds.X, (float)bounds.Bottom);
                shaderPaint.Shader = shader;
                shaderPaint.Color = SKColors.Black.WithAlpha((byte)Math.Round(255 * alpha));
                canvas.DrawRect(0, -drawn, (float)bounds.Width, drawn, shaderPaint);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private SKShader GetGradient(BarGaugeLayout layout, BarGaugeRow row, Color baseColor, BarGaugeRenderState state, ChartStyle style, float length)
    {
        var color = baseColor.ToSKColor();
        var key = (color, row.Percent);
        if (gradients.TryGetValue(key, out var shader))
        {
            return shader;
        }

        ThresholdSteps.Resolve(state.Thresholds, color, style, steps);
        var (colors, positions) = BarGaugeGradient.GetStops(CollectionsMarshal.AsSpan(steps), row.Percent, row.Ratio, color);
        var end = layout.IsHorizontal ? new SKPoint(length, 0) : new SKPoint(0, -length);
        shader = SKShader.CreateLinearGradient(SKPoint.Empty, end, colors, positions, SKShaderTileMode.Clamp);
        gradients[key] = shader;
        return shader;
    }

    public static SKShader CreateLitCell(SKColor color, float width, float height)
    {
        var matrix = SKMatrix.CreateScale(width / 2 * CornerScale, height / 2 * CornerScale);
        return SKShader.CreateRadialGradient(SKPoint.Empty, 1, [ChartColor.Fade(color, LitCenterOpacity), ChartColor.Fade(color, LitEdgeOpacity)], [LitCenterStop, 1], SKShaderTileMode.Clamp, matrix);
    }

    private void DrawCells(SKCanvas canvas, BarGaugeLayout layout, ChartRect bounds, double ratio, Color baseColor, BarGaugeRenderState state, ChartStyle style, double alpha)
    {
        var count = layout.CellCount;
        var size = (float)layout.CellSize;
        if ((count <= 0) || (size <= 0) || bounds.IsEmpty)
        {
            return;
        }

        var pitch = size + (float)layout.CellGap;
        var across = layout.IsHorizontal ? (float)bounds.Height : (float)bounds.Width;
        for (var c = 0; c < count; c++)
        {
            var fraction = (double)c / count;
            var color = GetColor(state, fraction * 100, baseColor, style).ToSKColor();
            var start = c * pitch;
            var cell = layout.IsHorizontal
                ? new SKRect((float)bounds.X + start, (float)bounds.Y, (float)bounds.X + start + size, (float)bounds.Bottom)
                : new SKRect((float)bounds.X, (float)bounds.Bottom - start - size, (float)bounds.Right, (float)bounds.Bottom - start);
            if (!Double.IsNaN(ratio) && (fraction <= ratio + Tolerance))
            {
                DrawLitCell(canvas, cell, color, layout.IsHorizontal ? size : across, layout.IsHorizontal ? across : size, alpha);
            }
            else
            {
                canvas.DrawRect(cell, paints.Fill(ChartColor.Fade(color, UnlitOpacity * alpha)));
            }
        }
    }

    private void DrawLitCell(SKCanvas canvas, SKRect cell, SKColor color, float width, float height, double alpha)
    {
        var key = (color, width, height);
        if (!cells.TryGetValue(key, out var shader))
        {
            shader = CreateLitCell(color, width, height);
            cells[key] = shader;
        }

        canvas.Save();
        try
        {
            canvas.Translate(cell.MidX, cell.MidY);
            shaderPaint.Shader = shader;
            shaderPaint.Color = SKColors.Black.WithAlpha((byte)Math.Round(255 * alpha));
            canvas.DrawRect(-width / 2, -height / 2, width, height, shaderPaint);
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void DrawTarget(SKCanvas canvas, BarGaugeLayout layout, ChartRect bounds, double ratio, SKColor color)
    {
        var position = layout.GetTrackLength() * ratio;
        var overhang = (float)BarGaugeLayout.TargetOverhang;
        var line = layout.IsHorizontal
            ? new SKRect((float)(bounds.X + position) - (TargetWidth / 2), (float)bounds.Y - overhang, (float)(bounds.X + position) + (TargetWidth / 2), (float)bounds.Bottom + overhang)
            : new SKRect((float)bounds.X - overhang, (float)(bounds.Bottom - position) - (TargetWidth / 2), (float)bounds.Right + overhang, (float)(bounds.Bottom - position) + (TargetWidth / 2));
        canvas.DrawRect(line, paints.Fill(color));
    }

    private void DrawAligned(SKCanvas canvas, string value, ChartRect bounds, double fontSize, int align, SKColor color, ChartStyle style)
    {
        var size = (float)fontSize;
        var drawable = text.Create(value, size, (float)bounds.Width);
        var metrics = text.GetMetrics(size);
        var baseline = (float)bounds.Y + (((float)bounds.Height - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
        var x = align switch
        {
            < 0 => (float)bounds.X,
            0 => (float)bounds.CenterX - (drawable.Width / 2),
            _ => (float)bounds.Right - drawable.Width
        };
        text.Draw(canvas, drawable, x, baseline, SKTextAlign.Left, color, style.TextHaloColor.ToSKColor(), (float)style.TextHaloWidth);
    }

    private void ClearShaders()
    {
        foreach (var shader in gradients.Values)
        {
            shader.Dispose();
        }

        gradients.Clear();
        foreach (var shader in cells.Values)
        {
            shader.Dispose();
        }

        cells.Clear();
    }
}
