namespace ClamChart.Rendering;

internal readonly record struct SelectionBlend(CartesianSelection Previous, CartesianSelection Current, double Progress, double Dimmed)
{
    public bool IsActive => Previous.Active || Current.Active;

    public double SeriesAlpha(int series) =>
        Blend(Previous.DimsSeries(series) ? Dimmed : 1, Current.DimsSeries(series) ? Dimmed : 1);

    public double PointAlpha(int series, int index, double x) =>
        Blend(Previous.DimsPoint(series, index, x) ? Dimmed : 1, Current.DimsPoint(series, index, x) ? Dimmed : 1);

    private double Blend(double from, double to) => from + ((to - from) * Progress);
}
