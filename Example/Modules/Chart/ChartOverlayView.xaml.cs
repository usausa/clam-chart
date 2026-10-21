namespace Example.Modules.Chart;

using SkiaSharp;

[View(ViewId.ChartOverlay)]
public sealed partial class ChartOverlayView
{
    public ChartOverlayView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ChartOverlayViewModel viewModel)
        {
            viewModel.SnapshotRequested += OnSnapshotRequested;
        }
    }

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is ChartOverlayViewModel viewModel)
        {
            viewModel.SwitchMetric();
        }
    }

    private void OnSnapshotRequested(object? sender, EventArgs e)
    {
        using var image = OverlayChart.RenderToImage(new Size(OverlayChart.Width, OverlayChart.Height), 2);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = data.ToArray();
        SnapshotImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
    }
}
