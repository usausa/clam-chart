namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartBarViewModel : ChartViewModelBase
{
    private static readonly string[] Products = ["Cold brew coffee", "Green tea latte", "Orange juice", "Sparkling water", "Ginger ale", "Strawberry milk", "Hot cocoa", "Mango smoothie"];

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
    public partial int StackIndex { get; set; }

    public IObserveCommand SelectStackCommand { get; }

    public IObserveCommand SelectSelectionCommand { get; }

    public IObserveCommand SelectLabelsCommand { get; }

    [ObservableProperty]
    public partial MonthlySales? SelectedSales { get; set; }

    public ChartBarViewModel()
    {
        Sales = [.. SampleData.Months.Take(6).Select(static x => new MonthlySales { Month = x, Store = Math.Round(SampleData.Next(300, 700)), Online = Math.Round(SampleData.Next(100, 500)), Wholesale = Math.Round(SampleData.Next(50, 300)) })];
        Ranking = [.. Products.Select(static x => new ProductRank { Name = x, Units = Math.Round(SampleData.Next(20, 400)) }).OrderByDescending(static x => x.Units)];
        SelectStackCommand = MakeDelegateCommand<int>(SelectStack);
        SelectSelectionCommand = MakeDelegateCommand<ChartSelectionMode>(x => SelectionMode = x);
        SelectLabelsCommand = MakeDelegateCommand<ChartDataLabelsMode>(x => LabelsMode = x);
    }

    private void SelectStack(int index)
    {
        StackIndex = index;
        StackGroup = index == 0 ? null : "total";
        StackMode = index == 2 ? ChartStackMode.Percent : ChartStackMode.Normal;
    }
}
