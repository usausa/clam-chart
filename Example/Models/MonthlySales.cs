namespace Example.Models;

public sealed class MonthlySales
{
    public static ChartAccessorCollection<MonthlySales> Accessors { get; } = new()
    {
        { "month", static x => x.Month },
        { "sales", static x => x.Sales },
        { "target", static x => x.Target },
        { "store", static x => x.Store },
        { "online", static x => x.Online },
        { "wholesale", static x => x.Wholesale }
    };

    public required string Month { get; init; }

    public double Sales { get; init; }

    public double Target { get; init; }

    public double Store { get; init; }

    public double Online { get; init; }

    public double Wholesale { get; init; }
}
