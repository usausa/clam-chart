namespace ClamChart.Layout;

using ClamChart.Rendering;

internal sealed record PieLayoutOptions
{
    public double InnerRadius { get; init; } = 0.6;

    public double StartAngle { get; init; }

    public double SweepAngle { get; init; } = 360;

    public double Gap { get; init; } = 2;

    public double ExplodeMargin { get; init; }

    public ChartSortMode SortMode { get; init; }

    public int MaxSlices { get; init; } = 8;

    public double OthersThreshold { get; init; }

    public string OthersLabel { get; init; } = "Other";

    public ChartPieLabelsMode LabelsMode { get; init; }

    public ChartPieLabelContent LabelContent { get; init; }

    public bool LegendVisible { get; init; }

    public ChartLegendPosition LegendPosition { get; init; } = ChartLegendPosition.Bottom;

    public int LegendMaxRows { get; init; } = LegendLayout.MaxRows;

    public ChartLegendValueMode LegendValueMode { get; init; }

    public ChartCenterValueMode CenterValueMode { get; init; }

    public double? CenterValue { get; init; }

    public string? CenterLabel { get; init; }

    public bool Selectable { get; init; }

    public ChartTextFormat ValueFormat { get; init; } = ChartTextFormat.Default;

    public IValueConverter? ValueConverter { get; init; }

    public Func<double, string>? ValueFormatter { get; init; }

    public ChartTextFormat CenterFormat { get; init; } = ChartTextFormat.Default;

    public IValueConverter? CenterConverter { get; init; }

    public Func<double, string>? CenterFormatter { get; init; }
}
