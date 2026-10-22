namespace ClamChart.Tests.Data;

internal sealed record TestHeatItem(string? Day, string? Slot, double Hour, double? Value)
{
    public static ChartAccessorCollection<TestHeatItem> Accessors { get; } = new()
    {
        { "day", static x => x.Day },
        { "slot", static x => x.Slot },
        { "hour", static x => x.Hour },
        { "value", static x => x.Value }
    };
}
