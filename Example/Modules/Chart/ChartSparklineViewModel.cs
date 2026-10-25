namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartSparklineViewModel : ChartViewModelBase
{
    private const int RowCount = 60;

    private const int PointCount = 30;

    private readonly IDispatcherTimer? timer;

    public IReadOnlyList<SparklineRow> Rows { get; }

    public ObservableCollection<double> LiveValues { get; }

    [ObservableProperty]
    public partial ChartSparklineKind Kind { get; set; }

    [ObservableProperty]
    public partial ChartSparklineMarkers Markers { get; set; } = ChartSparklineMarkers.Last;

    [ObservableProperty]
    public partial int MarkersIndex { get; set; } = 1;

    public IObserveCommand SelectKindCommand { get; }

    public IObserveCommand SelectMarkersCommand { get; }

    [ObservableProperty]
    public partial string LiveText { get; set; } = String.Empty;

    public ChartSparklineViewModel()
    {
        Rows = [.. Enumerable.Range(1, RowCount).Select(static x => new SparklineRow { Name = $"Item {x:D2}", Values = SampleData.Walk(PointCount, 0, 8, -100), Markers = ChartSparklineMarkers.Last })];
        LiveValues = [.. SampleData.Walk(PointCount, 50, 8, 0)];
        timer = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (timer is not null)
        {
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += OnTick;
        }

        SelectKindCommand = MakeDelegateCommand<ChartSparklineKind>(x => Kind = x);
        SelectMarkersCommand = MakeDelegateCommand<int>(SelectMarkers);
        SubscribeKind(x =>
        {
            foreach (var row in Rows)
            {
                row.Kind = x;
            }
        });
        SubscribeMarkers(x =>
        {
            foreach (var row in Rows)
            {
                row.Markers = x;
            }
        });
        UpdateLiveText();
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

    private void OnTick(object? sender, EventArgs e)
    {
        var last = LiveValues[^1];
        LiveValues.Add(Math.Round(Math.Max(0, last + SampleData.Next(-8, 8)), 1));
        LiveValues.RemoveAt(0);
        UpdateLiveText();
    }

    private void UpdateLiveText()
    {
        LiveText = $"{LiveValues[^1]:N1}";
    }

    private void SelectMarkers(int index)
    {
        MarkersIndex = index;
        Markers = index switch
        {
            1 => ChartSparklineMarkers.Last,
            2 => ChartSparklineMarkers.First | ChartSparklineMarkers.Last,
            3 => ChartSparklineMarkers.High | ChartSparklineMarkers.Low,
            _ => ChartSparklineMarkers.None
        };
    }
}
