namespace ClamChart.Layout;

using System.Globalization;
using System.Runtime.InteropServices;

internal sealed class BarGaugeLayout
{
    public const double LabelGap = 4;

    public const double NameGap = 8;

    public const double ValueGap = 8;

    public const double NameShare = 0.4;

    public const double TargetOverhang = 3;

    private const string PercentFormat = "P0";

    private const string OfMaximumSeparator = " / ";

    private readonly List<GaugeEntry> order = [];
    private readonly List<BarGaugeRow> rows = [];

    public int Version { get; private set; }

    public ChartRect Bounds { get; private set; }

    public ChartRect Content { get; private set; }

    public ChartOrientation Orientation { get; private set; } = ChartOrientation.Horizontal;

    public ChartBarGaugeMode DisplayMode { get; private set; }

    public ChartBarGaugeLabelPosition LabelPosition { get; private set; }

    public double LabelFontSize { get; private set; }

    public double ValueFontSize { get; private set; }

    public double Pitch { get; private set; }

    public double Spacing { get; private set; }

    public ChartRect LabelTemplate { get; private set; }

    public ChartRect TrackTemplate { get; private set; }

    public ChartRect ValueTemplate { get; private set; }

    public double DesiredHeight { get; private set; }

    public int CellCount { get; private set; }

    public double CellSize { get; private set; }

    public double CellGap { get; private set; }

    public IReadOnlyList<BarGaugeRow> Rows => rows;

    public bool IsHorizontal => Orientation == ChartOrientation.Horizontal;

    public ChartRect GetLabelBounds(double slot) => Offset(LabelTemplate, slot);

    public ChartRect GetTrackBounds(double slot) => Offset(TrackTemplate, slot);

    public ChartRect GetValueBounds(double slot) => Offset(ValueTemplate, slot);

    public double GetTrackLength() => IsHorizontal ? TrackTemplate.Width : TrackTemplate.Height;

    public int FindRow(double x, double y)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var slot = rows[i].Slot;
            var start = IsHorizontal ? Content.Y + (slot * Pitch) : Content.X + (slot * Pitch);
            var along = IsHorizontal ? y : x;
            var across = IsHorizontal ? x : y;
            var size = Pitch - Spacing;
            var low = IsHorizontal ? Content.X : Content.Y;
            var high = IsHorizontal ? Content.Right : Content.Bottom;
            if ((along >= start) && (along < start + size) && (across >= low) && (across < high))
            {
                return i;
            }
        }

        return -1;
    }

    public static double MeasureHeight(ReadOnlySpan<GaugeEntry> entries, ChartStyle style, ChartBarGaugeLabelPosition labelPosition, bool valueVisible, double barThickness, double itemSpacing, int? maxItems, IChartTextMeasurer measurer)
    {
        var count = CountRows(entries.Length, maxItems);
        if (count == 0)
        {
            return 0;
        }

        var labels = (labelPosition != ChartBarGaugeLabelPosition.None) && HasLabels(entries);
        var labelHeight = labels ? measurer.GetLineHeight(style.LegendFontSize) : 0;
        var valueHeight = valueVisible ? measurer.GetLineHeight(style.LegendFontSize) : 0;
        return GetStackHeight(count, GetRowHeight(labelPosition, labelHeight, valueHeight, barThickness), itemSpacing);
    }

    public void Update(double width, double height, Thickness padding, ChartStyle style, CultureInfo culture, BarGaugeLayoutOptions options, ReadOnlySpan<GaugeEntry> entries, IChartTextMeasurer measurer)
    {
        Version++;
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        Content = Bounds;
        Orientation = options.Orientation;
        DisplayMode = options.DisplayMode;
        LabelPosition = options.LabelPosition;
        LabelFontSize = style.LegendFontSize;
        ValueFontSize = style.LegendFontSize;
        CellGap = style.SegmentGap;
        Spacing = options.ItemSpacing;
        BuildRows(options, culture, entries);

        var labels = (LabelPosition != ChartBarGaugeLabelPosition.None) && HasLabels(entries);
        var labelHeight = labels ? measurer.GetLineHeight(LabelFontSize) : 0;
        var valueHeight = options.ValueVisible ? measurer.GetLineHeight(ValueFontSize) : 0;
        var valueWidth = options.ValueVisible ? MeasureValues(measurer) : 0;
        if (IsHorizontal)
        {
            PlaceRows(options, labels, labelHeight, valueWidth, valueHeight, padding, measurer);
        }
        else
        {
            PlaceColumns(options, labelHeight, valueHeight);
        }

        PlaceCells(options);
    }

    private static int CountRows(int count, int? maxItems) => maxItems is { } limit ? Math.Min(Math.Max(0, limit), count) : count;

    private static double GetRowHeight(ChartBarGaugeLabelPosition labelPosition, double labelHeight, double valueHeight, double barThickness)
    {
        var barLine = Math.Max(barThickness, valueHeight);
        if (labelHeight <= 0)
        {
            return barLine;
        }

        return labelPosition == ChartBarGaugeLabelPosition.Left ? Math.Max(labelHeight, barLine) : labelHeight + LabelGap + barLine;
    }

    private static double GetStackHeight(int count, double rowHeight, double itemSpacing) =>
        count > 0 ? (count * rowHeight) + ((count - 1) * itemSpacing) : 0;

    private static bool HasLabels(ReadOnlySpan<GaugeEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (!String.IsNullOrEmpty(entry.Label))
            {
                return true;
            }
        }

        return false;
    }

    private static ChartRect Shift(ChartRect bounds, double x, double y) => bounds with { X = bounds.X + x, Y = bounds.Y + y };

    private ChartRect Offset(ChartRect bounds, double slot) =>
        IsHorizontal ? Shift(bounds, 0, slot * Pitch) : Shift(bounds, slot * Pitch, 0);

    private double MeasureValues(IChartTextMeasurer measurer)
    {
        var width = 0d;
        foreach (var row in rows)
        {
            if (row.ValueText is { } text)
            {
                width = Math.Max(width, measurer.Measure(text, ValueFontSize, Double.PositiveInfinity));
            }
        }

        return width;
    }

    private double MeasureLabels(IChartTextMeasurer measurer)
    {
        var width = 0d;
        foreach (var row in rows)
        {
            if (!String.IsNullOrEmpty(row.Label))
            {
                width = Math.Max(width, measurer.Measure(row.Label, LabelFontSize, Double.PositiveInfinity));
            }
        }

        return width;
    }

    private void BuildRows(BarGaugeLayoutOptions options, CultureInfo culture, ReadOnlySpan<GaugeEntry> entries)
    {
        order.Clear();
        foreach (var entry in entries)
        {
            order.Add(entry);
        }

        var sorted = CollectionsMarshal.AsSpan(order);
        if (options.SortMode == ChartSortMode.Descending)
        {
            sorted.Sort(default(LargerFirst));
        }
        else if (options.SortMode == ChartSortMode.Ascending)
        {
            sorted.Sort(default(SmallerFirst));
        }

        var count = CountRows(sorted.Length, options.MaxItems);
        rows.Clear();
        for (var slot = 0; slot < count; slot++)
        {
            var entry = sorted[slot];
            var maximum = Double.IsFinite(entry.Maximum) ? entry.Maximum : options.Maximum;
            var range = maximum - options.Minimum;
            var valid = Double.IsFinite(entry.Value) && (range > 0);
            var ratio = valid ? (entry.Value - options.Minimum) / range : Double.NaN;
            var target = Double.IsFinite(entry.Target) && (range > 0) ? Math.Clamp((entry.Target - options.Minimum) / range, 0, 1) : Double.NaN;
            var text = options.ValueVisible && valid ? FormatValue(options, culture, entry.Value, maximum, ratio) : null;
            rows.Add(new BarGaugeRow(entry.DataIndex, slot, entry.Label, text, valid ? Math.Clamp(ratio, 0, 1) : 0, valid ? ratio * 100 : Double.NaN, target, entry.Color));
        }
    }

    private static string FormatValue(BarGaugeLayoutOptions options, CultureInfo culture, double value, double maximum, double ratio) => options.ValueMode switch
    {
        ChartBarGaugeValueMode.Percent => ratio.ToString(PercentFormat, culture),
        ChartBarGaugeValueMode.ValueOfMaximum => String.Concat(options.Formatting.Format(value), OfMaximumSeparator, options.Formatting.Format(maximum)),
        _ => options.Formatting.Format(value)
    };

    private void PlaceRows(BarGaugeLayoutOptions options, bool labels, double labelHeight, double valueWidth, double valueHeight, Thickness padding, IChartTextMeasurer measurer)
    {
        var content = Content;
        var barLine = Math.Max(options.BarThickness, valueHeight);
        var valueBlock = valueWidth > 0 ? valueWidth + ValueGap : 0;
        var rowHeight = GetRowHeight(LabelPosition, labelHeight, valueHeight, options.BarThickness);
        double barTop;
        double trackX;
        if (labels && (LabelPosition == ChartBarGaugeLabelPosition.Left))
        {
            var labelWidth = Math.Min(MeasureLabels(measurer), content.Width * NameShare);
            barTop = (rowHeight - barLine) / 2;
            trackX = content.X + labelWidth + NameGap;
            LabelTemplate = new ChartRect(content.X, content.Y, labelWidth, rowHeight);
        }
        else if (labels)
        {
            barTop = labelHeight + LabelGap;
            trackX = content.X;
            LabelTemplate = new ChartRect(content.X, content.Y, content.Width, labelHeight);
        }
        else
        {
            barTop = 0;
            trackX = content.X;
            LabelTemplate = default;
        }

        var trackWidth = Math.Max(0, content.Right - valueBlock - trackX);
        TrackTemplate = new ChartRect(trackX, content.Y + barTop + ((barLine - options.BarThickness) / 2), trackWidth, options.BarThickness);
        ValueTemplate = valueWidth > 0 ? new ChartRect(content.Right - valueWidth, content.Y + barTop, valueWidth, barLine) : default;
        Pitch = rowHeight + options.ItemSpacing;
        DesiredHeight = padding.VerticalThickness + GetStackHeight(rows.Count, rowHeight, options.ItemSpacing);
    }

    private void PlaceColumns(BarGaugeLayoutOptions options, double labelHeight, double valueHeight)
    {
        var content = Content;
        var count = Math.Max(1, rows.Count);
        var columnWidth = Math.Max(0, (content.Width - ((count - 1) * options.ItemSpacing)) / count);
        var top = content.Y + (valueHeight > 0 ? valueHeight + LabelGap : 0);
        var bottom = content.Bottom - (labelHeight > 0 ? labelHeight + LabelGap : 0);
        var barWidth = Math.Min(options.BarThickness, columnWidth);
        TrackTemplate = new ChartRect(content.X + ((columnWidth - barWidth) / 2), top, barWidth, Math.Max(0, bottom - top));
        ValueTemplate = valueHeight > 0 ? new ChartRect(content.X, content.Y, columnWidth, valueHeight) : default;
        LabelTemplate = labelHeight > 0 ? new ChartRect(content.X, bottom + LabelGap, columnWidth, labelHeight) : default;
        Pitch = columnWidth + options.ItemSpacing;
        DesiredHeight = Bounds.Height;
    }

    private void PlaceCells(BarGaugeLayoutOptions options)
    {
        var length = GetTrackLength();
        if ((DisplayMode != ChartBarGaugeMode.Segmented) || !(length > 0) || !(options.SegmentWidth > 0))
        {
            CellCount = 0;
            CellSize = 0;
            return;
        }

        CellCount = Math.Max(1, (int)Math.Floor(length / options.SegmentWidth));
        CellSize = Math.Max(0, (length - (CellGap * (CellCount - 1))) / CellCount);
    }

    private readonly struct LargerFirst : IComparer<GaugeEntry>
    {
        public int Compare(GaugeEntry x, GaugeEntry y)
        {
            var nan = Double.IsNaN(x.Value).CompareTo(Double.IsNaN(y.Value));
            if (nan != 0)
            {
                return nan;
            }

            var value = y.Value.CompareTo(x.Value);
            return value != 0 ? value : x.DataIndex.CompareTo(y.DataIndex);
        }
    }

    private readonly struct SmallerFirst : IComparer<GaugeEntry>
    {
        public int Compare(GaugeEntry x, GaugeEntry y)
        {
            var nan = Double.IsNaN(x.Value).CompareTo(Double.IsNaN(y.Value));
            if (nan != 0)
            {
                return nan;
            }

            var value = x.Value.CompareTo(y.Value);
            return value != 0 ? value : x.DataIndex.CompareTo(y.DataIndex);
        }
    }
}
