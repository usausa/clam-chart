namespace ClamChart;

using SkiaSharp;

public readonly record struct SparklineDrawOptions
{
    public SparklineDrawOptions()
    {
    }

    public ChartSparklineKind Kind { get; init; } = ChartSparklineKind.Line;

    public SKColor Color { get; init; } = new(0x2A, 0x78, 0xD6);

    public SKColor NegativeColor { get; init; } = new(0xE0, 0x2F, 0x44);

    public float Thickness { get; init; } = 2;

    public float AreaOpacity { get; init; } = 0.1f;

    public bool Smooth { get; init; }

    public ChartSparklineMarkers Markers { get; init; } = ChartSparklineMarkers.Last;

    public double? Minimum { get; init; }

    public double? Maximum { get; init; }
}
