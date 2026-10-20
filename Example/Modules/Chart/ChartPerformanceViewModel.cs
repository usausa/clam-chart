namespace Example.Modules.Chart;

using System.Diagnostics;

using Example.Models;

public sealed partial class ChartPerformanceViewModel : ChartViewModelBase
{
    private const int PointCount = 1000;

    private const int StreamCount = 60;

    private const int RowCount = 100;

    private const int RowPointCount = 30;

    private const int UpdateCount = 10;

    private const int WarmupSteps = 4;

    private const int IdleSteps = 8;

    private readonly List<(long Time, double Elapsed)> frames = [];

    private readonly IDispatcherTimer? timer;

    private PerformanceScenario scenario;

    private int step;

    private int updates;

    private long idleStart;

    public ObservableCollection<double> Stream { get; }

    public IReadOnlyList<SparklineRow> Rows { get; }

    [ObservableProperty]
    public partial IReadOnlyList<double> First { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<double> Second { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<double> Third { get; set; }

    [ObservableProperty]
    public partial bool LinesVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool StreamVisible { get; set; }

    [ObservableProperty]
    public partial bool ListVisible { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = String.Empty;

    public ChartPerformanceViewModel()
    {
        First = SampleData.Walk(PointCount, 60, 4, 0);
        Second = SampleData.Walk(PointCount, 40, 4, 0);
        Third = SampleData.Walk(PointCount, 20, 4, 0);
        Stream = [.. SampleData.Walk(StreamCount, 50, 8, 0)];
        Rows = [.. Enumerable.Range(1, RowCount).Select(static x => new SparklineRow { Name = $"Item {x:D3}", Values = SampleData.Walk(RowPointCount, 0, 8, -100), Markers = ChartSparklineMarkers.Last })];
        timer = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (timer is not null)
        {
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += OnTick;
        }
    }

    public void Record(ChartFrameEventArgs args)
    {
        frames.Add((Stopwatch.GetTimestamp(), args.ElapsedMilliseconds));
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
        Start(PerformanceScenario.Lines);
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        Start(PerformanceScenario.Stream);
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        Start(PerformanceScenario.List);
        return Task.CompletedTask;
    }

    private static double Percentile(List<double> values, double ratio)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        values.Sort();
        return values[Math.Max(0, (int)Math.Ceiling(ratio * values.Count) - 1)];
    }

    private void Start(PerformanceScenario value)
    {
        scenario = value;
        LinesVisible = value == PerformanceScenario.Lines;
        StreamVisible = value == PerformanceScenario.Stream;
        ListVisible = value == PerformanceScenario.List;
        step = 0;
        updates = 0;
        idleStart = 0;
        frames.Clear();
        Status = Describe();
        timer?.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        step++;
        if (scenario == PerformanceScenario.List)
        {
            if (step % 4 == 0)
            {
                Status = $"{Describe()}{Environment.NewLine}{FormatDraws(frames.Count)}";
            }

            return;
        }

        var interval = scenario == PerformanceScenario.Lines ? 3 : 4;
        var end = WarmupSteps + (UpdateCount * interval);
        if (step == WarmupSteps)
        {
            frames.Clear();
        }

        if ((step >= WarmupSteps) && ((step - WarmupSteps) % interval == 0) && (updates < UpdateCount))
        {
            Update();
            updates++;
        }
        else if ((step >= end) && (idleStart == 0))
        {
            idleStart = Stopwatch.GetTimestamp();
        }
        else if (step >= end + IdleSteps)
        {
            timer?.Stop();
            Status = Summarize();
        }
    }

    private void Update()
    {
        if (scenario == PerformanceScenario.Lines)
        {
            First = SampleData.Walk(PointCount, 60, 4, 0);
            Second = SampleData.Walk(PointCount, 40, 4, 0);
            Third = SampleData.Walk(PointCount, 20, 4, 0);
        }
        else
        {
            Stream.Add(Math.Round(Math.Max(0, Stream[^1] + SampleData.Next(-8, 8)), 1));
            Stream.RemoveAt(0);
        }
    }

    private string Describe() => scenario switch
    {
        PerformanceScenario.Lines => $"Lines 3 x {PointCount:N0} points, {UpdateCount} updates",
        PerformanceScenario.Stream => $"Streaming {StreamCount} points, {UpdateCount} updates",
        _ => $"{RowCount} sparkline rows"
    };

    private string FormatDraws(int count)
    {
        List<double> draws = [];
        var maximum = 0d;
        for (var i = 0; i < count; i++)
        {
            draws.Add(frames[i].Elapsed);
            maximum = Math.Max(maximum, frames[i].Elapsed);
        }

        return $"draws {count}  draw p50 {Percentile(draws, 0.5):F2}  p95 {Percentile(draws, 0.95):F2}  max {maximum:F2} ms";
    }

    private string Summarize()
    {
        var measured = 0;
        while ((measured < frames.Count) && (frames[measured].Time < idleStart))
        {
            measured++;
        }

        return $"{Describe()}{Environment.NewLine}{FormatDraws(measured)}{Environment.NewLine}idle draws {frames.Count - measured}";
    }
}
