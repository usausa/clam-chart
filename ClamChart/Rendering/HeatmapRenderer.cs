namespace ClamChart.Rendering;

using ClamChart.Animation;
using ClamChart.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class HeatmapRenderer : IDisposable
{
    private const float OutlineWidth = 2;

    private readonly PaintCache paints = new();
    private readonly ChartTextPainter text;
    private readonly SKColor[] palette = new SKColor[HeatmapPalette.Steps];
    private readonly List<SKColor> tooltipColors = [];
    private IReadOnlyList<Color>? paletteSource;

    public IChartTextMeasurer Measurer => text;

    public HeatmapRenderer(string fontFamily)
    {
        text = new ChartTextPainter(fontFamily);
    }

    public void Dispose()
    {
        paints.Dispose();
        text.Dispose();
    }

    public void DrawEmptyText(SKCanvas canvas, string value, ChartRect bounds, ChartStyle style) => text.DrawEmptyText(canvas, value, bounds, style);

    public void Render(SKCanvas canvas, HeatmapLayout layout, HeatmapInterpolator display, IReadOnlyList<Color> colors, HeatmapInteraction interaction, ChartStyle style, float width, float height)
    {
        if (!ReferenceEquals(colors, paletteSource))
        {
            paletteSource = colors;
            HeatmapPalette.Build(colors, palette);
        }

        var scale = canvas.TotalMatrix.ScaleX;
        DrawCells(canvas, layout, display, style, scale);
        DrawLabels(canvas, layout.RowLabels, style);
        DrawLabels(canvas, layout.ColumnLabels, style);
        DrawOutlines(canvas, layout, display, interaction, style, scale);
        DrawScale(canvas, layout, scale);
        DrawLabels(canvas, layout.ScaleLabels, style);
        DrawTooltip(canvas, layout, display, interaction, style, width, height);
    }

    private static float Snap(double value, float scale) => scale > 0 ? MathF.Round((float)value * scale) / scale : (float)value;

    private static SKRect GetCellRect(HeatmapLayout layout, int cell, float scale)
    {
        var grid = layout.GridBounds;
        var column = layout.GetColumn(cell);
        var row = layout.GetRow(cell);
        var gap = Snap(layout.Gap, scale);
        var pixel = scale > 0 ? 1 / scale : 1;
        var left = Snap(grid.X + (column * layout.CellWidth), scale);
        var top = Snap(grid.Y + (row * layout.CellHeight), scale);
        var right = Math.Max(left + pixel, Snap(grid.X + ((column + 1) * layout.CellWidth), scale) - gap);
        var bottom = Math.Max(top + pixel, Snap(grid.Y + ((row + 1) * layout.CellHeight), scale) - gap);
        return new SKRect(left, top, right, bottom);
    }

    private SKColor GetColor(HeatmapInterpolator display, int cell, ChartStyle style)
    {
        var value = display.GetValue(cell);
        return Double.IsFinite(value) ? palette[HeatmapPalette.GetIndex(value, display.Minimum, display.Maximum, palette.Length)] : ChartColor.GetTrack(style);
    }

    private void DrawCells(SKCanvas canvas, HeatmapLayout layout, HeatmapInterpolator display, ChartStyle style, float scale)
    {
        var count = layout.CellCount;
        if ((count == 0) || !(layout.CellWidth > 0) || !(layout.CellHeight > 0))
        {
            return;
        }

        var reveal = display.RevealRatio;
        if (reveal < 1)
        {
            var grid = layout.GridBounds;
            canvas.Save();
            canvas.ClipRect(new SKRect((float)grid.X, (float)grid.Y, (float)(grid.X + (grid.Width * reveal)), (float)grid.Bottom));
        }

        var columns = layout.ColumnCount;
        for (var cell = 0; cell < count; cell++)
        {
            if (!layout.IsPresent(cell))
            {
                continue;
            }

            var column = layout.GetColumn(cell);
            var alpha = display.GetAlpha(column, columns);
            var size = (float)display.GetScale(column, columns);
            if ((alpha <= 0) || (size <= 0))
            {
                continue;
            }

            var rect = GetCellRect(layout, cell, scale);
            if (size < 1)
            {
                rect.Inflate(-rect.Width * (1 - size) / 2, -rect.Height * (1 - size) / 2);
            }

            canvas.DrawRect(rect, paints.Fill(ChartColor.Fade(GetColor(display, cell, style), alpha)));
        }

        if (reveal < 1)
        {
            canvas.Restore();
        }
    }

    private void DrawLabels(SKCanvas canvas, IReadOnlyList<HeatmapLabel> labels, ChartStyle style)
    {
        var color = style.MutedTextColor.ToSKColor();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            text.DrawText(canvas, label.Text, label.Bounds, style.AxisLabelFontSize, label.MaxWidth, color, style);
        }
    }

    private void DrawOutlines(SKCanvas canvas, HeatmapLayout layout, HeatmapInterpolator display, HeatmapInteraction interaction, ChartStyle style, float scale)
    {
        var color = style.PrimaryTextColor.ToSKColor();
        var progress = interaction.SelectionProgress < 1 ? display.Ease(interaction.SelectionProgress) : 1;
        if (progress < 1)
        {
            DrawOutline(canvas, layout, layout.FindCellOfIndex(interaction.PreviousIndex), ChartColor.Fade(color, 1 - progress), scale);
        }

        DrawOutline(canvas, layout, layout.FindCellOfIndex(interaction.SelectedIndex), ChartColor.Fade(color, progress), scale);
        if (interaction.IsTracking)
        {
            DrawOutline(canvas, layout, interaction.TrackedCell, color, scale);
        }
    }

    private void DrawOutline(SKCanvas canvas, HeatmapLayout layout, int cell, SKColor color, float scale)
    {
        if ((cell < 0) || (cell >= layout.CellCount) || (color.Alpha == 0))
        {
            return;
        }

        var rect = GetCellRect(layout, cell, scale);
        rect.Inflate(OutlineWidth / 2, OutlineWidth / 2);
        canvas.DrawRect(rect, paints.Stroke(color, OutlineWidth, SKStrokeCap.Butt, SKStrokeJoin.Miter));
    }

    private void DrawScale(SKCanvas canvas, HeatmapLayout layout, float scale)
    {
        var bounds = layout.ScaleBounds;
        if (!layout.ShowsScale || bounds.IsEmpty)
        {
            return;
        }

        var steps = palette.Length;
        var vertical = layout.IsScaleVertical;
        var length = vertical ? bounds.Height : bounds.Width;
        var start = vertical ? bounds.Bottom : bounds.X;
        var direction = vertical ? -1 : 1;
        var from = Snap(start, scale);
        for (var i = 0; i < steps; i++)
        {
            var to = Snap(start + (direction * length * (i + 1) / steps), scale);
            if (!from.Equals(to))
            {
                var rect = vertical
                    ? new SKRect((float)bounds.X, Math.Min(from, to), (float)bounds.Right, Math.Max(from, to))
                    : new SKRect(Math.Min(from, to), (float)bounds.Y, Math.Max(from, to), (float)bounds.Bottom);
                canvas.DrawRect(rect, paints.Fill(palette[i]));
            }

            from = to;
        }
    }

    private void DrawTooltip(SKCanvas canvas, HeatmapLayout layout, HeatmapInterpolator display, HeatmapInteraction interaction, ChartStyle style, float width, float height)
    {
        if (!interaction.IsTracking || !interaction.TooltipVisible || (interaction.TooltipHeader is not { } header))
        {
            return;
        }

        var cell = interaction.TrackedCell;
        tooltipColors.Clear();
        tooltipColors.Add((cell >= 0) && (cell < layout.CellCount) ? GetColor(display, cell, style) : SKColors.Transparent);
        ChartTooltipPainter.Draw(canvas, text, paints, header, interaction.TrackedTexts, tooltipColors, interaction.TrackPoint, style, width, height);
    }
}
