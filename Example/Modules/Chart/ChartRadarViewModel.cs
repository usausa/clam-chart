namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartRadarViewModel : ChartViewModelBase
{
    private const double Spread = 10;

    private const double MaxScore = 100;

    private static readonly string[] Skills = ["性能", "価格", "品質", "デザイン", "サポート", "納期"];

    private static readonly double[] PremiumLevels = [90, 45, 90, 85, 85, 60];

    private static readonly double[] StandardLevels = [70, 70, 70, 65, 70, 75];

    private static readonly double[] LiteLevels = [50, 90, 55, 60, 45, 85];

    public ObservableCollection<SkillScore> Premium { get; } = [];

    public ObservableCollection<SkillScore> Standard { get; } = [];

    public ObservableCollection<SkillScore> Lite { get; } = [];

    [ObservableProperty]
    public partial ChartRadarGridShape GridShape { get; set; }

    [ObservableProperty]
    public partial ChartSelectionMode SelectionMode { get; set; } = ChartSelectionMode.Item;

    public ChartRadarViewModel()
    {
        Fill(Premium, PremiumLevels);
        Fill(Standard, StandardLevels);
        Fill(Lite, LiteLevels);
    }

    protected override Task OnNotifyFunction2()
    {
        GridShape = GridShape == ChartRadarGridShape.Polygon ? ChartRadarGridShape.Circle : ChartRadarGridShape.Polygon;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        SelectionMode = SelectionMode == ChartSelectionMode.Item ? ChartSelectionMode.Category : ChartSelectionMode.Item;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        Update(Premium, PremiumLevels);
        Update(Standard, StandardLevels);
        Update(Lite, LiteLevels);
        return Task.CompletedTask;
    }

    private static void Fill(ObservableCollection<SkillScore> scores, double[] levels)
    {
        for (var i = 0; i < Skills.Length; i++)
        {
            scores.Add(Create(i, levels));
        }
    }

    private static void Update(ObservableCollection<SkillScore> scores, double[] levels)
    {
        for (var i = 0; i < scores.Count; i++)
        {
            scores[i] = Create(i, levels);
        }
    }

    private static SkillScore Create(int index, double[] levels) =>
        new() { Skill = Skills[index], Score = Math.Round(Math.Clamp(levels[index] + SampleData.Next(-Spread, Spread), 0, MaxScore)) };
}
