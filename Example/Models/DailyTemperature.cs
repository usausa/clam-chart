namespace Example.Models;

public sealed class DailyTemperature
{
    public static ChartAccessorCollection<DailyTemperature> Accessors { get; } = new()
    {
        { "day", static x => x.Day },
        { "low", static x => x.Low },
        { "high", static x => x.High },
        { "average", static x => x.Average }
    };

    public required string Day { get; init; }

    public double Low { get; init; }

    public double High { get; init; }

    public double Average => Math.Round((Low + High) / 2, 1);
}
