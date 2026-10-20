namespace ClamChart.Layout;

using System.Globalization;

internal sealed class LegendLayout
{
    public const int MaxRows = 2;

    public const double ItemGap = 12;

    public const double MarkerGap = 4;

    public const double RowGap = 4;

    public const double ValueGap = 6;

    public const double MaxSideWidth = 180;

    private const double LineMarkerRatio = 1.5;

    private readonly List<LegendItemLayout> items = [];

    public IReadOnlyList<LegendItemLayout> Items => items;

    public int OverflowCount { get; private set; }

    public string OverflowText { get; private set; } = String.Empty;

    public ChartRect OverflowBounds { get; private set; }

    public ChartRect Bounds { get; private set; }

    public bool IsVisible { get; private set; }

    public static double GetMarkerWidth(LegendMarkerKind marker, double markerSize) =>
        marker == LegendMarkerKind.Line ? markerSize * LineMarkerRatio : markerSize;

    public void Clear()
    {
        items.Clear();
        OverflowCount = 0;
        OverflowText = String.Empty;
        OverflowBounds = default;
        Bounds = default;
        IsVisible = false;
    }

    public double MeasureRows(IReadOnlyList<LegendEntry> entries, double availableWidth, IChartTextMeasurer measurer, double fontSize, double markerSize, int maxRows = MaxRows)
    {
        Clear();
        var lineHeight = measurer.GetLineHeight(fontSize);
        var rowHeight = Math.Max(lineHeight, markerSize);
        var row = 0;
        var x = 0d;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var markerWidth = GetMarkerWidth(entry.Marker, markerSize);
            var valueWidth = MeasureValue(entry, measurer, fontSize);
            var valueSpace = entry.Value is null ? 0 : ValueGap + valueWidth;
            var textMaxWidth = Math.Max(0, availableWidth - markerWidth - MarkerGap - valueSpace);
            var textWidth = measurer.Measure(entry.Text, fontSize, textMaxWidth);
            var width = markerWidth + MarkerGap + textWidth + valueSpace;
            var start = x > 0 ? x + ItemGap : 0;
            if ((x > 0) && (start + width > availableWidth))
            {
                row++;
                start = 0;
            }

            if (row >= maxRows)
            {
                OverflowCount = entries.Count - i;
                break;
            }

            items.Add(CreateItem(entry.SeriesIndex, start, row * (rowHeight + RowGap), rowHeight, lineHeight, markerWidth, markerSize, textWidth, textMaxWidth, valueWidth, width));
            x = start + width;
        }

        if (OverflowCount > 0)
        {
            PlaceOverflowInRow(availableWidth, measurer, fontSize, rowHeight, lineHeight);
        }

        var rows = GetRowCount(rowHeight);
        var height = rows > 0 ? (rows * rowHeight) + ((rows - 1) * RowGap) : 0;
        Bounds = new ChartRect(0, 0, availableWidth, height);
        IsVisible = rows > 0;
        return height;
    }

    public double MeasureColumn(IReadOnlyList<LegendEntry> entries, double availableHeight, IChartTextMeasurer measurer, double fontSize, double markerSize)
    {
        Clear();
        var lineHeight = measurer.GetLineHeight(fontSize);
        var rowHeight = Math.Max(lineHeight, markerSize);
        var width = 0d;
        var valueColumn = 0d;
        var hasValues = false;
        for (var i = 0; i < entries.Count; i++)
        {
            var markerWidth = GetMarkerWidth(entries[i].Marker, markerSize);
            width = Math.Max(width, markerWidth + MarkerGap + measurer.Measure(entries[i].Text, fontSize, Double.PositiveInfinity));
            hasValues |= entries[i].Value is not null;
            valueColumn = Math.Max(valueColumn, MeasureValue(entries[i], measurer, fontSize));
        }

        var valueSpace = hasValues ? ValueGap + valueColumn : 0;
        width = Math.Min(width + valueSpace, MaxSideWidth);
        var capacity = (int)Math.Floor((availableHeight + RowGap) / (rowHeight + RowGap));
        var shown = entries.Count <= capacity ? entries.Count : Math.Max(0, capacity - 1);
        for (var i = 0; i < shown; i++)
        {
            var entry = entries[i];
            var markerWidth = GetMarkerWidth(entry.Marker, markerSize);
            var textMaxWidth = Math.Max(0, width - markerWidth - MarkerGap - valueSpace);
            var textWidth = measurer.Measure(entry.Text, fontSize, textMaxWidth);
            var itemWidth = hasValues ? width : markerWidth + MarkerGap + textWidth;
            items.Add(CreateItem(entry.SeriesIndex, 0, i * (rowHeight + RowGap), rowHeight, lineHeight, markerWidth, markerSize, textWidth, textMaxWidth, MeasureValue(entry, measurer, fontSize), itemWidth));
        }

        var rows = shown;
        if (shown < entries.Count)
        {
            OverflowCount = entries.Count - shown;
            OverflowText = FormatOverflow(OverflowCount);
            OverflowBounds = new ChartRect(0, (shown * (rowHeight + RowGap)) + ((rowHeight - lineHeight) / 2), Math.Min(width, measurer.Measure(OverflowText, fontSize, width)), lineHeight);
            rows++;
        }

        Bounds = new ChartRect(0, 0, width, rows > 0 ? (rows * rowHeight) + ((rows - 1) * RowGap) : 0);
        IsVisible = rows > 0;
        return width;
    }

    public void Offset(double x, double y)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            items[i] = item with { Bounds = Move(item.Bounds, x, y), Marker = Move(item.Marker, x, y), Text = Move(item.Text, x, y), Value = item.Value.IsEmpty ? item.Value : Move(item.Value, x, y) };
        }

        OverflowBounds = OverflowCount > 0 ? Move(OverflowBounds, x, y) : default;
        Bounds = Move(Bounds, x, y);
    }

    private static ChartRect Move(ChartRect rect, double x, double y) => rect with { X = rect.X + x, Y = rect.Y + y };

    private static string FormatOverflow(int count) => "+" + count.ToString(CultureInfo.InvariantCulture);

    private static double MeasureValue(LegendEntry entry, IChartTextMeasurer measurer, double fontSize) =>
        entry.Value is null ? 0 : measurer.Measure(entry.Value, fontSize, Double.PositiveInfinity);

    private static LegendItemLayout CreateItem(int seriesIndex, double x, double y, double rowHeight, double lineHeight, double markerWidth, double markerSize, double textWidth, double textMaxWidth, double valueWidth, double width) =>
        new(
            seriesIndex,
            new ChartRect(x, y, width, rowHeight),
            new ChartRect(x, y + ((rowHeight - markerSize) / 2), markerWidth, markerSize),
            new ChartRect(x + markerWidth + MarkerGap, y + ((rowHeight - lineHeight) / 2), textWidth, lineHeight),
            textMaxWidth,
            valueWidth > 0 ? new ChartRect(x + width - valueWidth, y + ((rowHeight - lineHeight) / 2), valueWidth, lineHeight) : default);

    private void PlaceOverflowInRow(double availableWidth, IChartTextMeasurer measurer, double fontSize, double rowHeight, double lineHeight)
    {
        while (true)
        {
            OverflowText = FormatOverflow(OverflowCount);
            var width = measurer.Measure(OverflowText, fontSize, Double.PositiveInfinity);
            var start = items.Count > 0 ? items[^1].Bounds.Right + ItemGap : 0;
            if ((items.Count == 0) || (start + width <= availableWidth))
            {
                var y = items.Count > 0 ? items[^1].Bounds.Y : 0;
                OverflowBounds = new ChartRect(start, y + ((rowHeight - lineHeight) / 2), width, lineHeight);
                return;
            }

            items.RemoveAt(items.Count - 1);
            OverflowCount++;
        }
    }

    private int GetRowCount(double rowHeight)
    {
        var bottom = items.Count > 0 ? items[^1].Bounds.Bottom : 0;
        if (OverflowCount > 0)
        {
            bottom = Math.Max(bottom, OverflowBounds.Bottom);
        }

        return bottom > 0 ? (int)Math.Round((bottom + RowGap) / (rowHeight + RowGap)) : 0;
    }
}
