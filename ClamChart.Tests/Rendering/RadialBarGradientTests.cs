namespace ClamChart.Tests.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class RadialBarGradientTests
{
    private const float Precision = 0.0001f;

    private static readonly ChartStyle Style = new();

    private static readonly SKColor Base = SKColors.Blue;

    private static readonly SKColor Good = Style.GoodColor.ToSKColor();

    private static readonly SKColor Warning = Style.WarningColor.ToSKColor();

    private static readonly SKColor Critical = Style.CriticalColor.ToSKColor();

    [Fact]
    public void ThresholdColorsSitAtTheirPositions()
    {
        // Act
        var (colors, positions) = GetThresholdStops(Thresholds());

        // Assert
        Assert.Equal([Good, Warning, Critical, Critical], colors);
        AssertPositions([0, 0.6f, 0.8f, 1], positions);
    }

    [Fact]
    public void WithoutABaseStepTheRingColorComesFirst()
    {
        // Act
        var (colors, positions) = GetThresholdStops([new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning }]);

        // Assert
        Assert.Equal([Base, Warning, Warning], colors);
        AssertPositions([0, 0.6f, 1], positions);
    }

    [Fact]
    public void StepsOverTheMaximumAreLeftOutAndTheRestAreSorted()
    {
        // Arrange
        IList<ChartThreshold> thresholds = [new() { Value = 120, Level = ChartThresholdLevel.Critical }, new() { Value = 50, Level = ChartThresholdLevel.Warning }, new() { Level = ChartThresholdLevel.Good }];

        // Act
        var (colors, positions) = GetThresholdStops(thresholds);

        // Assert
        Assert.Equal([Good, Warning, Warning], colors);
        AssertPositions([0, 0.5f, 1], positions);
    }

    [Fact]
    public void OneColorTurnsTowardTheValue()
    {
        // Arrange
        var colors = new List<SKColor>();
        var positions = new List<float>();
        var red = new SKColor(0xFF, 0, 0);

        // Act
        RadialBarGradient.GetColorStops(red, 0.25, true, colors, positions);
        var dark = colors.ToArray();
        var darkPositions = positions.ToArray();
        RadialBarGradient.GetColorStops(red, 0.25, false, colors, positions);
        var light = colors.ToArray();

        // Assert
        AssertPositions([0, 0.25f, 1], [.. darkPositions]);
        Assert.Equal(dark[1], dark[2]);
        Assert.Equal(light[1], light[2]);
        Assert.Equal(dark[1], light[0]);
        Assert.True(GetLightness(dark[1]) > GetLightness(dark[0]));
        Assert.True(GetLightness(light[0]) > GetLightness(light[1]));
        Assert.InRange(dark[1].Red, 252, 255);
        Assert.InRange(dark[1].Green, 140, 146);
        Assert.InRange(dark[1].Blue, 74, 80);
    }

    [Fact]
    public void ColorsBetweenStopsAreMixed()
    {
        // Act
        var middle = RadialBarGradient.GetColorAt([SKColors.Red, SKColors.Blue], [0, 1], 0.5);
        var below = RadialBarGradient.GetColorAt([SKColors.Red, SKColors.Blue], [0.2f, 1], 0.1);
        var above = RadialBarGradient.GetColorAt([SKColors.Red, SKColors.Blue], [0, 0.8f], 0.9);

        // Assert
        Assert.Equal(new SKColor(128, 0, 128), middle);
        Assert.Equal(SKColors.Red, below);
        Assert.Equal(SKColors.Blue, above);
    }

    private static List<ChartThreshold> Thresholds() =>
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 60, Level = ChartThresholdLevel.Warning },
        new() { Value = 80, Level = ChartThresholdLevel.Critical }
    ];

    private static (List<SKColor> Colors, List<float> Positions) GetThresholdStops(IList<ChartThreshold> thresholds)
    {
        var colors = new List<SKColor>();
        var positions = new List<float>();
        RadialBarGradient.GetThresholdStops(thresholds, Base, Style, colors, positions);
        return (colors, positions);
    }

    private static float GetLightness(SKColor color)
    {
        color.ToHsl(out _, out _, out var lightness);
        return lightness;
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
