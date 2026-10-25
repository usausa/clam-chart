namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartBubbleViewModel : ChartViewModelBase
{
    private const int CityCount = 12;

    private const int SuburbCount = 10;

    private const double LargeBubbleSize = 40;

    private const double SmallBubbleSize = 24;

    public ObservableCollection<StoreSample> City { get; } = [];

    public ObservableCollection<StoreSample> Suburb { get; } = [];

    [ObservableProperty]
    public partial ChartMarkerShape MarkerShape { get; set; }

    [ObservableProperty]
    public partial double MaxBubbleSize { get; set; } = LargeBubbleSize;

    [ObservableProperty]
    public partial bool SizeIncludesZero { get; set; } = true;

    public bool LargeBubbles
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged(nameof(LargeBubbles));
                MaxBubbleSize = value ? LargeBubbleSize : SmallBubbleSize;
            }
        }
    }
        = true;

    public IObserveCommand SelectShapeCommand { get; }

    public ChartBubbleViewModel()
    {
        SelectShapeCommand = MakeDelegateCommand<ChartMarkerShape>(x => MarkerShape = x);
        for (var i = 0; i < CityCount; i++)
        {
            City.Add(CreateCity());
        }

        for (var i = 0; i < SuburbCount; i++)
        {
            Suburb.Add(CreateSuburb());
        }
    }

    protected override Task OnNotifyFunction4()
    {
        for (var i = 0; i < City.Count; i++)
        {
            City[i] = CreateCity();
        }

        for (var i = 0; i < Suburb.Count; i++)
        {
            Suburb[i] = CreateSuburb();
        }

        return Task.CompletedTask;
    }

    private static StoreSample CreateCity()
    {
        var sales = Math.Round(SampleData.Next(400, 1200));
        return new StoreSample { Sales = sales, Margin = Math.Round(SampleData.Next(8, 22), 1), Visitors = Math.Round(sales * SampleData.Next(0.8, 1.6)) };
    }

    private static StoreSample CreateSuburb()
    {
        var sales = Math.Round(SampleData.Next(200, 800));
        return new StoreSample { Sales = sales, Margin = Math.Round(SampleData.Next(12, 30), 1), Visitors = Math.Round(sales * SampleData.Next(0.4, 0.9)) };
    }
}
