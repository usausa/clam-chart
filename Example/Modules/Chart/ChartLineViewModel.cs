namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartLineViewModel : ChartViewModelBase
{
    public ObservableCollection<MonthlySales> Sales { get; } = [];

    [ObservableProperty]
    public partial ChartLineInterpolation Interpolation { get; set; }

    [ObservableProperty]
    public partial ChartTrackballMode TrackballMode { get; set; } = ChartTrackballMode.LongPress;

    public IObserveCommand SelectInterpolationCommand { get; }

    public IObserveCommand SelectTrackballCommand { get; }

    [ObservableProperty]
    public partial bool TrackballSnapEnabled { get; set; } = true;

    [ObservableProperty]
    public partial MonthlySales? TrackedSales { get; set; }

    [ObservableProperty]
    public partial string Header { get; set; } = String.Empty;

    public ChartLineViewModel()
    {
        SelectInterpolationCommand = MakeDelegateCommand<ChartLineInterpolation>(x => Interpolation = x);
        SelectTrackballCommand = MakeDelegateCommand<ChartTrackballMode>(SelectTrackball);
        foreach (var month in SampleData.Months)
        {
            Sales.Add(Create(month));
        }

        SubscribeTrackedSales(_ => UpdateHeader());
        UpdateHeader();
    }

    protected override Task OnNotifyFunction4()
    {
        for (var i = 0; i < Sales.Count; i++)
        {
            Sales[i] = Create(Sales[i].Month);
        }

        UpdateHeader();
        return Task.CompletedTask;
    }

    private static MonthlySales Create(string month) =>
        new() { Month = month, Sales = Math.Round(SampleData.Next(800, 1600)), Target = Math.Round(SampleData.Next(1100, 1300)) };

    private void UpdateHeader()
    {
        Header = TrackedSales is { } tracked ? $"{tracked.Month}: {tracked.Sales:N0} / {tracked.Target:N0}" : $"Latest ({Sales[^1].Month}): {Sales[^1].Sales:N0}";
    }

    private void SelectTrackball(ChartTrackballMode mode)
    {
        TrackballSnapEnabled = true;
        TrackballMode = mode;
    }
}
