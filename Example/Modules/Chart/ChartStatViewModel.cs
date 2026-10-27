namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartStatViewModel : ChartViewModelBase
{
    private const int TrendCount = 24;

    private readonly IDispatcherTimer? timer;

    public ObservableCollection<double> SalesTrend { get; }

    public ObservableCollection<double> ErrorTrend { get; }

    public ObservableCollection<double> CpuTrend { get; }

    public ObservableCollection<double> MemoryTrend { get; }

    public ObservableCollection<double> PowerTrend { get; }

    public ObservableCollection<double> TemperatureTrend { get; }

    public ObservableCollection<double> UserTrend { get; }

    public ObservableCollection<double> RequestTrend { get; }

    public IList<ChartThreshold> SalesThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Critical },
        new() { Value = 8_000_000, Level = ChartThresholdLevel.Warning },
        new() { Value = 10_000_000, Level = ChartThresholdLevel.Good }
    ];

    public IList<ChartThreshold> ErrorThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 3, Level = ChartThresholdLevel.Warning },
        new() { Value = 5, Level = ChartThresholdLevel.Critical }
    ];

    public IList<ChartThreshold> CpuThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 70, Level = ChartThresholdLevel.Warning },
        new() { Value = 90, Level = ChartThresholdLevel.Critical }
    ];

    public IList<ChartThreshold> TemperatureThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 28, Level = ChartThresholdLevel.Warning },
        new() { Value = 31, Level = ChartThresholdLevel.Serious }
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
    public partial double Cpu { get; set; }

    [ObservableProperty]
    public partial double Memory { get; set; }

    [ObservableProperty]
    public partial double Power { get; set; }

    [ObservableProperty]
    public partial double Temperature { get; set; }

    [ObservableProperty]
    public partial double LastTemperature { get; set; }

    [ObservableProperty]
    public partial double Users { get; set; }

    [ObservableProperty]
    public partial double LastUsers { get; set; }

    [ObservableProperty]
    public partial double Requests { get; set; }

    [ObservableProperty]
    public partial ChartStatLayoutMode LayoutMode { get; set; }

    [ObservableProperty]
    public partial ChartStatColorMode SalesColorMode { get; set; } = ChartStatColorMode.Value;

    [ObservableProperty]
    public partial ChartFillMode FillMode { get; set; }

    public IObserveCommand SelectLayoutCommand { get; }

    public IObserveCommand SelectColorCommand { get; }

    public IObserveCommand SelectFillModeCommand { get; }

    public ChartStatViewModel()
    {
        SalesTrend = [.. SampleData.Walk(12, 10_500_000, 800_000, 7_000_000).Select(static x => Math.Round(x))];
        ErrorTrend = [.. SampleData.Walk(TrendCount, 2.5, 0.6, 0.2)];
        CpuTrend = [.. SampleData.Walk(TrendCount, 40, 8, 2).Select(static x => Math.Min(x, 99))];
        MemoryTrend = [.. SampleData.Walk(TrendCount, 55, 3, 20).Select(static x => Math.Min(x, 99))];
        PowerTrend = [.. SampleData.Walk(TrendCount, 140, 12, 60)];
        TemperatureTrend = [.. SampleData.Walk(TrendCount, 26, 0.6, 18)];
        UserTrend = [.. SampleData.Walk(14, 1200, 80, 600).Select(static x => Math.Round(x))];
        RequestTrend = [.. SampleData.Walk(TrendCount, 4200, 300, 1000).Select(static x => Math.Round(x))];
        Sales = SalesTrend[^1];
        LastSales = SalesTrend[^2];
        ErrorRate = ErrorTrend[^1];
        LastErrorRate = ErrorTrend[^2];
        Cpu = CpuTrend[^1];
        Memory = MemoryTrend[^1];
        Power = PowerTrend[^1];
        Temperature = TemperatureTrend[^1];
        LastTemperature = TemperatureTrend[^2];
        Users = UserTrend[^1];
        LastUsers = UserTrend[^2];
        Requests = RequestTrend[^1];
        SelectLayoutCommand = MakeDelegateCommand<ChartStatLayoutMode>(x => LayoutMode = x);
        SelectColorCommand = MakeDelegateCommand<ChartStatColorMode>(x => SalesColorMode = x);
        SelectFillModeCommand = MakeDelegateCommand<ChartFillMode>(x => FillMode = x);
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

    private static double Walk(double value, double step, double minimum, double maximum, int digits) =>
        Math.Round(Math.Clamp(value + SampleData.Next(-step, step), minimum, maximum), digits);

    private static void Stream(ObservableCollection<double> values, double value)
    {
        values.Add(value);
        values.RemoveAt(0);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Sales = Walk(Sales, 700_000, 7_000_000, 16_000_000, 0);
        SalesTrend[^1] = Sales;
        LastErrorRate = ErrorRate;
        ErrorRate = Walk(ErrorRate, 0.8, 0.2, 8, 1);
        Stream(ErrorTrend, ErrorRate);
        Cpu = Walk(Cpu, 12, 2, 99, 1);
        Stream(CpuTrend, Cpu);
        Memory = Walk(Memory, 4, 20, 99, 1);
        Stream(MemoryTrend, Memory);
        Power = Walk(Power, 15, 60, 250, 1);
        Stream(PowerTrend, Power);
        LastTemperature = Temperature;
        Temperature = Walk(Temperature, 0.6, 20, 34, 1);
        Stream(TemperatureTrend, Temperature);
        LastUsers = Users;
        Users = Walk(Users, 100, 600, 2000, 0);
        Stream(UserTrend, Users);
        Requests = Walk(Requests, 350, 1000, 9000, 0);
        Stream(RequestTrend, Requests);
    }
}
