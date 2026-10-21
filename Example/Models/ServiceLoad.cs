namespace Example.Models;

public sealed class ServiceLoad
{
    public static ChartAccessorCollection<ServiceLoad> Accessors { get; } = new()
    {
        { "label", static x => x.Label },
        { "web", static x => x.Web },
        { "api", static x => x.Api },
        { "db", static x => x.Db },
        { "batch", static x => x.Batch },
        { "cache", static x => x.Cache }
    };

    public required string Label { get; init; }

    public double Web { get; init; }

    public double Api { get; init; }

    public double Db { get; init; }

    public double Batch { get; init; }

    public double Cache { get; init; }
}
