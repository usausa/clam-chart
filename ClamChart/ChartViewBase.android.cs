namespace ClamChart;

using ClamChart.Platform;

public abstract partial class ChartViewBase
{
    partial void AttachPlatform()
    {
        if (!disposed && (Handler?.PlatformView is Android.Views.View view))
        {
            platform = new ChartPlatformBridge(view, () => CancelInput(true));
        }
    }
}
