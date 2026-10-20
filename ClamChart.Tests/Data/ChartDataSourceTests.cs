namespace ClamChart.Tests.Data;

public sealed class ChartDataSourceTests
{
    [Fact]
    public void ChangesAreObservedOnlyWhileActive()
    {
        // Arrange
        var notifications = 0;
        var source = new ChartDataSource(() => notifications++);
        var items = new ObservableCollection<TestSalesItem> { new() { Sales = 1 } };
        source.SetSource(items);

        // Act
        items.Add(new TestSalesItem { Sales = 2 });

        // Assert
        Assert.Equal(0, notifications);

        // Act
        source.Activate();

        // Assert
        Assert.True(source.Changes.IsReset);

        // Act
        source.Changes.Clear();
        items.Add(new TestSalesItem { Sales = 3 });
        items.RemoveAt(0);

        // Assert
        ChartChange[] expected = [new(ChartChangeKind.Add, 2, 1), new(ChartChangeKind.Remove, 0, 1)];
        Assert.Equal(2, notifications);
        Assert.Equal(expected, source.Changes.Changes);

        // Act
        source.Deactivate();
        items.Add(new TestSalesItem { Sales = 4 });
        items[0].Sales = 9;

        // Assert
        Assert.Equal(2, notifications);
        Assert.Equal(0, source.WatchedItemCount);
    }

    [Fact]
    public void ItemChangesAreRecordedAtEachPosition()
    {
        // Arrange
        var notifications = 0;
        var source = new ChartDataSource(() => notifications++);
        var shared = new TestSalesItem { Sales = 1 };
        var items = new ObservableCollection<TestSalesItem> { shared, new() { Sales = 2 }, shared };
        source.SetSource(items);
        source.Activate();
        source.Changes.Clear();

        // Act
        shared.Sales = 5;

        // Assert
        ChartChange[] expected = [new(ChartChangeKind.Replace, 0, 1), new(ChartChangeKind.Replace, 2, 1)];
        Assert.Equal(2, source.WatchedItemCount);
        Assert.Equal(1, notifications);
        Assert.Equal(expected, source.Changes.Changes);
    }

    [Fact]
    public void ItemChangesAreIgnoredWhenNotTracked()
    {
        // Arrange
        var notifications = 0;
        var source = new ChartDataSource(() => notifications++);
        var items = new ObservableCollection<TestSalesItem> { new() { Sales = 1 } };
        source.SetSource(items);
        source.Activate();
        source.SetTrackItemChanges(false);

        // Act
        items[0].Sales = 2;

        // Assert
        Assert.Equal(0, source.WatchedItemCount);
        Assert.Equal(0, notifications);
    }

    [Fact]
    public void RemovedItemsAreNoLongerWatched()
    {
        // Arrange
        var notifications = 0;
        var source = new ChartDataSource(() => notifications++);
        var removed = new TestSalesItem { Sales = 1 };
        var items = new ObservableCollection<TestSalesItem> { removed, new() { Sales = 2 } };
        source.SetSource(items);
        source.Activate();
        items.Remove(removed);
        source.Changes.Clear();

        // Act
        removed.Sales = 3;

        // Assert
        Assert.Equal(1, source.WatchedItemCount);
        Assert.Equal(1, notifications);
        Assert.Empty(source.Changes.Changes);
    }

    [Fact]
    public void ReplacingTheSourceResetsAndStopsTheOldSource()
    {
        // Arrange
        var notifications = 0;
        var source = new ChartDataSource(() => notifications++);
        var first = new ObservableCollection<TestSalesItem> { new() { Sales = 1 } };
        source.SetSource(first);
        source.Activate();
        source.Changes.Clear();

        // Act
        source.SetSource(new ObservableCollection<TestSalesItem> { new() { Sales = 2 } });
        first.Add(new TestSalesItem { Sales = 3 });
        first[0].Sales = 4;

        // Assert
        Assert.True(source.Changes.IsReset);
        Assert.Equal(0, notifications);
        Assert.Equal(1, source.WatchedItemCount);
    }
}
