namespace ClamChart.Layout;

internal readonly record struct AxisScale(double Minimum, double Maximum, double Interval)
{
    public double Span => Maximum - Minimum;
}
