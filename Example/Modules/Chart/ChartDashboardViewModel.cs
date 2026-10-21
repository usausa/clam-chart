namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartDashboardViewModel : ChartViewModelBase
{
    private const int PageCount = 3;

    private const int TrendCount = 24;

    private static readonly string[] Days = ["月", "火", "水", "木", "金", "土", "日"];

    private readonly IDispatcherTimer? timer;

    private int page;

    public ObservableCollection<double> SalesTrend { get; }

    public ObservableCollection<double> ErrorTrend { get; }

    public IList<ChartThreshold> ErrorThresholds { get; } = DashboardSample.CreateErrorThresholds();

    public ObservableCollection<ChartEntry> Goals { get; } = DashboardSample.CreateGoals();

    public ObservableCollection<ChartEntry> Channels { get; } = DashboardSample.CreateChannels();

    public ObservableCollection<ChartEntry> Requests { get; } = DashboardSample.CreateRequests();

    public ObservableCollection<ChartEntry> Storages { get; } = DashboardSample.CreateStorages();

    public ObservableCollection<ChartEntry> StepProgress { get; }

    public ObservableCollection<ChartEntry> Steps { get; }

    public ObservableCollection<ChartEntry> Heart { get; }

    public ObservableCollection<double> HeartRates { get; }

    public ObservableCollection<ChartEntry> Balances { get; }

    public ObservableCollection<double> BalanceTrend { get; }

    public ObservableCollection<ChartEntry> Weeks { get; }

    public ObservableCollection<ChartEntry> Expenses { get; } =
    [
        new("住居", 85_000),
        new("食費", 48_000),
        new("娯楽", 18_000),
        new("交通", 12_000),
        new("その他", 9_000)
    ];

    [ObservableProperty]
    public partial double Sales { get; set; }

    [ObservableProperty]
    public partial double LastSales { get; set; }

    [ObservableProperty]
    public partial double ErrorRate { get; set; }

    [ObservableProperty]
    public partial double LastErrorRate { get; set; }

    [ObservableProperty]
    public partial string StepText { get; set; } = String.Empty;

    [ObservableProperty]
    public partial double HeartAverage { get; set; }

    [ObservableProperty]
    public partial double Balance { get; set; }

    [ObservableProperty]
    public partial double LastBalance { get; set; }

    [ObservableProperty]
    public partial bool BusinessVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool KitVisible { get; set; }

    [ObservableProperty]
    public partial bool MoneyVisible { get; set; }

    public ChartDashboardViewModel()
    {
        SalesTrend = [.. SampleData.Walk(12, 11_000_000, 900_000, 7_000_000).Select(static x => Math.Round(x))];
        ErrorTrend = [.. SampleData.Walk(TrendCount, 2.5, 0.6, 0.2)];
        var today = Math.Round(SampleData.Next(5_000, 9_000));
        StepProgress = [new ChartEntry("歩数", today)];
        Steps = [.. Days.Select((day, index) => new ChartEntry(day, index == Days.Length - 1 ? today : Math.Round(SampleData.Next(4_000, 12_500))))];
        var rates = Enumerable.Range(0, 24).Select(static hour => Math.Round(GetRestingRate(hour) + SampleData.Next(-4, 4))).ToArray();
        Heart = [.. rates.Select(static (rate, hour) => new ChartEntry($"{hour}:00", rate))];
        HeartRates = [.. rates];
        var start = DateTime.Today.AddDays(-29);
        var balances = SampleData.Walk(30, 820_000, 30_000, 300_000).Select(static x => Math.Round(x)).ToArray();
        Balances = [.. balances.Select((balance, index) => new ChartEntry(start.AddDays(index).ToString("M/d", CultureInfo.CurrentCulture), balance))];
        BalanceTrend = [.. balances];
        Weeks = [.. Enumerable.Range(1, 5).Select(static week => new ChartEntry($"第{week}週", Math.Round(SampleData.Next(250, 600)) * 100))];
        Sales = SalesTrend[^1];
        LastSales = SalesTrend[^2];
        ErrorRate = ErrorTrend[^1];
        LastErrorRate = ErrorTrend[^2];
        Balance = balances[^1];
        LastBalance = balances[0];
        UpdateKit();

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

    protected override Task OnNotifyFunction2()
    {
        page = (page + 1) % PageCount;
        BusinessVisible = page == 0;
        KitVisible = page == 1;
        MoneyVisible = page == 2;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        if (Application.Current is { } application)
        {
            application.UserAppTheme = application.RequestedTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        }

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

    private static double GetRestingRate(int hour) => hour switch
    {
        < 6 => 56,
        < 9 => 68,
        < 18 => 74,
        < 20 => 104,
        _ => 64
    };

    private static double Walk(double value, double step, double minimum, double maximum, int digits) =>
        Math.Round(Math.Clamp(value + SampleData.Next(-step, step), minimum, maximum), digits);

    private static void Stream(ObservableCollection<double> values, double value)
    {
        values.Add(value);
        values.RemoveAt(0);
    }

    private static void Replace(ObservableCollection<ChartEntry> entries, int index, double value) =>
        entries[index] = entries[index] with { Value = value };

    private void OnTick(object? sender, EventArgs e)
    {
        LastSales = Sales;
        Sales = Walk(Sales, 600_000, 7_000_000, 16_000_000, 0);
        SalesTrend[^1] = Sales;
        LastErrorRate = ErrorRate;
        ErrorRate = Walk(ErrorRate, 0.8, 0.2, 8, 1);
        Stream(ErrorTrend, ErrorRate);
        for (var i = 0; i < Goals.Count; i++)
        {
            Replace(Goals, i, Walk(Goals[i].Value, 4, 20, 120, 0));
        }

        for (var i = 0; i < Channels.Count; i++)
        {
            Replace(Channels, i, Walk(Channels[i].Value, 200, 300, 6000, 0));
        }

        Replace(Requests, Requests.Count - 1, Walk(Requests[^1].Value, 400, 800, 6000, 0));
        Replace(Storages, 0, Walk(Storages[0].Value, 8, 100, 500, 0));
        Replace(StepProgress, 0, Math.Min(StepProgress[0].Value + Math.Round(SampleData.Next(0, 120)), 15_000));
        Replace(Steps, Steps.Count - 1, StepProgress[0].Value);
        var rate = Walk(Heart[^1].Value, 6, 52, 140, 0);
        Replace(Heart, Heart.Count - 1, rate);
        Stream(HeartRates, rate);
        Balance = Walk(Balance, 12_000, 300_000, 1_500_000, 0);
        Replace(Balances, Balances.Count - 1, Balance);
        BalanceTrend[^1] = Balance;
        Replace(Weeks, Weeks.Count - 1, Walk(Weeks[^1].Value, 2_000, 10_000, 80_000, 0));
        UpdateKit();
    }

    private void UpdateKit()
    {
        StepText = StepProgress[0].Value.ToString("N0", CultureInfo.CurrentCulture);
        HeartAverage = Math.Round(HeartRates.Average());
    }
}
