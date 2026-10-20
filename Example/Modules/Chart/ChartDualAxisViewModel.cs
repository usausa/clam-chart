namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartDualAxisViewModel : ChartViewModelBase
{
    private const int Hours = 48;

    public IReadOnlyList<WeatherSample> Samples { get; }

    [ObservableProperty]
    public partial ChartTickAlignment TickAlignment { get; set; } = ChartTickAlignment.AlignToPrimary;

    [ObservableProperty]
    public partial double? HumidityMinimum { get; set; }

    [ObservableProperty]
    public partial double? HumidityMaximum { get; set; }

    [ObservableProperty]
    public partial double? SoftMinimum { get; set; }

    [ObservableProperty]
    public partial double? SoftMaximum { get; set; }

    public ChartDualAxisViewModel()
    {
        var start = DateTime.Today.AddHours(DateTime.Now.Hour - Hours);
        var samples = new WeatherSample[Hours + 1];
        for (var i = 0; i <= Hours; i++)
        {
            var time = start.AddHours(i);
            var daily = Math.Sin((time.Hour - 9) * Math.PI / 12);
            samples[i] = new WeatherSample
            {
                Time = time,
                Temperature = Math.Round(16 + (daily * 6) + SampleData.Next(-0.6, 0.6), 1),
                Humidity = Math.Round(Math.Clamp(62 - (daily * 18) + SampleData.Next(-4, 4), 0, 100))
            };
        }

        Samples = samples;
    }

    protected override Task OnNotifyFunction2()
    {
        TickAlignment = TickAlignment == ChartTickAlignment.AlignToPrimary ? ChartTickAlignment.Independent : ChartTickAlignment.AlignToPrimary;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        var fixedRange = HumidityMinimum is null;
        HumidityMinimum = fixedRange ? 0 : null;
        HumidityMaximum = fixedRange ? 100 : null;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        var soft = SoftMinimum is null;
        SoftMinimum = soft ? 0 : null;
        SoftMaximum = soft ? 30 : null;
        return Task.CompletedTask;
    }
}
