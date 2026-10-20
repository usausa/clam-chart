namespace ClamChart;

public sealed class ChartThreshold
{
    public double Value { get; set; } = Double.NegativeInfinity;

    public Color? Color { get; set; }

    public ChartThresholdLevel Level { get; set; }

    public string? Label { get; set; }
}
