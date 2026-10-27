namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartHeatmapViewModel : ChartViewModelBase
{
    private const int Hours = 24;

    private const int WorkDays = 5;

    private const int CalendarDays = 182;

    private const double MissingRatio = 0.08;

    private static readonly string[] Days = ["月", "火", "水", "木", "金", "土", "日"];

    public ObservableCollection<AccessCount> Accesses { get; } = [];

    public ObservableCollection<ChartTimePoint> Activities { get; } = [];

    [ObservableProperty]
    public partial ChartLegendPosition LegendPosition { get; set; } = ChartLegendPosition.Bottom;

    public IObserveCommand SelectLegendCommand { get; }

    public ChartHeatmapViewModel()
    {
        SelectLegendCommand = MakeDelegateCommand<ChartLegendPosition>(x => LegendPosition = x);
        for (var day = 0; day < Days.Length; day++)
        {
            for (var hour = 0; hour < Hours; hour++)
            {
                Accesses.Add(CreateAccess(day, hour));
            }
        }

        var start = DateTime.Today.AddDays(1 - CalendarDays);
        for (var i = 0; i < CalendarDays; i++)
        {
            var date = start.AddDays(i);
            if (SampleData.Next(0, 1) >= MissingRatio)
            {
                Activities.Add(CreateActivity(date));
            }
        }
    }

    protected override Task OnNotifyFunction4()
    {
        for (var i = 0; i < Accesses.Count; i++)
        {
            Accesses[i] = CreateAccess(i / Hours, i % Hours);
        }

        for (var i = 0; i < Activities.Count; i++)
        {
            Activities[i] = CreateActivity(Activities[i].X);
        }

        return Task.CompletedTask;
    }

    private static AccessCount CreateAccess(int day, int hour)
    {
        var weekday = day < WorkDays;
        var peak = Math.Exp(-Math.Pow(hour - (weekday ? 14 : 16), 2) / (weekday ? 18 : 30));
        return new AccessCount { Day = Days[day], Hour = hour, Count = Math.Round(((weekday ? 120 : 60) * peak) + SampleData.Next(0, 20)) };
    }

    private static ChartTimePoint CreateActivity(DateTime date)
    {
        var weekday = date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        return new ChartTimePoint(date, Math.Round(weekday ? SampleData.Next(2, 12) : SampleData.Next(0, 5)));
    }
}
