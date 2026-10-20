namespace ClamChart.Tests.Layout;

public sealed class DensityResolverTests
{
    [Theory]
    [InlineData(400, 300, ChartDensity.Full)]
    [InlineData(320.5, 200.5, ChartDensity.Full)]
    [InlineData(320, 300, ChartDensity.Compact)]
    [InlineData(400, 200, ChartDensity.Compact)]
    [InlineData(160.5, 96.5, ChartDensity.Compact)]
    [InlineData(160, 300, ChartDensity.Minimal)]
    [InlineData(400, 96, ChartDensity.Minimal)]
    [InlineData(160, 96, ChartDensity.Minimal)]
    [InlineData(0, 0, ChartDensity.Minimal)]
    public void SizeAtOrBelowABoundarySelectsTheSmallerDensity(double width, double height, ChartDensity expected)
    {
        // Act & Assert
        Assert.Equal(expected, DensityResolver.Resolve(width, height, new Size(320, 200), new Size(160, 96)));
    }
}
