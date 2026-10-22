namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartPieViewModel : ChartViewModelBase
{
    private static readonly string[] Categories = ["食費", "住居", "光熱", "通信", "交通", "娯楽", "医療", "教育", "衣服", "交際", "保険", "雑費"];

    private int shape;

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
    public partial ChartEntry? SelectedExpense { get; set; }

    public ChartPieViewModel()
    {
        for (var i = 0; i < nextCategory; i++)
        {
            Expenses.Add(Create(Categories[i]));
        }
    }

    protected override Task OnNotifyFunction2()
    {
        shape = (shape + 1) % 3;
        InnerRadius = shape == 1 ? 0 : 0.6;
        StartAngle = shape == 2 ? 270 : 0;
        SweepAngle = shape == 2 ? 180 : 360;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        LabelsMode = LabelsMode switch
        {
            ChartPieLabelsMode.None => ChartPieLabelsMode.Inside,
            ChartPieLabelsMode.Inside => ChartPieLabelsMode.Outside,
            _ => ChartPieLabelsMode.None
        };
        return Task.CompletedTask;
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
}
