namespace ClamChart.Tests.Animation;

using ClamChart.Animation;

public sealed class ChangeMapTests
{
    [Fact]
    public void AddingAtTheEndAndRemovingTheHeadShiftsThePoints()
    {
        // Arrange
        var log = new ChartChangeLog();
        log.Clear();
        log.Record(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, 4, 3));
        log.Record(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, 1, 0));
        var map = new int[3];

        // Act
        var built = ChangeMap.TryBuild(log, 3, 3, map);

        // Assert
        Assert.True(built);
        Assert.Equal([-1, 0, 1], map);
    }

    [Fact]
    public void InsertingAndReplacingInTheMiddle()
    {
        // Arrange
        var log = new ChartChangeLog();
        log.Clear();
        log.Record(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, 9, 1));
        log.RecordItemChanged(0);
        var map = new int[3];

        // Act
        var built = ChangeMap.TryBuild(log, 3, 4, map);

        // Assert
        Assert.True(built);
        Assert.Equal([0, 2, 3], map);
    }

    [Fact]
    public void MovesAndResetsHaveNoMap()
    {
        // Arrange
        var reset = new ChartChangeLog();
        var moved = new ChartChangeLog();
        moved.Clear();
        moved.Record(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, 1, 2, 0));
        var map = new int[3];

        // Act & Assert
        Assert.False(ChangeMap.TryBuild(reset, 3, 3, map));
        Assert.False(ChangeMap.TryBuild(moved, 3, 3, map));
    }

    [Fact]
    public void MapsAreComposed()
    {
        // Arrange
        var first = new[] { -1, 0, 1 };
        var second = new[] { 1, -1 };

        // Act
        ChangeMap.Compose(first, second);

        // Assert
        Assert.Equal([-1, 1, -1], first);
    }
}
