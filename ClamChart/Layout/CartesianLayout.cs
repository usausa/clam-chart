namespace ClamChart.Layout;

using System.Globalization;

using ClamChart.Rendering;

internal sealed class CartesianLayout : IDisposable
{
    public const double LabelGap = 4;

    public const double TitleGap = 4;

    public const double LegendGap = 8;

    public const double LabelSpacing = 8;

    public const double VerticalLabelRatio = 1d / 3;

    private static readonly IReadOnlyList<string?> NoCategories = [];

    private readonly List<SeriesGeometry> geometries = [];
    private readonly List<AxisLayout> axes = [];
    private readonly List<LegendEntry> legendEntries = [];
    private readonly List<double> slotWidths = [];
    private readonly List<(int Last, int Minimum, int Maximum)> labelPoints = [];
    private readonly List<DataLabel> dataLabels = [];
    private readonly List<CartesianAnnotation> annotations = [];
    private readonly List<ChartThreshold> thresholds = [];
    private readonly CategoryMerger categories = new();
    private readonly StackCalculator stacks = new();
    private TimeLabelPrecision? timePrecision;

    public ChartRect Bounds { get; private set; }

    public ChartRect PlotBounds { get; private set; }

    public ChartOrientation Orientation { get; private set; }

    public AxisLayout XAxis { get; } = new();

    public AxisLayout YAxis { get; } = new();

    public AxisLayout SecondaryYAxis { get; } = new();

    public bool HasSecondaryAxis { get; private set; }

    public LegendLayout Legend { get; } = new();

    public IReadOnlyList<SeriesGeometry> Geometries => geometries;

    public IReadOnlyList<string?> Categories => XAxis.Options.Kind == ChartAxisKind.Category ? categories.Categories : NoCategories;

    public int BarSlotCount => stacks.BarSlotCount;

    public double BarColumnWidth { get; private set; } = 1;

    public IReadOnlyList<DataLabel> DataLabels => dataLabels;

    public double DataLabelHeight { get; private set; }

    public bool LastMarkerOnly { get; private set; }

    public IReadOnlyList<CartesianAnnotation> Annotations => annotations;

    public IReadOnlyList<ChartThreshold> Thresholds => thresholds;

    public void Dispose()
    {
        foreach (var geometry in geometries)
        {
            geometry.Dispose();
        }

        geometries.Clear();
        stacks.Dispose();
    }

    public AxisLayout GetValueAxis(ChartAxisTarget target) =>
        (target == ChartAxisTarget.Secondary) && HasSecondaryAxis ? SecondaryYAxis : YAxis;

    public string GetTrackingLabel(double x, IReadOnlyList<CartesianSeries> series)
    {
        if ((XAxis.Options.Kind != ChartAxisKind.DateTime) || !XAxis.HasDefaultFormat)
        {
            return XAxis.GetLabelText(x, Categories);
        }

        timePrecision ??= GetTimePrecision(series);
        return XAxis.GetTrackingTimeText(x, timePrecision.Value);
    }

    public void Update(double width, double height, Thickness padding, ChartStyle style, CultureInfo culture, CartesianLayoutOptions options, IReadOnlyList<CartesianSeries> series, IChartTextMeasurer measurer)
    {
        timePrecision = null;
        Orientation = options.Orientation;
        LastMarkerOnly = options.LastMarkerOnly;
        Bounds = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        HasSecondaryAxis = options.SecondaryYAxis is not null;
        ConfigureAxes(options, culture);
        PrepareAnnotations(options);
        PrepareThresholds(options.Thresholds);
        PrepareSeries(series, options.StackMode);
        YAxis.IsPercent = IsPercentAxis(series, ChartAxisTarget.Primary, options.StackMode);
        SecondaryYAxis.IsPercent = HasSecondaryAxis && IsPercentAxis(series, ChartAxisTarget.Secondary, options.StackMode);
        PrepareLegend(series);

        var lineHeight = measurer.GetLineHeight(style.AxisLabelFontSize);
        var titleLineHeight = measurer.GetLineHeight(style.AxisTitleFontSize);
        var legendVisible = options.LegendVisible && (legendEntries.Count > 0);
        var legendRows = legendVisible && (options.LegendPosition is ChartLegendPosition.Top or ChartLegendPosition.Bottom);
        var legendHeight = legendRows ? Legend.MeasureRows(legendEntries, Bounds.Width, measurer, style.LegendFontSize, style.LegendMarkerSize, options.LegendMaxRows) : 0;
        if (!legendRows)
        {
            Legend.Clear();
        }

        var top = Bounds.Y + (legendRows && (options.LegendPosition == ChartLegendPosition.Top) ? legendHeight + LegendGap : 0);
        var bottom = Bounds.Bottom - (legendRows && (options.LegendPosition == ChartLegendPosition.Bottom) ? legendHeight + LegendGap : 0);
        var titleRowTop = top;
        if (ForVerticalAxes(static axis => axis.ShowsTitle))
        {
            top += titleLineHeight + TitleGap;
        }

        var verticalLabels = ForVerticalAxes(static axis => axis.ShowsLabels);
        var labelOffset = Math.Max(LabelGap, verticalLabels ? lineHeight / 2 : 0);
        top += GetHorizontalExtent(AxisSide.Top, lineHeight, titleLineHeight, labelOffset, verticalLabels);
        bottom -= GetHorizontalExtent(AxisSide.Bottom, lineHeight, titleLineHeight, labelOffset, verticalLabels);
        var plotHeight = Math.Max(0, bottom - top);

        ResolveAxes(true, series, plotHeight, measurer, style.AxisLabelFontSize, Bounds.Width * VerticalLabelRatio);
        var left = Bounds.X + GetVerticalExtent(AxisSide.Left);
        var right = Bounds.Right - GetVerticalExtent(AxisSide.Right);
        if (legendVisible && !legendRows)
        {
            var legendWidth = Legend.MeasureColumn(legendEntries, plotHeight, measurer, style.LegendFontSize, style.LegendMarkerSize);
            if (right - left - legendWidth - LegendGap < legendWidth * 2)
            {
                Legend.Clear();
            }
            else if (options.LegendPosition == ChartLegendPosition.Left)
            {
                Legend.Offset(Bounds.X, top);
                left += legendWidth + LegendGap;
            }
            else
            {
                Legend.Offset(Bounds.Right - legendWidth, top);
                right -= legendWidth + LegendGap;
            }
        }

        PlotBounds = new ChartRect(left, top, Math.Max(0, right - left), plotHeight);
        ResolveAxes(false, series, PlotBounds.Width, measurer, style.AxisLabelFontSize, Double.PositiveInfinity);
        UpdateBarColumnWidth(series);
        foreach (var axis in axes)
        {
            axis.Place(PlotBounds, Bounds, lineHeight, labelOffset);
        }

        PlaceBars(series, style);
        PlaceTitles(series, measurer, style, titleRowTop, titleLineHeight, lineHeight, labelOffset);
        if (legendRows)
        {
            Legend.Offset(Bounds.X, options.LegendPosition == ChartLegendPosition.Top ? Bounds.Y : Bounds.Bottom - legendHeight);
        }

        PrepareDataLabels(series, options, style, culture, measurer);
    }

    private static bool IsVertical(AxisSide side) => side is AxisSide.Left or AxisSide.Right;

    private static AxisSide GetOppositeSide(AxisSide side) => side switch
    {
        AxisSide.Left => AxisSide.Right,
        AxisSide.Right => AxisSide.Left,
        AxisSide.Top => AxisSide.Bottom,
        _ => AxisSide.Top
    };

    private static double GetBarWidth(CartesianSeries series, ChartStyle style, double slot) =>
        Math.Min(slot, series.MaxBarWidth ?? style.MaxBarWidth);

    private static double GetBarWidthRatio(IReadOnlyList<CartesianSeries> series)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            if ((item.Kind == ChartSeriesKind.Bar) && item.IsVisible)
            {
                return item.BarWidthRatio;
            }
        }

        return CartesianSeries.DefaultBarWidthRatio;
    }

    private static (int Last, int Minimum, int Maximum) FindLabelPoints(SeriesGeometry geometry)
    {
        var last = -1;
        var minimum = -1;
        var maximum = -1;
        var x = geometry.X;
        var end = geometry.End;
        var values = geometry.Value;
        for (var i = 0; i < geometry.Count; i++)
        {
            if (!Double.IsFinite(x[i]) || !Double.IsFinite(end[i]) || !Double.IsFinite(values[i]))
            {
                continue;
            }

            last = i;
            if ((minimum < 0) || (values[i] < values[minimum]))
            {
                minimum = i;
            }

            if ((maximum < 0) || (values[i] > values[maximum]))
            {
                maximum = i;
            }
        }

        return (last, minimum, maximum);
    }

    internal static string FormatValue(double value, CartesianSeries series, AxisLayoutOptions axis, bool percent, CultureInfo culture)
    {
        if ((series.ValueFormatter is not null) || (series.ValueConverter is not null) || !series.ValueFormat.IsDefault)
        {
            return ChartTextFormat.FormatValue(value, series.ValueFormatter, series.ValueConverter, series.ValueFormat, culture);
        }

        if ((axis.LabelFormatter is not null) || (axis.LabelConverter is not null) || !axis.LabelFormat.IsDefault)
        {
            return ChartTextFormat.FormatValue(value, axis.LabelFormatter, axis.LabelConverter, axis.LabelFormat, culture);
        }

        return percent ? (value / 100).ToString("P0", culture) : CompactNumberFormat.Format(value, culture);
    }

    private void ConfigureAxes(CartesianLayoutOptions options, CultureInfo culture)
    {
        var vertical = options.Orientation == ChartOrientation.Vertical;
        var xOpposite = options.XAxis.Position == ChartAxisPosition.Opposite;
        var yOpposite = options.YAxis.Position == ChartAxisPosition.Opposite;
        var xSide = vertical ? (xOpposite ? AxisSide.Top : AxisSide.Bottom) : (xOpposite ? AxisSide.Right : AxisSide.Left);
        var ySide = vertical ? (yOpposite ? AxisSide.Right : AxisSide.Left) : (yOpposite ? AxisSide.Top : AxisSide.Bottom);
        XAxis.Configure(vertical ? options.XAxis : options.XAxis with { IsInversed = !options.XAxis.IsInversed }, xSide, culture);
        YAxis.Configure(options.YAxis, ySide, culture);
        SecondaryYAxis.Configure(options.SecondaryYAxis ?? options.YAxis with { IsVisible = false }, GetOppositeSide(ySide), culture);
        axes.Clear();
        axes.Add(XAxis);
        axes.Add(YAxis);
        if (HasSecondaryAxis)
        {
            axes.Add(SecondaryYAxis);
        }
    }

    private void PrepareSeries(IReadOnlyList<CartesianSeries> series, ChartStackMode mode)
    {
        while (geometries.Count < series.Count)
        {
            geometries.Add(new SeriesGeometry());
        }

        while (geometries.Count > series.Count)
        {
            geometries[^1].Dispose();
            geometries.RemoveAt(geometries.Count - 1);
        }

        for (var s = 0; s < series.Count; s++)
        {
            var data = series[s].Data;
            var geometry = geometries[s];
            geometry.Reset(data.Count);
            data.X.CopyTo(geometry.X);
        }

        if (XAxis.Options.Kind == ChartAxisKind.Category)
        {
            categories.Merge(series, geometries);
        }

        foreach (var geometry in geometries)
        {
            geometry.UpdateSorted();
        }

        stacks.Calculate(series, geometries, mode);
    }

    private void PrepareLegend(IReadOnlyList<CartesianSeries> series)
    {
        legendEntries.Clear();
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            if (item.LegendVisible && !String.IsNullOrEmpty(item.Title))
            {
                legendEntries.Add(new LegendEntry(s, item.Title, item.Kind == ChartSeriesKind.Line ? LegendMarkerKind.Line : LegendMarkerKind.Dot));
            }
        }
    }

    private bool ForVerticalAxes(Func<AxisLayout, bool> predicate)
    {
        foreach (var axis in axes)
        {
            if (axis.IsVertical && predicate(axis))
            {
                return true;
            }
        }

        return false;
    }

    private double GetHorizontalExtent(AxisSide side, double lineHeight, double titleLineHeight, double labelOffset, bool verticalLabels)
    {
        var extent = 0d;
        var labels = false;
        foreach (var axis in axes)
        {
            if (axis.Side == side)
            {
                labels |= axis.ShowsLabels;
                extent += (axis.ShowsLabels ? labelOffset + lineHeight : 0) + (axis.ShowsTitle ? TitleGap + titleLineHeight : 0);
            }
        }

        return verticalLabels && !labels ? extent + (lineHeight / 2) : extent;
    }

    private double GetVerticalExtent(AxisSide side)
    {
        var extent = 0d;
        foreach (var axis in axes)
        {
            if ((axis.Side == side) && axis.ShowsLabels)
            {
                extent += axis.LabelWidth + LabelGap;
            }
        }

        return extent;
    }

    private void ResolveAxes(bool vertical, IReadOnlyList<CartesianSeries> series, double length, IChartTextMeasurer measurer, double fontSize, double maxLabelWidth)
    {
        if (IsVertical(XAxis.Side) == vertical)
        {
            var (minimum, maximum) = GetPositionRange(series);
            XAxis.Resolve(minimum, maximum, length, Categories, null, measurer, fontSize, maxLabelWidth);
        }

        if (IsVertical(YAxis.Side) == vertical)
        {
            var (minimum, maximum) = GetValueRange(series, ChartAxisTarget.Primary);
            YAxis.Resolve(minimum, maximum, length, NoCategories, null, measurer, fontSize, maxLabelWidth);
            if (HasSecondaryAxis)
            {
                (minimum, maximum) = GetValueRange(series, ChartAxisTarget.Secondary);
                SecondaryYAxis.Resolve(minimum, maximum, length, NoCategories, YAxis, measurer, fontSize, maxLabelWidth);
            }
        }
    }

    private (double Minimum, double Maximum) GetPositionRange(IReadOnlyList<CartesianSeries> series)
    {
        var minimum = Double.NaN;
        var maximum = Double.NaN;
        var bars = false;
        for (var s = 0; s < series.Count; s++)
        {
            if (!series[s].IsVisible)
            {
                continue;
            }

            bars |= series[s].Kind == ChartSeriesKind.Bar;
            foreach (var x in geometries[s].X)
            {
                if (Double.IsFinite(x))
                {
                    minimum = Double.IsNaN(minimum) ? x : Math.Min(minimum, x);
                    maximum = Double.IsNaN(maximum) ? x : Math.Max(maximum, x);
                }
            }
        }

        if (bars && (XAxis.Options.Kind != ChartAxisKind.Category))
        {
            var gap = GetMinimumBarGap(series);
            if (Double.IsFinite(gap))
            {
                return (minimum - (gap / 2), maximum + (gap / 2));
            }
        }

        return (minimum, maximum);
    }

    private (double Minimum, double Maximum) GetValueRange(IReadOnlyList<CartesianSeries> series, ChartAxisTarget target)
    {
        var (low, high) = GetVisiblePositionRange();
        var minimum = Double.NaN;
        var maximum = Double.NaN;
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            if (!item.IsVisible || (GetValueAxis(item.Axis) != GetValueAxis(target)))
            {
                continue;
            }

            var geometry = geometries[s];
            var interpolation = item.Kind is ChartSeriesKind.Bar or ChartSeriesKind.Scatter ? (ChartLineInterpolation?)null : item.Interpolation;
            Include(geometry.X, geometry.End, interpolation, low, high, ref minimum, ref maximum);
            if (geometry.IsStacked)
            {
                Include(geometry.X, geometry.Start, interpolation, low, high, ref minimum, ref maximum);
            }
        }

        IncludeAnnotations(GetValueAxis(target), ref minimum, ref maximum);
        return (minimum, maximum);
    }

    private void IncludeAnnotations(AxisLayout axis, ref double minimum, ref double maximum)
    {
        foreach (var annotation in annotations)
        {
            if ((annotation.Axis != ChartAnnotationAxis.Y) || (GetValueAxis(annotation.Target) != axis))
            {
                continue;
            }

            IncludeValue(annotation.From, ref minimum, ref maximum);
            IncludeValue(annotation.To, ref minimum, ref maximum);
        }
    }

    private static void IncludeValue(double value, ref double minimum, ref double maximum)
    {
        if (Double.IsFinite(value))
        {
            minimum = Double.IsNaN(minimum) ? value : Math.Min(minimum, value);
            maximum = Double.IsNaN(maximum) ? value : Math.Max(maximum, value);
        }
    }

    private void PrepareAnnotations(CartesianLayoutOptions options)
    {
        annotations.Clear();
        var time = XAxis.Options.Kind == ChartAxisKind.DateTime;
        foreach (var annotation in options.Annotations)
        {
            var (from, to) = annotation.Resolve(time && (annotation.Axis == ChartAnnotationAxis.X));
            if (Double.IsFinite(from) && Double.IsFinite(to))
            {
                annotations.Add(annotation with { From = Math.Min(from, to), To = Math.Max(from, to) });
            }
        }
    }

    private void PrepareThresholds(IList<ChartThreshold>? source)
    {
        thresholds.Clear();
        if (source is null)
        {
            return;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < source.Count; i++)
        {
            var item = source[i];
            var index = thresholds.Count;
            while ((index > 0) && (thresholds[index - 1].Value > item.Value))
            {
                index--;
            }

            thresholds.Insert(index, item);
        }
    }

    private bool IsPercentAxis(IReadOnlyList<CartesianSeries> series, ChartAxisTarget target, ChartStackMode mode)
    {
        if (mode != ChartStackMode.Percent)
        {
            return false;
        }

        var percent = false;
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            if (!item.IsVisible || (GetValueAxis(item.Axis) != GetValueAxis(target)))
            {
                continue;
            }

            if (!geometries[s].IsStacked)
            {
                return false;
            }

            percent = true;
        }

        return percent;
    }

    private TimeLabelPrecision GetTimePrecision(IReadOnlyList<CartesianSeries> series)
    {
        var precision = TimeLabelPrecision.Month;
        for (var s = 0; s < series.Count; s++)
        {
            if (series[s].IsVisible)
            {
                precision = TimeInterval.GetValuePrecision(geometries[s].X, precision);
            }
        }

        return precision;
    }

    private (double Low, double High) GetVisiblePositionRange()
    {
        if (XAxis.Options.Kind == ChartAxisKind.Category)
        {
            return (Double.NegativeInfinity, Double.PositiveInfinity);
        }

        var (minimum, maximum) = AxisRangeResolver.GetFixedBounds(XAxis.Options.Range);
        return (minimum ?? Double.NegativeInfinity, maximum ?? Double.PositiveInfinity);
    }

    private static void Include(ReadOnlySpan<double> x, ReadOnlySpan<double> values, ChartLineInterpolation? interpolation, double low, double high, ref double minimum, ref double maximum)
    {
        var previous = -1;
        for (var i = 0; i < values.Length; i++)
        {
            if (!Double.IsFinite(x[i]) || !Double.IsFinite(values[i]))
            {
                previous = -1;
                continue;
            }

            if ((x[i] >= low) && (x[i] <= high))
            {
                Include(values[i], ref minimum, ref maximum);
            }

            if ((interpolation is { } shape) && (previous >= 0))
            {
                IncludeEdge(x[previous], values[previous], x[i], values[i], low, shape, ref minimum, ref maximum);
                IncludeEdge(x[previous], values[previous], x[i], values[i], high, shape, ref minimum, ref maximum);
            }

            previous = i;
        }
    }

    private static void IncludeEdge(double x0, double value0, double x1, double value1, double edge, ChartLineInterpolation interpolation, ref double minimum, ref double maximum)
    {
        if ((edge > Math.Min(x0, x1)) && (edge < Math.Max(x0, x1)))
        {
            Include(interpolation == ChartLineInterpolation.Step ? value0 : value0 + ((value1 - value0) * (edge - x0) / (x1 - x0)), ref minimum, ref maximum);
        }
    }

    private static void Include(double value, ref double minimum, ref double maximum)
    {
        minimum = Double.IsNaN(minimum) ? value : Math.Min(minimum, value);
        maximum = Double.IsNaN(maximum) ? value : Math.Max(maximum, value);
    }

    private double GetMinimumBarGap(IReadOnlyList<CartesianSeries> series)
    {
        var gap = Double.PositiveInfinity;
        for (var s = 0; s < series.Count; s++)
        {
            var geometry = geometries[s];
            if ((series[s].Kind != ChartSeriesKind.Bar) || !series[s].IsVisible || !geometry.IsSorted)
            {
                continue;
            }

            var x = geometry.X;
            for (var i = 1; i < x.Length; i++)
            {
                var difference = x[i] - x[i - 1];
                if (difference > 0)
                {
                    gap = Math.Min(gap, difference);
                }
            }
        }

        return gap;
    }

    private void UpdateBarColumnWidth(IReadOnlyList<CartesianSeries> series)
    {
        if (XAxis.Options.Kind == ChartAxisKind.Category)
        {
            BarColumnWidth = 1;
            return;
        }

        var gap = GetMinimumBarGap(series);
        BarColumnWidth = Double.IsFinite(gap) ? gap : XAxis.Scale.Span;
    }

    private void PlaceBars(IReadOnlyList<CartesianSeries> series, ChartStyle style)
    {
        var count = BarSlotCount;
        var length = IsVertical(XAxis.Side) ? PlotBounds.Height : PlotBounds.Width;
        if ((count == 0) || !(length > 0))
        {
            return;
        }

        var units = XAxis.Scale.Span / length;
        var column = BarColumnWidth / units;
        var group = column * GetBarWidthRatio(series);
        var gap = count > 1 ? Math.Min(style.SegmentGap, group / count / 3) : 0;
        var slot = Math.Max(0, (group - ((count - 1) * gap)) / count);
        slotWidths.Clear();
        for (var i = 0; i < count; i++)
        {
            slotWidths.Add(0);
        }

        for (var s = 0; s < series.Count; s++)
        {
            var index = geometries[s].BarSlot;
            if (index >= 0)
            {
                slotWidths[index] = Math.Max(slotWidths[index], GetBarWidth(series[s], style, slot));
            }
        }

        var total = (count - 1) * gap;
        foreach (var width in slotWidths)
        {
            total += width;
        }

        for (var s = 0; s < series.Count; s++)
        {
            var geometry = geometries[s];
            var index = geometry.BarSlot;
            if (index < 0)
            {
                continue;
            }

            var start = -total / 2;
            for (var i = 0; i < index; i++)
            {
                start += slotWidths[i] + gap;
            }

            var end = start + slotWidths[index];
            var hitStart = index == 0 ? -column / 2 : start - (gap / 2);
            var hitEnd = index == count - 1 ? column / 2 : end + (gap / 2);
            geometry.PlaceBar((start + end) / 2 * units, GetBarWidth(series[s], style, slot) * units, hitStart * units, hitEnd * units);
        }
    }

    private void PrepareDataLabels(IReadOnlyList<CartesianSeries> series, CartesianLayoutOptions options, ChartStyle style, CultureInfo culture, IChartTextMeasurer measurer)
    {
        dataLabels.Clear();
        labelPoints.Clear();
        DataLabelHeight = measurer.GetLineHeight(style.DataLabelFontSize);
        var mode = options.DataLabelsMode;
        if (mode == ChartDataLabelsMode.None)
        {
            return;
        }

        for (var s = 0; s < series.Count; s++)
        {
            labelPoints.Add(series[s].IsVisible ? FindLabelPoints(geometries[s]) : (-1, -1, -1));
        }

        var all = mode == ChartDataLabelsMode.All;
        var last = all || (mode == ChartDataLabelsMode.Last);
        var extremes = all || (mode == ChartDataLabelsMode.MinMax);
        for (var s = 0; s < series.Count; s++)
        {
            if (last && (labelPoints[s].Last >= 0))
            {
                AddDataLabel(series, s, labelPoints[s].Last, options.StackMode, style, culture, measurer);
            }
        }

        for (var s = 0; s < series.Count; s++)
        {
            var (end, _, maximum) = labelPoints[s];
            if (extremes && (maximum >= 0) && !(last && (maximum == end)))
            {
                AddDataLabel(series, s, maximum, options.StackMode, style, culture, measurer);
            }
        }

        for (var s = 0; s < series.Count; s++)
        {
            var (end, minimum, maximum) = labelPoints[s];
            if (extremes && (minimum >= 0) && (minimum != maximum) && !(last && (minimum == end)))
            {
                AddDataLabel(series, s, minimum, options.StackMode, style, culture, measurer);
            }
        }

        if (!all)
        {
            return;
        }

        for (var s = 0; s < series.Count; s++)
        {
            var (end, minimum, maximum) = labelPoints[s];
            if (end < 0)
            {
                continue;
            }

            var geometry = geometries[s];
            for (var i = 0; i < end; i++)
            {
                if ((i != minimum) && (i != maximum) && Double.IsFinite(geometry.X[i]) && Double.IsFinite(geometry.End[i]) && Double.IsFinite(geometry.Value[i]))
                {
                    AddDataLabel(series, s, i, options.StackMode, style, culture, measurer);
                }
            }
        }
    }

    private void AddDataLabel(IReadOnlyList<CartesianSeries> series, int seriesIndex, int index, ChartStackMode mode, ChartStyle style, CultureInfo culture, IChartTextMeasurer measurer)
    {
        var item = series[seriesIndex];
        var geometry = geometries[seriesIndex];
        var percent = geometry.IsStacked && (mode == ChartStackMode.Percent);
        var text = FormatValue(geometry.Value[index], item, GetValueAxis(item.Axis).Options, percent, culture);
        dataLabels.Add(new DataLabel(seriesIndex, index, text, measurer.Measure(text, style.DataLabelFontSize, Double.PositiveInfinity)));
    }

    private void PlaceTitles(IReadOnlyList<CartesianSeries> series, IChartTextMeasurer measurer, ChartStyle style, double titleRowTop, double titleLineHeight, double lineHeight, double labelOffset)
    {
        var both = ForVerticalAxes(static axis => axis.ShowsTitle && (axis.Side == AxisSide.Left)) && ForVerticalAxes(static axis => axis.ShowsTitle && (axis.Side == AxisSide.Right));
        foreach (var axis in axes)
        {
            var markers = GetTitleMarkerCount(series, axis);
            ChartRect row;
            if (axis.IsVertical)
            {
                axis.MeasureTitle(measurer, style.AxisTitleFontSize, both ? (Bounds.Width - LabelGap) / 2 : Bounds.Width, markers, style.LegendMarkerSize);
                row = new ChartRect(Bounds.X, titleRowTop, Bounds.Width, titleLineHeight);
            }
            else
            {
                axis.MeasureTitle(measurer, style.AxisTitleFontSize, PlotBounds.Width, markers, style.LegendMarkerSize);
                var labels = axis.ShowsLabels ? labelOffset + lineHeight : 0;
                var y = axis.Side == AxisSide.Top ? PlotBounds.Y - labels - TitleGap - titleLineHeight : PlotBounds.Bottom + labels + TitleGap;
                row = new ChartRect(PlotBounds.X, y, PlotBounds.Width, titleLineHeight);
            }

            axis.PlaceTitle(row, titleLineHeight, markers, style.LegendMarkerSize);
        }
    }

    private int GetTitleMarkerCount(IReadOnlyList<CartesianSeries> series, AxisLayout axis)
    {
        if (!HasSecondaryAxis || (axis == XAxis))
        {
            return 0;
        }

        var count = 0;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            if (item.IsVisible && (GetValueAxis(item.Axis) == axis))
            {
                count++;
            }
        }

        return count;
    }
}
