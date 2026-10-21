namespace Example.Models;

public sealed class MonitorSample
{
    public static ChartAccessorCollection<MonitorSample> Accessors { get; } = new()
    {
        { "time", static x => x.Time },
        { "cpu0", static x => x.Cores[0] },
        { "cpu1", static x => x.Cores[1] },
        { "cpu2", static x => x.Cores[2] },
        { "cpu3", static x => x.Cores[3] },
        { "cpu4", static x => x.Cores[4] },
        { "cpu5", static x => x.Cores[5] },
        { "cpu6", static x => x.Cores[6] },
        { "cpu7", static x => x.Cores[7] },
        { "package", static x => x.Package },
        { "core0", static x => x.Core0 },
        { "core1", static x => x.Core1 },
        { "pch", static x => x.Pch },
        { "rx", static x => x.Received },
        { "tx", static x => x.Sent },
        { "queue", static x => x.RunQueue }
    };

    public DateTime Time { get; init; }

    public required IReadOnlyList<double> Cores { get; init; }

    public double Package { get; init; }

    public double Core0 { get; init; }

    public double Core1 { get; init; }

    public double Pch { get; init; }

    public double Received { get; init; }

    public double Sent { get; init; }

    public double RunQueue { get; init; }
}
