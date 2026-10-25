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

    [ObservableProperty]
    public partial int SizeIndex { get; set; }

    public IObserveCommand SelectSizeCommand { get; }

    public ChartDensityViewModel()
    {
        Sales = [.. SampleData.Months.Take(6).Select(static x => new MonthlySales { Month = x, Store = Math.Round(SampleData.Next(300, 700)), Target = 500 })];
        Expenses = [new("Food", 32000), new("Rent", 48000), new("Utilities", 12000), new("Phone", 8000), new("Fun", 15000)];
        SelectSizeCommand = MakeDelegateCommand<int>(SelectSize);
    }

    private void SelectSize(int index)
    {
        SizeIndex = index;
        switch (index)
        {
            case 0:
                Resize(160, 96);
                break;
            case 1:
                Resize(340, 160);
                break;
            default:
                Resize(340, 340);
                break;
        }
    }

    private void Resize(double width, double height)
    {
        ChartWidth = width;
        ChartHeight = height;
    }
}
