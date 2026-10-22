namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartStateTimelineViewModel : ChartViewModelBase
{
    private const int ServerHours = 24;

    private const int CpuHours = 6;

    private const int CpuStepMinutes = 10;

    private static readonly string[] Servers = ["web-1", "web-2", "db-1", "cache"];

    private static readonly string[] Hosts = ["app-1", "app-2"];

    public ObservableCollection<ServerState> States { get; } = [];

    public ObservableCollection<CpuLevel> Levels { get; } = [];

    [ObservableProperty]
    public partial DateTime Now { get; set; }

    [ObservableProperty]
    public partial ChartValueAlignment AlignValue { get; set; } = ChartValueAlignment.Left;

    [ObservableProperty]
    public partial bool MergeValues { get; set; } = true;

    public ChartStateTimelineViewModel()
    {
        Generate();
    }

    protected override Task OnNotifyFunction2()
    {
        AlignValue = AlignValue switch
        {
            ChartValueAlignment.Left => ChartValueAlignment.Center,
            ChartValueAlignment.Center => ChartValueAlignment.Right,
            _ => ChartValueAlignment.Left
        };
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        MergeValues = !MergeValues;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        Generate();
        return Task.CompletedTask;
    }

    private static string NextState(string current)
    {
        var roll = SampleData.Next(0, 1);
        if (current == "Running")
        {
            return roll < 0.7 ? "Degraded" : "Stopped";
        }

        return roll < 0.8 ? "Running" : "Degraded";
    }

    private void Generate()
    {
        var current = DateTime.Now;
        var now = new DateTime(current.Year, current.Month, current.Day, current.Hour, current.Minute, 0, current.Kind);
        Now = now;
        States.Clear();
        foreach (var server in Servers)
        {
            var time = now.AddHours(-ServerHours);
            var state = "Running";
            while (time < now)
            {
                States.Add(new ServerState { Server = server, Start = time, State = state });
                time = time.AddMinutes(Math.Round(state == "Running" ? SampleData.Next(120, 480) : SampleData.Next(15, 90)));
                state = NextState(state);
            }
        }

        Levels.Clear();
        foreach (var host in Hosts)
        {
            var phase = SampleData.Next(0, Math.Tau);
            var time = now.AddHours(-CpuHours);
            for (var i = 0; time < now; i++)
            {
                var level = 55 + (30 * Math.Sin(phase + (i / 6.0))) + SampleData.Next(-10, 10);
                Levels.Add(new CpuLevel { Host = host, Time = time, Level = Math.Round(Math.Clamp(level, 5, 99)) });
                time = time.AddMinutes(CpuStepMinutes);
            }
        }
    }
}
