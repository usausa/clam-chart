namespace ClamChart;

public sealed class ChartTrackingEventArgs(int index, object? item, object? x, IReadOnlyList<ChartTrackingInfo> points) : EventArgs
{
    public int Index { get; } = index;

    public object? Item { get; } = item;

    public object? X { get; } = x;

    public IReadOnlyList<ChartTrackingInfo> Points { get; } = points;
}
