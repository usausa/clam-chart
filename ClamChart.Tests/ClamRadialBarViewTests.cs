namespace ClamChart.Tests;

using ClamChart.Tests.Data;

public sealed class ClamRadialBarViewTests
{
    [Fact]
    public void DefaultsFollowTheSpecification()
    {
        // Arrange
        using var view = new ClamRadialBarView();

        // Assert
        Assert.Equal(0, view.Minimum);
        Assert.Equal(100, view.Maximum);
        Assert.Equal(0, view.StartAngle);
        Assert.Equal(360, view.SweepAngle);
        Assert.Equal(0.4, view.InnerRadius);
        Assert.Null(view.RingThickness);
        Assert.Equal(4, view.RingSpacing);
        Assert.Equal(ChartCapStyle.Flat, view.CapStyle);
        Assert.Equal(ChartRadialBarMode.Basic, view.DisplayMode);
        Assert.Equal(20, view.SegmentCount);
        Assert.True(view.TrackVisible);
        Assert.Equal(ChartOverflowMode.Clamp, view.OverflowMode);
        Assert.Empty(view.Thresholds);
        Assert.Equal(ChartCenterValueMode.Auto, view.CenterValueMode);
        Assert.Null(view.CenterText);
        Assert.Null(view.CenterLabel);
        Assert.Null(view.LegendVisible);
        Assert.Equal(ChartLegendPosition.Right, view.LegendPosition);
        Assert.Equal(ChartLegendValueMode.None, view.LegendValueMode);
        Assert.Equal(ChartSelectionMode.None, view.SelectionMode);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.True(view.TrackItemChanges);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new ClamRadialBarView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Minimum = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Maximum = Double.NegativeInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.StartAngle = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SweepAngle = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SweepAngle = 361);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.InnerRadius = 0.96);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.RingThickness = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.RingSpacing = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SegmentCount = 0);
        Assert.Throws<ArgumentNullException>(() => view.Thresholds = null!);
        view.RingThickness = 10;
        view.RingThickness = null;
        view.SweepAngle = 220;
        Assert.Equal(220, view.SweepAngle);
    }

    [Fact]
    public void EntriesAreReadWithoutKeys()
    {
        // Arrange
        using var view = Rings([new ChartEntry("Move", 50, Colors.Red, 200), new ChartEntry("Stand", 75)]);

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var rows = view.ChartLayout.Rows;
        Assert.Equal(["Move", "Stand"], rows.Select(static x => x.Label));
        Assert.Equal([0.25, 0.75], rows.Select(static x => x.Ratio));
        Assert.Equal(Colors.Red, rows[0].Color);
        Assert.Equal(2, view.RenderedPointCount);
    }

    [Fact]
    public void KeysAndSelectorsAreRead()
    {
        // Arrange
        using var keys = Rings(new List<TestSalesItem> { new() { Month = "Jan", Sales = 30, Count = 60, Color = Colors.Lime } });
        keys.ValueAccessors = TestSalesItem.Accessors;
        keys.ValueKey = "sales";
        keys.LabelKey = "month";
        keys.MaximumKey = "count";
        keys.ColorKey = "color";
        using var selectors = Rings(new List<TestSalesItem> { new() { Month = "Jan", Sales = 10, Count = 40 } });
        selectors.ValueSelector = static x => ((TestSalesItem)x).Count;
        selectors.LabelSelector = static x => ((TestSalesItem)x).Month + "!";

        // Act
        using var first = keys.Paint(300, 300);
        using var second = selectors.Paint(300, 300);

        // Assert
        Assert.Equal("Jan", keys.ChartLayout.Rows[0].Label);
        Assert.Equal(0.5, keys.ChartLayout.Rows[0].Ratio);
        Assert.Equal(Colors.Lime, keys.ChartLayout.Rows[0].Color);
        Assert.Equal(0.4, selectors.ChartLayout.Rows[0].Ratio, 9);
        Assert.Equal("Jan!", selectors.ChartLayout.Rows[0].Label);
    }

    [Fact]
    public void CollectionChangesUpdateTheRings()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>([new ChartEntry("A", 10)]);
        using var view = Rings(entries);
        view.OnAttached();
        using var first = view.Paint(300, 300);

        // Act
        entries.Add(new ChartEntry("B", 60));
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(2, view.ChartLayout.Rows.Count);
        Assert.Equal(1, view.ChartLayout.Rows[1].Slot);
    }

    [Fact]
    public void ThresholdChangesRedoTheLayout()
    {
        // Arrange
        using var view = Rings([new ChartEntry("A", 10)]);
        view.OnAttached();
        using var first = view.Paint(300, 300);
        var version = view.ChartLayout.Version;

        // Act
        view.Thresholds.Add(new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Critical });
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(version + 1, view.ChartLayout.Version);
    }

    [Fact]
    public void TheLegendFollowsTheDensity()
    {
        // Arrange
        using var view = Rings([new ChartEntry("Move", 50), new ChartEntry("Stand", 75)]);

        // Act
        using var full = view.Paint(400, 240);
        var shown = view.ChartLayout.Legend.IsVisible;
        using var compact = view.Paint(300, 180);
        var hidden = view.ChartLayout.Legend.IsVisible;
        view.LegendVisible = true;
        using var forced = view.Paint(300, 180);

        // Assert
        Assert.True(shown);
        Assert.False(hidden);
        Assert.True(view.ChartLayout.Legend.IsVisible);
    }

    [Fact]
    public void SelectedIndexSelectsTheRingAndTheCenterFollowsIt()
    {
        // Arrange
        using var view = Rings([new ChartEntry("Move", 50), new ChartEntry("Stand", 75)]);
        view.SelectionMode = ChartSelectionMode.Item;

        // Act
        view.SelectedIndex = 1;
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal("75%", view.ChartLayout.GetCenter(1).Value);
        Assert.Equal("50%", view.ChartLayout.Center.Value);
        Assert.Same(view.ItemsSource!.Cast<object>().ElementAt(1), view.SelectedItem);
    }

    [Fact]
    public void DisposedViewIgnoresChanges()
    {
        // Arrange
        var view = Rings([new ChartEntry("A", 10)]);
        using var bitmap = view.Paint(300, 300);

        // Act
        view.Dispose();
        view.ItemsSource = new List<ChartEntry> { new("B", 1) };
        view.SweepAngle = 180;

        // Assert
        Assert.Throws<ObjectDisposedException>(view.Invalidate);
    }

    private static TestClamRadialBarView Rings(IEnumerable items) => new() { ItemsSource = items };

    private static TestClamRadialBarView Rings(List<ChartEntry> entries) => new() { ItemsSource = entries };
}
