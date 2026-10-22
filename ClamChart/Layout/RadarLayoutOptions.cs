namespace ClamChart.Layout;

using ClamChart.Rendering;

internal sealed record RadarLayoutOptions
{
    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public ChartRadarGridShape GridShape { get; init; }

    public int LevelCount { get; init; } = 5;

    public bool LabelsVisible { get; init; } = true;

    public double RingMargin { get; init; }

    public bool LegendVisible { get; init; }

    public ChartLegendPosition LegendPosition { get; init; } = ChartLegendPosition.Bottom;

    public int LegendMaxRows { get; init; } = LegendLayout.MaxRows;

    public ChartTextFormat ValueFormat { get; init; } = ChartTextFormat.Default;

    public IValueConverter? ValueConverter { get; init; }

    public Func<double, string>? ValueFormatter { get; init; }
}
