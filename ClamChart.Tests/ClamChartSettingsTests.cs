namespace ClamChart.Tests;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamChartSettingsTests
{
    [Fact]
    public void AnimationScaleRejectsNegativeAndNonFiniteValues()
    {
        try
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(static () => ClamChartSettings.AnimationScale = -0.1);
            Assert.Throws<ArgumentOutOfRangeException>(static () => ClamChartSettings.AnimationScale = Double.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(static () => ClamChartSettings.AnimationScale = Double.PositiveInfinity);
            Assert.Equal(1d, ClamChartSettings.AnimationScale);

            // Act & Assert
            ClamChartSettings.AnimationScale = 0;
            Assert.Equal(0d, ClamChartSettings.AnimationScale);
        }
        finally
        {
            ClamChartSettings.AnimationScale = 1;
        }
    }

    [Fact]
    public void ResolvedScaleCombinesTheSettings()
    {
        try
        {
            // Act & Assert
            Assert.Equal(1d, ClamChartSettings.ResolveAnimationScale());

            // Act & Assert
            ClamChartSettings.AnimationScale = 0.5;
            Assert.Equal(0.5, ClamChartSettings.ResolveAnimationScale());

            // Act & Assert
            ClamChartSettings.RespectSystemAnimationScale = false;
            Assert.Equal(0.5, ClamChartSettings.ResolveAnimationScale());

            // Act & Assert
            ClamChartSettings.AnimationEnabled = false;
            Assert.Equal(0d, ClamChartSettings.ResolveAnimationScale());
        }
        finally
        {
            ClamChartSettings.AnimationEnabled = true;
            ClamChartSettings.AnimationScale = 1;
            ClamChartSettings.RespectSystemAnimationScale = true;
        }
    }
}
