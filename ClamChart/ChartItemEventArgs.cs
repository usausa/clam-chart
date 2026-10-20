namespace ClamChart;

public sealed class ChartItemEventArgs(ChartSeries? series, int seriesIndex, int index, object? item, object? x, double y, Point position) : EventArgs
{
    public ChartSeries? Series { get; } = series;

    public int SeriesIndex { get; } = seriesIndex;

    public int Index { get; } = index;

    public object? Item { get; } = item;

    public object? X { get; } = x;

    public double Y { get; } = y;

    public Point Position { get; } = position;

    public bool Handled { get; set; }
}
