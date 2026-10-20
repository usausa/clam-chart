namespace ClamChart.Layout;

internal sealed record AxisRangeOptions
{
    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public double? SoftMinimum { get; init; }

    public double? SoftMaximum { get; init; }

    public double? Interval { get; init; }

    public bool IncludeZero { get; init; }

    public bool NiceRange { get; init; } = true;
}
