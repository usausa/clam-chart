namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartRangeBarViewModel : ChartViewModelBase
{
    private static readonly string[] Days = ["月", "火", "水", "木", "金", "土", "日"];

    public ObservableCollection<DailyTemperature> Week { get; } = [];

    public ObservableCollection<DailyTemperature> Year { get; } = [];

    [ObservableProperty]
    public partial ChartDataLabelsMode LabelsMode { get; set; } = ChartDataLabelsMode.All;

    [ObservableProperty]
    public partial bool TrackVisible { get; set; } = true;

    public ChartRangeBarViewModel()
    {
        foreach (var day in Days)
        {
            Week.Add(CreateDay(day));
        }

        for (var i = 0; i < SampleData.Months.Length; i++)
        {
            Year.Add(CreateMonth(i));
        }
    }

    protected override Task OnNotifyFunction2()
    {
        LabelsMode = LabelsMode == ChartDataLabelsMode.None ? ChartDataLabelsMode.All : ChartDataLabelsMode.None;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        TrackVisible = !TrackVisible;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        for (var i = 0; i < Week.Count; i++)
        {
            Week[i] = CreateDay(Days[i]);
        }

        for (var i = 0; i < Year.Count; i++)
        {
            Year[i] = CreateMonth(i);
        }

        return Task.CompletedTask;
    }

    private static DailyTemperature CreateDay(string day)
    {
        var low = Math.Round(SampleData.Next(8, 16));
        return new DailyTemperature { Day = day, Low = low, High = low + Math.Round(SampleData.Next(4, 12)) };
    }

    private static DailyTemperature CreateMonth(int month)
    {
        var center = 15 - (11 * Math.Cos((month - 0.5) * Math.PI / 6));
        return new DailyTemperature { Day = SampleData.Months[month], Low = Math.Round(center - SampleData.Next(3, 6)), High = Math.Round(center + SampleData.Next(3, 6)) };
    }
}
