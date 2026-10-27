namespace ClamChart.Tests;

public sealed class ChartViewStyleTests
{
    [Fact]
    public void SnapshotMatchesTheDefaultStyleWhenNothingIsSet()
    {
        // Arrange
        using var view = new ClamChartView();

        // Act & Assert
        Assert.Equal(new ChartStyle(), view.EffectiveStyle);
    }

    [Fact]
    public void SnapshotMatchesTheAssignedStyle()
    {
        // Arrange
        using var view = new ClamChartView();

        // Act
        view.ApplyStyle(ChartStyles.Dark);

        // Assert
        Assert.Equal(ChartStyles.Dark.PrimaryTextColor, view.EffectiveStyle.PrimaryTextColor);
        Assert.Equal(ChartStyles.Dark.SkeletonColor, view.EffectiveStyle.SkeletonColor);
        Assert.Equal(ChartStyles.Dark.TrackColor, view.EffectiveStyle.TrackColor);
        Assert.Same(ChartStyles.Dark.Palette, view.EffectiveStyle.Palette);
    }

    [Fact]
    public void PropertyOfTheViewWinsOverTheStyle()
    {
        // Arrange
        using var view = new ClamChartView().ApplyStyle(ChartStyles.Dark);

        // Act
        view.PrimaryTextColor = Colors.Red;

        // Assert
        Assert.Equal(Colors.Red, view.EffectiveStyle.PrimaryTextColor);
        Assert.Equal(ChartStyles.Dark.SecondaryTextColor, view.EffectiveStyle.SecondaryTextColor);
    }

    [Fact]
    public void SnapshotFollowsEveryChange()
    {
        // Arrange
        using var view = new ClamChartView();
        var first = view.EffectiveStyle.SegmentGap;

        // Act
        view.SegmentGap = first + 3;
        var changed = view.EffectiveStyle.SegmentGap;
        view.MutedTextColor = Colors.Red;

        // Assert
        Assert.Equal(first + 3, changed);
        Assert.Equal(first + 3, view.EffectiveStyle.SegmentGap);
        Assert.Equal(Colors.Red, view.EffectiveStyle.MutedTextColor);
        Assert.Equal(new ChartStyle().SkeletonColor, view.EffectiveStyle.SkeletonColor);
    }

    [Fact]
    public void PaletteOfTheViewWinsOverTheStyle()
    {
        // Arrange
        using var view = new ClamChartView().ApplyStyle(ChartStyles.Dark);
        Color[] palette = [Colors.Red, Colors.Lime];

        // Act & Assert
        Assert.Same(ChartStyles.Dark.Palette, view.EffectiveStyle.Palette);
        view.Palette = palette;
        Assert.Same(palette, view.EffectiveStyle.Palette);
    }

    [Fact]
    public void InvalidValuesThrowWhenAssigned()
    {
        // Arrange
        using var view = new ClamChartView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.LegendFontSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.TooltipFontSize = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SegmentGap = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.MarkerKnockout = Double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.DimmedOpacity = 1.5);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.CompactSize = new Size(-1, 10));
    }

    [Fact]
    public void ColorsOnlyRedrawWhileSizesRelayout()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 100);
        var updates = view.LayoutUpdates;

        // Act
        view.PrimaryTextColor = Colors.Red;
        view.Paint(200, 100);
        var afterColor = view.LayoutUpdates;
        view.LegendFontSize = 20;
        view.Paint(200, 100);

        // Assert
        Assert.Equal(updates, afterColor);
        Assert.Equal(updates + 1, view.LayoutUpdates);
        Assert.Equal(Colors.Red, view.EffectiveStyle.PrimaryTextColor);
        Assert.Equal(20d, view.EffectiveStyle.LegendFontSize);
    }

    [Fact]
    public void SnapshotIsSharedByEveryChartType()
    {
        // Arrange
        using var pie = new ClamPieChartView();
        using var gauge = new ClamBarGaugeView();

        // Act
        pie.MutedTextColor = Colors.Red;
        gauge.MutedTextColor = Colors.Red;

        // Assert
        Assert.Equal(Colors.Red, pie.EffectiveStyle.MutedTextColor);
        Assert.Equal(Colors.Red, gauge.EffectiveStyle.MutedTextColor);
    }

    [Fact]
    public void ExplicitThemeValuesHaveLightDefaults()
    {
        // Arrange
        using var view = new ClamChartView();
        using var radial = new ClamRadialBarView();
        using var stat = new ClamStatView();

        // Act & Assert
        Assert.Equal(Color.FromArgb("#12000000"), view.TrackColor);
        Assert.Equal(Color.FromArgb("#4D24292E"), radial.UnlitColor);
        Assert.Equal(5d, radial.GradientShift);
        Assert.Equal(Color.FromArgb("#202226"), stat.AccentTextColor);
        Assert.Equal(-0.7d, stat.GradientStrength);
    }

    [Fact]
    public void InvalidExplicitThemeValuesThrow()
    {
        // Arrange
        using var radial = new ClamRadialBarView();
        using var stat = new ClamStatView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => radial.GradientShift = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => stat.GradientStrength = Double.PositiveInfinity);
    }

    [Fact]
    public void GradientShiftDecidesTheDirection()
    {
        // Arrange
        using var light = Rings(70);
        using var dark = Rings(70);
        light.FillMode = ChartFillMode.Gradient;
        dark.FillMode = ChartFillMode.Gradient;
        dark.GradientShift = -15;

        // Act
        using var first = light.Paint(160, 160);
        using var second = dark.Paint(160, 160);

        // Assert
        Assert.NotEqual(first.Bytes, second.Bytes);
    }

    private static TestClamRadialBarView Rings(params double[] values)
    {
        var view = new TestClamRadialBarView
        {
            AnimationDriver = new TestAnimationDriver(),
            ItemsSource = values.Select(static (x, i) => new ChartEntry($"R{i}", x)).ToList(),
            EntranceAnimation = ChartEntranceAnimation.None,
            Density = ChartDensity.Full,
            ContentPadding = new Thickness(0),
            CenterValueMode = ChartCenterValueMode.None,
            LegendVisible = false
        };
        view.OnAttached();
        return view;
    }
}
