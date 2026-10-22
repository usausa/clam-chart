namespace Example.Models;

public sealed class AccessCount
{
    public static ChartAccessorCollection<AccessCount> Accessors { get; } = new()
    {
        { "day", static x => x.Day },
        { "hour", static x => x.Hour },
        { "count", static x => x.Count }
    };

    public required string Day { get; init; }

    public double Hour { get; init; }

    public double Count { get; init; }
}
