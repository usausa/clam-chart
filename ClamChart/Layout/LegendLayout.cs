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

    public const double CellPaddingX = 8;

    public const double CellPaddingY = 2;

    public const double SeparatorThickness = 1;

    public const double MinNameWidth = 55;

    public const double SortArrowGap = 3;

    private const double LineMarkerRatio = 1.5;

    private const double SortArrowRatio = 0.6;

    private readonly List<LegendItemLayout> items = [];
    private readonly List<LegendCell> cells = [];
    private readonly List<double> columnWidths = [];
    private readonly List<ChartRect> headerColumns = [];

    public IReadOnlyList<LegendItemLayout> Items => items;

    public IReadOnlyList<LegendCell> Cells => cells;

    public IReadOnlyList<ChartRect> HeaderColumns => headerColumns;

    public ChartRect SortArrow { get; private set; }

    public bool SortDescending { get; private set; }

    public int OverflowCount { get; private set; }

    public string OverflowText { get; private set; } = String.Empty;

    public ChartRect OverflowBounds { get; private set; }

    public ChartRect Bounds { get; private set; }

    public ChartRect HeaderBounds { get; private set; }

    public bool IsTable { get; private set; }

    public bool IsVisible { get; private set; }

    public static double GetMarkerWidth(LegendMarkerKind marker, double markerSize) =>
        marker == LegendMarkerKind.Line ? markerSize * LineMarkerRatio : markerSize;

    public void Clear()
    {
        items.Clear();
        cells.Clear();
        headerColumns.Clear();
        OverflowCount = 0;
        OverflowText = String.Empty;
        OverflowBounds = default;
        Bounds = default;
        HeaderBounds = default;
        SortArrow = default;
        SortDescending = false;
        IsTable = false;
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

    public double MeasureColumn(IReadOnlyList<LegendEntry> entries, double availableHeight, IChartTextMeasurer measurer, double fontSize, double markerSize, double maxWidth = MaxSideWidth)
    {
        Clear();
        var lineHeight = measurer.GetLineHeight(fontSize);
        var rowHeight = Math.Max(lineHeight, markerSize);
        var width = 0d;
        var valueColumn = 0d;
        var hasValues = false;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var markerWidth = GetMarkerWidth(entry.Marker, markerSize);
            width = Math.Max(width, markerWidth + MarkerGap + measurer.Measure(entry.Text, fontSize, Double.PositiveInfinity));
            hasValues |= entry.Value is not null;
            valueColumn = Math.Max(valueColumn, MeasureValue(entry, measurer, fontSize));
        }

        var valueSpace = hasValues ? ValueGap + valueColumn : 0;
        width = Math.Min(width + valueSpace, maxWidth);
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

    public static double GetSortArrowWidth(double fontSize) => fontSize * SortArrowRatio;

    public double MeasureTable(IReadOnlyList<LegendEntry> entries, IReadOnlyList<string> headers, IReadOnlyList<string> values, double width, double maxHeight, IChartTextMeasurer measurer, double fontSize, double markerSize, int sortColumn = -1, bool sortDescending = false)
    {
        Clear();
        var lineHeight = measurer.GetLineHeight(fontSize);
        var contentHeight = Math.Max(lineHeight, markerSize);
        var rowHeight = contentHeight + (CellPaddingY * 2) + SeparatorThickness;
        var capacity = (int)Math.Floor(maxHeight / rowHeight) - 1;
        var shown = entries.Count <= capacity ? entries.Count : capacity - 1;
        if (shown <= 0)
        {
            return 0;
        }

        var markerColumn = 0d;
        for (var i = 0; i < shown; i++)
        {
            markerColumn = Math.Max(markerColumn, GetMarkerWidth(entries[i].Marker, markerSize));
        }

        markerColumn += CellPaddingX * 2;
        var arrow = GetSortArrowWidth(fontSize);
        var columns = headers.Count - 1;
        var used = markerColumn + MinNameWidth;
        columnWidths.Clear();
        for (var c = 0; c < columns; c++)
        {
            var columnWidth = measurer.Measure(headers[c + 1], fontSize, Double.PositiveInfinity) + (c + 1 == sortColumn ? SortArrowGap + arrow : 0);
            for (var i = 0; i < shown; i++)
            {
                columnWidth = Math.Max(columnWidth, measurer.Measure(values[(i * columns) + c], fontSize, Double.PositiveInfinity));
            }

            columnWidth += CellPaddingX * 2;
            if (used + columnWidth > width)
            {
                break;
            }

            columnWidths.Add(columnWidth);
            used += columnWidth;
        }

        IsTable = true;
        SortDescending = sortDescending;
        var nameX = markerColumn + CellPaddingX;
        var nameMaxWidth = Math.Max(0, width - used + MinNameWidth - (CellPaddingX * 2));
        var textY = CellPaddingY + ((contentHeight - lineHeight) / 2);
        HeaderBounds = new ChartRect(0, 0, width, rowHeight);
        AddHeaderCells(headers, width, markerColumn, nameMaxWidth, textY, lineHeight, rowHeight, measurer, fontSize, sortColumn, arrow);
        for (var i = 0; i < shown; i++)
        {
            var entry = entries[i];
            var y = (i + 1) * rowHeight;
            items.Add(new LegendItemLayout(
                entry.SeriesIndex,
                new ChartRect(0, y, width, rowHeight),
                new ChartRect(CellPaddingX, y + CellPaddingY + ((contentHeight - markerSize) / 2), GetMarkerWidth(entry.Marker, markerSize), markerSize),
                new ChartRect(nameX, y + textY, measurer.Measure(entry.Text, fontSize, nameMaxWidth), lineHeight),
                nameMaxWidth));
            AddValueCells(i, values, i * columns, y, width, textY, lineHeight, measurer, fontSize);
        }

        var rows = shown + 1;
        if (shown < entries.Count)
        {
            OverflowCount = entries.Count - shown;
            OverflowText = FormatOverflow(OverflowCount);
            OverflowBounds = new ChartRect(CellPaddingX, (rows * rowHeight) + textY, measurer.Measure(OverflowText, fontSize, Math.Max(0, width - (CellPaddingX * 2))), lineHeight);
            rows++;
        }

        Bounds = new ChartRect(0, 0, width, rows * rowHeight);
        IsVisible = true;
        return Bounds.Height;
    }

    public void Offset(double x, double y)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            items[i] = item with { Bounds = Move(item.Bounds, x, y), Marker = Move(item.Marker, x, y), Text = Move(item.Text, x, y), Value = item.Value.IsEmpty ? item.Value : Move(item.Value, x, y) };
        }

        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            cells[i] = cell with { Bounds = Move(cell.Bounds, x, y) };
        }

        for (var i = 0; i < headerColumns.Count; i++)
        {
            headerColumns[i] = Move(headerColumns[i], x, y);
        }

        OverflowBounds = OverflowCount > 0 ? Move(OverflowBounds, x, y) : default;
        HeaderBounds = IsTable ? Move(HeaderBounds, x, y) : default;
        SortArrow = SortArrow.IsEmpty ? default : Move(SortArrow, x, y);
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

    private void AddHeaderCells(IReadOnlyList<string> headers, double width, double markerColumn, double nameMaxWidth, double textY, double lineHeight, double rowHeight, IChartTextMeasurer measurer, double fontSize, int sortColumn, double arrow)
    {
        var arrowHeight = arrow / 2;
        var arrowY = textY + ((lineHeight - arrowHeight) / 2);
        var right = width;
        foreach (var columnWidth in columnWidths)
        {
            right -= columnWidth;
        }

        var nameX = markerColumn + CellPaddingX;
        var maxWidth = sortColumn == 0 ? Math.Max(0, nameMaxWidth - SortArrowGap - arrow) : nameMaxWidth;
        var nameWidth = measurer.Measure(headers[0], fontSize, maxWidth);
        cells.Add(new LegendCell(-1, headers[0], new ChartRect(nameX, textY, nameWidth, lineHeight), maxWidth, sortColumn == 0));
        headerColumns.Add(new ChartRect(markerColumn, 0, Math.Max(0, right - markerColumn), rowHeight));
        if (sortColumn == 0)
        {
            SortArrow = new ChartRect(nameX + nameWidth + SortArrowGap, arrowY, arrow, arrowHeight);
        }

        for (var c = 0; c < columnWidths.Count; c++)
        {
            headerColumns.Add(new ChartRect(right, 0, columnWidths[c], rowHeight));
            right += columnWidths[c];
            var end = right - CellPaddingX;
            if (c + 1 == sortColumn)
            {
                SortArrow = new ChartRect(end - arrow, arrowY, arrow, arrowHeight);
                end -= arrow + SortArrowGap;
            }

            var text = headers[c + 1];
            var textWidth = measurer.Measure(text, fontSize, Double.PositiveInfinity);
            cells.Add(new LegendCell(-1, text, new ChartRect(end - textWidth, textY, textWidth, lineHeight), Double.PositiveInfinity, c + 1 == sortColumn));
        }
    }

    private void AddValueCells(int row, IReadOnlyList<string> texts, int offset, double y, double width, double textY, double lineHeight, IChartTextMeasurer measurer, double fontSize)
    {
        var right = width;
        foreach (var columnWidth in columnWidths)
        {
            right -= columnWidth;
        }

        for (var c = 0; c < columnWidths.Count; c++)
        {
            right += columnWidths[c];
            var text = texts[offset + c];
            if (text.Length > 0)
            {
                var textWidth = measurer.Measure(text, fontSize, Double.PositiveInfinity);
                cells.Add(new LegendCell(row, text, new ChartRect(right - CellPaddingX - textWidth, y + textY, textWidth, lineHeight), Double.PositiveInfinity));
            }
        }
    }

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
