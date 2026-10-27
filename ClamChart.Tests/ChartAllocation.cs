namespace ClamChart.Tests;

internal static class ChartAllocation
{
    private const int Attempts = 8;

    public static long Measure(Action action)
    {
        var allocated = 0L;
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            action();
            var before = GC.GetAllocatedBytesForCurrentThread();
            action();
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated == 0)
            {
                break;
            }
        }

        return allocated;
    }
}
