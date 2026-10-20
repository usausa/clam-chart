namespace ClamChart.Layout;

internal readonly record struct RadialBarCenter(string? Value, string? Caption, double ValueFontSize, double CaptionFontSize, ChartRect ValueBounds, ChartRect CaptionBounds, double Number, bool IsPercent)
{
    public bool IsVisible => Value is not null;
}
