namespace ClamChart.Tests;

using ClamChart.Tests.Data;

public sealed class ClamPieChartViewTests
{
    [Fact]
    public void DefaultsFollowTheSpecification()
    {
        // Arrange
        using var view = new ClamPieChartView();

        // Assert
        Assert.Equal(0.6, view.InnerRadius);
        Assert.Equal(0, view.StartAngle);
        Assert.Equal(360, view.SweepAngle);
        Assert.Null(view.SliceGap);
        Assert.Equal(ChartSortMode.None, view.SortMode);
        Assert.Equal(8, view.MaxSlices);
        Assert.Equal(0, view.OthersThreshold);
        Assert.Equal("Other", view.OthersLabel);
        Assert.Equal(ChartPieLabelsMode.None, view.LabelsMode);
        Assert.Equal(ChartPieLabelContent.Percent, view.LabelContent);
        Assert.Null(view.LegendVisible);
        Assert.Equal(ChartLegendPosition.Bottom, view.LegendPosition);
        Assert.Equal(ChartLegendValueMode.None, view.LegendValueMode);
        Assert.Equal(ChartCenterValueMode.Auto, view.CenterValueMode);
        Assert.Equal(ChartSelectionMode.None, view.SelectionMode);
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Equal(6, view.ExplodeOffset);
        Assert.True(view.TrackItemChanges);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new ClamPieChartView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.InnerRadius = 0.96);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.InnerRadius = -0.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.InnerRadius = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.StartAngle = Double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SweepAngle = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SweepAngle = 361);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SliceGap = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.MaxSlices = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.OthersThreshold = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.OthersThreshold = -0.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ExplodeOffset = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.CenterValue = Double.NaN);
        Assert.Throws<ArgumentNullException>(() => view.OthersLabel = null!);
        view.InnerRadius = 0;
        view.SweepAngle = 180;
        view.SliceGap = null;
        view.CenterValue = null;
        Assert.Equal(0, view.InnerRadius);
    }

    [Fact]
    public void EntriesAreReadWithoutKeys()
    {
        // Arrange
        using var view = Pie(Entries(10, 30, 60));

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var slices = view.ChartLayout.Slices;
        Assert.Equal(["C0", "C1", "C2"], slices.Select(static x => x.Label));
        Assert.Equal([10d, 30, 60], slices.Select(static x => x.Value));
        Assert.Equal(3, view.RenderedPointCount);
    }

    [Fact]
    public void AccessorsReadTheValueLabelAndColor()
    {
        // Arrange
        using var view = Pie(new List<TestSalesItem> { new() { Month = "Jan", Sales = 10, Color = Colors.Lime }, new() { Month = "Feb", Sales = 30 } });
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";
        view.LabelKey = "month";
        view.ColorKey = "color";

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        var slices = view.ChartLayout.Slices;
        Assert.Equal(["Jan", "Feb"], slices.Select(static x => x.Label));
        Assert.Equal([10d, 30], slices.Select(static x => x.Value));
        Assert.Equal(Colors.Lime, slices[0].Color);
    }

    [Fact]
    public void SelectorsAreUsedBeforeKeys()
    {
        // Arrange
        using var view = Pie(new List<TestSalesItem> { new() { Month = "Jan", Sales = 10, Count = 4 } });
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";
        view.ValueSelector = static x => ((TestSalesItem)x).Count;
        view.LabelSelector = static x => ((TestSalesItem)x).Month + "!";

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(4, view.ChartLayout.Slices[0].Value);
        Assert.Equal("Jan!", view.ChartLayout.Slices[0].Label);
    }

    [Fact]
    public void CollectionChangesUpdateTheSlices()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(10, 30));
        using var view = Pie(entries);
        view.OnAttached();
        using var first = view.Paint(300, 300);

        // Act
        entries.Add(new ChartEntry("C2", 60));
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(3, view.ChartLayout.Slices.Count);
        Assert.Equal(100, view.ChartLayout.Total);
    }

    [Fact]
    public void ItemChangesUpdateTheSlicesWhileTracked()
    {
        // Arrange
        var item = new TestSalesItem { Month = "Jan", Sales = 10 };
        using var view = Pie(new List<TestSalesItem> { item, new() { Month = "Feb", Sales = 30 } });
        view.ValueAccessors = TestSalesItem.Accessors;
        view.ValueKey = "sales";
        view.OnAttached();
        using var first = view.Paint(300, 300);

        // Act
        item.Sales = 70;
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(100, view.ChartLayout.Total);
    }

    [Fact]
    public void NegativeValuesAreReportedOnce()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = Pie(Entries(10, -5));

        // Act
        using var first = view.Paint(300, 300);
        view.InnerRadius = 0.5;
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(1, capture.Count("ignores negative values"));
        Assert.Single(view.ChartLayout.Slices);
    }

    [Fact]
    public void SlicesBeyondThePaletteAreReported()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = Pie(Entries(1, 2, 3));
        view.Palette = [Colors.Red, Colors.Blue];

        // Act
        using var bitmap = view.Paint(300, 300);

        // Assert
        Assert.Equal(1, capture.Count("has no color in the palette"));
    }

    [Fact]
    public void SelectedItemFromTheViewModelSelectsTheSlice()
    {
        // Arrange
        var entries = Entries(10, 30, 60);
        using var view = Pie(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(300, 300);

        // Act
        view.SelectedItem = entries[1];

        // Assert
        Assert.Equal(1, view.SelectedIndex);
        Assert.Equal(1, view.ChartLayout.FindSlice(1));

        // Act
        view.SelectedIndex = 2;

        // Assert
        Assert.Same(entries[2], view.SelectedItem);
    }

    [Fact]
    public void PendingItemIsSelectedWhenItArrivesAndClearedWhenRemoved()
    {
        // Arrange
        var entries = new ObservableCollection<ChartEntry>(Entries(10, 30));
        var pending = new ChartEntry("Late", 20);
        using var view = Pie(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        view.OnAttached();
        using var first = view.Paint(300, 300);

        // Act
        view.SelectedItem = pending;

        // Assert
        Assert.Equal(-1, view.SelectedIndex);
        Assert.Same(pending, view.SelectedItem);

        // Act
        entries.Add(pending);
        using var second = view.Paint(300, 300);

        // Assert
        Assert.Equal(2, view.SelectedIndex);

        // Act
        entries.Remove(pending);
        using var third = view.Paint(300, 300);

        // Assert
        Assert.Null(view.SelectedItem);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void SelectionChangesAreRaisedWithTheCommand()
    {
        // Arrange
        var entries = Entries(10, 30);
        using var view = Pie(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        var events = new List<ChartSelectionChangedEventArgs>();
        var commands = new List<object?>();
        view.SelectionChanged += (_, e) => events.Add(e);
        view.SelectionChangedCommand = new Command(commands.Add);
        using var bitmap = view.Paint(300, 300);

        // Act
        view.SelectedIndex = 1;

        // Assert
        var args = Assert.Single(events);
        Assert.Null(args.OldItem);
        Assert.Same(entries[1], args.NewItem);
        Assert.Equal(-1, args.OldIndex);
        Assert.Equal(1, args.NewIndex);
        Assert.Same(args, Assert.Single(commands));
    }

    [Fact]
    public void SelectionModeNoneClearsTheSelection()
    {
        // Arrange
        var entries = Entries(10, 30);
        using var view = Pie(entries);
        view.SelectionMode = ChartSelectionMode.Item;
        using var bitmap = view.Paint(300, 300);
        view.SelectedIndex = 0;

        // Act
        view.SelectionMode = ChartSelectionMode.None;

        // Assert
        Assert.Null(view.SelectedItem);
        Assert.Equal(-1, view.SelectedIndex);
    }

    [Fact]
    public void FullDensityShowsTheLegendAndMinimalHidesIt()
    {
        // Arrange
        using var full = Pie(Entries(10, 30));
        using var compact = Pie(Entries(10, 30));
        using var forced = Pie(Entries(10, 30));
        using var minimal = Pie(Entries(10, 30));
        forced.LegendVisible = true;
        minimal.LabelsMode = ChartPieLabelsMode.Inside;

        // Act
        using var first = full.Paint(340, 340);
        using var second = compact.Paint(340, 160);
        using var third = forced.Paint(340, 160);
        using var fourth = minimal.Paint(160, 96);

        // Assert
        Assert.True(full.ChartLayout.Legend.IsVisible);
        Assert.False(compact.ChartLayout.Legend.IsVisible);
        Assert.True(forced.ChartLayout.Legend.IsVisible);
        Assert.False(minimal.ChartLayout.Legend.IsVisible);
        Assert.False(minimal.ChartLayout.ShowsLabels);
        Assert.True(minimal.ChartLayout.Center.IsVisible);
        Assert.True(compact.ChartLayout.Center.IsVisible);
    }

    [Fact]
    public void ExplodeMarginIsReservedOnlyWhenSlicesCanBeSelected()
    {
        // Arrange
        using var plain = Pie(Entries(10, 30));
        using var selectable = Pie(Entries(10, 30));
        plain.LegendVisible = false;
        selectable.LegendVisible = false;
        selectable.SelectionMode = ChartSelectionMode.Item;

        // Act
        using var first = plain.Paint(340, 340);
        using var second = selectable.Paint(340, 340);

        // Assert
        Assert.Equal(plain.ChartLayout.OuterRadius - 6, selectable.ChartLayout.OuterRadius, 6);
    }

    [Fact]
    public void CenterFormatFallsBackToTheValueFormat()
    {
        // Arrange
        using var view = Pie(Entries(1000, 234.6));
        view.ValueFormat = "N1";
        view.Culture = CultureInfo.GetCultureInfo("en-US");

        // Act
        using var first = view.Paint(340, 340);
        var inherited = view.ChartLayout.Center.Value;
        view.CenterValueFormat = "{0:N0} total";
        using var second = view.Paint(340, 340);

        // Assert
        Assert.Equal("1,234.6", inherited);
        Assert.Equal("1,235 total", view.ChartLayout.Center.Value);
        Assert.Equal("1,000.0", view.ChartLayout.Slices[0].ValueText);
    }

    [Fact]
    public void DisposedViewIgnoresChanges()
    {
        // Arrange
        var view = Pie(Entries(10, 30));
        using var bitmap = view.Paint(300, 300);

        // Act
        view.Dispose();
        view.ItemsSource = Entries(1);
        view.InnerRadius = 0.2;

        // Assert
        Assert.Throws<ObjectDisposedException>(view.Invalidate);
    }

    private static TestClamPieChartView Pie(IEnumerable items) => new() { ItemsSource = items };

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];
}
