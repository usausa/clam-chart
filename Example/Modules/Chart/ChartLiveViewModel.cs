namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartLiveViewModel : ChartViewModelBase
{
    private const int HistoryCount = 60;

    private const int Capacity = 300;

    private const int TrendCount = 30;

    private static readonly TimeSpan SlowInterval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan FastInterval = TimeSpan.FromMilliseconds(250);

    private static readonly TimeSpan LongWindow = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan ShortWindow = TimeSpan.FromSeconds(15);

    private readonly IDispatcherTimer? timer;

    private double level = 120;

    public ObservableCollection<ChartTimePoint> Requests { get; } = [];

    public ObservableCollection<double> CpuTrend { get; } = [];

    public ObservableCollection<double> MemoryTrend { get; } = [];

    public ObservableCollection<double> ConnectionTrend { get; } = [];

    public IList<ChartThreshold> CpuThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 70, Level = ChartThresholdLevel.Warning },
        new() { Value = 90, Level = ChartThresholdLevel.Critical }
    ];

    [ObservableProperty]
    public partial double Cpu { get; set; } = 35;

    [ObservableProperty]
    public partial double Memory { get; set; } = 520;

    [ObservableProperty]
    public partial double Connections { get; set; } = 18;

    [ObservableProperty]
    public partial TimeSpan Window { get; set; } = LongWindow;

    public ChartLiveViewModel()
    {
        var now = DateTime.Now;
        var time = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Kind) - (SlowInterval * (HistoryCount - 1));
        for (var i = 0; i < HistoryCount; i++)
        {
            Push(time + (SlowInterval * i));
        }

        timer = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (timer is not null)
        {
            timer.Interval = SlowInterval;
            timer.Tick += OnTick;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && (timer is not null))
        {
            timer.Stop();
            timer.Tick -= OnTick;
        }

        base.Dispose(disposing);
    }

    protected override Task OnNotifyFunction2()
    {
        if (timer is not null)
        {
            timer.Interval = timer.Interval == SlowInterval ? FastInterval : SlowInterval;
        }

        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        Window = Window == LongWindow ? ShortWindow : LongWindow;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        if (timer is { IsRunning: true })
        {
            timer.Stop();
        }
        else
        {
            timer?.Start();
        }

        return Task.CompletedTask;
    }

    private static void PushTrend(ObservableCollection<double> trend, double value)
    {
        trend.Add(value);
        if (trend.Count > TrendCount)
        {
            trend.RemoveAt(0);
        }
    }

    private void OnTick(object? sender, EventArgs e) => Push(Requests[^1].X + (timer?.Interval ?? SlowInterval));

    private void Push(DateTime time)
    {
        level = Math.Clamp(level + SampleData.Next(-8, 8), 60, 200);
        var spike = SampleData.Next(0, 1) < 0.05 ? SampleData.Next(40, 120) : 0;
        Requests.Add(new ChartTimePoint(time, Math.Round(level + spike, 1)));
        if (Requests.Count > Capacity)
        {
            Requests.RemoveAt(0);
        }

        Cpu = Math.Round(Math.Clamp(Cpu + SampleData.Next(-12, 12), 2, 99), 1);
        Memory = Math.Round(Math.Clamp(Memory + SampleData.Next(-24, 24), 300, 900));
        Connections = Math.Round(Math.Clamp(Connections + SampleData.Next(-4, 4), 0, 60));
        PushTrend(CpuTrend, Cpu);
        PushTrend(MemoryTrend, Memory);
        PushTrend(ConnectionTrend, Connections);
    }
}
