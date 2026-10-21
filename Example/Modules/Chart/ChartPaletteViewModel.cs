namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartPaletteViewModel : ChartViewModelBase
{
    private static readonly string[] Quarter = ["Q1", "Q2", "Q3", "Q4"];

    public IReadOnlyList<ServiceLoad> Quarters { get; } = [.. Quarter.Select(static x => new ServiceLoad
    {
        Label = x,
        Web = Math.Round(SampleData.Next(30, 70)),
        Api = Math.Round(SampleData.Next(30, 70)),
        Db = Math.Round(SampleData.Next(30, 70)),
        Batch = Math.Round(SampleData.Next(30, 70)),
        Cache = Math.Round(SampleData.Next(30, 70))
    })];

    public IReadOnlyList<ServiceLoad> Months { get; } = CreateMonths();

    public IReadOnlyList<ChartEntry> Shares { get; } =
    [
        new("web", 32),
        new("api", 24),
        new("db", 18),
        new("batch", 14),
        new("cache", 12)
    ];

    [ObservableProperty]
    public partial IReadOnlyList<Color>? Palette { get; set; } = AppPalettes.Material;

    protected override Task OnNotifyFunction2()
    {
        Palette = Palette is null ? AppPalettes.Material : null;
        return Task.CompletedTask;
    }

    private static ServiceLoad[] CreateMonths()
    {
        var web = SampleData.Walk(SampleData.Months.Length, 20, 4, 0);
        var api = SampleData.Walk(SampleData.Months.Length, 35, 4, 0);
        var db = SampleData.Walk(SampleData.Months.Length, 50, 4, 0);
        var batch = SampleData.Walk(SampleData.Months.Length, 65, 4, 0);
        var cache = SampleData.Walk(SampleData.Months.Length, 80, 4, 0);
        return [.. SampleData.Months.Select((month, i) => new ServiceLoad { Label = month, Web = web[i], Api = api[i], Db = db[i], Batch = batch[i], Cache = cache[i] })];
    }
}
