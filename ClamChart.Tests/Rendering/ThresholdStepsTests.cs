namespace ClamChart.Tests.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ThresholdStepsTests
{
    private static readonly ChartStyle Style = new();

    private static readonly SKColor Base = SKColors.Blue;

    [Fact]
    public void ResolvedStepsAreSortedAndKeepTheOrderOfEqualValues()
    {
        // Arrange
        IList<ChartThreshold> thresholds =
        [
            new() { Value = 80, Color = Colors.Red },
            new() { Value = 40, Color = Colors.Lime },
            new() { Level = ChartThresholdLevel.Good },
            new() { Value = 40, Color = Colors.Yellow }
        ];
        var steps = new List<ChartDrawingThreshold>();

        // Act
        ThresholdSteps.Resolve(thresholds, Base, Style, steps);

        // Assert
        Assert.Equal(
        [
            new ChartDrawingThreshold(Double.NegativeInfinity, Style.GoodColor.ToSKColor()),
            new ChartDrawingThreshold(40, SKColors.Lime),
            new ChartDrawingThreshold(40, SKColors.Yellow),
            new ChartDrawingThreshold(80, SKColors.Red)
        ], steps);
    }

    [Fact]
    public void StepsWithoutAColorUseTheBaseColor()
    {
        // Arrange
        var steps = new List<ChartDrawingThreshold>();

        // Act
        ThresholdSteps.Resolve([new ChartThreshold { Value = 50 }], Base, Style, steps);

        // Assert
        Assert.Equal([new ChartDrawingThreshold(50, Base)], steps);
    }

    [Fact]
    public void ColorsMatchTheThresholdsOfTheViews()
    {
        // Arrange
        IList<ChartThreshold> thresholds =
        [
            new() { Value = 60, Level = ChartThresholdLevel.Warning },
            new() { Value = 20, Color = Colors.Lime },
            new() { Value = 60, Color = Colors.Red },
            new() { Value = 90 }
        ];
        var steps = new List<ChartDrawingThreshold>();
        ThresholdSteps.Resolve(thresholds, Base, Style, steps);

        // Act & Assert
        foreach (var percent in (double[])[Double.NaN, -10, 20, 59.9, 60, 75, 90, 120])
        {
            var expected = ThresholdResolver.GetColor(ThresholdResolver.Resolve(thresholds, percent), Style)?.ToSKColor() ?? Base;
            Assert.Equal(expected, ThresholdSteps.GetColor([.. steps], percent, Base));
        }
    }
}
