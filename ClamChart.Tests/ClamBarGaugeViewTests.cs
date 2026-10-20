namespace ClamChart.Tests;

using ClamChart.Tests.Data;

public sealed class ClamBarGaugeViewTests
{
    [Fact]
    public void DefaultsFollowTheSpecification()
    {
        // Arrange
        using var view = new ClamBarGaugeView();

        // Assert
        Assert.Equal(0, view.Minimum);
        Assert.Equal(100, view.Maximum);
        Assert.Equal(ChartOrientation.Horizontal, view.Orientation);
        Assert.Equal(ChartBarGaugeMode.Basic, view.DisplayMode);
        Assert.Equal(12, view.SegmentWidth);
        Assert.Equal(8, view.BarThickness);
        Assert.Equal(12, view.ItemSpacing);
        Assert.Equal(ChartBarGaugeLabelPosition.Top, view.LabelPosition);
        Assert.True(view.ValueVisible);
        Assert.Equal(ChartBarGaugeValueMode.Percent, view.ValueMode);
        Assert.True(view.TrackVisible);
        Assert.True(view.TargetVisible);
        Assert.Empty(view.Thresholds);
        Assert.Null(view.Color);
        Assert.Equal(ChartSortMode.None, view.SortMode);
        Assert.Null(view.MaxItems);
        Assert.True(view.AutoHeight);
        Assert.True(view.TrackItemChanges);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new ClamBarGaugeView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Minimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Maximum = Double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SegmentWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.BarThickness = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ItemSpacing = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.MaxItems = -1);
        Assert.Throws<ArgumentNullException>(() => view.Thresholds = null!);
        view.ItemSpacing = 0;
        view.MaxItems = 0;
        view.MaxItems = null;
        Assert.Equal(0, view.ItemSpacing);
    }

    [Fact]
    public void EntriesAreReadWithoutKeys()
    {
        // Arrange
        using var view = Gauge([new ChartEntry("Tokyo", 82, Target: 90), new ChartEntry("Osaka", 58, Colors.Lime, 200)]);

        // Act
        using var bitmap = view.Paint(300, 120);

        // Assert
        var rows = view.ChartLayout.Rows;
        Assert.Equal(["Tokyo", "Osaka"], rows.Select(static x => x.Label));
        Assert.Equal([0.82, 0.29], rows.Select(static x => Math.Round(x.Ratio, 9)));
        Assert.Equal(0.9, rows[0].TargetRatio, 9);
        Assert.True(Double.IsNaN(rows[1].TargetRatio));
        Assert.Equal(Colors.Lime, rows[1].Color);
        Assert.Equal(2, view.RenderedPointCount);
    }

    [Fact]
    public void KeysReadTheMaximumAndTheTarget()
    {
        // Arrange
        using var view = Gauge(new List<TestSalesItem> { new() { Month = "Jan", Sales = 30, Count = 60, Target = 45 }, new() { Month = "Feb", Sales = 20, Count = 40 } });
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";
        view.LabelKey = "month";
        view.MaximumKey = "count";
        view.TargetKey = "target";

        // Act
        using var bitmap = view.Paint(300, 120);

        // Assert
        var rows = view.ChartLayout.Rows;
        Assert.Equal(["Jan", "Feb"], rows.Select(static x => x.Label));
        Assert.Equal([0.5, 0.5], rows.Select(static x => x.Ratio));
        Assert.Equal(0.75, rows[0].TargetRatio, 9);
        Assert.True(Double.IsNaN(rows[1].TargetRatio));
    }

    [Fact]
    public void SelectorsAreUsedBeforeKeys()
    {
        // Arrange
        using var view = Gauge(new List<TestSalesItem> { new() { Month = "Jan", Sales = 10, Count = 40 } });
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";
        view.ValueSelector = static x => ((TestSalesItem)x).Count;
        view.LabelSelector = static x => ((TestSalesItem)x).Month + "!";

        // Act
        using var bitmap = view.Paint(300, 120);

        // Assert
        Assert.Equal(0.4, view.ChartLayout.Rows[0].Ratio, 9);
        Assert.Equal("Jan!", view.ChartLayout.Rows[0].Label);
    }

    [Fact]
    public void CollectionChangesUpdateTheRows()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>([new ChartEntry("A", 10)]);
        using var view = Gauge(entries);
        view.OnAttached();
        using var first = view.Paint(300, 120);

        // Act
        entries.Add(new ChartEntry("B", 60));
        using var second = view.Paint(300, 120);

        // Assert
        Assert.Equal(2, view.ChartLayout.Rows.Count);
        Assert.Equal(0.6, view.ChartLayout.Rows[1].Ratio, 9);
    }

    [Fact]
    public void ThresholdChangesRedoTheLayout()
    {
        // Arrange
        using var view = Gauge([new ChartEntry("A", 10)]);
        view.OnAttached();
        using var first = view.Paint(300, 120);
        var version = view.ChartLayout.Version;

        // Act
        view.Thresholds.Add(new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Critical });
        using var second = view.Paint(300, 120);

        // Assert
        Assert.Equal(version + 1, view.ChartLayout.Version);
    }

    [Fact]
    public void AutoHeightMeasuresTheRowsThatAreShown()
    {
        // Arrange
        using var view = Gauge([new ChartEntry("A", 10), new ChartEntry("B", 20), new ChartEntry("C", 30)]);

        // Act
        var three = view.MeasureFor(360);
        using var bitmap = view.Paint(360, three.Height);
        view.MaxItems = 1;
        var one = view.MeasureFor(360);

        // Assert
        Assert.Equal(view.ChartLayout.DesiredHeight, three.Height);
        Assert.Equal(16 + ((three.Height - 16 - 24) / 3), one.Height, 9);
    }

    [Fact]
    public void AutoHeightDecidesTheDensityByTheWidthOnly()
    {
        // Arrange
        using var view = Gauge([new ChartEntry("A", 10)]);

        // Act
        using var wide = view.Paint(360, 40);
        var full = view.EffectiveDensity;
        using var medium = view.Paint(300, 40);
        var compact = view.EffectiveDensity;
        using var narrow = view.Paint(150, 400);
        var minimal = view.EffectiveDensity;

        // Assert
        Assert.Equal(ChartDensity.Full, full);
        Assert.Equal(ChartDensity.Compact, compact);
        Assert.Equal(ChartDensity.Minimal, minimal);
        Assert.Equal(new ChartRect(0, 0, 150, 400), view.ChartLayout.Bounds);
    }

    [Fact]
    public void MeasuringUsesThePaddingOfTheWidth()
    {
        // Arrange
        using var view = Gauge([new ChartEntry("A", 10)]);

        // Act
        var full = view.MeasureFor(360);
        var compact = view.MeasureFor(300);
        var minimal = view.MeasureFor(150);
        view.ContentPadding = new Thickness(2);
        var padded = view.MeasureFor(360);

        // Assert
        Assert.Equal(8, full.Height - compact.Height, 9);
        Assert.Equal(8, compact.Height - minimal.Height, 9);
        Assert.Equal(4, padded.Height - minimal.Height, 9);
    }

    [Fact]
    public void FixedHeightsUseBothSidesForTheDensity()
    {
        // Arrange
        using var off = Gauge([new ChartEntry("A", 10)]);
        off.AutoHeight = false;
        using var requested = Gauge([new ChartEntry("A", 10)]);
        requested.HeightRequest = 40;
        using var vertical = Gauge([new ChartEntry("A", 10)]);
        vertical.Orientation = ChartOrientation.Vertical;

        // Act
        using var first = off.Paint(360, 40);
        using var second = requested.Paint(360, 40);
        using var third = vertical.Paint(360, 40);

        // Assert
        Assert.Equal(ChartDensity.Minimal, off.EffectiveDensity);
        Assert.Equal(ChartDensity.Minimal, requested.EffectiveDensity);
        Assert.Equal(ChartDensity.Minimal, vertical.EffectiveDensity);
        Assert.Equal(0, off.MeasureFor(360).Height);
        Assert.Equal(0, vertical.MeasureFor(360).Height);
    }

    [Fact]
    public void DisposedViewIgnoresChanges()
    {
        // Arrange
        var view = Gauge([new ChartEntry("A", 10)]);
        using var bitmap = view.Paint(300, 120);

        // Act
        view.Dispose();
        view.ItemsSource = new List<ChartEntry> { new("B", 1) };
        view.Maximum = 50;

        // Assert
        Assert.Throws<ObjectDisposedException>(view.Invalidate);
    }

    private static TestClamBarGaugeView Gauge(IEnumerable items) => new() { ItemsSource = items };

    private static TestClamBarGaugeView Gauge(List<ChartEntry> entries) => new() { ItemsSource = entries };
}
