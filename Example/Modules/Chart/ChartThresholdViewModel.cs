namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartThresholdViewModel : ChartViewModelBase
{
    private const int PointCount = 72;

    private const int RowPointCount = 30;

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    private readonly IDispatcherTimer? timer;

    public ObservableCollection<ChartTimePoint> Cpu { get; } = [];

    public IReadOnlyList<ThresholdRow> Servers { get; }

    public IList<ChartThreshold> LoadThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 70, Level = ChartThresholdLevel.Warning },
        new() { Value = 90, Level = ChartThresholdLevel.Critical }
    ];

    public DateTime MaintenanceStart { get; }

    public DateTime MaintenanceEnd { get; }

    public DateTime DeployTime { get; }

    [ObservableProperty]
    public partial double Average { get; set; }

    [ObservableProperty]
    public partial ChartThresholdsMode ThresholdsMode { get; set; } = ChartThresholdsMode.Lines;

    [ObservableProperty]
    public partial int ThresholdIndex { get; set; } = 1;

    public IObserveCommand SelectThresholdCommand { get; }

    public IObserveCommand SelectKindCommand { get; }

    [ObservableProperty]
    public partial ChartSparklineKind Kind { get; set; }

    [ObservableProperty]
    public partial bool IsLine { get; set; } = true;

    [ObservableProperty]
    public partial bool IsArea { get; set; }

    [ObservableProperty]
    public partial bool IsBar { get; set; }

    public ChartThresholdViewModel()
    {
        var end = DateTime.Today.AddHours(DateTime.Now.Hour);
        var start = end - (Interval * (PointCount - 1));
        var values = SampleData.Walk(PointCount, 60, 8, 5);
        for (var i = 0; i < PointCount; i++)
        {
            Cpu.Add(new ChartTimePoint(start + (Interval * i), Math.Min(100, values[i])));
        }

        MaintenanceStart = end.AddHours(-6);
        MaintenanceEnd = end.AddHours(-3);
        DeployTime = end.AddHours(-9);
        Servers =
        [
            new ThresholdRow { Name = "web-01", Values = Walk(50), NormalMinimum = 40, NormalMaximum = 60 },
            new ThresholdRow { Name = "web-02", Values = Walk(50), Emphasis = ChartSparklineEmphasis.Last },
            new ThresholdRow { Name = "db-01", Values = Walk(70), Thresholds = LoadThresholds, ThresholdsMode = ChartThresholdsMode.ColorSeries | ChartThresholdsMode.Lines },
            new ThresholdRow { Name = "batch", Values = Walk(70), Emphasis = ChartSparklineEmphasis.Last, Thresholds = LoadThresholds, ThresholdsMode = ChartThresholdsMode.ColorSeries }
        ];
        timer = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (timer is not null)
        {
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += OnTick;
        }

        SelectThresholdCommand = MakeDelegateCommand<int>(SelectThreshold);
        SelectKindCommand = MakeDelegateCommand<ChartSparklineKind>(x => Kind = x);
        SubscribeKind(x =>
        {
            IsLine = x == ChartSparklineKind.Line;
            IsArea = x == ChartSparklineKind.Area;
            IsBar = x == ChartSparklineKind.Bar;
            foreach (var row in Servers)
            {
                row.Kind = x;
            }
        });
        UpdateAverage();
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

    private static ObservableCollection<double> Walk(double start) =>
        [.. SampleData.Walk(RowPointCount, start, 8, 5).Select(static x => Math.Min(100, x))];

    private static double Next(double last) => Math.Round(Math.Clamp(last + SampleData.Next(-8, 8), 5, 100), 1);

    private void OnTick(object? sender, EventArgs e)
    {
        var last = Cpu[^1];
        Cpu.Add(new ChartTimePoint(last.X + Interval, Next(last.Y)));
        Cpu.RemoveAt(0);
        foreach (var row in Servers)
        {
            row.Push(Next(row.Last));
        }

        UpdateAverage();
    }

    private void UpdateAverage()
    {
        Average = Math.Round(Cpu.Average(static x => x.Y), 1);
    }

    private void SelectThreshold(int index)
    {
        ThresholdIndex = index;
        ThresholdsMode = index switch
        {
            1 => ChartThresholdsMode.Lines,
            2 => ChartThresholdsMode.Bands,
            3 => ChartThresholdsMode.Lines | ChartThresholdsMode.Bands,
            4 => ChartThresholdsMode.ColorSeries,
            _ => ChartThresholdsMode.None
        };
    }
}
