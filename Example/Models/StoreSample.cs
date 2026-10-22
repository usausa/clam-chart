namespace Example.Models;

public sealed class StoreSample
{
    public static ChartAccessorCollection<StoreSample> Accessors { get; } = new()
    {
        { "sales", static x => x.Sales },
        { "margin", static x => x.Margin },
        { "visitors", static x => x.Visitors }
    };

    public double Sales { get; init; }

    public double Margin { get; init; }

    public double Visitors { get; init; }
}
