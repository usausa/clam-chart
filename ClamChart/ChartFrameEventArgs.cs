namespace ClamChart;

public sealed class ChartFrameEventArgs(double elapsedMilliseconds, int pointCount, bool animating) : EventArgs
{
    public double ElapsedMilliseconds { get; } = elapsedMilliseconds;

    public int PointCount { get; } = pointCount;

    public bool Animating { get; } = animating;
}
