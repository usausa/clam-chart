namespace ClamChart.Layout;

using System.Globalization;
using System.Runtime.InteropServices;

using ClamChart.Rendering;

internal sealed class AxisLayout
{
    private const int MaxCachedLabels = 256;

    private const double DisplayTolerance = 0.5;

    private const string Ellipsis = "…";

    private static readonly AxisLayoutOptions DefaultOptions = new();

    private static readonly double SampleTime = new DateTime(2000, 10, 20, 20, 0, 0).Ticks;

    private readonly List<double> ticks = [];
    private readonly List<AxisLabel> labels = [];
    private readonly List<LabelExtent> extents = [];
    private readonly List<int> selected = [];
    private readonly List<ChartRect> titleMarkers = [];
    private readonly Dictionary<double, string> cache = [];
    private CultureInfo formatCulture = CultureInfo.InvariantCulture;

    public AxisLayoutOptions Options { get; private set; } = DefaultOptions;

    public AxisSide Side { get; private set; }

    public bool IsVertical => Side is AxisSide.Left or AxisSide.Right;

    public bool ShowsLabels => Options.IsVisible && Options.LabelsVisible;

    public bool ShowsTitle => Options.IsVisible && !String.IsNullOrEmpty(Options.Title);

    public bool RotatesTitle => IsVertical && ShowsTitle && (Options.TitleOrientation == ChartOrientation.Vertical);

    public bool HasDefaultFormat => (Options.LabelFormatter is null) && (Options.LabelConverter is null) && Options.LabelFormat.IsDefault;

    public bool IsPercent
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                cache.Clear();
            }
        }
    }

    public AxisScale Scale { get; private set; } = new(0, 1, 1);

    public AxisScale DisplayScale { get; set; } = new(0, 1, 1);

    public TimeInterval TimeInterval { get; private set; }

    public IReadOnlyList<double> Ticks => ticks;

    public IReadOnlyList<AxisLabel> Labels => labels;

    public double LabelWidth { get; private set; }

    public ChartRect Plot { get; private set; }

    public double TitleWidth { get; private set; }

    public double TitleMaxWidth { get; private set; }

    public ChartRect TitleBounds { get; private set; }

    public ChartRect TitleArea { get; private set; }

    public double TitleRotation { get; private set; }

    public IReadOnlyList<ChartRect> TitleMarkers => titleMarkers;

    public void Configure(AxisLayoutOptions options, AxisSide side, CultureInfo culture)
    {
        if (!options.Equals(Options) || !culture.Equals(formatCulture))
        {
            cache.Clear();
        }

        Options = options;
        Side = side;
        formatCulture = culture;
    }

    public void Resolve(double dataMinimum, double dataMaximum, double length, IReadOnlyList<string?> categories, AxisLayout? primary, IChartTextMeasurer measurer, double fontSize, double maxLabelWidth)
    {
        ticks.Clear();
        switch (Options.Kind)
        {
            case ChartAxisKind.Category:
                Scale = AxisRangeResolver.ResolveCategory(categories.Count, Options.LabelPlacement);
                TickGenerator.Category(categories.Count, ticks);
                break;
            case ChartAxisKind.DateTime:
                ResolveTime(dataMinimum, dataMaximum, length, measurer, fontSize);
                break;
            default:
                ResolveNumber(dataMinimum, dataMaximum, length, primary);
                break;
        }

        DisplayScale = Scale;
        CreateLabels(categories, measurer, fontSize, maxLabelWidth);
    }

    public void Place(ChartRect plot, ChartRect bounds, double lineHeight, double labelOffset)
    {
        Plot = plot;
        if (labels.Count == 0)
        {
            return;
        }

        extents.Clear();
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            var position = ToPixel(label.Value);
            var rect = Side switch
            {
                AxisSide.Left => new ChartRect(plot.X - CartesianLayout.LabelGap - label.Width, position - (lineHeight / 2), label.Width, lineHeight),
                AxisSide.Right => new ChartRect(plot.Right + CartesianLayout.LabelGap, position - (lineHeight / 2), label.Width, lineHeight),
                AxisSide.Top => new ChartRect(ClampStart(position - (label.Width / 2), label.Width, bounds), plot.Y - labelOffset - lineHeight, label.Width, lineHeight),
                _ => new ChartRect(ClampStart(position - (label.Width / 2), label.Width, bounds), plot.Bottom + labelOffset, label.Width, lineHeight)
            };
            labels[i] = label with { Bounds = rect };
            extents.Add(IsVertical ? new LabelExtent(rect.Y, rect.Bottom) : new LabelExtent(rect.X, rect.Right));
        }

        LabelThinning.Select(CollectionsMarshal.AsSpan(extents), CartesianLayout.LabelSpacing, Options.MaxLabelCount ?? Int32.MaxValue, selected);
        if (selected.Count < labels.Count)
        {
            for (var i = 0; i < selected.Count; i++)
            {
                labels[i] = labels[selected[i]];
            }

            labels.RemoveRange(selected.Count, labels.Count - selected.Count);
        }
    }

    public void MeasureTitle(IChartTextMeasurer measurer, double fontSize, double maxWidth, int markerCount, double markerSize)
    {
        TitleMaxWidth = Math.Max(0, maxWidth - GetMarkersWidth(markerCount, markerSize));
        TitleWidth = ShowsTitle ? measurer.Measure(Options.Title!, fontSize, TitleMaxWidth) : 0;
    }

    public void PlaceTitle(ChartRect row, double lineHeight, int markerCount, double markerSize)
    {
        TitleArea = default;
        TitleRotation = 0;
        PlaceTitleText(row, lineHeight, markerCount, markerSize, !IsVertical);
    }

    public void PlaceRotatedTitle(ChartRect area, double lineHeight, int markerCount, double markerSize)
    {
        TitleArea = area;
        TitleRotation = Side == AxisSide.Left ? -90 : 90;
        PlaceTitleText(new ChartRect(-area.Height / 2, -lineHeight / 2, area.Height, lineHeight), lineHeight, markerCount, markerSize, true);
    }

    public string GetLabelText(double value, IReadOnlyList<string?> categories) => GetText(value, categories);

    public string GetTrackingTimeText(double value, TimeLabelPrecision precision)
    {
        var finest = precision < TimeInterval.LabelPrecision ? precision : TimeInterval.LabelPrecision;
        var spansDays = (long)(Scale.Minimum / TimeSpan.TicksPerDay) != (long)(Scale.Maximum / TimeSpan.TicksPerDay);
        return ChartTextFormat.FormatTime(value, null, null, ChartTextFormat.Default, TimeInterval.GetTrackingFormat(finest, spansDays), formatCulture);
    }

    public double ToPixel(double value) => Map(value, Scale);

    public double ToDisplayPixel(double value) => Map(value, DisplayScale);

    public ChartRect GetDisplayBounds(AxisLabel label)
    {
        var shift = ToDisplayPixel(label.Value) - ToPixel(label.Value);
        return IsVertical ? label.Bounds with { Y = label.Bounds.Y + shift } : label.Bounds with { X = label.Bounds.X + shift };
    }

    public bool IsDisplayed(double value)
    {
        var pixel = ToDisplayPixel(value);
        return IsVertical ? (pixel >= Plot.Y - DisplayTolerance) && (pixel <= Plot.Bottom + DisplayTolerance) : (pixel >= Plot.X - DisplayTolerance) && (pixel <= Plot.Right + DisplayTolerance);
    }

    public double ToValue(double pixel)
    {
        var fraction = IsVertical ? (Plot.Bottom - pixel) / Plot.Height : (pixel - Plot.X) / Plot.Width;
        if (Options.IsInversed)
        {
            fraction = 1 - fraction;
        }

        return Scale.Minimum + (fraction * Scale.Span);
    }

    private void PlaceTitleText(ChartRect row, double lineHeight, int markerCount, double markerSize, bool centered)
    {
        titleMarkers.Clear();
        if (!ShowsTitle)
        {
            TitleBounds = default;
            return;
        }

        var markersWidth = GetMarkersWidth(markerCount, markerSize);
        var total = markersWidth + TitleWidth;
        var start = centered ? row.CenterX - (total / 2) : Side == AxisSide.Right ? row.Right - total : row.X;
        start = Math.Max(row.X, Math.Min(start, row.Right - total));
        var textX = Side == AxisSide.Right ? start : start + markersWidth;
        var markerX = Side == AxisSide.Right ? start + TitleWidth + LegendLayout.MarkerGap : start;
        for (var i = 0; i < markerCount; i++)
        {
            titleMarkers.Add(new ChartRect(markerX + (i * (markerSize + LegendLayout.MarkerGap)), row.Y + ((lineHeight - markerSize) / 2), markerSize, markerSize));
        }

        TitleBounds = new ChartRect(textX, row.Y, TitleWidth, lineHeight);
    }

    private static double GetMarkersWidth(int markerCount, double markerSize) =>
        markerCount > 0 ? (markerCount * (markerSize + LegendLayout.MarkerGap)) : 0;

    private double Map(double value, AxisScale scale)
    {
        var fraction = (value - scale.Minimum) / scale.Span;
        if (Options.IsInversed)
        {
            fraction = 1 - fraction;
        }

        return IsVertical ? Plot.Bottom - (fraction * Plot.Height) : Plot.X + (fraction * Plot.Width);
    }

    private static double ClampStart(double start, double width, ChartRect bounds) =>
        Math.Max(bounds.X, Math.Min(start, bounds.Right - width));

    private double GetSpacing() => IsVertical ? TickGenerator.VerticalLabelSpacing : TickGenerator.HorizontalLabelSpacing;

    private void ResolveNumber(double dataMinimum, double dataMaximum, double length, AxisLayout? primary)
    {
        if ((primary is not null) && (Options.TickAlignment == ChartTickAlignment.AlignToPrimary))
        {
            Scale = AxisRangeResolver.ResolveAligned(dataMinimum, dataMaximum, Options.Range, primary.Scale);
            TickGenerator.Aligned(primary.Scale, primary.Ticks, Scale, ticks);
            return;
        }

        Scale = AxisRangeResolver.Resolve(dataMinimum, dataMaximum, Options.Range, TickGenerator.GetMaxDivisions(length, GetSpacing(), Options.MaxLabelCount));
        TickGenerator.Numeric(Scale, ticks);
    }

    private void ResolveTime(double dataMinimum, double dataMaximum, double length, IChartTextMeasurer measurer, double fontSize)
    {
        var (minimum, maximum) = AxisRangeResolver.ResolveTime(dataMinimum, dataMaximum, Options.Range);
        Scale = new AxisScale(minimum, maximum, maximum - minimum);
        var interval = Options.TimeInterval ?? TickGenerator.SelectTimeInterval(
            maximum - minimum,
            length,
            TickGenerator.GetMaxDivisions(length, GetSpacing(), Options.MaxLabelCount),
            CartesianLayout.LabelSpacing,
            MeasureTimeLabels(measurer, fontSize),
            Options.MinimumTimeUnit);
        if (interval != TimeInterval)
        {
            cache.Clear();
        }

        TimeInterval = interval;
        TickGenerator.Time(minimum, maximum, interval, Options.FirstDayOfWeek, ticks);
    }

    private TimeLabelWidths MeasureTimeLabels(IChartTextMeasurer measurer, double fontSize)
    {
        if (!ShowsLabels)
        {
            return default;
        }

        var day = MeasureTime(TimeLabelPrecision.Day);
        return new TimeLabelWidths(
            Math.Max(MeasureTime(TimeLabelPrecision.Second), day),
            Math.Max(MeasureTime(TimeLabelPrecision.Minute), day),
            day,
            MeasureTime(TimeLabelPrecision.Month));

        double MeasureTime(TimeLabelPrecision precision) =>
            measurer.Measure(ChartTextFormat.FormatTime(SampleTime, Options.LabelFormatter, Options.LabelConverter, Options.LabelFormat, TimeInterval.GetDefaultFormat(precision), formatCulture), fontSize, Double.PositiveInfinity);
    }

    private void CreateLabels(IReadOnlyList<string?> categories, IChartTextMeasurer measurer, double fontSize, double maxLabelWidth)
    {
        labels.Clear();
        LabelWidth = 0;
        if (!ShowsLabels)
        {
            return;
        }

        if (cache.Count > MaxCachedLabels)
        {
            cache.Clear();
        }

        foreach (var tick in ticks)
        {
            var text = Shorten(GetText(tick, categories));
            var width = measurer.Measure(text, fontSize, maxLabelWidth);
            labels.Add(new AxisLabel(tick, text, width, maxLabelWidth, default));
            LabelWidth = Math.Max(LabelWidth, width);
        }
    }

    private string GetText(double value, IReadOnlyList<string?> categories)
    {
        if (Options.Kind == ChartAxisKind.Category)
        {
            var index = (int)value;
            var category = (index >= 0) && (index < categories.Count) ? categories[index] ?? String.Empty : String.Empty;
            return ChartTextFormat.FormatCategory(category, Options.CategoryFormatter, Options.LabelConverter, Options.LabelFormat, formatCulture);
        }

        if (!cache.TryGetValue(value, out var text))
        {
            text = Options.Kind switch
            {
                ChartAxisKind.DateTime => ChartTextFormat.FormatTime(value, Options.LabelFormatter, Options.LabelConverter, Options.LabelFormat, TimeInterval.GetTickFormat(value), formatCulture),
                _ when IsPercent && HasDefaultFormat => FormatPercent(value, formatCulture),
                _ => ChartTextFormat.FormatValue(value, Options.LabelFormatter, Options.LabelConverter, Options.LabelFormat, formatCulture)
            };
            cache.Add(value, text);
        }

        return text;
    }

    private string Shorten(string text)
    {
        if (Options.LabelMaxLength is not { } maxLength)
        {
            return text;
        }

        var info = new StringInfo(text);
        return info.LengthInTextElements > maxLength ? info.SubstringByTextElements(0, maxLength).TrimEnd() + Ellipsis : text;
    }

    private static string FormatPercent(double value, CultureInfo culture)
    {
        var format = IsWhole(value) ? "P0" : IsWhole(value * 10) ? "P1" : "P2";
        return (value / 100).ToString(format, culture);
    }

    private static bool IsWhole(double value) => Math.Abs(value - Math.Round(value)) < NiceScale.Tolerance;
}
