namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartScatterViewModel : ChartViewModelBase
{
    private const int WebCount = 80;

    private const int ApiCount = 60;

    public ObservableCollection<ChartPoint> Web { get; } = [];

    public ObservableCollection<ChartPoint> Api { get; } = [];

    public IList<ChartThreshold> LatencyThresholds { get; } =
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 300, Level = ChartThresholdLevel.Warning },
        new() { Value = 500, Level = ChartThresholdLevel.Critical }
    ];

    [ObservableProperty]
    public partial ChartMarkerShape MarkerShape { get; set; }

    [ObservableProperty]
    public partial ChartThresholdsMode ThresholdsMode { get; set; }

    public ChartScatterViewModel()
    {
        for (var i = 0; i < WebCount; i++)
        {
            Web.Add(CreateWeb());
        }

        for (var i = 0; i < ApiCount; i++)
        {
            Api.Add(CreateApi());
        }
    }

    protected override Task OnNotifyFunction2()
    {
        MarkerShape = MarkerShape == ChartMarkerShape.Circle ? ChartMarkerShape.Square : ChartMarkerShape.Circle;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        ThresholdsMode = ThresholdsMode switch
        {
            ChartThresholdsMode.None => ChartThresholdsMode.ColorSeries,
            ChartThresholdsMode.ColorSeries => ChartThresholdsMode.ColorSeries | ChartThresholdsMode.Bands,
            _ => ChartThresholdsMode.None
        };
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        for (var i = 0; i < Web.Count; i++)
        {
            Web[i] = CreateWeb();
        }

        for (var i = 0; i < Api.Count; i++)
        {
            Api[i] = CreateApi();
        }

        return Task.CompletedTask;
    }

    private static ChartPoint CreateWeb()
    {
        var cpu = SampleData.Next(5, 95);
        return new ChartPoint(Math.Round(cpu, 1), Math.Round(80 + (cpu * cpu * 0.05) + SampleData.Next(-40, 40)));
    }

    private static ChartPoint CreateApi()
    {
        var cpu = SampleData.Next(5, 95);
        return new ChartPoint(Math.Round(cpu, 1), Math.Round(150 + (cpu * 2.5) + SampleData.Next(-60, 60)));
    }
}
