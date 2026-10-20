namespace ClamChart;

using ClamChart.Data;

public readonly record struct ChartTrackingInfo
{
    private readonly double x;
    private readonly ChartXKind kind;
    private readonly string? category;

    internal ChartTrackingInfo(ChartSeries series, int seriesIndex, int index, object? item, double x, ChartXKind kind, string? category, double y)
    {
        Series = series;
        SeriesIndex = seriesIndex;
        Index = index;
        Item = item;
        this.x = x;
        this.kind = kind;
        this.category = category;
        Y = y;
    }

    public ChartSeries Series { get; }

    public int SeriesIndex { get; }

    public int Index { get; }

    public object? Item { get; }

    public object? X => kind switch
    {
        ChartXKind.Category => category,
        ChartXKind.Time => ChartDateTime.FromTicks(x),
        _ => x
    };

    public double Y { get; }
}
