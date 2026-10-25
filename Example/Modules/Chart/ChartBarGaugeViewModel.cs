namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartBarGaugeViewModel : ChartViewModelBase
{
    private readonly IDispatcherTimer? timer;

    public ObservableCollection<ChartEntry> Stores { get; } =
    [
        new("東京", 92, Target: 85),
        new("大阪", 74, Target: 80),
        new("名古屋", 58, Target: 65),
        new("福岡", 81, Target: 70),
        new("札幌", 43, Target: 50)
    ];

    public ObservableCollection<ChartEntry> Storages { get; } =
    [
        new("/", 320, Maximum: 500),
        new("/var", 46, Maximum: 64),
        new("/home", 870, Maximum: 1000),
        new("/backup", 1200, Maximum: 2000)
    ];

    public ObservableCollection<ChartEntry> Servers { get; } =
    [
        new("web-01", 35),
        new("web-02", 62),
        new("db-01", 88),
        new("batch", 15)
    ];

    public ObservableCollection<ChartEntry> Months { get; } =
    [
        new("1月", 62),
        new("2月", 75),
        new("3月", 81),
        new("4月", 68),
        new("5月", 90),
        new("6月", 77)
    ];

    public IList<ChartThreshold> AchievementThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Critical },
        new() { Value = 50, Level = ChartThresholdLevel.Warning },
        new() { Value = 80, Level = ChartThresholdLevel.Good }
    ];

    public IList<ChartThreshold> UsageThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 70, Level = ChartThresholdLevel.Warning },
        new() { Value = 90, Level = ChartThresholdLevel.Critical }
    ];

    public IList<ChartThreshold> LoadThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 60, Level = ChartThresholdLevel.Warning },
        new() { Value = 85, Level = ChartThresholdLevel.Critical }
    ];

    [ObservableProperty]
    public partial ChartBarGaugeMode DisplayMode { get; set; }

    [ObservableProperty]
    public partial ChartSortMode SortMode { get; set; }

    public IObserveCommand SelectDisplayModeCommand { get; }

    public IObserveCommand SelectSortCommand { get; }

    public ChartBarGaugeViewModel()
    {
        SelectDisplayModeCommand = MakeDelegateCommand<ChartBarGaugeMode>(x => DisplayMode = x);
        SelectSortCommand = MakeDelegateCommand<ChartSortMode>(x => SortMode = x);
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
            var limit = entry.Maximum ?? maximum;
            entries[i] = entry with { Value = Math.Round(Math.Clamp(entry.Value + SampleData.Next(-step * limit, step * limit), minimum, limit)) };
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Walk(Stores, 0.08, 20, 100);
        Walk(Storages, 0.03, 0, 100);
        Walk(Servers, 0.2, 2, 100);
        Walk(Months, 0.1, 30, 100);
    }
}
