namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartLineViewModel : ChartViewModelBase
{
    public ObservableCollection<MonthlySales> Sales { get; } = [];

    [ObservableProperty]
    public partial ChartLineInterpolation Interpolation { get; set; }

    [ObservableProperty]
    public partial ChartTrackballMode TrackballMode { get; set; } = ChartTrackballMode.LongPress;

    [ObservableProperty]
    public partial bool TrackballSnapEnabled { get; set; } = true;

    [ObservableProperty]
    public partial MonthlySales? TrackedSales { get; set; }

    [ObservableProperty]
    public partial string Header { get; set; } = String.Empty;

    public ChartLineViewModel()
    {
        foreach (var month in SampleData.Months)
        {
            Sales.Add(Create(month));
        }

        SubscribeTrackedSales(_ => UpdateHeader());
        UpdateHeader();
    }

    protected override Task OnNotifyFunction2()
    {
        Interpolation = Interpolation switch
        {
            ChartLineInterpolation.Linear => ChartLineInterpolation.Smooth,
            ChartLineInterpolation.Smooth => ChartLineInterpolation.Step,
            ChartLineInterpolation.Step => ChartLineInterpolation.StepBefore,
            _ => ChartLineInterpolation.Linear
        };
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        if ((TrackballMode == ChartTrackballMode.Touch) && TrackballSnapEnabled)
        {
            TrackballSnapEnabled = false;
            return Task.CompletedTask;
        }

        TrackballSnapEnabled = true;
        TrackballMode = TrackballMode switch
        {
            ChartTrackballMode.LongPress => ChartTrackballMode.Touch,
            ChartTrackballMode.Touch => ChartTrackballMode.None,
            _ => ChartTrackballMode.LongPress
        };
        return Task.CompletedTask;
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
}
