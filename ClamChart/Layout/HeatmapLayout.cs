namespace ClamChart.Layout;

using System.Globalization;
using System.Runtime.InteropServices;

using ClamChart.Data;
using ClamChart.Rendering;

internal sealed class HeatmapLayout
{
    public const double LegendGap = 8;

    public const double LabelGap = 4;

    public const double LabelSpacing = 8;

    public const double ScaleThickness = 9;

    public const double ScaleLabelGap = 2;

    public const int DaysPerWeek = 7;

    public const int MaxWeeks = 1000;

    private const double ScaleLabelSpacing = 36;

    private const double ScaleVerticalSpacing = 20;

    private const double LabelWidthRatio = 1d / 3;

    private readonly Dictionary<string, int> rowIndexes = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, int> columnIndexes = [with(StringComparer.Ordinal)];
    private readonly List<string?> rowNames = [];
    private readonly List<string?> columnNames = [];
    private readonly List<double> columnValues = [];
    private readonly List<HeatmapLabel> rowLabels = [];
    private readonly List<HeatmapLabel> columnLabels = [];
    private readonly List<HeatmapLabel> scaleLabels = [];
    private readonly List<HeatmapLabel> candidates = [];
    private readonly List<LabelExtent> extents = [];
    private readonly List<int> selected = [];
    private readonly List<string> scaleTexts = [];
    private double[] values = [];
    private int[] indices = [];
    private bool[] present = [];
    private int nullRow = -1;
    private int nullColumn = -1;
    private int firstDay;
    private DayOfWeek firstDayOfWeek;
    private ChartXKind columnKind;

    public ChartRect Bounds { get; private set; }

    public ChartRect PlotBounds { get; private set; }

    public ChartRect GridBounds { get; private set; }

    public ChartRect ScaleBounds { get; private set; }

    public ChartHeatmapLayoutMode Mode { get; private set; }

    public int RowCount { get; private set; }

    public int ColumnCount { get; private set; }

    public int CellCount => RowCount * ColumnCount;

    public double CellWidth { get; private set; }

    public double CellHeight { get; private set; }

    public double Gap { get; private set; }

    public double Minimum { get; private set; }

    public double Maximum { get; private set; } = 1;

    public bool HasValues { get; private set; }

    public bool HasDates { get; private set; }

    public bool ShowsScale { get; private set; }

    public bool IsScaleVertical { get; private set; }

    public double LabelHeight { get; private set; }

    public DateTime CalendarStart { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyList<string?> RowNames => rowNames;

    public IReadOnlyList<string?> ColumnNames => columnNames;

    public IReadOnlyList<HeatmapLabel> RowLabels => rowLabels;

    public IReadOnlyList<HeatmapLabel> ColumnLabels => columnLabels;

    public IReadOnlyList<HeatmapLabel> ScaleLabels => scaleLabels;

    public ChartValueFormatting ValueFormatting { get; private set; } = ChartValueFormatting.Default;

    public double GetValue(int cell) => values[cell];

    public int GetDataIndex(int cell) => indices[cell];

    public bool IsPresent(int cell) => present[cell];

    public int GetRow(int cell) => cell / ColumnCount;

    public int GetColumn(int cell) => cell % ColumnCount;

    public ChartRect GetCellBounds(int cell) =>
        new(GridBounds.X + (GetColumn(cell) * CellWidth), GridBounds.Y + (GetRow(cell) * CellHeight), CellWidth, CellHeight);

    public DateTime GetDate(int cell) => CalendarStart.AddDays((GetColumn(cell) * DaysPerWeek) + GetRow(cell));

    public string FormatValue(double value) =>
        ValueFormatting.IsDefault ? CompactNumberFormat.Format(value, ValueFormatting.Culture) : ValueFormatting.Format(value);

    public object? GetCellX(int cell)
    {
        if (Mode == ChartHeatmapLayoutMode.Calendar)
        {
            return GetDate(cell);
        }

        var column = GetColumn(cell);
        return columnKind switch
        {
            ChartXKind.Category => columnNames[column],
            ChartXKind.Time => ChartDateTime.FromTicks(columnValues[column]),
            _ => columnValues[column]
        };
    }

    public string GetCellText(int cell)
    {
        var culture = ValueFormatting.Culture;
        if (Mode == ChartHeatmapLayoutMode.Calendar)
        {
            var date = GetDate(cell);
            return date.ToString("d", culture) + " (" + culture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek) + ")";
        }

        var row = rowNames[GetRow(cell)];
        var column = columnNames[GetColumn(cell)];
        if (String.IsNullOrEmpty(row))
        {
            return column ?? String.Empty;
        }

        return String.IsNullOrEmpty(column) ? row : row + " " + column;
    }

    public int FindCellOfIndex(int dataIndex)
    {
        if (dataIndex < 0)
        {
            return -1;
        }

        var count = CellCount;
        for (var i = 0; i < count; i++)
        {
            if (indices[i] == dataIndex)
            {
                return i;
            }
        }

        return -1;
    }

    public int FindCell(double x, double y) => GridBounds.Contains(x, y) ? FindNearestCell(x, y) : -1;

    public int FindNearestCell(double x, double y)
    {
        if ((CellCount == 0) || !(CellWidth > 0) || !(CellHeight > 0))
        {
            return -1;
        }

        var column = (int)Math.Clamp(Math.Floor((x - GridBounds.X) / CellWidth), 0, ColumnCount - 1);
        var row = (int)Math.Clamp(Math.Floor((y - GridBounds.Y) / CellHeight), 0, RowCount - 1);
        var cell = (row * ColumnCount) + column;
        return present[cell] ? cell : -1;
    }

    public void Update(double width, double height, Thickness padding, ChartStyle style, CultureInfo culture, HeatmapLayoutOptions options, DataSnapshot data, IChartTextMeasurer measurer)
    {
        Version++;
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        Mode = options.LayoutMode;
        Gap = options.CellGap;
        LabelHeight = measurer.GetLineHeight(style.AxisLabelFontSize);
        ValueFormatting = new ChartValueFormatting(options.ValueFormatter, options.ValueConverter, options.ValueFormat, culture);
        if (Mode == ChartHeatmapLayoutMode.Calendar)
        {
            BuildCalendar(data, culture, options.FirstDayOfWeek);
        }
        else
        {
            BuildMatrix(data, culture, options.HasRows);
        }

        ResolveRange(options);
        PlaceScale(options, style, measurer);
        if (Mode == ChartHeatmapLayoutMode.Calendar)
        {
            PlaceCalendar(options, style, culture, measurer);
        }
        else
        {
            PlaceMatrix(options, style, measurer);
        }

        PlaceScaleLabels(style, measurer);
    }

    private static void RemoveDuplicates(List<double> list)
    {
        var write = 0;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var read = 0; read < list.Count; read++)
        {
            if ((write == 0) || !list[read].Equals(list[write - 1]))
            {
                list[write++] = list[read];
            }
        }

        list.RemoveRange(write, list.Count - write);
    }

    private void Allocate(bool filled)
    {
        var size = CellCount;
        if (values.Length < size)
        {
            values = new double[size];
            indices = new int[size];
            present = new bool[size];
        }

        Array.Fill(values, Double.NaN, 0, size);
        Array.Fill(indices, -1, 0, size);
        Array.Fill(present, filled, 0, size);
    }

    private void Clear()
    {
        rowIndexes.Clear();
        columnIndexes.Clear();
        rowNames.Clear();
        columnNames.Clear();
        columnValues.Clear();
        nullRow = -1;
        nullColumn = -1;
        RowCount = 0;
        ColumnCount = 0;
        HasDates = false;
    }

    private void BuildMatrix(DataSnapshot data, CultureInfo culture, bool hasRows)
    {
        Clear();
        columnKind = data.XKind;
        var count = data.Count;
        var x = data.X;
        if (columnKind == ChartXKind.Category)
        {
            for (var i = 0; i < count; i++)
            {
                GetColumnIndex(data.GetCategory(i));
            }
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                if (Double.IsFinite(x[i]))
                {
                    columnValues.Add(x[i]);
                }
            }

            columnValues.Sort();
            RemoveDuplicates(columnValues);
            foreach (var value in columnValues)
            {
                columnNames.Add(FormatColumn(value, culture));
            }
        }

        if (hasRows)
        {
            for (var i = 0; i < count; i++)
            {
                GetRowIndex(data.GetLabel(i));
            }
        }
        else if (columnNames.Count > 0)
        {
            rowNames.Add(null);
        }

        RowCount = rowNames.Count;
        ColumnCount = columnNames.Count;
        Allocate(true);
        if (CellCount == 0)
        {
            return;
        }

        var y = data.Y;
        for (var i = 0; i < count; i++)
        {
            var column = columnKind == ChartXKind.Category ? GetColumnIndex(data.GetCategory(i)) : columnValues.BinarySearch(x[i]);
            if (column < 0)
            {
                continue;
            }

            var cell = ((hasRows ? GetRowIndex(data.GetLabel(i)) : 0) * ColumnCount) + column;
            if (indices[cell] < 0)
            {
                indices[cell] = i;
                values[cell] = y[i];
            }
        }
    }

    private string? FormatColumn(double value, CultureInfo culture) => columnKind switch
    {
        ChartXKind.Number => CompactNumberFormat.Format(value, culture),
        ChartXKind.Time => ChartDateTime.FromTicks(value).ToString("d", culture),
        _ => null
    };

    private int GetRowIndex(string? name) => GetIndex(name, rowIndexes, rowNames, ref nullRow);

    private int GetColumnIndex(string? name) => GetIndex(name, columnIndexes, columnNames, ref nullColumn);

    private static int GetIndex(string? name, Dictionary<string, int> indexes, List<string?> names, ref int nullIndex)
    {
        if (name is null)
        {
            if (nullIndex < 0)
            {
                nullIndex = names.Count;
                names.Add(null);
            }

            return nullIndex;
        }

        if (!indexes.TryGetValue(name, out var index))
        {
            index = names.Count;
            indexes.Add(name, index);
            names.Add(name);
        }

        return index;
    }

    private void BuildCalendar(DataSnapshot data, CultureInfo culture, DayOfWeek firstDayOfWeekValue)
    {
        Clear();
        columnKind = data.XKind;
        firstDayOfWeek = firstDayOfWeekValue;
        HasDates = columnKind == ChartXKind.Time;
        var x = data.X;
        var first = DateTime.MaxValue;
        var last = DateTime.MinValue;
        for (var i = 0; HasDates && (i < data.Count); i++)
        {
            if (Double.IsFinite(x[i]))
            {
                var date = ChartDateTime.FromTicks(x[i]).Date;
                first = date < first ? date : first;
                last = date > last ? date : last;
            }
        }

        if ((first > last) || (last.Ticks < TimeSpan.TicksPerDay * DaysPerWeek * MaxWeeks))
        {
            Allocate(false);
            return;
        }

        var oldest = last.AddDays(-GetWeekOffset(last) - (DaysPerWeek * (MaxWeeks - 1)));
        CalendarStart = first < oldest ? oldest : first.AddDays(-GetWeekOffset(first));
        firstDay = Math.Max(0, (first - CalendarStart).Days);
        var end = (last - CalendarStart).Days;
        RowCount = DaysPerWeek;
        ColumnCount = (end / DaysPerWeek) + 1;
        Allocate(false);
        for (var day = firstDay; day <= end; day++)
        {
            present[GetCalendarCell(day)] = true;
        }

        var y = data.Y;
        for (var i = 0; i < data.Count; i++)
        {
            if (!Double.IsFinite(x[i]))
            {
                continue;
            }

            var day = (ChartDateTime.FromTicks(x[i]).Date - CalendarStart).Days;
            if (day < firstDay)
            {
                continue;
            }

            var cell = GetCalendarCell(day);
            if (indices[cell] < 0)
            {
                indices[cell] = i;
                values[cell] = y[i];
            }
        }

        for (var r = 0; r < DaysPerWeek; r++)
        {
            rowNames.Add(culture.DateTimeFormat.GetAbbreviatedDayName(GetDayOfWeek(r)));
        }

        for (var c = 0; c < ColumnCount; c++)
        {
            columnNames.Add(null);
        }
    }

    private int GetWeekOffset(DateTime date) => ((int)date.DayOfWeek - (int)firstDayOfWeek + DaysPerWeek) % DaysPerWeek;

    private int GetCalendarCell(int day) => ((day % DaysPerWeek) * ColumnCount) + (day / DaysPerWeek);

    private DayOfWeek GetDayOfWeek(int row) => (DayOfWeek)(((int)firstDayOfWeek + row) % DaysPerWeek);

    private void ResolveRange(HeatmapLayoutOptions options)
    {
        var minimum = Double.NaN;
        var maximum = Double.NaN;
        var size = CellCount;
        for (var i = 0; i < size; i++)
        {
            var value = values[i];
            if (present[i] && Double.IsFinite(value))
            {
                minimum = Double.IsNaN(minimum) ? value : Math.Min(minimum, value);
                maximum = Double.IsNaN(maximum) ? value : Math.Max(maximum, value);
            }
        }

        HasValues = !Double.IsNaN(minimum);
        var (fixedMinimum, fixedMaximum) = AxisRangeResolver.GetFixedBounds(new AxisRangeOptions { Minimum = options.Minimum, Maximum = options.Maximum });
        Minimum = fixedMinimum ?? (HasValues ? minimum : 0);
        Maximum = fixedMaximum ?? (HasValues ? maximum : 1);
    }

    private void PlaceScale(HeatmapLayoutOptions options, ChartStyle style, IChartTextMeasurer measurer)
    {
        var area = Bounds;
        PlotBounds = area;
        ScaleBounds = default;
        IsScaleVertical = options.LegendPosition is ChartLegendPosition.Left or ChartLegendPosition.Right;
        ShowsScale = options.LegendVisible && HasValues;
        if (!ShowsScale)
        {
            return;
        }

        if (!IsScaleVertical)
        {
            var height = ScaleThickness + ScaleLabelGap + LabelHeight;
            var top = options.LegendPosition == ChartLegendPosition.Top;
            var used = Math.Min(area.Height, height + LegendGap);
            PlotBounds = new ChartRect(area.X, top ? area.Y + used : area.Y, area.Width, area.Height - used);
            ScaleBounds = new ChartRect(area.X, top ? area.Y : area.Bottom - height, area.Width, ScaleThickness);
            return;
        }

        var size = style.AxisLabelFontSize;
        var width = ScaleThickness + LabelGap + Math.Max(measurer.Measure(FormatValue(Minimum), size, Double.PositiveInfinity), measurer.Measure(FormatValue(Maximum), size, Double.PositiveInfinity));
        if (area.Width - width - LegendGap < width * 2)
        {
            ShowsScale = false;
            return;
        }

        var left = options.LegendPosition == ChartLegendPosition.Left;
        PlotBounds = new ChartRect(left ? area.X + width + LegendGap : area.X, area.Y, area.Width - width - LegendGap, area.Height);
        ScaleBounds = new ChartRect(left ? area.X : area.Right - width, area.Y, ScaleThickness, area.Height);
    }

    private void PlaceMatrix(HeatmapLayoutOptions options, ChartStyle style, IChartTextMeasurer measurer)
    {
        rowLabels.Clear();
        columnLabels.Clear();
        GridBounds = default;
        CellWidth = 0;
        CellHeight = 0;
        if (CellCount == 0)
        {
            return;
        }

        var area = PlotBounds;
        var size = style.AxisLabelFontSize;
        var maxWidth = Bounds.Width * LabelWidthRatio;
        var rowWidth = 0d;
        var named = false;
        if (options.LabelsVisible)
        {
            foreach (var name in rowNames)
            {
                if (name is not null)
                {
                    rowWidth = Math.Max(rowWidth, measurer.Measure(name, size, maxWidth));
                }
            }

            foreach (var name in columnNames)
            {
                named |= name is not null;
            }
        }

        var left = area.X + (rowWidth > 0 ? rowWidth + LabelGap : 0);
        var bottom = area.Bottom - (named ? LabelHeight + LabelGap : 0);
        CellWidth = Math.Max(0, area.Right - left) / ColumnCount;
        CellHeight = Math.Max(0, bottom - area.Y) / RowCount;
        GridBounds = new ChartRect(left, area.Y, CellWidth * ColumnCount, CellHeight * RowCount);
        if (rowWidth > 0)
        {
            candidates.Clear();
            extents.Clear();
            for (var r = 0; r < RowCount; r++)
            {
                if (rowNames[r] is { } name)
                {
                    AddRowLabel(name, r, measurer.Measure(name, size, maxWidth), maxWidth);
                }
            }

            SelectLabels(rowLabels);
        }

        if (named)
        {
            candidates.Clear();
            extents.Clear();
            for (var c = 0; c < ColumnCount; c++)
            {
                if (columnNames[c] is not { } name)
                {
                    continue;
                }

                var width = measurer.Measure(name, size, Double.PositiveInfinity);
                var center = GridBounds.X + (c * CellWidth) + ((CellWidth - Gap) / 2);
                var x = Math.Clamp(center - (width / 2), Bounds.X, Math.Max(Bounds.X, Bounds.Right - width));
                candidates.Add(new HeatmapLabel(name, new ChartRect(x, GridBounds.Bottom + LabelGap, width, LabelHeight), Double.PositiveInfinity));
                extents.Add(new LabelExtent(x, x + width));
            }

            SelectLabels(columnLabels);
        }
    }

    private void AddRowLabel(string name, int row, double width, double maxWidth)
    {
        var center = GridBounds.Y + (row * CellHeight) + ((CellHeight - Gap) / 2);
        var bounds = new ChartRect(GridBounds.X - LabelGap - width, center - (LabelHeight / 2), width, LabelHeight);
        candidates.Add(new HeatmapLabel(name, bounds, maxWidth));
        extents.Add(new LabelExtent(bounds.Y, bounds.Bottom));
    }

    private void SelectLabels(List<HeatmapLabel> destination)
    {
        LabelThinning.Select(CollectionsMarshal.AsSpan(extents), LabelSpacing, Int32.MaxValue, selected);
        foreach (var index in selected)
        {
            destination.Add(candidates[index]);
        }
    }

    private void PlaceCalendar(HeatmapLayoutOptions options, ChartStyle style, CultureInfo culture, IChartTextMeasurer measurer)
    {
        rowLabels.Clear();
        columnLabels.Clear();
        GridBounds = default;
        CellWidth = 0;
        CellHeight = 0;
        if (CellCount == 0)
        {
            return;
        }

        var area = PlotBounds;
        var size = style.AxisLabelFontSize;
        var weekdayWidth = 0d;
        for (var r = 0; options.LabelsVisible && (r < DaysPerWeek); r++)
        {
            if (IsLabeledDay(r))
            {
                weekdayWidth = Math.Max(weekdayWidth, measurer.Measure(rowNames[r]!, size, Double.PositiveInfinity));
            }
        }

        var labelsWidth = weekdayWidth > 0 ? weekdayWidth + LabelGap : 0;
        var monthHeight = options.LabelsVisible ? LabelHeight + LabelGap : 0;
        var pitch = Math.Max(0, Math.Min((area.Width - labelsWidth) / ColumnCount, (area.Height - monthHeight) / DaysPerWeek));
        var blockWidth = labelsWidth + (pitch * ColumnCount);
        var blockHeight = monthHeight + (pitch * DaysPerWeek);
        var x = area.X + Math.Max(0, (area.Width - blockWidth) / 2);
        var y = area.Y + Math.Max(0, (area.Height - blockHeight) / 2);
        if (ShowsScale && IsScaleVertical)
        {
            var reserved = Bounds.Width - area.Width;
            var start = Bounds.X + Math.Max(0, (Bounds.Width - blockWidth - reserved) / 2);
            var left = ScaleBounds.X < area.X;
            x = left ? start + reserved : start;
            ScaleBounds = ScaleBounds with { X = left ? start : x + blockWidth + LegendGap };
        }
        else if (ShowsScale)
        {
            var reserved = Bounds.Height - area.Height;
            var start = Bounds.Y + Math.Max(0, (Bounds.Height - blockHeight - reserved) / 2);
            var top = ScaleBounds.Y < area.Y;
            y = top ? start + reserved : start;
            ScaleBounds = ScaleBounds with { Y = top ? start : y + blockHeight + LegendGap };
        }

        CellWidth = pitch;
        CellHeight = pitch;
        GridBounds = new ChartRect(x + labelsWidth, y + monthHeight, pitch * ColumnCount, pitch * DaysPerWeek);
        if ((weekdayWidth > 0) && (pitch * 2 >= LabelHeight))
        {
            for (var r = 0; r < DaysPerWeek; r++)
            {
                if (IsLabeledDay(r))
                {
                    var name = rowNames[r]!;
                    var center = GridBounds.Y + (r * pitch) + ((pitch - Gap) / 2);
                    var width = measurer.Measure(name, size, Double.PositiveInfinity);
                    rowLabels.Add(new HeatmapLabel(name, new ChartRect(GridBounds.X - LabelGap - width, center - (LabelHeight / 2), width, LabelHeight), Double.PositiveInfinity));
                }
            }
        }

        if (options.LabelsVisible)
        {
            PlaceMonthLabels(culture, size, measurer, y);
        }
    }

    private bool IsLabeledDay(int row) => GetDayOfWeek(row) is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday;

    private void PlaceMonthLabels(CultureInfo culture, double size, IChartTextMeasurer measurer, double top)
    {
        candidates.Clear();
        var previous = -1;
        for (var c = 0; c < ColumnCount; c++)
        {
            var date = CalendarStart.AddDays(Math.Max(c * DaysPerWeek, firstDay));
            var month = (date.Year * 12) + date.Month;
            if (month == previous)
            {
                continue;
            }

            previous = month;
            var text = culture.DateTimeFormat.GetAbbreviatedMonthName(date.Month);
            candidates.Add(new HeatmapLabel(text, new ChartRect(GridBounds.X + (c * CellWidth), top, measurer.Measure(text, size, Double.PositiveInfinity), LabelHeight), Double.PositiveInfinity));
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var label = candidates[i];
            var limit = i < candidates.Count - 1 ? candidates[i + 1].Bounds.X - LabelSpacing : Bounds.Right;
            if (label.Bounds.Right <= limit)
            {
                columnLabels.Add(label);
            }
        }
    }

    private void PlaceScaleLabels(ChartStyle style, IChartTextMeasurer measurer)
    {
        scaleLabels.Clear();
        if (!ShowsScale || (CellCount == 0))
        {
            ShowsScale = false;
            return;
        }

        ScaleBounds = IsScaleVertical
            ? ScaleBounds with { Y = GridBounds.Y, Height = Math.Max(0, GridBounds.Height - Gap) }
            : ScaleBounds with { X = GridBounds.X, Width = Math.Max(0, GridBounds.Width - Gap) };
        var length = IsScaleVertical ? ScaleBounds.Height : ScaleBounds.Width;
        if (!(length > 0))
        {
            return;
        }

        var size = style.AxisLabelFontSize;
        var span = Maximum - Minimum;
        var count = 1;
        if (span > 0)
        {
            var widest = Math.Max(measurer.Measure(FormatValue(Minimum), size, Double.PositiveInfinity), measurer.Measure(FormatValue(Maximum), size, Double.PositiveInfinity));
            count = Math.Max(2, (int)Math.Floor(length / (IsScaleVertical ? LabelHeight + ScaleVerticalSpacing : widest + ScaleLabelSpacing)));
            while ((count > 2) && !FitsScale(count, length, size, measurer))
            {
                count--;
            }

            CreateScaleTexts(count);
        }
        else
        {
            scaleTexts.Clear();
            scaleTexts.Add(FormatValue(Minimum));
        }

        for (var i = 0; i < count; i++)
        {
            var text = scaleTexts[i];
            var width = measurer.Measure(text, size, Double.PositiveInfinity);
            var ratio = count > 1 ? (GetScaleValue(i, count) - Minimum) / span : 0;
            var anchor = i == 0 ? 0 : i == count - 1 ? 1 : 0.5;
            ChartRect bounds;
            if (IsScaleVertical)
            {
                var position = ScaleBounds.Bottom - (ratio * length);
                bounds = new ChartRect(ScaleBounds.Right + LabelGap, position - (LabelHeight * (1 - anchor)), width, LabelHeight);
            }
            else
            {
                var position = ScaleBounds.X + (ratio * length);
                bounds = new ChartRect(position - (width * anchor), ScaleBounds.Bottom + ScaleLabelGap, width, LabelHeight);
            }

            scaleLabels.Add(new HeatmapLabel(text, bounds, Double.PositiveInfinity));
        }
    }

    private double GetScaleValue(int index, int count)
    {
        var value = Minimum + ((Maximum - Minimum) * index / (count - 1));
        return Double.IsInteger(Minimum) && Double.IsInteger(Maximum) ? Math.Floor(value + 0.5) : value;
    }

    private void CreateScaleTexts(int count)
    {
        scaleTexts.Clear();
        for (var i = 0; i < count; i++)
        {
            scaleTexts.Add(FormatValue(GetScaleValue(i, count)));
        }
    }

    private bool FitsScale(int count, double length, double size, IChartTextMeasurer measurer)
    {
        CreateScaleTexts(count);
        var widest = 0d;
        for (var i = 0; i < count; i++)
        {
            widest = Math.Max(widest, measurer.Measure(scaleTexts[i], size, Double.PositiveInfinity));
            for (var j = 0; j < i; j++)
            {
                if (String.Equals(scaleTexts[i], scaleTexts[j], StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return count * (IsScaleVertical ? LabelHeight + ScaleVerticalSpacing : widest + ScaleLabelSpacing) <= length;
    }
}
