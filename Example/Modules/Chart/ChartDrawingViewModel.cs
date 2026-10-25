namespace Example.Modules.Chart;

using Example.Models;

public sealed partial class ChartDrawingViewModel : ChartViewModelBase
{
    private const int ColumnCount = 5;

    private const int TrendCount = 48;

    [ObservableProperty]
    public partial int ModeIndex { get; set; }

    public IObserveCommand SelectModeCommand { get; }

    private int version;

    private double storage;

    private double cpu;

    private double[] activity = [];

    private double[] goals = [];

    private double[] targets = [];

    private double[] columns = [];

    private double[] trend = [];

    [ObservableProperty]
    public partial ImageSource? StorageImage { get; set; }

    [ObservableProperty]
    public partial ImageSource? ActivityImage { get; set; }

    [ObservableProperty]
    public partial ImageSource? CpuImage { get; set; }

    [ObservableProperty]
    public partial ImageSource? ColumnsImage { get; set; }

    [ObservableProperty]
    public partial ImageSource? GoalsImage { get; set; }

    [ObservableProperty]
    public partial ImageSource? TrendImage { get; set; }

    public ChartDrawingViewModel()
    {
        SelectModeCommand = MakeAsyncCommand<int>(SelectModeAsync);
        Randomize();
    }

    private Task SelectModeAsync(int index)
    {
        ModeIndex = index;
        return RenderAsync();
    }

    public override Task OnNavigatedToAsync(INavigationContext context) => RenderAsync();

    protected override Task OnNotifyFunction3()
    {
        Randomize();
        return RenderAsync();
    }

    private static ImageSource ToSource(byte[] png) => ImageSource.FromStream(() => new MemoryStream(png));

    private void Randomize()
    {
        storage = Math.Round(SampleData.Next(40, 95));
        cpu = Math.Round(SampleData.Next(20, 98));
        activity = [Math.Round(SampleData.Next(60, 140)), Math.Round(SampleData.Next(30, 100)), Math.Round(SampleData.Next(50, 100))];
        goals = [Math.Round(SampleData.Next(30, 100)), Math.Round(SampleData.Next(30, 100)), Math.Round(SampleData.Next(30, 100))];
        targets = [Math.Round(SampleData.Next(60, 90)), Math.Round(SampleData.Next(60, 90)), Math.Round(SampleData.Next(60, 90))];
        columns = new double[ColumnCount];
        for (var i = 0; i < columns.Length; i++)
        {
            columns[i] = Math.Round(SampleData.Next(10, 100));
        }

        trend = SampleData.Walk(TrendCount, 50, 6, 0);
    }

    private async Task RenderAsync()
    {
        var current = ++version;
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var scale = (float)DeviceDisplay.Current.MainDisplayInfo.Density;
        var ring = (ChartRadialBarMode)ModeIndex;
        var bar = (ChartBarGaugeMode)ModeIndex;
        var (storageValue, cpuValue, activityValues, goalValues, targetValues, columnValues, trendValues) = (storage, cpu, activity, goals, targets, columns, trend);
        var images = await Task.Run(() => new[]
        {
            DrawingWidgets.Storage(storageValue, ring, dark, scale),
            DrawingWidgets.Activity(activityValues, ring, dark, scale),
            DrawingWidgets.Cpu(cpuValue, ring, dark, scale),
            DrawingWidgets.Columns(columnValues, bar, dark, scale),
            DrawingWidgets.Goals(goalValues, targetValues, bar, dark, scale),
            DrawingWidgets.Trend(trendValues, dark, scale)
        });
        if (current != version)
        {
            return;
        }

        StorageImage = ToSource(images[0]);
        ActivityImage = ToSource(images[1]);
        CpuImage = ToSource(images[2]);
        ColumnsImage = ToSource(images[3]);
        GoalsImage = ToSource(images[4]);
        TrendImage = ToSource(images[5]);
    }
}
