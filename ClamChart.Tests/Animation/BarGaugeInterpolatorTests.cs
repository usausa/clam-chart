namespace ClamChart.Tests.Animation;

using ClamChart.Animation;
using ClamChart.Tests.Layout;

public sealed class BarGaugeInterpolatorTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void CompleteShowsTheRowsAsTheyAre()
    {
        // Arrange
        var interpolator = new BarGaugeInterpolator();

        // Act
        interpolator.Complete(Layout(50, 80));

        // Assert
        Assert.Equal(2, interpolator.Count);
        Assert.Equal(0.8, interpolator.GetRatio(1), Precision);
        Assert.Equal(1, interpolator.GetSlot(1));
        Assert.Equal(1, interpolator.GetAlpha(1));
        Assert.False(interpolator.IsMorphing);
    }

    [Fact]
    public void MatchedRowsMorphFromTheShownRatio()
    {
        // Arrange
        var interpolator = Linear(Layout(50, 50));

        // Act
        var started = interpolator.Begin(Layout(100, 50), [0, 1], false, 2);
        interpolator.Apply(0.5);

        // Assert
        Assert.True(started);
        Assert.True(interpolator.IsMorphing);
        Assert.Equal(0.75, interpolator.GetRatio(0), Precision);
        Assert.Equal(0.5, interpolator.GetRatio(1), Precision);

        // Act
        interpolator.Begin(Layout(0, 50), [0, 1], false, 2);

        // Assert
        Assert.Equal(0.75, interpolator.GetRatio(0), Precision);
        interpolator.Apply(0.5);
        Assert.Equal(0.375, interpolator.GetRatio(0), Precision);
    }

    [Fact]
    public void RemovedRowsShrinkAndFadeWhereTheyWere()
    {
        // Arrange
        var interpolator = Linear(Layout(50, 80));

        // Act
        interpolator.Begin(Layout(80), [-1, 0], false, 1);
        interpolator.Apply(0.5);

        // Assert
        Assert.Equal(2, interpolator.Count);
        Assert.Equal(0.5, interpolator.GetSlot(0), Precision);
        Assert.Equal(0.8, interpolator.GetRatio(0), Precision);
        Assert.Equal(0, interpolator.GetSlot(1));
        Assert.Equal(0.25, interpolator.GetRatio(1), Precision);
        Assert.Equal(0.5, interpolator.GetAlpha(1), Precision);

        // Act
        interpolator.Complete(Layout(80));

        // Assert
        Assert.Equal(1, interpolator.Count);
    }

    [Fact]
    public void AddedRowsGrowFromZeroAndFadeIn()
    {
        // Arrange
        var interpolator = Linear(Layout(50));

        // Act
        interpolator.Begin(Layout(50, 30), [0], false, 2);
        interpolator.Apply(0.5);

        // Assert
        Assert.Equal(2, interpolator.Count);
        Assert.Equal(0.5, interpolator.GetRatio(0), Precision);
        Assert.Equal(0.15, interpolator.GetRatio(1), Precision);
        Assert.Equal(0.5, interpolator.GetAlpha(1), Precision);
        Assert.Equal(1, interpolator.GetSlot(1));
    }

    [Fact]
    public void PositionalMatchingPairsTheRowsByIndex()
    {
        // Arrange
        var interpolator = Linear(Layout(20, 40, 60));

        // Act
        interpolator.Begin(Layout(60, 40), [], true, 2);
        interpolator.Apply(0.5);

        // Assert
        Assert.Equal(3, interpolator.Count);
        Assert.Equal(0.4, interpolator.GetRatio(0), Precision);
        Assert.Equal(0.4, interpolator.GetRatio(1), Precision);
        Assert.Equal(0.3, interpolator.GetRatio(2), Precision);
    }

    [Fact]
    public void UnchangedRowsDoNotStartAMorph()
    {
        // Arrange
        var interpolator = Linear(Layout(50, 80));

        // Act
        var started = interpolator.Begin(Layout(50, 80), [0, 1], false, 2);

        // Assert
        Assert.False(started);
        Assert.False(interpolator.IsMorphing);
        Assert.Equal(1, interpolator.Progress);
    }

    [Fact]
    public void TheEntranceGrowsEachRowAfterTheStagger()
    {
        // Arrange
        var interpolator = new BarGaugeInterpolator();

        // Act
        interpolator.BeginEntrance(Layout(100, 100, 100), ChartEntranceAnimation.Auto, 0.2);
        interpolator.Apply(0.4);

        // Assert
        Assert.Equal(ChartEntranceAnimation.Grow, interpolator.Entrance);
        Assert.Equal(0.5, interpolator.GetRatio(0), Precision);
        Assert.Equal(0.375, interpolator.GetRatio(1), Precision);
        Assert.Equal(0.25, interpolator.GetRatio(2), Precision);
        Assert.Equal(1, interpolator.EntranceAlpha);
        Assert.Equal(0, interpolator.OverlayAlpha);
    }

    [Fact]
    public void RevealAndFadeEntrancesMoveAllRowsTogether()
    {
        // Arrange
        var reveal = new BarGaugeInterpolator();
        var fade = new BarGaugeInterpolator();

        // Act
        reveal.BeginEntrance(Layout(100, 50), ChartEntranceAnimation.Reveal, 0.2);
        reveal.Apply(0.4);
        fade.BeginEntrance(Layout(100, 50), ChartEntranceAnimation.Fade, 0.2);
        fade.Apply(0.4);

        // Assert
        Assert.Equal(0.4, reveal.GetRatio(0), Precision);
        Assert.Equal(0.2, reveal.GetRatio(1), Precision);
        Assert.Equal(1, fade.GetRatio(0), Precision);
        Assert.Equal(0.4, fade.EntranceAlpha, Precision);
    }

    private static BarGaugeInterpolator Linear(BarGaugeLayout layout)
    {
        var interpolator = new BarGaugeInterpolator();
        interpolator.Complete(layout);
        return interpolator;
    }

    private static BarGaugeLayout Layout(params double[] values)
    {
        var entries = values.Select(static (value, index) => new BarGaugeEntry(index, $"C{index}", value, Double.NaN, Double.NaN, null)).ToArray();
        var layout = new BarGaugeLayout();
        layout.Update(300, 200, new Thickness(0), new ChartStyle(), Culture, new BarGaugeLayoutOptions(), entries, Measurer);
        return layout;
    }
}
