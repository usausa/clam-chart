namespace ClamChart.Layout;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using ClamChart.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class StateTimelineLayout
{
    public const double LegendGap = 8;

    public const double LabelGap = 8;

    private const double LabelWidthRatio = 1d / 3;

    private static readonly string?[] NoCategories = [];

    private readonly List<(int Row, StateTimelineEntry Entry)> sorted = [];
    private readonly List<string?> rowNames = [];
    private readonly Dictionary<string, int> rowIndexes = [with(StringComparer.Ordinal)];
    private readonly List<double> rowMinimums = [];
    private readonly List<double> rowMaximums = [];
    private readonly List<StateTimelineSegment> segments = [];
    private readonly List<int> rowStarts = [];
    private readonly List<StateTimelineState> states = [];
    private readonly Dictionary<(string Text, SKColor Color), int> stateIndexes = [];
    private readonly Dictionary<string, ChartValueMapping> mappings = [with(StringComparer.Ordinal)];
    private readonly List<ChartThreshold> steps = [];
    private readonly List<string> stepLabels = [];
    private readonly List<SKColor> stepColors = [];
    private readonly List<HeatmapLabel> rowLabels = [];
    private readonly List<LegendEntry> legendEntries = [];
    private readonly List<string> legendTexts = [];
    private readonly List<SKColor> legendColors = [];
    private readonly List<LabelExtent> extents = [];
    private readonly List<int> selected = [];
    private readonly List<double> times = [];
    private readonly Color[] continuous = new Color[3];
    private bool hasNumbers;

    public int Version { get; private set; }

    public ChartRect Bounds { get; private set; }

    public ChartRect Plot { get; private set; }

    public AxisLayout Axis { get; } = new();

    public LegendLayout Legend { get; } = new();

    public double RowHeight { get; private set; }

    public double RowPitch { get; private set; }

    public bool HasRowNames { get; private set; }

    public TimeLabelPrecision TimePrecision { get; private set; } = TimeLabelPrecision.Month;

    public ChartValueFormatting Formatting { get; private set; } = ChartValueFormatting.Default;

    public int RowCount => rowNames.Count;

    public IReadOnlyList<string?> RowNames => rowNames;

    public IReadOnlyList<StateTimelineSegment> Segments => segments;

    public IReadOnlyList<StateTimelineState> States => states;

    public IReadOnlyList<HeatmapLabel> RowLabels => rowLabels;

    public IReadOnlyList<string> LegendTexts => legendTexts;

    public IReadOnlyList<SKColor> LegendColors => legendColors;

    public double GetRowTop(int row) => Plot.Y + (row * RowPitch);

    public string GetText(int segment) => states[segments[segment].State].Text;

    public SKColor GetColor(int segment) => states[segments[segment].State].Color;

    public static double GetEnd(StateTimelineSegment segment, double maximum) => Double.IsFinite(segment.End) ? segment.End : maximum;

    public int FindRow(double y)
    {
        for (var row = 0; row < rowNames.Count; row++)
        {
            var top = GetRowTop(row);
            if ((y >= top) && (y < top + RowHeight))
            {
                return row;
            }
        }

        return -1;
    }

    public int FindNearestRow(double y)
    {
        if ((rowNames.Count == 0) || !(RowPitch > 0))
        {
            return -1;
        }

        var row = (int)Math.Floor((y - Plot.Y + ((RowPitch - RowHeight) / 2)) / RowPitch);
        return Math.Clamp(row, 0, rowNames.Count - 1);
    }

    public int FindSegment(int row, double time, double maximum)
    {
        if ((row < 0) || (row >= rowNames.Count) || Double.IsNaN(time))
        {
            return -1;
        }

        for (var i = rowStarts[row]; i < rowStarts[row + 1]; i++)
        {
            var segment = segments[i];
            if ((time >= segment.Start) && (time < GetEnd(segment, maximum)))
            {
                return i;
            }
        }

        return -1;
    }

    public int GetLastSegment(int row) => (row >= 0) && (row < rowNames.Count) && (rowStarts[row + 1] > rowStarts[row]) ? rowStarts[row + 1] - 1 : -1;

    public void Update(double width, double height, Thickness padding, ChartStyle style, CultureInfo culture, StateTimelineLayoutOptions options, ReadOnlySpan<StateTimelineEntry> entries, IChartTextMeasurer measurer)
    {
        Version++;
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        Formatting = options.Formatting;
        PrepareColors(options, style);
        BuildRows(entries);
        BuildSegments(options, style);
        BuildLegend();
        var area = PlaceLegend(options, style, measurer);
        PlaceRows(options, style, culture, measurer, area);
    }

    public static string FormatDuration(double ticks)
    {
        var milliseconds = ticks / TimeSpan.TicksPerMillisecond;
        if (!(milliseconds >= 0))
        {
            return String.Empty;
        }

        var seconds = Math.Floor(milliseconds / 1000);
        var minutes = Math.Floor(seconds / 60);
        seconds %= 60;
        var hours = Math.Floor(minutes / 60);
        minutes %= 60;
        var days = Math.Floor(hours / 24);
        hours %= 24;
        var years = Math.Floor(days / 365);
        if (years > 0)
        {
            days %= 365;
        }

        var months = Math.Floor(days / 30);
        if (months > 0)
        {
            days %= 30;
        }

        var weeks = Math.Floor(days / 7);
        if (weeks > 0)
        {
            days %= 7;
        }

        var rest = Math.Round((milliseconds % 1000) * 1000) / 1000;
        var builder = new StringBuilder();
        if (years > 0)
        {
            AppendPart(builder, years, "y");
            AppendPart(builder, months, "mo");
            AppendPart(builder, weeks, "w");
            AppendPart(builder, days, "d");
        }
        else if (months > 0)
        {
            AppendPart(builder, months, "mo");
            AppendPart(builder, weeks, "w");
            AppendPart(builder, days, "d");
        }
        else if (weeks > 0)
        {
            AppendPart(builder, weeks, "w");
            AppendPart(builder, days, "d");
        }
        else if (days > 0)
        {
            AppendPart(builder, days, "d");
            AppendPart(builder, hours, "h");
        }
        else if (hours > 0)
        {
            AppendPart(builder, hours, "h");
            AppendPart(builder, minutes, "m");
        }
        else if (minutes > 0)
        {
            AppendPart(builder, minutes, "m");
            AppendPart(builder, seconds, "s");
        }
        else if (seconds > 0)
        {
            AppendPart(builder, seconds, "s");
            AppendPart(builder, rest, "ms");
        }
        else
        {
            AppendPart(builder, rest, "ms");
        }

        return builder.Length > 0 ? builder.ToString(0, builder.Length - 1) : "0";
    }

    private static void AppendPart(StringBuilder builder, double value, string unit)
    {
        if (value > 0)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append(unit).Append(' ');
        }
    }

    private static string? ToKey(object? value) => value switch
    {
        null => null,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static Color? GetLevelColor(ChartThresholdLevel level, ChartStyle style) => level switch
    {
        ChartThresholdLevel.Good => style.GoodColor,
        ChartThresholdLevel.Warning => style.WarningColor,
        ChartThresholdLevel.Serious => style.SeriousColor,
        ChartThresholdLevel.Critical => style.CriticalColor,
        _ => null
    };

    private void PrepareColors(StateTimelineLayoutOptions options, ChartStyle style)
    {
        mappings.Clear();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < options.Mappings.Count; i++)
        {
            var mapping = options.Mappings[i];
            if (ToKey(mapping.Value) is { } key)
            {
                mappings.TryAdd(key, mapping);
            }
        }

        steps.Clear();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < options.Thresholds.Count; i++)
        {
            var threshold = options.Thresholds[i];
            var j = steps.Count;
            while ((j > 0) && (steps[j - 1].Value > threshold.Value))
            {
                j--;
            }

            steps.Insert(j, threshold);
        }

        stepLabels.Clear();
        stepColors.Clear();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            stepColors.Add((ThresholdResolver.GetColor(step, style) ?? style.OtherColor).ToSKColor());
            stepLabels.Add(step.Label ?? (Double.IsNegativeInfinity(step.Value) && (i < steps.Count - 1)
                ? "< " + options.Formatting.Format(steps[i + 1].Value)
                : options.Formatting.Format(step.Value) + "+"));
        }

        continuous[0] = style.GoodColor;
        continuous[1] = style.WarningColor;
        continuous[2] = style.CriticalColor;
    }

    private int GetRowIndex(string? name)
    {
        var key = name ?? String.Empty;
        if (!rowIndexes.TryGetValue(key, out var index))
        {
            index = rowNames.Count;
            rowIndexes.Add(key, index);
            rowNames.Add(name);
            rowMinimums.Add(Double.PositiveInfinity);
            rowMaximums.Add(Double.NegativeInfinity);
        }

        return index;
    }

    private void BuildRows(ReadOnlySpan<StateTimelineEntry> entries)
    {
        sorted.Clear();
        rowNames.Clear();
        rowIndexes.Clear();
        rowMinimums.Clear();
        rowMaximums.Clear();
        HasRowNames = false;
        hasNumbers = false;
        foreach (var entry in entries)
        {
            if (!Double.IsFinite(entry.Start))
            {
                continue;
            }

            var row = GetRowIndex(entry.Row);
            HasRowNames |= entry.Row is not null;
            sorted.Add((row, entry));
            if ((entry.Text is null) && Double.IsFinite(entry.Number))
            {
                hasNumbers = true;
                rowMinimums[row] = Math.Min(rowMinimums[row], entry.Number);
                rowMaximums[row] = Math.Max(rowMaximums[row], entry.Number);
            }
        }

        CollectionsMarshal.AsSpan(sorted).Sort(static (x, y) =>
        {
            var row = x.Row.CompareTo(y.Row);
            if (row != 0)
            {
                return row;
            }

            var start = x.Entry.Start.CompareTo(y.Entry.Start);
            return start != 0 ? start : x.Entry.DataIndex.CompareTo(y.Entry.DataIndex);
        });
    }

    private void BuildSegments(StateTimelineLayoutOptions options, ChartStyle style)
    {
        segments.Clear();
        rowStarts.Clear();
        states.Clear();
        stateIndexes.Clear();
        var count = sorted.Count;
        var i = 0;
        for (var row = 0; row < rowNames.Count; row++)
        {
            rowStarts.Add(segments.Count);
            var first = segments.Count;
            var lastKey = default(StateKey);
            while ((i < count) && (sorted[i].Row == row))
            {
                var entry = sorted[i].Entry;
                var next = (i + 1 < count) && (sorted[i + 1].Row == row) ? sorted[i + 1].Entry.Start : Double.PositiveInfinity;
                i++;
                var end = Double.IsFinite(entry.End) && (entry.End > entry.Start) ? Math.Min(entry.End, next) : next;
                if (!(end > entry.Start) || !entry.HasState)
                {
                    continue;
                }

                var (state, key) = ResolveState(entry, row, options, style);
                if (options.MergeValues && (segments.Count > first) && (key == lastKey) && segments[^1].End.Equals(entry.Start))
                {
                    segments[^1] = segments[^1] with { End = end };
                    continue;
                }

                segments.Add(new StateTimelineSegment(row, entry.Start, end, state, entry.DataIndex, entry.Text is null ? entry.Number : Double.NaN));
                lastKey = key;
            }
        }

        rowStarts.Add(segments.Count);
    }

    private (int State, StateKey Key) ResolveState(StateTimelineEntry entry, int row, StateTimelineLayoutOptions options, ChartStyle style)
    {
        var number = entry.Text is null;
        if (number && (steps.Count > 0) && options.MergeValues)
        {
            var step = FindStep(entry.Number);
            return (AddState(stepLabels[step], stepColors[step], true), new StateKey(true, null, step));
        }

        var key = new StateKey(false, entry.Text, number ? entry.Number : Double.NaN);
        var raw = entry.Text ?? options.Formatting.Format(entry.Number);
        if ((mappings.Count > 0) && mappings.TryGetValue(entry.Text ?? entry.Number.ToString(CultureInfo.InvariantCulture), out var mapping))
        {
            var color = (mapping.Color ?? GetLevelColor(mapping.Level, style))?.ToSKColor() ?? GetDefaultColor(entry, row);
            return (AddState(mapping.Text ?? raw, color, true), key);
        }

        return (AddState(raw, GetDefaultColor(entry, row), !number || (steps.Count > 0)), key);
    }

    private SKColor GetDefaultColor(StateTimelineEntry entry, int row)
    {
        if (entry.Text is not null)
        {
            return steps.Count > 0 ? stepColors[0] : continuous[0].ToSKColor();
        }

        if (steps.Count > 0)
        {
            return stepColors[FindStep(entry.Number)];
        }

        var range = rowMaximums[row] - rowMinimums[row];
        var percent = range > 0 ? (entry.Number - rowMinimums[row]) / range : 0;
        return HeatmapPalette.GetColor(continuous, percent);
    }

    private int FindStep(double value)
    {
        var found = 0;
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Value <= value)
            {
                found = i;
            }
        }

        return found;
    }

    private int AddState(string text, SKColor color, bool discrete)
    {
        if (!stateIndexes.TryGetValue((text, color), out var index))
        {
            index = states.Count;
            stateIndexes.Add((text, color), index);
            states.Add(new StateTimelineState(text, color, discrete));
        }

        return index;
    }

    private void BuildLegend()
    {
        legendEntries.Clear();
        legendTexts.Clear();
        legendColors.Clear();
        if ((steps.Count > 0) && hasNumbers)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                legendEntries.Add(new LegendEntry(legendEntries.Count, stepLabels[i], LegendMarkerKind.Dot));
                legendTexts.Add(stepLabels[i]);
                legendColors.Add(stepColors[i]);
            }

            return;
        }

        foreach (var state in states)
        {
            if (state.Discrete && (state.Text.Length > 0))
            {
                legendEntries.Add(new LegendEntry(legendEntries.Count, state.Text, LegendMarkerKind.Dot));
                legendTexts.Add(state.Text);
                legendColors.Add(state.Color);
            }
        }
    }

    private ChartRect PlaceLegend(StateTimelineLayoutOptions options, ChartStyle style, IChartTextMeasurer measurer)
    {
        var area = Bounds;
        if (!options.LegendVisible || (legendEntries.Count == 0))
        {
            Legend.Clear();
            return area;
        }

        if (options.LegendPosition is ChartLegendPosition.Top or ChartLegendPosition.Bottom)
        {
            var height = Legend.MeasureRows(legendEntries, area.Width, measurer, style.LegendFontSize, style.LegendMarkerSize);
            var top = options.LegendPosition == ChartLegendPosition.Top;
            Legend.Offset(area.X, top ? area.Y : area.Bottom - height);
            var used = Math.Min(area.Height, height + LegendGap);
            return new ChartRect(area.X, top ? area.Y + used : area.Y, area.Width, area.Height - used);
        }

        var width = Legend.MeasureColumn(legendEntries, area.Height, measurer, style.LegendFontSize, style.LegendMarkerSize);
        if (area.Width - width - LegendGap < width * 2)
        {
            Legend.Clear();
            return area;
        }

        var left = options.LegendPosition == ChartLegendPosition.Left;
        Legend.Offset(left ? area.X : area.Right - width, area.Y + Math.Max(0, (area.Height - Legend.Bounds.Height) / 2));
        return new ChartRect(left ? area.X + width + LegendGap : area.X, area.Y, area.Width - width - LegendGap, area.Height);
    }

    private void PlaceRows(StateTimelineLayoutOptions options, ChartStyle style, CultureInfo culture, IChartTextMeasurer measurer, ChartRect area)
    {
        var fontSize = style.AxisLabelFontSize;
        var lineHeight = measurer.GetLineHeight(fontSize);
        var labelWidth = 0d;
        if (options.LabelsVisible && HasRowNames)
        {
            foreach (var name in rowNames)
            {
                labelWidth = Math.Max(labelWidth, measurer.Measure(name ?? String.Empty, fontSize, Double.PositiveInfinity));
            }

            labelWidth = Math.Min(labelWidth, area.Width * LabelWidthRatio);
        }

        Axis.Configure(new AxisLayoutOptions
        {
            Kind = ChartAxisKind.DateTime,
            LabelsVisible = options.LabelsVisible,
            Range = new AxisRangeOptions { Minimum = options.Minimum, Maximum = options.Maximum },
            FirstDayOfWeek = options.FirstDayOfWeek
        }, AxisSide.Bottom, culture);
        var left = labelWidth > 0 ? area.X + labelWidth + LabelGap : area.X;
        var axisHeight = Axis.ShowsLabels ? lineHeight + CartesianLayout.LabelGap : 0;
        var plot = new ChartRect(left, area.Y, Math.Max(0, area.Right - left), Math.Max(0, area.Height - axisHeight));
        var (minimum, maximum) = GetDataRange();
        Axis.Resolve(minimum, maximum, plot.Width, NoCategories, null, measurer, fontSize, plot.Width);
        Axis.Place(plot, area, lineHeight, CartesianLayout.LabelGap);
        Plot = plot;

        var rows = rowNames.Count;
        RowHeight = rows > 0 ? plot.Height * options.RowHeight / rows : 0;
        RowPitch = rows > 1 ? RowHeight + (plot.Height * (1 - options.RowHeight) / (rows - 1)) : RowHeight;
        PlaceRowLabels(labelWidth, lineHeight, area);
        TimePrecision = GetTimePrecision();
    }

    private (double Minimum, double Maximum) GetDataRange()
    {
        var minimum = Double.NaN;
        var maximum = Double.NaN;
        foreach (var segment in segments)
        {
            var end = Double.IsFinite(segment.End) ? segment.End : segment.Start;
            minimum = Double.IsNaN(minimum) ? segment.Start : Math.Min(minimum, segment.Start);
            maximum = Double.IsNaN(maximum) ? end : Math.Max(maximum, end);
        }

        return (minimum, maximum);
    }

    private void PlaceRowLabels(double labelWidth, double lineHeight, ChartRect area)
    {
        rowLabels.Clear();
        if (!(labelWidth > 0))
        {
            return;
        }

        extents.Clear();
        for (var row = 0; row < rowNames.Count; row++)
        {
            var center = GetRowTop(row) + (RowHeight / 2);
            extents.Add(new LabelExtent(center - (lineHeight / 2), center + (lineHeight / 2)));
        }

        LabelThinning.Select(CollectionsMarshal.AsSpan(extents), 0, Int32.MaxValue, selected);
        foreach (var row in selected)
        {
            var extent = extents[row];
            rowLabels.Add(new HeatmapLabel(rowNames[row] ?? String.Empty, new ChartRect(area.X, extent.Start, labelWidth, lineHeight), labelWidth));
        }
    }

    private TimeLabelPrecision GetTimePrecision()
    {
        times.Clear();
        foreach (var segment in segments)
        {
            times.Add(segment.Start);
            if (Double.IsFinite(segment.End))
            {
                times.Add(segment.End);
            }
        }

        return TimeInterval.GetValuePrecision(CollectionsMarshal.AsSpan(times), TimeLabelPrecision.Month);
    }

    private readonly record struct StateKey(bool Step, string? Text, double Number);
}
