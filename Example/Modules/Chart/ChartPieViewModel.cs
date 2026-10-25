namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartPieViewModel : ChartViewModelBase
{
    private static readonly string[] Categories = ["食費", "住居", "光熱", "通信", "交通", "娯楽", "医療", "教育", "衣服", "交際", "保険", "雑費"];

    private int updates;

    private int nextCategory = 8;

    public ObservableCollection<ChartEntry> Expenses { get; } = [];

    [ObservableProperty]
    public partial double InnerRadius { get; set; } = 0.6;

    [ObservableProperty]
    public partial double StartAngle { get; set; }

    [ObservableProperty]
    public partial double SweepAngle { get; set; } = 360;

    [ObservableProperty]
    public partial ChartPieLabelsMode LabelsMode { get; set; }

    [ObservableProperty]
    public partial int ShapeIndex { get; set; }

    public IObserveCommand SelectShapeCommand { get; }

    public IObserveCommand SelectLabelsCommand { get; }

    [ObservableProperty]
    public partial ChartEntry? SelectedExpense { get; set; }

    public ChartPieViewModel()
    {
        for (var i = 0; i < nextCategory; i++)
        {
            Expenses.Add(Create(Categories[i]));
        }

        SelectShapeCommand = MakeDelegateCommand<int>(SelectShape);
        SelectLabelsCommand = MakeDelegateCommand<ChartPieLabelsMode>(x => LabelsMode = x);
    }

    protected override Task OnNotifyFunction4()
    {
        updates++;
        if (updates % 3 == 0)
        {
            Expenses.RemoveAt(1);
            Expenses.Add(Create(Categories[nextCategory % Categories.Length]));
            nextCategory++;
        }
        else
        {
            for (var i = 0; i < Expenses.Count; i++)
            {
                Expenses[i] = Create(Expenses[i].Label ?? String.Empty);
            }
        }

        return Task.CompletedTask;
    }

    private static ChartEntry Create(string label) => new(label, Math.Round(SampleData.Next(30, 500)) * 100);

    private void SelectShape(int index)
    {
        ShapeIndex = index;
        InnerRadius = index == 1 ? 0 : 0.6;
        StartAngle = index == 2 ? 270 : 0;
        SweepAngle = index == 2 ? 180 : 360;
    }
}
