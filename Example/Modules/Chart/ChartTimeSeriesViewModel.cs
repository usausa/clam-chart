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

    [ObservableProperty]
    public partial int RangeIndex { get; set; }

    public IObserveCommand SelectRangeCommand { get; }

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
        SelectRangeCommand = MakeDelegateCommand<int>(SelectRange);
        ShowRange(TimeSpan.FromDays(1));
    }

    private void SelectRange(int index)
    {
        RangeIndex = index;
        ShowRange(TimeSpan.FromDays(index switch { 0 => 1, 1 => 7, _ => Days }));
    }

    private void ShowRange(TimeSpan span)
    {
        Minimum = end - span;
        Maximum = end;
    }
}
