namespace ClamChart.Layout;

internal readonly record struct CartesianAnnotation(int Index, bool IsBand, ChartAnnotationAxis Axis, ChartAxisTarget Target, double From, double To, double FromTicks, double ToTicks)
{
    public (double From, double To) Resolve(bool time) =>
        time ? (FromTicks, ToTicks) : (From, To);
}
