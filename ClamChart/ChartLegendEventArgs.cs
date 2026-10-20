namespace ClamChart;

public sealed class ChartLegendEventArgs(ChartSeries? series, object? item, int index) : EventArgs
{
    public ChartSeries? Series { get; } = series;

    public object? Item { get; } = item;

    public int Index { get; } = index;

    public bool Handled { get; set; }
}
