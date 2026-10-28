namespace ClamChart.Tests.Data;

public sealed class ChartCollectionWatcherTests
{
    [Fact]
    public void ItemChangesAreObservedWhenWatchingItems()
    {
        // Arrange
        var notifications = 0;
        var watcher = new ChartCollectionWatcher(_ => notifications++, true);
        var items = new ObservableCollection<ChartThreshold> { new() { Value = 60 } };
        watcher.Watch(items);
        watcher.Activate();

        // Act
        items[0].Color = Colors.Red;

        // Assert
        Assert.Equal(1, notifications);

        // Act
        items.Add(new ChartThreshold { Value = 80 });
        items[1].Color = Colors.Blue;

        // Assert
        Assert.Equal(3, notifications);
    }

    [Fact]
    public void ItemChangesAreIgnoredWithoutWatchingItems()
    {
        // Arrange
        var notifications = 0;
        var watcher = new ChartCollectionWatcher(_ => notifications++);
        var items = new ObservableCollection<ChartThreshold> { new() { Value = 60 } };
        watcher.Watch(items);
        watcher.Activate();

        // Act
        items[0].Color = Colors.Red;

        // Assert
        Assert.Equal(0, notifications);
    }

    [Fact]
    public void ItemChangesAreIgnoredAfterDeactivate()
    {
        // Arrange
        var notifications = 0;
        var watcher = new ChartCollectionWatcher(_ => notifications++, true);
        var items = new ObservableCollection<ChartThreshold> { new() { Value = 60 } };
        watcher.Watch(items);
        watcher.Activate();
        watcher.Deactivate();

        // Act
        items[0].Color = Colors.Red;
        items.Add(new ChartThreshold());

        // Assert
        Assert.Equal(0, notifications);
    }
}
