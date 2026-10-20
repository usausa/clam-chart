namespace ClamChart.Layout;

using System.Globalization;
using System.Runtime.InteropServices;

using ClamChart.Data;
using ClamChart.Rendering;

internal sealed class PieLayout
{
    public const double LegendGap = 8;

    public const double LabelRadius = 0.62;

    public const double CenterPadding = 4;

    public const double CaptionGap = 2;

    public const double MaxCenterFontSize = 32;

    public const double MinCenterFontSize = 6;

    public const int TotalCenterKey = -1;

    private const double CenterFontRatio = 0.5;

    private const double CaptionFontRatio = 0.45;

    private const double MinExtent = 1e-6;

    private const int FitAttempts = 3;

    private readonly List<PieCandidate> candidates = [];
    private readonly List<PieSlice> slices = [];
    private readonly List<PieCenterText> sliceCenters = [];
    private readonly List<LegendEntry> legendEntries = [];

    public ChartRect Bounds { get; private set; }

    public ChartRect PieBounds { get; private set; }

    public double CenterX { get; private set; }

    public double CenterY { get; private set; }

    public double OuterRadius { get; private set; }

    public double InnerRadius { get; private set; }

    public double StartAngle { get; private set; }

    public double SweepAngle { get; private set; } = 360;

    public double Gap { get; private set; }

    public double Total { get; private set; }

    public bool HasNegativeValues { get; private set; }

    public bool ShowsLabels { get; private set; }

    public double LabelHeight { get; private set; }

    public ChartLegendValueMode LegendValueMode { get; private set; }

    public IReadOnlyList<PieSlice> Slices => slices;

    public LegendLayout Legend { get; } = new();

    public PieCenterText Center { get; private set; }

    public IReadOnlyList<PieCenterText> SliceCenters => sliceCenters;

    public ChartValueFormatting CenterFormatting { get; private set; } = ChartValueFormatting.Default;

    public static double Normalize(double angle)
    {
        var value = angle % 360;
        return value < 0 ? value + 360 : value;
    }

    public static double GetClockAngle(double dx, double dy) => Math.Atan2(dx, -dy) * 180 / Math.PI;

    public static (double X, double Y) GetDirection(double clockAngle)
    {
        var radians = clockAngle * Math.PI / 180;
        return (Math.Sin(radians), -Math.Cos(radians));
    }

    public static (double Left, double Top, double Right, double Bottom) GetUnitBounds(double startAngle, double sweepAngle, double innerRatio)
    {
        if (sweepAngle >= 360)
        {
            return (-1, -1, 1, 1);
        }

        var left = Double.PositiveInfinity;
        var top = Double.PositiveInfinity;
        var right = Double.NegativeInfinity;
        var bottom = Double.NegativeInfinity;
        var (startX, startY) = GetDirection(startAngle);
        var (endX, endY) = GetDirection(startAngle + sweepAngle);
        Include(ref left, ref top, ref right, ref bottom, startX, startY);
        Include(ref left, ref top, ref right, ref bottom, endX, endY);
        Include(ref left, ref top, ref right, ref bottom, startX * innerRatio, startY * innerRatio);
        Include(ref left, ref top, ref right, ref bottom, endX * innerRatio, endY * innerRatio);
        for (var k = 0; k < 4; k++)
        {
            var angle = k * 90d;
            if (Normalize(angle - startAngle) <= sweepAngle)
            {
                var (x, y) = GetDirection(angle);
                Include(ref left, ref top, ref right, ref bottom, x, y);
            }
        }

        return (left, top, right, bottom);
    }

    public double GetAngle(double fraction) => StartAngle + (SweepAngle * fraction);

    public double GetLabelRadius() => InnerRadius > 0 ? (OuterRadius + InnerRadius) / 2 : OuterRadius * LabelRadius;

    public void Update(double width, double height, Thickness padding, ChartStyle style, CultureInfo culture, PieLayoutOptions options, DataSnapshot data, IChartTextMeasurer measurer)
    {
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        StartAngle = options.StartAngle;
        SweepAngle = options.SweepAngle;
        Gap = options.Gap;
        ShowsLabels = options.LabelsMode == ChartPieLabelsMode.Inside;
        LabelHeight = measurer.GetLineHeight(style.DataLabelFontSize);
        LegendValueMode = options.LegendValueMode;
        BuildSlices(data, options, style, culture, measurer);
        PlaceLegend(options, style, measurer);
        PlacePie(options);
        PrepareCenters(options, style, culture, measurer);
    }

    public PieCenterText GetCenter(int key)
    {
        var slice = FindSlice(key);
        return (slice >= 0) && (slice < sliceCenters.Count) && sliceCenters[slice].IsVisible ? sliceCenters[slice] : Center;
    }

    public int GetCenterKey(int key)
    {
        var slice = FindSlice(key);
        return (slice >= 0) && (slice < sliceCenters.Count) && sliceCenters[slice].IsVisible ? key : TotalCenterKey;
    }

    public int FindSlice(int dataIndex)
    {
        if (dataIndex < 0)
        {
            return -1;
        }

        for (var i = 0; i < slices.Count; i++)
        {
            if (slices[i].DataIndex == dataIndex)
            {
                return i;
            }
        }

        return -1;
    }

    public (double X, double Y) GetExplodeOffset(int sliceIndex, double distance)
    {
        var slice = slices[sliceIndex];
        var (x, y) = GetDirection(GetAngle((slice.Start + slice.End) / 2));
        return (x * distance, y * distance);
    }

    public int FindSliceAt(double x, double y)
    {
        if (slices.Count == 0)
        {
            return -1;
        }

        var dx = x - CenterX;
        var dy = y - CenterY;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        if ((distance < InnerRadius) || (distance > OuterRadius))
        {
            return -1;
        }

        var fraction = Normalize(GetClockAngle(dx, dy) - StartAngle) / SweepAngle;
        if (fraction > 1)
        {
            return -1;
        }

        for (var i = 0; i < slices.Count; i++)
        {
            if ((fraction < slices[i].End) || (i == slices.Count - 1))
            {
                return fraction >= slices[i].Start ? i : -1;
            }
        }

        return -1;
    }

    public ChartHit HitTest(double x, double y, int explodedSlice, double explodeOffset)
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

        var exploded = (explodedSlice >= 0) && (explodedSlice < slices.Count) && (explodeOffset > 0);
        if (exploded)
        {
            var (dx, dy) = GetExplodeOffset(explodedSlice, explodeOffset);
            if (FindSliceAt(x - dx, y - dy) == explodedSlice)
            {
                return new ChartHit(ChartHitKind.Item, -1, explodedSlice);
            }
        }

        var index = FindSliceAt(x, y);
        return (index >= 0) && (!exploded || (index != explodedSlice)) ? new ChartHit(ChartHitKind.Item, -1, index) : ChartHit.None;
    }

    private static void Include(ref double left, ref double top, ref double right, ref double bottom, double x, double y)
    {
        left = Math.Min(left, x);
        top = Math.Min(top, y);
        right = Math.Max(right, x);
        bottom = Math.Max(bottom, y);
    }

    private static int CountKept(ReadOnlySpan<PieCandidate> sorted, double total, PieLayoutOptions options)
    {
        var small = 0;
        if (options.OthersThreshold > 0)
        {
            for (var k = sorted.Length - 1; (k >= 0) && (sorted[k].Value / total < options.OthersThreshold); k--)
            {
                small++;
            }
        }

        var kept = small >= 2 ? sorted.Length - small : sorted.Length;
        var limit = Math.Max(1, options.MaxSlices);
        var count = kept + (kept < sorted.Length ? 1 : 0);
        return count > limit ? limit - 1 : kept;
    }

    private static string? GetInsideText(ChartPieLabelContent content, string? label, string percent, string value) => content switch
    {
        ChartPieLabelContent.Value => value,
        ChartPieLabelContent.Label => String.IsNullOrEmpty(label) ? null : label,
        ChartPieLabelContent.LabelAndPercent => String.IsNullOrEmpty(label) ? percent : label + " " + percent,
        _ => percent
    };

    private static double RoundSize(double size) => Math.Floor(size * 2) / 2;

    private void BuildSlices(DataSnapshot data, PieLayoutOptions options, ChartStyle style, CultureInfo culture, IChartTextMeasurer measurer)
    {
        slices.Clear();
        candidates.Clear();
        HasNegativeValues = false;
        var values = data.Y;
        var total = 0d;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (value < 0)
            {
                HasNegativeValues = true;
            }
            else if (Double.IsFinite(value) && (value > 0))
            {
                candidates.Add(new PieCandidate(value, i, -1));
                total += value;
            }
        }

        Total = Double.IsFinite(total) ? total : 0;
        if ((candidates.Count == 0) || (Total <= 0))
        {
            Total = 0;
            return;
        }

        var sorted = CollectionsMarshal.AsSpan(candidates);
        sorted.Sort(default(LargerFirst));
        var kept = CountKept(sorted, total, options);
        var shown = sorted[..kept];
        shown.Sort(default(DataOrder));
        for (var k = 0; k < shown.Length; k++)
        {
            shown[k] = shown[k] with { Rank = k };
        }

        if (options.SortMode == ChartSortMode.Descending)
        {
            shown.Sort(default(LargerFirst));
        }

        var cumulative = 0d;
        for (var k = 0; k < shown.Length; k++)
        {
            var item = shown[k];
            var start = cumulative / total;
            cumulative += item.Value;
            var end = (k == shown.Length - 1) && (kept == sorted.Length) ? 1 : Math.Min(1, cumulative / total);
            slices.Add(CreateSlice(item.Index, item.Value, start, end, item.Rank, data.GetColor(item.Index), data.GetLabel(item.Index), total, options, style, culture, measurer));
        }

        if (kept < sorted.Length)
        {
            var others = 0d;
            for (var k = kept; k < sorted.Length; k++)
            {
                others += sorted[k].Value;
            }

            slices.Add(CreateSlice(-1, others, Math.Min(1, cumulative / total), 1, -1, null, options.OthersLabel, total, options, style, culture, measurer));
        }
    }

    private PieSlice CreateSlice(int index, double value, double start, double end, int rank, Color? color, string? label, double total, PieLayoutOptions options, ChartStyle style, CultureInfo culture, IChartTextMeasurer measurer)
    {
        var percent = (value / total).ToString("P0", culture);
        var text = ChartTextFormat.FormatValue(value, options.ValueFormatter, options.ValueConverter, options.ValueFormat, culture);
        var inside = ShowsLabels ? GetInsideText(options.LabelContent, label, percent, text) : null;
        var width = inside is null ? 0 : measurer.Measure(inside, style.DataLabelFontSize, Double.PositiveInfinity);
        return new PieSlice(index, value, start, end, rank, color, label, percent, text, inside, width);
    }

    private void PlaceLegend(PieLayoutOptions options, ChartStyle style, IChartTextMeasurer measurer)
    {
        legendEntries.Clear();
        for (var i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            if (String.IsNullOrEmpty(slice.Label))
            {
                continue;
            }

            var value = options.LegendValueMode switch
            {
                ChartLegendValueMode.Value => slice.ValueText,
                ChartLegendValueMode.Percent => slice.PercentText,
                _ => null
            };
            legendEntries.Add(new LegendEntry(i, slice.Label, LegendMarkerKind.Dot, value));
        }

        var area = Bounds;
        PieBounds = area;
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
            PieBounds = new ChartRect(area.X, top ? area.Y + used : area.Y, area.Width, area.Height - used);
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
        PieBounds = new ChartRect(left ? area.X + width + LegendGap : area.X, area.Y, area.Width - width - LegendGap, area.Height);
    }

    private void PlacePie(PieLayoutOptions options)
    {
        var margin = options.ExplodeMargin;
        var area = new ChartRect(PieBounds.X + margin, PieBounds.Y + margin, Math.Max(0, PieBounds.Width - (margin * 2)), Math.Max(0, PieBounds.Height - (margin * 2)));
        var (left, top, right, bottom) = GetUnitBounds(StartAngle, SweepAngle, options.InnerRadius);
        var radius = Math.Max(0, Math.Min(area.Width / Math.Max(right - left, MinExtent), area.Height / Math.Max(bottom - top, MinExtent)));
        OuterRadius = radius;
        InnerRadius = radius * options.InnerRadius;
        CenterX = area.CenterX - (radius * (left + right) / 2);
        CenterY = area.CenterY - (radius * (top + bottom) / 2);
    }

    private void PrepareCenters(PieLayoutOptions options, ChartStyle style, CultureInfo culture, IChartTextMeasurer measurer)
    {
        sliceCenters.Clear();
        Center = default;
        CenterFormatting = new ChartValueFormatting(options.CenterFormatter, options.CenterConverter, options.CenterFormat, culture);
        var mode = options.CenterValueMode;
        if ((mode == ChartCenterValueMode.None) || (InnerRadius <= 0) || (slices.Count == 0))
        {
            return;
        }

        if (mode is ChartCenterValueMode.Auto or ChartCenterValueMode.Total)
        {
            Center = FitCenter(options.CenterValue ?? Total, options.CenterLabel, style, measurer);
        }

        if (!options.Selectable || (mode == ChartCenterValueMode.Total))
        {
            return;
        }

        foreach (var slice in slices)
        {
            var caption = String.IsNullOrEmpty(slice.Label) ? slice.PercentText : slice.Label + " " + slice.PercentText;
            sliceCenters.Add(slice.IsOthers ? default : FitCenter(slice.Value, caption, style, measurer));
        }
    }

    private PieCenterText FitCenter(double number, string? caption, ChartStyle style, IChartTextMeasurer measurer)
    {
        var value = CenterFormatting.Format(number);
        var available = InnerRadius - CenterPadding;
        var valueSize = RoundSize(Math.Min(MaxCenterFontSize, InnerRadius * CenterFontRatio));
        for (var attempt = 0; attempt < FitAttempts; attempt++)
        {
            if ((available <= 0) || (valueSize < MinCenterFontSize))
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
            var top = Math.Clamp(CenterY - (height / 2), PieBounds.Y, Math.Max(PieBounds.Y, PieBounds.Bottom - height));
            var reach = Math.Max(Math.Abs(top - CenterY), Math.Abs(top + height - CenterY));
            var extent = Math.Sqrt((width * width / 4) + (reach * reach));
            if (extent <= available)
            {
                var valueBounds = new ChartRect(CenterX - (valueWidth / 2), top, valueWidth, valueHeight);
                var captionBounds = text is null ? default : new ChartRect(CenterX - (captionWidth / 2), top + valueHeight + CaptionGap, captionWidth, captionHeight);
                return new PieCenterText(value, text, valueSize, captionSize, valueBounds, captionBounds, number);
            }

            valueSize = RoundSize(valueSize * available / extent);
        }

        return default;
    }

    private readonly record struct PieCandidate(double Value, int Index, int Rank);

    private readonly struct LargerFirst : IComparer<PieCandidate>
    {
        public int Compare(PieCandidate x, PieCandidate y)
        {
            var value = y.Value.CompareTo(x.Value);
            return value != 0 ? value : x.Index.CompareTo(y.Index);
        }
    }

    private readonly struct DataOrder : IComparer<PieCandidate>
    {
        public int Compare(PieCandidate x, PieCandidate y) => x.Index.CompareTo(y.Index);
    }
}
