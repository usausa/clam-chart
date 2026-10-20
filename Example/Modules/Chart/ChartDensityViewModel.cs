namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartDensityViewModel : ChartViewModelBase
{
    public IReadOnlyList<MonthlySales> Sales { get; }

    public IReadOnlyList<ChartEntry> Expenses { get; }

    [ObservableProperty]
    public partial double ChartWidth { get; set; } = 160;

    [ObservableProperty]
    public partial double ChartHeight { get; set; } = 96;

    public ChartDensityViewModel()
    {
        Sales = [.. SampleData.Months.Take(6).Select(static x => new MonthlySales { Month = x, Store = Math.Round(SampleData.Next(300, 700)), Target = 500 })];
        Expenses = [new("Food", 32000), new("Rent", 48000), new("Utilities", 12000), new("Phone", 8000), new("Fun", 15000)];
    }

    protected override Task OnNotifyFunction2()
    {
        Resize(160, 96);
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        Resize(340, 160);
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        Resize(340, 340);
        return Task.CompletedTask;
    }

    private void Resize(double width, double height)
    {
        ChartWidth = width;
        ChartHeight = height;
    }
}
