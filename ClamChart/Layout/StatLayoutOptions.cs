namespace ClamChart.Layout;

internal sealed record StatLayoutOptions
{
    public string? Title { get; init; }

    public string? ValueText { get; init; }

    public string? Unit { get; init; }

    public double ValueFontSize { get; init; } = 32;

    public double MinValueFontSize { get; init; } = 16;

    public StatDelta Delta { get; init; }

    public string? Caption { get; init; }

    public ChartThresholdLevel IconLevel { get; init; }

    public bool ShowBand { get; init; }

    public ChartStatLayoutMode LayoutMode { get; init; }

    public ChartTrendPlacement TrendPlacement { get; init; }

    public bool HasTrend { get; init; }
}
