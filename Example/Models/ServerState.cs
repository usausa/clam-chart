namespace Example.Models;

public sealed class ServerState
{
    public static ChartAccessorCollection<ServerState> Accessors { get; } = new()
    {
        { "server", static x => x.Server },
        { "start", static x => x.Start },
        { "state", static x => x.State }
    };

    public required string Server { get; init; }

    public DateTime Start { get; init; }

    public required string State { get; init; }
}
