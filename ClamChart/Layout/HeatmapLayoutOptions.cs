namespace ClamChart.Layout;

using ClamChart.Rendering;

internal sealed record HeatmapLayoutOptions
{
    public ChartHeatmapLayoutMode LayoutMode { get; init; }

    public bool HasRows { get; init; }

    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public double CellGap { get; init; } = 1;

    public bool LabelsVisible { get; init; } = true;

    public bool LegendVisible { get; init; }

    public ChartLegendPosition LegendPosition { get; init; } = ChartLegendPosition.Bottom;

    public DayOfWeek FirstDayOfWeek { get; init; }

    public ChartTextFormat ValueFormat { get; init; } = ChartTextFormat.Default;

    public IValueConverter? ValueConverter { get; init; }

    public Func<double, string>? ValueFormatter { get; init; }
}
