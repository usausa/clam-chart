namespace ClamChart.Layout;

using ClamChart.Rendering;

internal sealed record RadialBarLayoutOptions
{
    public double Minimum { get; init; }

    public double Maximum { get; init; } = 100;

    public double StartAngle { get; init; }

    public double SweepAngle { get; init; } = 360;

    public double InnerRadius { get; init; } = 0.4;

    public double? RingThickness { get; init; }

    public double RingSpacing { get; init; } = 4;

    public ChartCapStyle CapStyle { get; init; }

    public ChartRadialBarMode DisplayMode { get; init; }

    public int SegmentCount { get; init; } = 20;

    public ChartOverflowMode OverflowMode { get; init; }

    public bool BarGlow { get; init; }

    public bool ThresholdMarkers { get; init; }

    public ChartValueFormatting Formatting { get; init; } = ChartValueFormatting.Default;

    public bool LegendVisible { get; init; }

    public ChartLegendPosition LegendPosition { get; init; } = ChartLegendPosition.Right;

    public int LegendMaxRows { get; init; } = LegendLayout.MaxRows;

    public ChartLegendValueMode LegendValueMode { get; init; }

    public ChartCenterValueMode CenterValueMode { get; init; }

    public string? CenterText { get; init; }

    public string? CenterLabel { get; init; }

    public bool Selectable { get; init; }
}
