namespace ClamChart.Platform;

internal interface IChartPlatformBridge : IDisposable
{
    void SetParentIntercept(bool allow);

    void PerformHaptic(ChartHaptic haptic);
}
