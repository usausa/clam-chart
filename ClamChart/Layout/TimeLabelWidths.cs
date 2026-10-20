namespace ClamChart.Layout;

internal readonly record struct TimeLabelWidths(double Second, double Minute, double Day, double Month)
{
    public double Get(TimeLabelPrecision precision) => precision switch
    {
        TimeLabelPrecision.Second => Second,
        TimeLabelPrecision.Minute => Minute,
        TimeLabelPrecision.Day => Day,
        _ => Month
    };
}
