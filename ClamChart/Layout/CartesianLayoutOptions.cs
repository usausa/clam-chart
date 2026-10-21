namespace ClamChart.Layout;

internal sealed record CartesianLayoutOptions
{
    public ChartOrientation Orientation { get; init; }

    public ChartStackMode StackMode { get; init; }

    public AxisLayoutOptions XAxis { get; init; } = new();

    public AxisLayoutOptions YAxis { get; init; } = new();

    public AxisLayoutOptions? SecondaryYAxis { get; init; }

    public bool LegendVisible { get; init; }

    public ChartLegendPosition LegendPosition { get; init; } = ChartLegendPosition.Bottom;

    public int LegendMaxRows { get; init; } = LegendLayout.MaxRows;

    public ChartDataLabelsMode DataLabelsMode { get; init; }

    public bool LastMarkerOnly { get; init; }

    public IReadOnlyList<CartesianAnnotation> Annotations { get; init; } = [];

    public IList<ChartThreshold>? Thresholds { get; init; }
}
