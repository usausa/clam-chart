namespace ClamChart.Tests.Data;

public sealed class ChartChangeLogTests
{
    [Fact]
    public void LogStartsAsAResetAndClears()
    {
        // Arrange
        var log = new ChartChangeLog();

        // Act & Assert
        Assert.True(log.IsReset);
        log.Clear();
        Assert.False(log.IsReset);
        Assert.Empty(log.Changes);
    }

    [Fact]
    public void CollectionChangesAreRecordedWithTheirPositions()
    {
        // Arrange
        var log = new ChartChangeLog();
        log.Clear();
        var collection = new ObservableCollection<int> { 1, 2, 3 };
        collection.CollectionChanged += (_, e) => log.Record(e);
        ChartChange[] expected =
        [
            new(ChartChangeKind.Add, 3, 1),
            new(ChartChangeKind.Remove, 0, 1),
            new(ChartChangeKind.Replace, 1, 1),
            new(ChartChangeKind.Move, 2, 1, 0),
            new(ChartChangeKind.Replace, 1, 1)
        ];

        // Act
        collection.Add(4);
        collection.RemoveAt(0);
        collection[1] = 9;
        collection.Move(0, 2);
        log.RecordItemChanged(1);

        // Assert
        Assert.False(log.IsReset);
        Assert.Equal(expected, log.Changes);
        Assert.Equal(ChartChangeKind.Move, log.Changes[3].Kind);
        Assert.Equal(2, log.Changes[3].Index);
        Assert.Equal(0, log.Changes[3].OldIndex);
        Assert.Equal(-1, log.Changes[0].OldIndex);
    }

    [Fact]
    public void ResetDropsTheRecordedChanges()
    {
        // Arrange
        var log = new ChartChangeLog();
        log.Clear();
        var collection = new ObservableCollection<int> { 1, 2, 3 };
        collection.CollectionChanged += (_, e) => log.Record(e);
        collection.Add(4);

        // Act
        collection.Clear();
        collection.Add(5);

        // Assert
        Assert.True(log.IsReset);
        Assert.Empty(log.Changes);
    }

    [Fact]
    public void TooManyChangesBecomeAReset()
    {
        // Arrange
        var log = new ChartChangeLog();
        log.Clear();

        // Act
        for (var i = 0; i <= ChartChangeLog.Limit; i++)
        {
            log.RecordItemChanged(i);
        }

        // Assert
        Assert.True(log.IsReset);
        Assert.Empty(log.Changes);
    }

    [Fact]
    public void ChangesWithoutAPositionBecomeAReset()
    {
        // Arrange
        var log = new ChartChangeLog();
        log.Clear();

        // Act
        log.Record(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, 1));

        // Assert
        Assert.True(log.IsReset);
    }
}
