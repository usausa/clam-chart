namespace ClamChart.Layout;

using System.Globalization;

using ClamChart.Rendering;

internal sealed class RadialBarLayout
{
    public const double LegendGap = 8;

    public const double CenterPadding = 4;

    public const double CaptionGap = 2;

    public const double MaxCenterFontSize = 32;

    public const double MinCenterFontSize = 6;

    public const double MaxThicknessRatio = 1d / 6;

    public const int DefaultCenterKey = -1;

    private const double CenterFontRatio = 0.5;

    private const double CaptionFontRatio = 0.45;

    private const double MinExtent = 1e-6;

    private const double MinThickness = 1;

    private const int FitAttempts = 3;

    private const string PercentPattern = "P0";

    private static readonly ChartTextFormat PercentFormat = ChartTextFormat.Parse(PercentPattern);

    private readonly List<RadialBarRow> rows = [];
    private readonly List<RadialBarCenter> ringCenters = [];
    private readonly List<LegendEntry> legendEntries = [];
    private int defaultKey = DefaultCenterKey;

    public int Version { get; private set; }

    public ChartRect Bounds { get; private set; }

    public ChartRect RingBounds { get; private set; }

    public double CenterX { get; private set; }

    public double CenterY { get; private set; }

    public double OuterRadius { get; private set; }

    public double Thickness { get; private set; }

    public double Spacing { get; private set; }

    public double StartAngle { get; private set; }

    public double SweepAngle { get; private set; } = 360;

    public ChartCapStyle CapStyle { get; private set; }

    public ChartRadialBarMode DisplayMode { get; private set; }

    public ChartOverflowMode OverflowMode { get; private set; }

    public int SegmentCount { get; private set; }

    public double SegmentGap { get; private set; }

    public ChartLegendValueMode LegendValueMode { get; private set; }

    public IReadOnlyList<RadialBarRow> Rows => rows;

    public LegendLayout Legend { get; } = new();

    public RadialBarCenter Center { get; private set; }

    public IReadOnlyList<RadialBarCenter> RingCenters => ringCenters;

    public ChartValueFormatting PercentFormatting { get; private set; } = ChartValueFormatting.Default;

    public ChartValueFormatting ValueFormatting { get; private set; } = ChartValueFormatting.Default;

    public bool IsFullCircle => SweepAngle >= 360;

    public double GetRingRadius(double slot) => OuterRadius - (Thickness / 2) - (slot * (Thickness + Spacing));

    public double GetInnerEdge() => rows.Count > 0 ? GetRingRadius(rows.Count - 1) - (Thickness / 2) : OuterRadius;

    public double GetAngle(double fraction) => StartAngle + (SweepAngle * fraction);

    public int FindRow(int dataIndex)
    {
        if (dataIndex < 0)
        {
            return -1;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].DataIndex == dataIndex)
            {
                return i;
            }
        }

        return -1;
    }

    public RadialBarCenter GetCenter(int key)
    {
        var row = FindRow(key);
        return (row >= 0) && (row < ringCenters.Count) && ringCenters[row].IsVisible ? ringCenters[row] : Center;
    }

    public int GetCenterKey(int key)
    {
        var row = FindRow(key);
        return (row >= 0) && (row < ringCenters.Count) && ringCenters[row].IsVisible ? key : defaultKey;
    }

    public int FindRingAt(double x, double y)
    {
        if ((rows.Count == 0) || (Thickness <= 0))
        {
            return -1;
        }

        var dx = x - CenterX;
        var dy = y - CenterY;
        if (!IsFullCircle && (PieLayout.Normalize(PieLayout.GetClockAngle(dx, dy) - StartAngle) > SweepAngle))
        {
            return -1;
        }

        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        var half = (Thickness + Spacing) / 2;
        for (var i = 0; i < rows.Count; i++)
        {
            if (Math.Abs(distance - GetRingRadius(rows[i].Slot)) <= half)
            {
                return i;
            }
        }

        return -1;
    }

    public ChartHit HitTest(double x, double y)
    {
        var items = Legend.Items;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Bounds.Contains(x, y))
            {
                return new ChartHit(ChartHitKind.Legend, -1, items[i].SeriesIndex);
            }
        }

        var index = FindRingAt(x, y);
        return index >= 0 ? new ChartHit(ChartHitKind.Item, -1, index) : ChartHit.None;
    }

    public void Update(double width, double height, Thickness padding, ChartStyle style, CultureInfo culture, RadialBarLayoutOptions options, ReadOnlySpan<GaugeEntry> entries, IChartTextMeasurer measurer)
    {
        Version++;
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        StartAngle = options.StartAngle;
        SweepAngle = options.SweepAngle;
        CapStyle = options.CapStyle;
        DisplayMode = options.DisplayMode;
        OverflowMode = options.OverflowMode;
        SegmentCount = options.SegmentCount;
        SegmentGap = style.SegmentGap;
        Spacing = options.RingSpacing;
        LegendValueMode = options.LegendValueMode;
        ValueFormatting = options.Formatting;
        PercentFormatting = new ChartValueFormatting(null, null, PercentFormat, culture);
        BuildRows(options, culture, entries);
        PlaceLegend(options, style, measurer);
        PlaceRings(options);
        PrepareCenters(options, style, entries, measurer);
    }

    private static double RoundSize(double size) => Math.Floor(size * 2) / 2;

    private void BuildRows(RadialBarLayoutOptions options, CultureInfo culture, ReadOnlySpan<GaugeEntry> entries)
    {
        rows.Clear();
        var wrap = (options.OverflowMode == ChartOverflowMode.Wrap) && (options.DisplayMode != ChartRadialBarMode.Segmented);
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var maximum = Double.IsFinite(entry.Maximum) ? entry.Maximum : options.Maximum;
            var range = maximum - options.Minimum;
            var valid = Double.IsFinite(entry.Value) && (range > 0);
            var raw = valid ? (entry.Value - options.Minimum) / range : Double.NaN;
            var ratio = !valid ? 0 : wrap ? Math.Max(0, raw) : Math.Clamp(raw, 0, 1);
            var percentText = valid ? raw.ToString(PercentPattern, culture) : null;
            var valueText = valid ? options.Formatting.Format(entry.Value) : null;
            rows.Add(new RadialBarRow(entry.DataIndex, i, entry.Label, ratio, valid ? raw * 100 : Double.NaN, entry.Color, percentText, valueText));
        }
    }

    private void PlaceLegend(RadialBarLayoutOptions options, ChartStyle style, IChartTextMeasurer measurer)
    {
        legendEntries.Clear();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (String.IsNullOrEmpty(row.Label))
            {
                continue;
            }

            var value = options.LegendValueMode switch
            {
                ChartLegendValueMode.Value => row.ValueText,
                ChartLegendValueMode.Percent => row.PercentText,
                _ => null
            };
            legendEntries.Add(new LegendEntry(i, row.Label, LegendMarkerKind.Dot, value));
        }

        var area = Bounds;
        RingBounds = area;
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
            RingBounds = new ChartRect(area.X, top ? area.Y + used : area.Y, area.Width, area.Height - used);
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
        RingBounds = new ChartRect(left ? area.X + width + LegendGap : area.X, area.Y, area.Width - width - LegendGap, area.Height);
    }

    private void PlaceRings(RadialBarLayoutOptions options)
    {
        var area = RingBounds;
        var (left, top, right, bottom) = PieLayout.GetUnitBounds(StartAngle, SweepAngle, options.InnerRadius);
        var radius = Math.Max(0, Math.Min(area.Width / Math.Max(right - left, MinExtent), area.Height / Math.Max(bottom - top, MinExtent)));
        var thickness = ResolveThickness(radius, options);
        if ((CapStyle == ChartCapStyle.Round) && !IsFullCircle)
        {
            radius = Math.Max(0, radius - (thickness / 2));
            thickness = ResolveThickness(radius, options);
        }

        OuterRadius = radius;
        Thickness = thickness;
        CenterX = area.CenterX - (radius * (left + right) / 2);
        CenterY = area.CenterY - (radius * (top + bottom) / 2);
    }

    private double ResolveThickness(double radius, RadialBarLayoutOptions options)
    {
        if (radius <= 0)
        {
            return 0;
        }

        if (options.RingThickness is { } thickness)
        {
            return thickness;
        }

        var limit = radius * MaxThicknessRatio;
        var count = rows.Count;
        if (count == 0)
        {
            return limit;
        }

        var share = ((radius * (1 - options.InnerRadius)) - (Spacing * (count - 1))) / count;
        return Math.Max(MinThickness, Math.Min(limit, share));
    }

    private void PrepareCenters(RadialBarLayoutOptions options, ChartStyle style, ReadOnlySpan<GaugeEntry> entries, IChartTextMeasurer measurer)
    {
        ringCenters.Clear();
        Center = default;
        defaultKey = DefaultCenterKey;
        var available = GetInnerEdge() - CenterPadding;
        if ((rows.Count == 0) || (available <= 0))
        {
            return;
        }

        var mode = options.CenterValueMode;
        if (options.CenterText is { } text)
        {
            Center = FitCenter(text, options.CenterLabel, Double.NaN, false, available, style, measurer);
        }
        else if (mode == ChartCenterValueMode.Auto)
        {
            var first = rows[0];
            if (first.PercentText is { } percent)
            {
                Center = FitCenter(percent, options.CenterLabel ?? first.Label, first.Percent / 100, true, available, style, measurer);
                defaultKey = options.CenterLabel is null ? first.DataIndex : DefaultCenterKey;
            }
        }
        else if (mode == ChartCenterValueMode.Total)
        {
            var total = 0d;
            foreach (var entry in entries)
            {
                if (Double.IsFinite(entry.Value))
                {
                    total += entry.Value;
                }
            }

            Center = FitCenter(ValueFormatting.Format(total), options.CenterLabel, total, false, available, style, measurer);
        }

        if (!options.Selectable || (mode is not (ChartCenterValueMode.Auto or ChartCenterValueMode.Selected)))
        {
            return;
        }

        foreach (var row in rows)
        {
            ringCenters.Add(row.PercentText is { } percent ? FitCenter(percent, row.Label, row.Percent / 100, true, available, style, measurer) : default);
        }
    }

    private RadialBarCenter FitCenter(string value, string? caption, double number, bool isPercent, double available, ChartStyle style, IChartTextMeasurer measurer)
    {
        var valueSize = RoundSize(Math.Min(MaxCenterFontSize, (available + CenterPadding) * CenterFontRatio));
        for (var attempt = 0; attempt < FitAttempts; attempt++)
        {
            if (valueSize < MinCenterFontSize)
            {
                return default;
            }

            var captionSize = RoundSize(Math.Min(style.LegendFontSize, valueSize * CaptionFontRatio));
            var text = !String.IsNullOrEmpty(caption) && (captionSize >= MinCenterFontSize) ? caption : null;
            var valueWidth = measurer.Measure(value, valueSize, Double.PositiveInfinity);
            var valueHeight = measurer.GetLineHeight(valueSize);
            var captionWidth = text is null ? 0 : measurer.Measure(text, captionSize, Double.PositiveInfinity);
            var captionHeight = text is null ? 0 : measurer.GetLineHeight(captionSize);
            var height = valueHeight + (text is null ? 0 : CaptionGap + captionHeight);
            var width = Math.Max(valueWidth, captionWidth);
            var top = CenterY - (height / 2);
            var extent = Math.Sqrt((width * width / 4) + (height * height / 4));
            if (extent <= available)
            {
                var valueBounds = new ChartRect(CenterX - (valueWidth / 2), top, valueWidth, valueHeight);
                var captionBounds = text is null ? default : new ChartRect(CenterX - (captionWidth / 2), top + valueHeight + CaptionGap, captionWidth, captionHeight);
                return new RadialBarCenter(value, text, valueSize, captionSize, valueBounds, captionBounds, number, isPercent);
            }

            valueSize = RoundSize(valueSize * available / extent);
        }

        return default;
    }
}
