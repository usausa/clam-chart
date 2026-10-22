namespace ClamChart.Layout;

internal readonly record struct PieOutsideLabel(ChartRect Bounds, double ElbowX, double ElbowY, double EndX)
{
    public bool IsVisible => Bounds.Width > 0;
}
