namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartRadialBarViewModel : ChartViewModelBase
{
    private readonly IDispatcherTimer? timer;

    public ObservableCollection<ChartEntry> Activities { get; } =
    [
        new("ムーブ", 120),
        new("エクササイズ", 58),
        new("スタンド", 75)
    ];

    public ObservableCollection<ChartEntry> Cpu { get; } =
    [
        new("CPU", 72)
    ];

    public ObservableCollection<ChartEntry> Memory { get; } =
    [
        new("メモリ", 64)
    ];

    public ObservableCollection<ChartEntry> Servers { get; } =
    [
        new("web-01", 35),
        new("web-02", 62),
        new("db-01", 88),
        new("batch", 15)
    ];

    public IList<ChartThreshold> LoadThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 60, Level = ChartThresholdLevel.Warning },
        new() { Value = 85, Level = ChartThresholdLevel.Critical }
    ];

    [ObservableProperty]
    public partial ChartRadialBarMode DisplayMode { get; set; }

    [ObservableProperty]
    public partial ChartCapStyle CapStyle { get; set; }

    public IObserveCommand SelectDisplayModeCommand { get; }

    public IObserveCommand SelectCapCommand { get; }

    public ChartRadialBarViewModel()
    {
        SelectDisplayModeCommand = MakeDelegateCommand<ChartRadialBarMode>(x => DisplayMode = x);
        SelectCapCommand = MakeDelegateCommand<ChartCapStyle>(x => CapStyle = x);
        timer = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (timer is not null)
        {
            timer.Interval = TimeSpan.FromSeconds(2);
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

    private static void Walk(ObservableCollection<ChartEntry> entries, double step, double minimum, double maximum)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            entries[i] = entry with { Value = Math.Round(Math.Clamp(entry.Value + SampleData.Next(-step, step), minimum, maximum)) };
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Walk(Activities, 15, 10, 150);
        Walk(Cpu, 12, 5, 100);
        Walk(Memory, 6, 20, 100);
        Walk(Servers, 15, 2, 100);
    }
}
