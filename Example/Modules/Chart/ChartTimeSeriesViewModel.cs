namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartTimeSeriesViewModel : ChartViewModelBase
{
    private const int Days = 30;

    private readonly DateTime end = DateTime.Today.AddHours(DateTime.Now.Hour);

    public IReadOnlyList<WeatherSample> Samples { get; }

    [ObservableProperty]
    public partial DateTime? Minimum { get; set; }

    [ObservableProperty]
    public partial DateTime? Maximum { get; set; }

    public ChartTimeSeriesViewModel()
    {
        var hours = Days * 24;
        var start = end.AddHours(-hours);
        var drift = 0d;
        var samples = new WeatherSample[hours + 1];
        for (var i = 0; i <= hours; i++)
        {
            var time = start.AddHours(i);
            drift = Math.Clamp(drift + SampleData.Next(-0.3, 0.3), -4, 4);
            var daily = Math.Sin((time.Hour - 9) * Math.PI / 12) * 5;
            samples[i] = new WeatherSample { Time = time, Temperature = Math.Round(18 + daily + drift + SampleData.Next(-0.5, 0.5), 1) };
        }

        Samples = samples;
        ShowRange(TimeSpan.FromDays(1));
    }

    protected override Task OnNotifyFunction2()
    {
        ShowRange(TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        ShowRange(TimeSpan.FromDays(7));
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        ShowRange(TimeSpan.FromDays(Days));
        return Task.CompletedTask;
    }

    private void ShowRange(TimeSpan span)
    {
        Minimum = end - span;
        Maximum = end;
    }
}
