namespace ClamChart.Layout;

internal readonly record struct PieCenterText(string? Value, string? Caption, double ValueFontSize, double CaptionFontSize, ChartRect ValueBounds, ChartRect CaptionBounds)
{
    public bool IsVisible => Value is not null;
}
