namespace ClamChart.Tests.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class BarGaugeGradientTests
{
    private const float Precision = 0.0001f;

    private static readonly ChartStyle Style = new();

    private static readonly SKColor Base = Colors.Blue.ToSKColor();

    private static readonly SKColor Good = Style.GoodColor.ToSKColor();

    private static readonly SKColor Warning = Style.WarningColor.ToSKColor();

    private static readonly SKColor Critical = Style.CriticalColor.ToSKColor();

    [Fact]
    public void ThresholdColorsChangeHalfwayFromTheLastStep()
    {
        // Act
        var (colors, positions) = GetStops(Thresholds(), 90, 0.9);

        // Assert
        Assert.Equal([Good, Good, Warning, Critical], colors);
        AssertPositions([0, 0.3f, 0.7f, 0.9f], positions);
    }

    [Fact]
    public void StepsAboveTheValueAreSkipped()
    {
        // Act
        var (colors, positions) = GetStops(Thresholds(), 50, 0.5);

        // Assert
        Assert.Equal([Good, Good], colors);
        AssertPositions([0, 0.5f], positions);
    }

    [Fact]
    public void WithoutABaseStepTheBarColorComesFirst()
    {
        // Act
        var (colors, positions) = GetStops([new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning }], 70, 0.7);

        // Assert
        Assert.Equal([Base, Base, Warning], colors);
        AssertPositions([0, 0.3f, 0.7f], positions);
    }

    [Fact]
    public void StepsAreSortedByValue()
    {
        // Arrange
        var thresholds = Thresholds();
        IList<ChartThreshold> shuffled = [thresholds[2], thresholds[0], thresholds[1]];

        // Act
        var (colors, positions) = GetStops(shuffled, 90, 0.9);

        // Assert
        Assert.Equal([Good, Good, Warning, Critical], colors);
        AssertPositions([0, 0.3f, 0.7f, 0.9f], positions);
    }

    [Fact]
    public void WithoutThresholdsTheBarIsOneColor()
    {
        // Act
        var (colors, positions) = GetStops([], 40, 0.4);

        // Assert
        Assert.Equal([Base, Base], colors);
        AssertPositions([0, 0.4f], positions);
    }

    [Fact]
    public void ValuesOverTheMaximumEndAtTheTrackEnd()
    {
        // Act
        var (colors, positions) = GetStops(Thresholds(), 150, 1);

        // Assert
        Assert.Equal([Good, Good, Warning, Critical], colors);
        AssertPositions([0, 0.3f, 0.7f, 1], positions);
    }

    private static List<ChartThreshold> Thresholds() =>
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 60, Level = ChartThresholdLevel.Warning },
        new() { Value = 80, Level = ChartThresholdLevel.Critical }
    ];

    private static (List<SKColor> Colors, List<float> Positions) GetStops(IList<ChartThreshold> thresholds, double percent, double ratio)
    {
        var colors = new List<SKColor>();
        var positions = new List<float>();
        BarGaugeGradient.GetStops(thresholds, percent, ratio, Colors.Blue, Style, colors, positions);
        return (colors, positions);
    }

    private static void AssertPositions(float[] expected, List<float> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Precision);
        }
    }
}
