namespace ClamChart.Layout;

using ClamChart.Data;
using ClamChart.Rendering;

internal sealed record CartesianSeries
{
    public const double DefaultBarWidthRatio = 0.6;

    public required DataSnapshot Data { get; init; }

    public ChartSeriesKind Kind { get; init; }

    public ChartAxisTarget Axis { get; init; }

    public ChartLineInterpolation Interpolation { get; init; }

    public string? StackGroup { get; init; }

    public double? BaselineValue { get; init; }

    public double BarWidthRatio { get; init; } = DefaultBarWidthRatio;

    public double? MaxBarWidth { get; init; }

    public bool IsVisible { get; init; } = true;

    public string? Title { get; init; }

    public bool LegendVisible { get; init; } = true;

    public ChartTextFormat ValueFormat { get; init; } = ChartTextFormat.Default;

    public IValueConverter? ValueConverter { get; init; }

    public Func<double, string>? ValueFormatter { get; init; }
}
