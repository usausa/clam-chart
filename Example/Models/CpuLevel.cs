namespace Example.Models;

public sealed class CpuLevel
{
    public static ChartAccessorCollection<CpuLevel> Accessors { get; } = new()
    {
        { "host", static x => x.Host },
        { "time", static x => x.Time },
        { "level", static x => x.Level }
    };

    public required string Host { get; init; }

    public DateTime Time { get; init; }

    public double Level { get; init; }
}
