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

    public ChartLegendMode LegendMode { get; init; }

    public ChartLegendValues LegendValues { get; init; }

    public double LegendWidth { get; init; } = LegendLayout.MaxSideWidth;

    public ChartLegendSortKey LegendSortBy { get; init; }

    public ChartSortMode LegendSortMode { get; init; }

    public ChartDataLabelsMode DataLabelsMode { get; init; }

    public bool LastMarkerOnly { get; init; }

    public bool GlowEnabled { get; init; }

    public double LiveIndicatorExtent { get; init; }

    public IReadOnlyList<CartesianAnnotation> Annotations { get; init; } = [];

    public IList<ChartThreshold>? Thresholds { get; init; }
}
