namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartOverlayViewModel : ChartViewModelBase
{
    private const int TrendCount = 30;

    private const int SignalCount = 60;

    private static readonly (string Title, double Start, double Step, string Format)[] Metrics =
    [
        ("今月の売上", 820, 40, "{0:N0} 万円"),
        ("アクティブユーザー", 12000, 600, "{0:N0} 人"),
        ("エラー率", 1.2, 0.3, "{0:N2} %")
    ];

    private readonly IDispatcherTimer? timer;
    private int metric;

    public ObservableCollection<double> Trend { get; } = [];

    public ObservableCollection<double> Signal { get; } = [];

    public IReadOnlyList<ChartEntry> Hours { get; } = CreateHours();

    [ObservableProperty]
    public partial string TrendTitle { get; set; } = String.Empty;

    [ObservableProperty]
    public partial string TrendValue { get; set; } = String.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public event EventHandler? SnapshotRequested;

    public ChartOverlayViewModel()
    {
        LoadMetric();
        foreach (var value in SampleData.Walk(SignalCount, 50, 6, 0))
        {
            Signal.Add(value);
        }

        timer = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (timer is not null)
        {
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += OnTick;
        }
    }

    public void SwitchMetric()
    {
        metric = (metric + 1) % Metrics.Length;
        LoadMetric();
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
        IsBusy = !IsBusy;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        SnapshotRequested?.Invoke(this, EventArgs.Empty);
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

    private static ChartEntry[] CreateHours()
    {
        var values = SampleData.Walk(24, 18, 1.5, 5);
        return [.. values.Select(static (value, hour) => new ChartEntry($"{hour}:00", Math.Round(value)))];
    }

    private void LoadMetric()
    {
        var (title, start, step, _) = Metrics[metric];
        Trend.Clear();
        foreach (var value in SampleData.Walk(TrendCount, start, step, 0))
        {
            Trend.Add(value);
        }

        TrendTitle = title;
        UpdateValue();
    }

    private void UpdateValue() => TrendValue = String.Format(CultureInfo.CurrentCulture, Metrics[metric].Format, Trend[^1]);

    private void OnTick(object? sender, EventArgs e)
    {
        var (_, _, step, _) = Metrics[metric];
        Trend.Add(Math.Max(0, Math.Round(Trend[^1] + SampleData.Next(-step, step), 2)));
        Trend.RemoveAt(0);
        UpdateValue();
        Signal.Add(Math.Round(Math.Clamp(Signal[^1] + SampleData.Next(-6, 6), 5, 95), 1));
        Signal.RemoveAt(0);
    }
}
