namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartBarViewModel : ChartViewModelBase
{
    private static readonly string[] Products = ["Coffee", "Tea", "Juice", "Water", "Soda", "Milk", "Cocoa", "Smoothie"];

    private int stack;

    public IReadOnlyList<MonthlySales> Sales { get; }

    public IReadOnlyList<ProductRank> Ranking { get; }

    [ObservableProperty]
    public partial string? StackGroup { get; set; }

    [ObservableProperty]
    public partial ChartStackMode StackMode { get; set; }

    [ObservableProperty]
    public partial ChartSelectionMode SelectionMode { get; set; } = ChartSelectionMode.Item;

    [ObservableProperty]
    public partial ChartDataLabelsMode LabelsMode { get; set; }

    [ObservableProperty]
    public partial MonthlySales? SelectedSales { get; set; }

    public ChartBarViewModel()
    {
        Sales = [.. SampleData.Months.Take(6).Select(static x => new MonthlySales { Month = x, Store = Math.Round(SampleData.Next(300, 700)), Online = Math.Round(SampleData.Next(100, 500)), Wholesale = Math.Round(SampleData.Next(50, 300)) })];
        Ranking = [.. Products.Select(static x => new ProductRank { Name = x, Units = Math.Round(SampleData.Next(20, 400)) }).OrderByDescending(static x => x.Units)];
    }

    protected override Task OnNotifyFunction2()
    {
        stack = (stack + 1) % 3;
        StackGroup = stack == 0 ? null : "total";
        StackMode = stack == 2 ? ChartStackMode.Percent : ChartStackMode.Normal;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        SelectionMode = SelectionMode switch
        {
            ChartSelectionMode.Item => ChartSelectionMode.Category,
            ChartSelectionMode.Category => ChartSelectionMode.None,
            _ => ChartSelectionMode.Item
        };
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        LabelsMode = LabelsMode switch
        {
            ChartDataLabelsMode.None => ChartDataLabelsMode.All,
            ChartDataLabelsMode.All => ChartDataLabelsMode.Last,
            _ => ChartDataLabelsMode.None
        };
        return Task.CompletedTask;
    }
}
