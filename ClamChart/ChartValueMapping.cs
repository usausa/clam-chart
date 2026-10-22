namespace ClamChart;

public sealed class ChartValueMapping
{
    public object? Value { get; set; }

    public string? Text { get; set; }

    public Color? Color { get; set; }

    public ChartThresholdLevel Level { get; set; }
}
