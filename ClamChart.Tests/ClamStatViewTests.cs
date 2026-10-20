namespace ClamChart.Tests;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamStatViewTests
{
    [Fact]
    public void DefaultsFollowTheSpecification()
    {
        // Arrange
        using var view = new ClamStatView();
        using var other = new ClamStatView();

        // Act & Assert
        Assert.Equal(0, view.Value);
        Assert.Equal(32, view.ValueFontSize);
        Assert.Equal(16, view.MinValueFontSize);
        Assert.Equal(ChartDeltaMode.Percent, view.DeltaMode);
        Assert.True(view.IncreaseIsGood);
        Assert.Equal(ChartSparklineKind.Area, view.TrendKind);
        Assert.Equal(ChartTrendPlacement.Bottom, view.TrendPlacement);
        Assert.Equal(ChartStatColorMode.None, view.ColorMode);
        Assert.Equal(ChartStatLayoutMode.Auto, view.LayoutMode);
        Assert.Null(view.Fill);
        Assert.Empty(view.Thresholds);
        Assert.NotSame(view.Thresholds, other.Thresholds);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new ClamStatView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ValueFontSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.MinValueFontSize = Double.NaN);
        Assert.Throws<ArgumentNullException>(() => view.Thresholds = null!);
    }

    [Fact]
    public void ValueUsesTheFormatTheConverterAndTheFormatter()
    {
        // Arrange
        using var view = Stat(12900);
        view.Culture = CultureInfo.InvariantCulture;

        // Act
        using var compact = view.Paint(200, 120);
        var first = view.ChartLayout.ValueText;
        view.ValueFormat = "N0";
        using var formatted = view.Paint(200, 120);
        var second = view.ChartLayout.ValueText;
        view.ValueFormatter = static value => $"{value / 1000:0}K yen";
        using var custom = view.Paint(200, 120);

        // Assert
        Assert.Equal("12.9K", first);
        Assert.Equal("12,900", second);
        Assert.Equal("13K yen", view.ChartLayout.ValueText);
    }

    [Fact]
    public void MissingValueShowsTheEmptyTextWithoutUnitOrDelta()
    {
        // Arrange
        using var view = Stat(Double.NaN);
        view.Unit = "JPY";
        view.PreviousValue = 10;
        view.EmptyText = "No data";

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.Equal("No data", view.ChartLayout.ValueText);
        Assert.Null(view.ChartLayout.Unit);
        Assert.False(view.ChartLayout.Delta.IsVisible);
    }

    [Fact]
    public void DeltaComesFromThePreviousValue()
    {
        // Arrange
        using var view = Stat(110);
        view.Culture = CultureInfo.GetCultureInfo("en-US");
        view.PreviousValue = 100;
        view.DeltaCaption = "MoM";

        // Act
        using var bitmap = view.Paint(200, 120);
        var percent = view.ChartLayout.Delta;
        view.DeltaMode = ChartDeltaMode.Absolute;
        view.IncreaseIsGood = false;
        using var absolute = view.Paint(200, 120);

        // Assert
        Assert.Equal("+10.0%", percent.Text);
        Assert.Equal(StatDeltaTone.Good, percent.Tone);
        Assert.Equal("+10", view.ChartLayout.Delta.Text);
        Assert.Equal(StatDeltaTone.Bad, view.ChartLayout.Delta.Tone);
        Assert.Equal("MoM", view.ChartLayout.Caption);
    }

    [Fact]
    public void ThresholdsSetTheStateOnlyWithAColorMode()
    {
        // Arrange
        using var view = Stat(75);
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 70, Level = ChartThresholdLevel.Warning });

        // Act
        using var none = view.Paint(200, 120);
        var plain = view.ChartLayout.IconLevel;
        view.ColorMode = ChartStatColorMode.Indicator;
        using var indicator = view.Paint(200, 120);

        // Assert
        Assert.Equal(ChartThresholdLevel.None, plain);
        Assert.Equal(ChartThresholdLevel.Warning, view.ChartLayout.IconLevel);
        Assert.False(view.ChartLayout.BandBounds.IsEmpty);
    }

    [Fact]
    public void ThresholdChangesUpdateTheState()
    {
        // Arrange
        using var view = Stat(95);
        view.ColorMode = ChartStatColorMode.Indicator;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        using var first = view.Paint(200, 120);

        // Act
        view.Thresholds.Add(new ChartThreshold { Value = 90, Level = ChartThresholdLevel.Critical });
        using var second = view.Paint(200, 120);
        var added = view.ChartLayout.IconLevel;
        view.Thresholds = [new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Serious }];
        using var third = view.Paint(200, 120);

        // Assert
        Assert.Equal(ChartThresholdLevel.Critical, added);
        Assert.Equal(ChartThresholdLevel.Serious, view.ChartLayout.IconLevel);
    }

    [Fact]
    public void PaddingIsTheCardPaddingWithAColorMode()
    {
        // Arrange
        using var view = Stat(1);

        // Act
        using var plain = view.Paint(160, 96);
        var minimal = view.ChartLayout.Content;
        view.ColorMode = ChartStatColorMode.Background;
        using var card = view.Paint(160, 96);
        var padded = view.ChartLayout.Content;
        view.ContentPadding = new Thickness(2);
        using var given = view.Paint(160, 96);

        // Assert
        Assert.Equal(new ChartRect(0, 0, 160, 96), minimal);
        Assert.Equal(new ChartRect(12, 12, 136, 72), padded);
        Assert.Equal(new ChartRect(2, 2, 156, 92), view.ChartLayout.Content);
    }

    [Fact]
    public void TrendReadsTheValues()
    {
        // Arrange
        var values = new ObservableCollection<double> { 1, 2, 3 };
        using var view = Stat(3);
        view.TrendValues = values;

        // Act
        using var first = view.Paint(200, 120);
        var count = view.TrendLayout.Count;
        values.Add(4);
        using var second = view.Paint(200, 120);

        // Assert
        Assert.Equal(3, count);
        Assert.Equal(4, view.TrendLayout.Count);
        Assert.True(view.ChartLayout.HasTrend);
        Assert.Equal(view.ChartLayout.TrendBounds.Width, view.TrendLayout.Bounds.Width, 9);
    }

    [Fact]
    public void TrendNeedsAFiniteValue()
    {
        // Arrange
        using var view = Stat(3);
        view.TrendValues = [Double.NaN, Double.NaN];

        // Act
        using var bitmap = view.Paint(200, 120);

        // Assert
        Assert.False(view.ChartLayout.HasTrend);
    }

    [Fact]
    public void WideTilesUseTheHorizontalLayout()
    {
        // Arrange
        using var view = Stat(42);

        // Act
        using var tall = view.Paint(160, 120);
        var first = view.ChartLayout.Mode;
        using var wide = view.Paint(340, 120);

        // Assert
        Assert.Equal(ChartStatLayoutMode.Vertical, first);
        Assert.Equal(ChartStatLayoutMode.Horizontal, view.ChartLayout.Mode);
    }

    private static TestClamStatView Stat(double value)
    {
        var view = new TestClamStatView { AnimationDriver = new TestAnimationDriver(), Value = value, Title = "Sales", EntranceAnimation = ChartEntranceAnimation.None };
        view.OnAttached();
        return view;
    }
}
