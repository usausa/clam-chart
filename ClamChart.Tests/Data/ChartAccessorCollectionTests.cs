namespace ClamChart.Tests.Data;

public sealed class ChartAccessorCollectionTests
{
    [Fact]
    public void AccessorsAreRegisteredByKeyWithTheirValueType()
    {
        // Arrange
        IChartAccessorProvider provider = TestSalesItem.Accessors;
        string[] keys = ["month", "sales", "target", "count", "date", "time", "color"];

        // Act & Assert
        Assert.Equal(7, TestSalesItem.Accessors.Count);
        Assert.Equal(keys, TestSalesItem.Accessors);
        Assert.True(TestSalesItem.Accessors.ContainsKey("sales"));
        Assert.Equal(typeof(TestSalesItem), provider.FindAccessor("sales")!.ItemType);
        Assert.Equal(typeof(double), provider.FindAccessor("sales")!.ValueType);
        Assert.Equal(typeof(double?), provider.FindAccessor("target")!.ValueType);
        Assert.Equal(typeof(double), provider.FindAccessor("count")!.ValueType);
        Assert.Equal(ChartValueKind.Time, provider.FindAccessor("date")!.Kind);
        Assert.Equal(ChartValueKind.Time, provider.FindAccessor("time")!.Kind);
        Assert.Equal(ChartValueKind.Text, provider.FindAccessor("month")!.Kind);
        Assert.Equal(ChartValueKind.Color, provider.FindAccessor("color")!.Kind);
        Assert.Null(provider.FindAccessor("unknown"));
    }

    [Fact]
    public void DuplicateKeysAreRejected()
    {
        // Arrange
        var accessors = new ChartAccessorCollection<TestSalesItem>
        {
            { "sales", static x => x.Sales }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => accessors.Add("sales", static x => x.Target));
    }
}
