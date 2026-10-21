namespace ClamChart.Tests.Rendering;

public sealed class LttbDownsamplerTests
{
    [Fact]
    public void ShortRunsAreKept()
    {
        // Arrange
        float[] along = [0, 1, 2];
        float[] across = [5, 6, 7];
        float[] baseline = [0, 0, 0];

        // Act
        var count = LttbDownsampler.Reduce(along, across, baseline, 3);

        // Assert
        Assert.Equal(3, count);
        Assert.Equal([5f, 6f, 7f], across);
    }

    [Fact]
    public void LongRunsKeepTwoPointsPerUnitWithTheEndsAndThePeak()
    {
        // Arrange
        const int Count = 1000;
        var along = Enumerable.Range(0, Count).Select(static i => i * 0.1f).ToArray();
        var across = new float[Count];
        var baseline = Enumerable.Repeat(9f, Count).ToArray();
        across[500] = 50;

        // Act
        var count = LttbDownsampler.Reduce(along, across, baseline, Count);

        // Assert
        Assert.Equal(200, count);
        Assert.Equal(0f, along[0]);
        Assert.Equal(99.9f, along[count - 1], 3);
        Assert.Contains(50f, across.Take(count));
        Assert.All(baseline.Take(count), static x => Assert.Equal(9f, x));
        Assert.All(Enumerable.Range(1, count - 1), i => Assert.True(along[i] > along[i - 1]));
    }

    [Fact]
    public void GapsStaySeparated()
    {
        // Arrange
        const int Count = 601;
        var along = Enumerable.Range(0, Count).Select(static i => i * 0.01f).ToArray();
        var across = Enumerable.Range(0, Count).Select(static i => i == 300 ? Single.NaN : (float)Math.Sin(i)).ToArray();
        var baseline = new float[Count];

        // Act
        var count = LttbDownsampler.Reduce(along, across, baseline, Count);

        // Assert
        var gaps = Enumerable.Range(0, count).Where(i => Single.IsNaN(across[i])).ToArray();
        Assert.Single(gaps);
        Assert.Equal(6, gaps[0]);
        Assert.Equal(13, count);
    }
}
