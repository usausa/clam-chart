namespace ClamChart;

public sealed class ChartTappedEventArgs(Point position) : EventArgs
{
    public Point Position { get; } = position;

    public bool Handled { get; set; }
}
