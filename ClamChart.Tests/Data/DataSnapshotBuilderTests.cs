namespace ClamChart.Tests.Data;

public sealed class DataSnapshotBuilderTests
{
    [Fact]
    public void NullSourceIsEmpty()
    {
        // Act
        var snapshot = Build(null);

        // Assert
        Assert.Same(DataSnapshot.Empty, snapshot);
        Assert.Equal(0, snapshot.Count);
    }

    [Fact]
    public void NumbersAreIndexedWithoutKeys()
    {
        // Act
        using var snapshot = Build(new ObservableCollection<double> { 3, 5, 4 });

        // Assert
        Assert.Equal(ChartXKind.Index, snapshot.XKind);
        Assert.Equal([0d, 1, 2], snapshot.X.ToArray());
        Assert.Equal([3d, 5, 4], snapshot.Y.ToArray());
        Assert.Equal(5d, snapshot.GetItem(1));
    }

    [Fact]
    public void NullableNumbersAreMissingWhenNull()
    {
        // Act
        using var snapshot = Build(new List<double?> { 1, null, 3 });

        // Assert
        Assert.Equal(1d, snapshot.Y[0]);
        Assert.True(Double.IsNaN(snapshot.Y[1]));
        Assert.Equal(3d, snapshot.Y[2]);
    }

    [Fact]
    public void BoxedNumbersOfEachNumericTypeAreRead()
    {
        // Arrange
        object?[] values = [1, 2.5f, 3L, 4.5m, (short)5, 6.5, null];

        // Act
        using var snapshot = Build(values);

        // Assert
        Assert.Equal([1d, 2.5, 3, 4.5, 5, 6.5], snapshot.Y[..6].ToArray());
        Assert.True(Double.IsNaN(snapshot.Y[6]));
    }

    [Fact]
    public void ModelsSelectTheKindOfX()
    {
        // Act
        using var points = Build(new[] { new ChartPoint(1.5, 10), new ChartPoint(3, 20) });
        using var times = Build(new[] { new ChartTimePoint(new DateTime(2026, 10, 19, 9, 0, 0), 1) });
        using var entries = Build(new[] { new ChartEntry("Tokyo", 82, Colors.Red), new ChartEntry("Osaka", 58) });

        // Assert
        Assert.Equal(ChartXKind.Number, points.XKind);
        Assert.Equal([1.5, 3], points.X.ToArray());
        Assert.Equal([10d, 20], points.Y.ToArray());
        Assert.Equal(ChartXKind.Time, times.XKind);
        Assert.Equal(new DateTime(2026, 10, 19, 9, 0, 0).Ticks, times.X[0]);
        Assert.Equal(ChartXKind.Category, entries.XKind);
        Assert.Equal("Tokyo", entries.GetCategory(0));
        Assert.Equal("Osaka", entries.GetLabel(1));
        Assert.Equal(Colors.Red, entries.GetColor(0));
        Assert.Null(entries.GetColor(1));
        Assert.Equal([82d, 58], entries.Y.ToArray());
    }

    [Fact]
    public void KeysReadCustomItems()
    {
        // Arrange
        var january = new TestSalesItem { Month = "Jan", Sales = 10, Color = Colors.Red };
        var february = new TestSalesItem { Month = "Feb", Sales = 20, Target = 25 };
        var options = new ChartValueOptions { Accessors = TestSalesItem.Accessors, XKey = "month", YKey = "sales", LabelKey = "month", ColorKey = "color" };

        // Act
        using var snapshot = Build(new[] { january, february }, options);
        using var targets = Build(new[] { january, february }, options with { YKey = "target" });

        // Assert
        Assert.Equal(ChartXKind.Category, snapshot.XKind);
        Assert.Equal("Feb", snapshot.GetCategory(1));
        Assert.Equal("Jan", snapshot.GetLabel(0));
        Assert.Equal(Colors.Red, snapshot.GetColor(0));
        Assert.Equal([10d, 20], snapshot.Y.ToArray());
        Assert.Same(february, snapshot.GetItem(1));
        Assert.True(Double.IsNaN(targets.Y[0]));
        Assert.Equal(25d, targets.Y[1]);
    }

    [Fact]
    public void TimesAreAlignedToTheLocalClock()
    {
        // Arrange
        var utc = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);
        var offset = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.FromHours(3));
        var unspecified = new DateTime(2026, 10, 19, 12, 0, 0);
        var item = new TestSalesItem { Date = utc, Time = offset };
        var options = new ChartValueOptions { Accessors = TestSalesItem.Accessors, YKey = "sales" };

        // Act
        using var dates = Build(new[] { item }, options with { XKey = "date" });
        using var times = Build(new[] { item }, options with { XKey = "time" });
        using var points = Build(new[] { new ChartTimePoint(unspecified, 1) });

        // Assert
        Assert.Equal(ChartXKind.Time, dates.XKind);
        Assert.Equal(utc.ToLocalTime().Ticks, dates.X[0]);
        Assert.Equal(offset.LocalDateTime.Ticks, times.X[0]);
        Assert.Equal(unspecified.Ticks, points.X[0]);
    }

    [Fact]
    public void SelectorsComeBeforeKeysAndModels()
    {
        // Arrange
        var item = new TestSalesItem { Month = "Jan", Sales = 10 };
        var options = new ChartValueOptions
        {
            Accessors = TestSalesItem.Accessors,
            YKey = "sales",
            LabelKey = "month",
            ValueSelector = static value => ((TestSalesItem)value).Sales * 10,
            LabelSelector = static value => $"<{((TestSalesItem)value).Month}>"
        };

        // Act
        using var snapshot = Build(new[] { item }, options);
        using var entries = Build(new[] { new ChartEntry("Tokyo", 82) }, new ChartValueOptions { ValueSelector = static _ => 1 });

        // Assert
        Assert.Equal(100d, snapshot.Y[0]);
        Assert.Equal("<Jan>", snapshot.GetLabel(0));
        Assert.Equal(1d, entries.Y[0]);
        Assert.Equal("Tokyo", entries.GetLabel(0));
    }

    [Fact]
    public void KeyErrorsAreReportedWhenTheReaderIsCreated()
    {
        // Arrange
        var options = new ChartValueOptions { Accessors = TestSalesItem.Accessors };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(static () => new ChartValueReader(new ChartValueOptions { YKey = "sales" }));
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { YKey = "unknown" }));
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { YKey = "month" }));
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { XKey = "color" }));
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { LabelKey = "sales" }));
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { ColorKey = "month" }));
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { MaximumKey = "month" }));
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { TargetKey = "unknown" }));
    }

    [Fact]
    public void MaximumsAndTargetsComeFromEntriesOrKeys()
    {
        // Arrange
        var january = new TestSalesItem { Month = "Jan", Sales = 10, Count = 40, Target = 25 };
        var february = new TestSalesItem { Month = "Feb", Sales = 20 };
        var options = new ChartValueOptions { Accessors = TestSalesItem.Accessors, YKey = "sales", MaximumKey = "count", TargetKey = "target" };

        // Act
        using var entries = Build(new List<ChartEntry> { new("A", 1, Maximum: 5, Target: 3), new("B", 2) });
        using var items = Build(new[] { january, february }, options);
        using var plain = Build(new[] { january }, options with { MaximumKey = null, TargetKey = null });

        // Assert
        Assert.Equal(5, entries.GetMaximum(0));
        Assert.Equal(3, entries.GetTarget(0));
        Assert.True(Double.IsNaN(entries.GetMaximum(1)));
        Assert.True(Double.IsNaN(entries.GetTarget(1)));
        Assert.Equal(40, items.GetMaximum(0));
        Assert.Equal(25, items.GetTarget(0));
        Assert.Equal(0, items.GetMaximum(1));
        Assert.True(Double.IsNaN(items.GetTarget(1)));
        Assert.True(Double.IsNaN(plain.GetMaximum(0)));
        Assert.True(Double.IsNaN(plain.GetTarget(0)));
    }

    [Fact]
    public void LowValuesComeFromKeysOrSelectors()
    {
        // Arrange
        var items = new[] { new TestSalesItem { Month = "Jan", Sales = 30, Count = 10 }, new TestSalesItem { Month = "Feb", Sales = 40, Count = 20 } };
        var options = new ChartValueOptions { Accessors = TestSalesItem.Accessors, XKey = "month", YKey = "sales", LowKey = "count" };

        // Act
        using var keys = Build(items, options);
        using var selectors = Build(items, options with { LowKey = null, LowSelector = static x => ((TestSalesItem)x).Sales - 5 });
        using var plain = Build(items, options with { LowKey = null });

        // Assert
        Assert.Equal([10d, 20], keys.Low.ToArray());
        Assert.Equal([25d, 35], selectors.Low.ToArray());
        Assert.True(plain.Low.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => new ChartValueReader(options with { LowKey = "month" }));
    }

    [Fact]
    public void MissingLowValuesFollowTheEmptyPointMode()
    {
        // Arrange
        var points = new[] { new ChartPoint(0, 10), new ChartPoint(1, 20), new ChartPoint(2, 30) };
        var options = new ChartValueOptions { LowSelector = static x => ((ChartPoint)x).X.Equals(1d) ? null : ((ChartPoint)x).Y - 10 };

        // Act
        using var gap = Build(points, options);
        using var zero = Build(points, options, ChartEmptyPointMode.Zero);
        using var interpolated = Build(points, options, ChartEmptyPointMode.Interpolate);

        // Assert
        Assert.True(Double.IsNaN(gap.Low[1]));
        Assert.Equal([0d, 0, 20], zero.Low.ToArray());
        Assert.Equal([0d, 10, 20], interpolated.Low.ToArray());
    }

    [Fact]
    public void SizesComeFromKeysOrSelectorsAndStayMissing()
    {
        // Arrange
        var items = new[] { new TestSalesItem { Month = "Jan", Sales = 30, Count = 10 }, new TestSalesItem { Month = "Feb", Sales = 40, Count = 20 } };
        var options = new ChartValueOptions { Accessors = TestSalesItem.Accessors, XKey = "month", YKey = "sales", SizeKey = "count" };
        var points = new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) };

        // Act
        using var keys = Build(items, options);
        using var selectors = Build(points, new ChartValueOptions { SizeSelector = static x => ((ChartPoint)x).X.Equals(1d) ? null : 5 }, ChartEmptyPointMode.Zero);

        // Assert
        Assert.Equal([10d, 20], keys.Size.ToArray());
        Assert.Equal(5, selectors.Size[0]);
        Assert.True(Double.IsNaN(selectors.Size[1]));
        Assert.True(keys.Low.IsEmpty);
    }

    [Fact]
    public void UnsupportedOrMismatchedItemsAreReportedWhenBuilding()
    {
        // Arrange
        var options = new ChartValueOptions { Accessors = TestSalesItem.Accessors, YKey = "sales" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(static () => Build(new[] { new TestSalesItem() }));
        Assert.Throws<InvalidOperationException>(() => Build(new[] { new ChartPoint(0, 1) }, options));
        Assert.Throws<InvalidOperationException>(static () => Build(new object[] { new ChartPoint(0, 1), new ChartTimePoint(DateTime.Today, 2) }));
    }

    [Fact]
    public void EnumerablesThatAreNotListsAreRead()
    {
        // Act
        using var snapshot = Build(Enumerable.Range(0, 40).Select(static value => (object)(value * 2)));

        // Assert
        Assert.Equal(40, snapshot.Count);
        Assert.Equal(78d, snapshot.Y[39]);
        Assert.Equal(39d, snapshot.X[39]);
    }

    [Fact]
    public void MissingValuesFollowTheEmptyPointMode()
    {
        // Arrange
        var values = new List<double?> { null, 1, null, null, 4, null };

        // Act
        using var gap = Build(values);
        using var zero = Build(values, mode: ChartEmptyPointMode.Zero);
        using var interpolated = Build(values, mode: ChartEmptyPointMode.Interpolate);

        // Assert
        Assert.True(Double.IsNaN(gap.Y[2]));
        Assert.Equal([0d, 1, 0, 0, 4, 0], zero.Y.ToArray());
        Assert.True(Double.IsNaN(interpolated.Y[0]));
        Assert.Equal([1d, 2, 3, 4], interpolated.Y[1..5].ToArray());
        Assert.True(Double.IsNaN(interpolated.Y[5]));
    }

    [Fact]
    public void InterpolationFollowsTheDistanceOfX()
    {
        // Arrange
        var points = new[] { new ChartPoint(0, 0), new ChartPoint(1, Double.NaN), new ChartPoint(4, 8) };

        // Act
        using var snapshot = Build(points, mode: ChartEmptyPointMode.Interpolate);

        // Assert
        Assert.Equal(2d, snapshot.Y[1]);
    }

    [Fact]
    public void DisposedSnapshotsAreEmpty()
    {
        // Arrange
        var snapshot = Build(new List<double> { 1, 2 });

        // Act
        snapshot.Dispose();
        snapshot.Dispose();

        // Assert
        Assert.Equal(0, snapshot.Count);
        Assert.True(snapshot.Y.IsEmpty);
    }

    private static DataSnapshot Build(IEnumerable? source, ChartValueOptions? options = null, ChartEmptyPointMode mode = ChartEmptyPointMode.Gap) =>
        DataSnapshotBuilder.Build(source, new ChartValueReader(options ?? new ChartValueOptions()), mode);
}
