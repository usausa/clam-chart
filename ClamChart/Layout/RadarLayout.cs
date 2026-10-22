namespace ClamChart.Layout;

using System.Globalization;
using System.Runtime.InteropServices;

using ClamChart.Data;
using ClamChart.Rendering;

internal sealed class RadarLayout
{
    public const double LegendGap = 8;

    public const double LabelGap = 8;

    public const double LevelLabelPadding = 2;

    public const double PointRadius = 22;

    private const double LabelWidthRatio = 0.25;

    private const double MinLabeledRadiusRatio = 0.4;

    private const double AlignThreshold = 0.05;

    private const double Tolerance = 1e-9;

    private readonly Dictionary<string, int> indexes = [with(StringComparer.Ordinal)];
    private readonly List<string?> names = [];
    private readonly List<string> headers = [];
    private readonly List<double> labelWidths = [];
    private readonly List<RadarAxis> axes = [];
    private readonly List<RadarLevel> levels = [];
    private readonly List<RadarLevel> candidates = [];
    private readonly List<LabelExtent> extents = [];
    private readonly List<int> selected = [];
    private readonly List<LegendEntry> legendEntries = [];
    private readonly List<double> ticks = [];
    private double[] values = [];
    private int[] indices = [];
    private int nullIndex = -1;

    public ChartRect Bounds { get; private set; }

    public ChartRect PlotBounds { get; private set; }

    public double CenterX { get; private set; }

    public double CenterY { get; private set; }

    public double Radius { get; private set; }

    public AxisScale Scale { get; private set; } = new(0, 1, 1);

    public ChartRadarGridShape GridShape { get; private set; }

    public bool IsCircle => (GridShape == ChartRadarGridShape.Circle) || (AxisCount < 3);

    public bool ShowsLabels { get; private set; }

    public double LabelHeight { get; private set; }

    public int SeriesCount { get; private set; }

    public int AxisCount { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyList<RadarAxis> Axes => axes;

    public IReadOnlyList<RadarLevel> Levels => levels;

    public IReadOnlyList<double> Ticks => ticks;

    public LegendLayout Legend { get; } = new();

    public ChartValueFormatting ValueFormatting { get; private set; } = ChartValueFormatting.Default;

    public static double GetRatio(double value, AxisScale scale) =>
        scale.Span > 0 ? Math.Clamp((value - scale.Minimum) / scale.Span, 0, 1) : 0;

    public static string FormatValue(double value, CartesianSeries series, ChartValueFormatting formatting)
    {
        if ((series.ValueFormatter is not null) || (series.ValueConverter is not null) || !series.ValueFormat.IsDefault)
        {
            return ChartTextFormat.FormatValue(value, series.ValueFormatter, series.ValueConverter, series.ValueFormat, formatting.Culture);
        }

        return FormatLevel(value, formatting);
    }

    public double GetValue(int seriesIndex, int axis) => values[(seriesIndex * AxisCount) + axis];

    public int GetDataIndex(int seriesIndex, int axis) => indices[(seriesIndex * AxisCount) + axis];

    public int FindAxis(int seriesIndex, int dataIndex)
    {
        for (var k = 0; k < AxisCount; k++)
        {
            if (GetDataIndex(seriesIndex, k) == dataIndex)
            {
                return k;
            }
        }

        return -1;
    }

    public string GetAxisText(int axis) => headers[axis];

    public (double X, double Y) GetPoint(int axis, double ratio) =>
        (CenterX + (axes[axis].DirectionX * Radius * ratio), CenterY + (axes[axis].DirectionY * Radius * ratio));

    public void Update(double width, double height, Thickness padding, ChartStyle style, CultureInfo culture, RadarLayoutOptions options, IReadOnlyList<CartesianSeries> series, IChartTextMeasurer measurer)
    {
        Version++;
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        GridShape = options.GridShape;
        SeriesCount = series.Count;
        ValueFormatting = new ChartValueFormatting(options.ValueFormatter, options.ValueConverter, options.ValueFormat, culture);
        LabelHeight = measurer.GetLineHeight(style.AxisLabelFontSize);
        MergeAxes(series);
        ResolveScale(series, options);
        PlaceLegend(series, options, style, measurer);
        PlaceRadar(options, style, measurer);
        PlaceLevels(style, measurer);
    }

    public ChartHit HitTest(double x, double y, IReadOnlyList<CartesianSeries> series)
    {
        var items = Legend.Items;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Bounds.Contains(x, y))
            {
                return new ChartHit(ChartHitKind.Legend, items[i].SeriesIndex);
            }
        }

        var (seriesIndex, axis) = FindNearestPoint(x, y, series);
        if (seriesIndex >= 0)
        {
            return new ChartHit(ChartHitKind.Item, seriesIndex, GetDataIndex(seriesIndex, axis));
        }

        return IsInside(x, y) ? new ChartHit(ChartHitKind.Plot) : ChartHit.None;
    }

    public (int Series, int Axis) FindNearestPoint(double x, double y, IReadOnlyList<CartesianSeries> series)
    {
        var result = (Series: -1, Axis: -1);
        var nearest = Double.PositiveInfinity;
        for (var s = Math.Min(series.Count, SeriesCount) - 1; s >= 0; s--)
        {
            if (!series[s].IsVisible)
            {
                continue;
            }

            for (var k = 0; k < AxisCount; k++)
            {
                var value = GetValue(s, k);
                if (!Double.IsFinite(value))
                {
                    continue;
                }

                var (px, py) = GetPoint(k, GetRatio(value, Scale));
                var distance = ((px - x) * (px - x)) + ((py - y) * (py - y));
                if (distance < nearest)
                {
                    nearest = distance;
                    result = (s, k);
                }
            }
        }

        return nearest <= PointRadius * PointRadius ? result : (-1, -1);
    }

    public bool IsInside(double x, double y)
    {
        var dx = x - CenterX;
        var dy = y - CenterY;
        var limit = Radius + LabelGap;
        return (Radius > 0) && ((dx * dx) + (dy * dy) <= limit * limit);
    }

    public int FindAxis(double x, double y)
    {
        var dx = x - CenterX;
        var dy = y - CenterY;
        if ((AxisCount == 0) || ((dx * dx) + (dy * dy) < Tolerance))
        {
            return -1;
        }

        var step = 360d / AxisCount;
        return (int)Math.Round(PieLayout.Normalize(PieLayout.GetClockAngle(dx, dy)) / step) % AxisCount;
    }

    private static string FormatLevel(double value, ChartValueFormatting formatting) =>
        formatting.IsDefault ? CompactNumberFormat.Format(value, formatting.Culture) : formatting.Format(value);

    private static double GetDirection(RadarAxis axis, bool vertical) => vertical ? axis.DirectionY : axis.DirectionX;

    private static (double Left, double Right) GetLabelOffsets(double direction, double width)
    {
        if (Math.Abs(direction) < AlignThreshold)
        {
            return (-width / 2, width / 2);
        }

        return direction > 0 ? (0, width) : (-width, 0);
    }

    private void MergeAxes(IReadOnlyList<CartesianSeries> series)
    {
        indexes.Clear();
        names.Clear();
        nullIndex = -1;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var s = 0; s < series.Count; s++)
        {
            var data = series[s].Data;
            if (data.XKind == ChartXKind.Category)
            {
                for (var i = 0; i < data.Count; i++)
                {
                    GetIndex(data.GetCategory(i));
                }
            }
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var s = 0; s < series.Count; s++)
        {
            var data = series[s].Data;
            if (data.XKind != ChartXKind.Category)
            {
                while (names.Count < data.Count)
                {
                    names.Add(null);
                }
            }
        }

        AxisCount = names.Count;
        var size = series.Count * AxisCount;
        if (values.Length < size)
        {
            values = new double[size];
            indices = new int[size];
        }

        Array.Fill(values, Double.NaN, 0, size);
        Array.Fill(indices, -1, 0, size);
        for (var s = 0; s < series.Count; s++)
        {
            var data = series[s].Data;
            var y = data.Y;
            var category = data.XKind == ChartXKind.Category;
            for (var i = 0; i < data.Count; i++)
            {
                var slot = (s * AxisCount) + (category ? GetIndex(data.GetCategory(i)) : i);
                if (indices[slot] < 0)
                {
                    indices[slot] = i;
                    values[slot] = y[i];
                }
            }
        }
    }

    private int GetIndex(string? category)
    {
        if (category is null)
        {
            if (nullIndex < 0)
            {
                nullIndex = names.Count;
                names.Add(null);
            }

            return nullIndex;
        }

        if (!indexes.TryGetValue(category, out var index))
        {
            index = names.Count;
            indexes.Add(category, index);
            names.Add(category);
        }

        return index;
    }

    private void ResolveScale(IReadOnlyList<CartesianSeries> series, RadarLayoutOptions options)
    {
        var minimum = Double.NaN;
        var maximum = Double.NaN;
        for (var s = 0; s < series.Count; s++)
        {
            if (!series[s].IsVisible)
            {
                continue;
            }

            for (var k = 0; k < AxisCount; k++)
            {
                var value = GetValue(s, k);
                if (Double.IsFinite(value))
                {
                    minimum = Double.IsNaN(minimum) ? value : Math.Min(minimum, value);
                    maximum = Double.IsNaN(maximum) ? value : Math.Max(maximum, value);
                }
            }
        }

        Scale = AxisRangeResolver.Resolve(minimum, maximum, new AxisRangeOptions { Minimum = options.Minimum, Maximum = options.Maximum, IncludeZero = true }, options.LevelCount);
        TickGenerator.Numeric(Scale, ticks);
    }

    private void PlaceLegend(IReadOnlyList<CartesianSeries> series, RadarLayoutOptions options, ChartStyle style, IChartTextMeasurer measurer)
    {
        legendEntries.Clear();
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            if (item.LegendVisible && !String.IsNullOrEmpty(item.Title))
            {
                legendEntries.Add(new LegendEntry(s, item.Title, LegendMarkerKind.Dot));
            }
        }

        var area = Bounds;
        PlotBounds = area;
        if (!options.LegendVisible || (legendEntries.Count == 0))
        {
            Legend.Clear();
            return;
        }

        if (options.LegendPosition is ChartLegendPosition.Top or ChartLegendPosition.Bottom)
        {
            var height = Legend.MeasureRows(legendEntries, area.Width, measurer, style.LegendFontSize, style.LegendMarkerSize, options.LegendMaxRows);
            var top = options.LegendPosition == ChartLegendPosition.Top;
            Legend.Offset(area.X, top ? area.Y : area.Bottom - height);
            var used = Math.Min(area.Height, height + LegendGap);
            PlotBounds = new ChartRect(area.X, top ? area.Y + used : area.Y, area.Width, area.Height - used);
            return;
        }

        var width = Legend.MeasureColumn(legendEntries, area.Height, measurer, style.LegendFontSize, style.LegendMarkerSize);
        if (area.Width - width - LegendGap < width * 2)
        {
            Legend.Clear();
            return;
        }

        var left = options.LegendPosition == ChartLegendPosition.Left;
        Legend.Offset(left ? area.X : area.Right - width, area.Y + Math.Max(0, (area.Height - Legend.Bounds.Height) / 2));
        PlotBounds = new ChartRect(left ? area.X + width + LegendGap : area.X, area.Y, area.Width - width - LegendGap, area.Height);
    }

    private void PlaceRadar(RadarLayoutOptions options, ChartStyle style, IChartTextMeasurer measurer)
    {
        axes.Clear();
        headers.Clear();
        labelWidths.Clear();
        var area = PlotBounds;
        var maxWidth = area.Width * LabelWidthRatio;
        var named = false;
        for (var k = 0; k < AxisCount; k++)
        {
            var text = names[k];
            var (x, y) = PieLayout.GetDirection(360d * k / AxisCount);
            axes.Add(new RadarAxis(text, x, y, default, maxWidth));
            headers.Add(text ?? (k + 1).ToString(ValueFormatting.Culture));
            labelWidths.Add(text is null ? 0 : measurer.Measure(text, style.AxisLabelFontSize, maxWidth));
            named |= text is not null;
        }

        var margin = options.RingMargin;
        var (radius, centerX, centerY) = Fit(area, margin, false);
        ShowsLabels = false;
        if (options.LabelsVisible && named)
        {
            var labeled = Fit(area, margin, true);
            if ((labeled.Radius > 0) && (labeled.Radius >= radius * MinLabeledRadiusRatio))
            {
                ShowsLabels = true;
                (radius, centerX, centerY) = labeled;
            }
        }

        Radius = radius;
        CenterX = centerX;
        CenterY = centerY;
        if (!ShowsLabels)
        {
            return;
        }

        for (var k = 0; k < AxisCount; k++)
        {
            var axis = axes[k];
            if (axis.Text is null)
            {
                continue;
            }

            var (left, _) = GetLabelOffsets(axis.DirectionX, labelWidths[k]);
            var x = CenterX + ((Radius + LabelGap) * axis.DirectionX) + left;
            var y = CenterY + ((Radius + LabelGap) * axis.DirectionY) + ((axis.DirectionY - 1) * LabelHeight / 2);
            axes[k] = axis with { LabelBounds = new ChartRect(x, y, labelWidths[k], LabelHeight) };
        }
    }

    private (double Radius, double CenterX, double CenterY) Fit(ChartRect area, double margin, bool labeled)
    {
        var count = (IsCircle ? 2 : AxisCount) + (labeled ? AxisCount : 0);
        var radius = Math.Min(Solve(false, count, area.X, area.Right, margin), Solve(true, count, area.Y, area.Bottom, margin));
        return (radius, GetCenter(false, count, area.X, area.Right, margin, radius), GetCenter(true, count, area.Y, area.Bottom, margin, radius));
    }

    private double Solve(bool vertical, int count, double low, double high, double margin)
    {
        var upper = Double.PositiveInfinity;
        var lower = 0d;
        for (var i = 0; i < count; i++)
        {
            var (from, start, _) = GetExtent(i, vertical, margin);
            for (var j = 0; j < count; j++)
            {
                var (to, _, end) = GetExtent(j, vertical, margin);
                var room = high - low + start - end;
                var slope = to - from;
                if (slope > Tolerance)
                {
                    upper = Math.Min(upper, room / slope);
                }
                else if (slope < -Tolerance)
                {
                    lower = Math.Max(lower, room / slope);
                }
                else if (room < 0)
                {
                    return 0;
                }
            }
        }

        return Double.IsFinite(upper) && (lower <= upper) ? Math.Max(0, upper) : 0;
    }

    private double GetCenter(bool vertical, int count, double low, double high, double margin, double radius)
    {
        var from = Double.NegativeInfinity;
        var to = Double.PositiveInfinity;
        for (var i = 0; i < count; i++)
        {
            var (direction, start, end) = GetExtent(i, vertical, margin);
            from = Math.Max(from, low - (radius * direction) - start);
            to = Math.Min(to, high - (radius * direction) - end);
        }

        return from <= to ? (from + to) / 2 : (low + high) / 2;
    }

    private (double Direction, double Start, double End) GetExtent(int index, bool vertical, double margin)
    {
        var rings = IsCircle ? 2 : AxisCount;
        if (index < rings)
        {
            return (IsCircle ? (index == 0 ? -1 : 1) : GetDirection(axes[index], vertical), -margin, margin);
        }

        var k = index - rings;
        var axis = axes[k];
        var direction = GetDirection(axis, vertical);
        if (axis.Text is null)
        {
            return (direction, -margin, margin);
        }

        if (vertical)
        {
            var top = (LabelGap * direction) + ((direction - 1) * LabelHeight / 2);
            return (direction, top, top + LabelHeight);
        }

        var (left, right) = GetLabelOffsets(direction, labelWidths[k]);
        return (direction, (LabelGap * direction) + left, (LabelGap * direction) + right);
    }

    private void PlaceLevels(ChartStyle style, IChartTextMeasurer measurer)
    {
        levels.Clear();
        candidates.Clear();
        extents.Clear();
        if (!ShowsLabels || (Radius <= 0))
        {
            return;
        }

        var half = (LabelHeight / 2) + LevelLabelPadding;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < ticks.Count; i++)
        {
            var value = ticks[i];
            if ((value <= Scale.Minimum) || (value > Scale.Maximum))
            {
                continue;
            }

            var text = FormatLevel(value, ValueFormatting);
            var radius = Radius * GetRatio(value, Scale);
            candidates.Add(new RadarLevel(value, text, measurer.Measure(text, style.AxisLabelFontSize, Double.PositiveInfinity)));
            extents.Add(new LabelExtent(radius - half, radius + half));
        }

        LabelThinning.Select(CollectionsMarshal.AsSpan(extents), 0, Int32.MaxValue, selected);
        foreach (var index in selected)
        {
            levels.Add(candidates[index]);
        }
    }
}
