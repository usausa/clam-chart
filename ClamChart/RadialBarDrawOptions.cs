namespace ClamChart;

using SkiaSharp;

public readonly record struct RadialBarDrawOptions
{
    public RadialBarDrawOptions()
    {
    }

    public double Minimum { get; init; }

    public double Maximum { get; init; } = 100;

    public float StartAngle { get; init; }

    public float SweepAngle { get; init; } = 360;

    public float InnerRadius { get; init; } = 0.4f;

    public float? Thickness { get; init; }

    public float Spacing { get; init; } = 4;

    public ChartCapStyle CapStyle { get; init; }

    public ChartRadialBarMode DisplayMode { get; init; }

    public int SegmentCount { get; init; } = 20;

    public float SegmentGap { get; init; } = 2;

    public bool TrackVisible { get; init; } = true;

    public SKColor? TrackColor { get; init; }

    public float GlowRadius { get; init; }

    public ChartOverflowMode OverflowMode { get; init; }

    public IReadOnlyList<SKColor>? Colors { get; init; }

    public IReadOnlyList<ChartDrawingThreshold>? Thresholds { get; init; }

    public bool IsDark { get; init; }
}
