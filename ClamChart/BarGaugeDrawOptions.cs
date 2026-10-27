namespace ClamChart;

using SkiaSharp;

public readonly record struct BarGaugeDrawOptions
{
    public BarGaugeDrawOptions()
    {
    }

    public double Minimum { get; init; }

    public double Maximum { get; init; } = 100;

    public ChartOrientation Orientation { get; init; } = ChartOrientation.Horizontal;

    public ChartBarGaugeMode DisplayMode { get; init; }

    public ChartFillMode FillMode { get; init; }

    public float SegmentWidth { get; init; } = 12;

    public float SegmentGap { get; init; } = 2;

    public float Spacing { get; init; } = 12;

    public bool TrackVisible { get; init; } = true;

    public SKColor? TrackColor { get; init; }

    public SKColor? Color { get; init; }

    public IReadOnlyList<ChartDrawingThreshold>? Thresholds { get; init; }

    public bool IsDark { get; init; }
}
