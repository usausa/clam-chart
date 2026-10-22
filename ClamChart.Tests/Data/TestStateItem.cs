namespace ClamChart.Tests.Data;

internal sealed record TestStateItem(string? Server, DateTime Start, string? State, DateTime End = default, double Level = Double.NaN)
{
    public static ChartAccessorCollection<TestStateItem> Accessors { get; } = new()
    {
        { "server", static x => x.Server },
        { "start", static x => x.Start },
        { "end", static x => x.End },
        { "state", static x => x.State },
        { "level", static x => x.Level }
    };
}
