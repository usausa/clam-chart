namespace ClamChart.Layout;

using ClamChart.Rendering;

internal sealed record BarGaugeLayoutOptions
{
    public ChartOrientation Orientation { get; init; } = ChartOrientation.Horizontal;

    public ChartBarGaugeMode DisplayMode { get; init; }

    public ChartBarGaugeLabelPosition LabelPosition { get; init; }

    public bool ValueVisible { get; init; } = true;

    public ChartBarGaugeValueMode ValueMode { get; init; } = ChartBarGaugeValueMode.Percent;

    public ChartValueFormatting Formatting { get; init; } = ChartValueFormatting.Default;

    public double Minimum { get; init; }

    public double Maximum { get; init; } = 100;

    public double BarThickness { get; init; } = 8;

    public double ItemSpacing { get; init; } = 12;

    public double SegmentWidth { get; init; } = 12;

    public ChartSortMode SortMode { get; init; }

    public int? MaxItems { get; init; }
}
