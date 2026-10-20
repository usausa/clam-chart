namespace Example.Models;

public sealed class ProductRank
{
    public static ChartAccessorCollection<ProductRank> Accessors { get; } = new()
    {
        { "name", static x => x.Name },
        { "units", static x => x.Units }
    };

    public required string Name { get; init; }

    public double Units { get; init; }
}
