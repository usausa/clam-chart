namespace ClamChart.Tests.Layout;

public sealed class StatLayoutTests
{
    private const int Precision = 9;

    private static readonly StatDelta Up = new("+8.2%", 1, StatDeltaTone.Good);

    [Fact]
    public void AutoTurnsHorizontalAtTwoAndAHalfTimesTheHeight()
    {
        // Act
        var square = Layout(339, 136, new StatLayoutOptions { ValueText = "1" });
        var wide = Layout(340, 136, new StatLayoutOptions { ValueText = "1" });
        var forced = Layout(340, 136, new StatLayoutOptions { ValueText = "1", LayoutMode = ChartStatLayoutMode.Vertical });

        // Assert
        Assert.Equal(ChartStatLayoutMode.Vertical, square.Mode);
        Assert.Equal(ChartStatLayoutMode.Horizontal, wide.Mode);
        Assert.Equal(ChartStatLayoutMode.Vertical, forced.Mode);
    }

    [Fact]
    public void VerticalStacksTheTitleTheValueAndTheDelta()
    {
        // Act
        var layout = Layout(200, 200, new StatLayoutOptions { Title = "Sales", ValueText = "12.9M", Unit = "JPY", Delta = Up, Caption = "MoM" });

        // Assert
        Assert.Equal(ChartStatLayoutMode.Vertical, layout.Mode);
        Assert.Equal(new ChartRect(0, 0, 200, 14), layout.TitleBounds);
        Assert.Equal(32, layout.ValueFontSize);
        Assert.Equal(new ChartRect(0, 16, 80, 34), layout.ValueBounds);
        Assert.Equal(16, layout.UnitFontSize);
        Assert.Equal(4, layout.UnitGap);
        Assert.Equal(new ChartRect(84, 16, 24, 34), layout.UnitBounds);
        Assert.Equal(52, layout.ArrowBounds.Y);
        Assert.Equal(7.2, layout.ArrowBounds.Width, Precision);
        Assert.Equal(10.2, layout.DeltaBounds.X, Precision);
        Assert.Equal(30, layout.DeltaBounds.Width, Precision);
        Assert.Equal(46.2, layout.CaptionBounds.X, Precision);
        Assert.Equal(18, layout.CaptionBounds.Width, Precision);
        Assert.False(layout.HasTrend);
    }

    [Fact]
    public void BottomTrendFillsTheRestOfTheHeight()
    {
        // Act
        var layout = Layout(200, 200, new StatLayoutOptions { Title = "Sales", ValueText = "12.9M", Delta = Up, HasTrend = true });

        // Assert
        Assert.Equal(new ChartRect(0, 72, 200, 128), layout.TrendBounds);
    }

    [Fact]
    public void ValueShrinksToTheWidth()
    {
        // Act
        var layout = Layout(60, 200, new StatLayoutOptions { ValueText = "12.9M", Unit = "%" });

        // Assert
        Assert.Equal(20, layout.ValueFontSize);
        Assert.Equal(12, layout.UnitFontSize);
        Assert.True(layout.ValueBounds.Width + layout.UnitGap + layout.UnitBounds.Width <= 60);
    }

    [Fact]
    public void TrendGoesBeforeTheValueFallsBelowTheMinimum()
    {
        // Act
        var layout = Layout(200, 70, new StatLayoutOptions { Title = "Sales", ValueText = "12.9M", Delta = Up, HasTrend = true, LayoutMode = ChartStatLayoutMode.Vertical });

        // Assert
        Assert.False(layout.HasTrend);
        Assert.True(layout.Delta.IsVisible);
        Assert.Equal(32, layout.ValueFontSize);
    }

    [Fact]
    public void DeltaGoesNextAndThenTheUnit()
    {
        // Act
        var withoutDelta = Layout(200, 40, new StatLayoutOptions { Title = "Sales", ValueText = "12.9M", Delta = Up, Caption = "MoM", LayoutMode = ChartStatLayoutMode.Vertical });
        var withoutUnit = Layout(48, 200, new StatLayoutOptions { ValueText = "12.9M", Unit = "JPY" });

        // Assert
        Assert.False(withoutDelta.Delta.IsVisible);
        Assert.Null(withoutDelta.Caption);
        Assert.Equal(22, withoutDelta.ValueFontSize);
        Assert.Null(withoutUnit.Unit);
        Assert.Equal(19, withoutUnit.ValueFontSize);
    }

    [Fact]
    public void NarrowWidthDropsOnlyTheUnit()
    {
        // Act
        var layout = Layout(120, 200, new StatLayoutOptions { Title = "Users", ValueText = "1,454", Unit = "persons/day", Delta = Up, LayoutMode = ChartStatLayoutMode.Horizontal });

        // Assert
        Assert.Null(layout.Unit);
        Assert.True(layout.Delta.IsVisible);
        Assert.Equal(32, layout.ValueFontSize);
    }

    [Fact]
    public void ValueGoesBelowTheMinimumAndThenHides()
    {
        // Act
        var small = Layout(30, 200, new StatLayoutOptions { ValueText = "12.9M", Unit = "JPY" });
        var hidden = Layout(10, 200, new StatLayoutOptions { ValueText = "12.9M" });

        // Assert
        Assert.Equal(12, small.ValueFontSize);
        Assert.Null(small.Unit);
        Assert.Null(hidden.ValueText);
        Assert.Equal(0, hidden.ValueFontSize);
    }

    [Fact]
    public void HorizontalPutsTheValueAtTheTopRight()
    {
        // Act
        var layout = Layout(400, 100, new StatLayoutOptions { Title = "CPU", ValueText = "42.5", Unit = "%", Delta = Up, Caption = "1h", HasTrend = true });

        // Assert
        Assert.Equal(ChartStatLayoutMode.Horizontal, layout.Mode);
        Assert.Equal(32, layout.ValueFontSize);
        Assert.Equal(new ChartRect(400 - 8, 0, 8, 34), layout.UnitBounds);
        Assert.Equal(400 - 8 - 4 - 64, layout.ValueBounds.X, Precision);
        Assert.Equal(0, layout.TitleBounds.X);
        Assert.Equal(400 - 8 - 4 - 64 - 8, layout.TitleBounds.Width, Precision);
        Assert.Equal(400, layout.CaptionBounds.Right, Precision);
        Assert.Equal(36, layout.DeltaBounds.Y, Precision);
        Assert.Equal(new ChartRect(0, 56, 400, 44), layout.TrendBounds);
    }

    [Fact]
    public void HorizontalKeepsRoomForTheTitle()
    {
        // Act
        var layout = Layout(200, 60, new StatLayoutOptions { Title = "Requests per second", ValueText = "12,345,678" });

        // Assert
        Assert.True(layout.ValueBounds.Width <= (200 * (1 - StatLayout.TitleShare)) - StatLayout.ValueGap);
        Assert.True(layout.TitleBounds.Width >= 200 * StatLayout.TitleShare);
    }

    [Fact]
    public void RightTrendTakesFortyPercentOfTheWidth()
    {
        // Act
        var layout = Layout(200, 100, new StatLayoutOptions { ValueText = "12.9M", HasTrend = true, TrendPlacement = ChartTrendPlacement.Right }, new Thickness(10));

        // Assert
        Assert.Equal(new ChartRect(118, 10, 72, 80), layout.TrendBounds);
        Assert.True(layout.ValueBounds.Right <= 118 - StatLayout.TrendGap);
    }

    [Fact]
    public void BackgroundTrendCoversTheBottomEdgeToEdge()
    {
        // Act
        var layout = Layout(200, 100, new StatLayoutOptions { ValueText = "12.9M", HasTrend = true, TrendPlacement = ChartTrendPlacement.Background }, new Thickness(10));

        // Assert
        Assert.Equal(new ChartRect(0, 40, 200, 60), layout.TrendBounds);
        Assert.Equal(32, layout.ValueFontSize);
    }

    [Fact]
    public void CaptionIsShortenedOrDropped()
    {
        // Act
        var shortened = Layout(80, 200, new StatLayoutOptions { ValueText = "1", Delta = Up, Caption = "Previous month" });
        var dropped = Layout(52, 200, new StatLayoutOptions { ValueText = "1", Delta = Up, Caption = "Previous month" });
        var hidden = Layout(20, 200, new StatLayoutOptions { ValueText = "1", Delta = Up });

        // Assert
        Assert.Equal(80, shortened.CaptionBounds.Right, Precision);
        Assert.Null(dropped.Caption);
        Assert.True(dropped.Delta.IsVisible);
        Assert.False(hidden.Delta.IsVisible);
    }

    [Fact]
    public void IconSitsBeforeTheTitleAndTheBandAtTheLeftEdge()
    {
        // Act
        var layout = Layout(200, 120, new StatLayoutOptions { Title = "Errors", ValueText = "3", IconLevel = ChartThresholdLevel.Critical, ShowBand = true }, new Thickness(12));

        // Assert
        Assert.Equal(new ChartRect(12, 12, 12, 14), layout.IconBounds);
        Assert.Equal(28, layout.TitleBounds.X);
        Assert.Equal(new ChartRect(0, 0, 4, 120), layout.BandBounds);
    }

    [Fact]
    public void MissingValueKeepsTheTitle()
    {
        // Act
        var layout = Layout(200, 120, new StatLayoutOptions { Title = "Sales", Unit = "JPY" });

        // Assert
        Assert.Null(layout.ValueText);
        Assert.Null(layout.Unit);
        Assert.Equal(new ChartRect(0, 0, 200, 14), layout.TitleBounds);
    }

    private static StatLayout Layout(double width, double height, StatLayoutOptions options, Thickness padding = default)
    {
        var layout = new StatLayout();
        layout.Update(width, height, padding, ChartStyles.Light, options, new TestTextMeasurer());
        return layout;
    }
}
