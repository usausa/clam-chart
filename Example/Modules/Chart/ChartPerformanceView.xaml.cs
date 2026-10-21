namespace Example.Modules.Chart;

[View(ViewId.ChartPerformance)]
public sealed partial class ChartPerformanceView
{
    public ChartPerformanceView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ChartPerformanceViewModel viewModel)
        {
            viewModel.EntranceRequested += OnEntranceRequested;
        }
    }

    private void OnFrameRendered(object? sender, ChartFrameEventArgs e)
    {
        if (BindingContext is ChartPerformanceViewModel viewModel)
        {
            viewModel.Record(e);
        }
    }

    private void OnEntranceRequested(object? sender, EventArgs e)
    {
        SalesStat.PlayEntranceAnimation();
        ErrorStat.PlayEntranceAnimation();
        GoalRadial.PlayEntranceAnimation();
        ChannelPie.PlayEntranceAnimation();
        RequestChart.PlayEntranceAnimation();
        StorageGauge.PlayEntranceAnimation();
    }
}
