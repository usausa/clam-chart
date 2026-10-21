namespace Example.Models;

public static class DashboardSample
{
    public static IList<ChartThreshold> CreateErrorThresholds() =>
    [
        new() { Level = ChartThresholdLevel.Good },
        new() { Value = 3, Level = ChartThresholdLevel.Warning },
        new() { Value = 5, Level = ChartThresholdLevel.Critical }
    ];

    public static ObservableCollection<ChartEntry> CreateGoals() =>
    [
        new("売上", 82),
        new("新規", 64),
        new("継続", 91)
    ];

    public static ObservableCollection<ChartEntry> CreateChannels() =>
    [
        new("Web", 4200),
        new("店舗", 3100),
        new("アプリ", 2600),
        new("その他", 900)
    ];

    public static ObservableCollection<ChartEntry> CreateRequests() =>
        [.. SampleData.Walk(24, 3200, 400, 800).Select(static (value, hour) => new ChartEntry($"{hour}:00", Math.Round(value)))];

    public static ObservableCollection<ChartEntry> CreateStorages() =>
    [
        new("/", 320, Maximum: 500),
        new("/var", 46, Maximum: 64),
        new("/home", 870, Maximum: 1000)
    ];
}
