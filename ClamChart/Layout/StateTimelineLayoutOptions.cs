namespace ClamChart.Layout;

using ClamChart.Rendering;

internal sealed record StateTimelineLayoutOptions
{
    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public bool MergeValues { get; init; } = true;

    public double RowHeight { get; init; } = 0.9;

    public bool LabelsVisible { get; init; } = true;

    public bool LegendVisible { get; init; }

    public ChartLegendPosition LegendPosition { get; init; } = ChartLegendPosition.Bottom;

    public IList<ChartValueMapping> Mappings { get; init; } = [];

    public IList<ChartThreshold> Thresholds { get; init; } = [];

    public DayOfWeek FirstDayOfWeek { get; init; }

    public ChartValueFormatting Formatting { get; init; } = ChartValueFormatting.Default;
}
