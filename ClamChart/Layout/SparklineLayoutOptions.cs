namespace ClamChart.Layout;

internal sealed record SparklineLayoutOptions
{
    public ChartSparklineKind Kind { get; init; }

    public double? Thickness { get; init; }

    public ChartSparklineMarkers Markers { get; init; } = ChartSparklineMarkers.Last;

    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public double? ReferenceValue { get; init; }

    public double? NormalRangeMinimum { get; init; }

    public double? NormalRangeMaximum { get; init; }

    public IList<ChartThreshold>? Thresholds { get; init; }

    public bool EdgeToEdge { get; init; }

    public bool LiveIndicator { get; init; }
}
