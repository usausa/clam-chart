namespace ClamChart.Layout;

internal static class DensityResolver
{
    public static ChartDensity Resolve(double width, double height, Size compactSize, Size minimalSize)
    {
        if ((width <= minimalSize.Width) || (height <= minimalSize.Height))
        {
            return ChartDensity.Minimal;
        }

        return (width <= compactSize.Width) || (height <= compactSize.Height) ? ChartDensity.Compact : ChartDensity.Full;
    }
}
