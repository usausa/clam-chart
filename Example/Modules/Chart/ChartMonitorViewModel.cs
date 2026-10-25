namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartMonitorViewModel : ChartViewModelBase
{
    private const int CoreCount = 8;

    private const int Capacity = 300;

    private const int HistoryCount = 60;

    private const int TrendCount = 30;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan LongWindow = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan ShortWindow = TimeSpan.FromSeconds(30);

    private readonly IDispatcherTimer? timer;

    public ObservableCollection<MonitorSample> Samples { get; } = [];

    public ObservableCollection<double> CpuTrend { get; } = [];

    public ObservableCollection<double> MemoryTrend { get; } = [];

    public ObservableCollection<double> TemperatureTrend { get; } = [];

    [ObservableProperty]
    public partial double Cpu { get; set; }

    [ObservableProperty]
    public partial double Memory { get; set; } = 41.5;

    [ObservableProperty]
    public partial double Temperature { get; set; }

    [ObservableProperty]
    public partial double RunQueue { get; set; }

    [ObservableProperty]
    public partial TimeSpan Window { get; set; } = LongWindow;

    [ObservableProperty]
    public partial ChartLegendMode LegendMode { get; set; } = ChartLegendMode.Table;

    public bool LongRange
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged(nameof(LongRange));
                Window = value ? LongWindow : ShortWindow;
            }
        }
    }
        = true;

    public IObserveCommand SelectLegendCommand { get; }

    public ChartMonitorViewModel()
    {
        var now = DateTime.Now;
        var time = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Kind) - (Interval * (HistoryCount - 1));
        MonitorSample? previous = null;
        for (var i = 0; i < HistoryCount; i++)
        {
            previous = CreateSample(time + (Interval * i), previous);
            Push(previous);
        }

        SelectLegendCommand = MakeDelegateCommand<ChartLegendMode>(x => LegendMode = x);
        timer = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (timer is not null)
        {
            timer.Interval = Interval;
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

    private static MonitorSample CreateSample(DateTime time, MonitorSample? previous)
    {
        var cores = new double[CoreCount];
        for (var i = 0; i < CoreCount; i++)
        {
            var value = ((previous?.Cores[i] ?? 5) * 0.5) + SampleData.Next(0, 6);
            if (SampleData.Next(0, 1) < 0.05)
            {
                value += SampleData.Next(20, 70);
            }

            cores[i] = Math.Round(Math.Min(100, value), 1);
        }

        var package = Math.Round(Math.Clamp((previous?.Package ?? 45) + SampleData.Next(-1.5, 1.5) + ((cores.Average() - 12) / 20), 38, 85), 1);
        return new MonitorSample
        {
            Time = time,
            Cores = cores,
            Package = package,
            Core0 = Math.Round(package - SampleData.Next(0, 3), 1),
            Core1 = Math.Round(package - SampleData.Next(0, 3), 1),
            Pch = Math.Round(Math.Clamp((previous?.Pch ?? 52) + SampleData.Next(-0.5, 0.5), 48, 56), 1),
            Received = Math.Round(SampleData.Next(0, 30) + (SampleData.Next(0, 1) < 0.05 ? SampleData.Next(200, 800) : 0), 1),
            Sent = Math.Round(SampleData.Next(0, 10) + (SampleData.Next(0, 1) < 0.05 ? SampleData.Next(50, 200) : 0), 1),
            RunQueue = Math.Round(SampleData.Next(0, 2.4) + (SampleData.Next(0, 1) < 0.1 ? 2 : 0))
        };
    }

    private static void PushTrend(ObservableCollection<double> trend, double value)
    {
        trend.Add(value);
        if (trend.Count > TrendCount)
        {
            trend.RemoveAt(0);
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var previous = Samples[^1];
        Push(CreateSample(previous.Time + Interval, previous));
    }

    private void Push(MonitorSample sample)
    {
        Samples.Add(sample);
        if (Samples.Count > Capacity)
        {
            Samples.RemoveAt(0);
        }

        Cpu = Math.Round(sample.Cores.Average(), 1);
        Memory = Math.Round(Math.Clamp(Memory + SampleData.Next(-0.4, 0.4), 38, 48), 1);
        Temperature = sample.Package;
        RunQueue = sample.RunQueue;
        PushTrend(CpuTrend, Cpu);
        PushTrend(MemoryTrend, Memory);
        PushTrend(TemperatureTrend, Temperature);
    }
}
