namespace ClamChart.Layout;

internal sealed record SparklineLayoutOptions
{
    public ChartSparklineKind Kind { get; init; }

    public double? Thickness { get; init; }

    public ChartSparklineMarkers Markers { get; init; } = ChartSparklineMarkers.Last;

    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public double? ReferenceValue { get; init; }
}
