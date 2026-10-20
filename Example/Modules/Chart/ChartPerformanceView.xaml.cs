namespace Example.Modules.Chart;

[View(ViewId.ChartPerformance)]
public sealed partial class ChartPerformanceView
{
    public ChartPerformanceView()
    {
        InitializeComponent();
    }

    private void OnFrameRendered(object? sender, ChartFrameEventArgs e)
    {
        if (BindingContext is ChartPerformanceViewModel viewModel)
        {
            viewModel.Record(e);
        }
    }
}
