namespace ClamChart.Tests;

using Microsoft.Maui.Dispatching;

internal sealed class TestDispatcherProvider : IDispatcherProvider
{
    [ThreadStatic]
    private static TestDispatcher? current;

    [ThreadStatic]
    private static bool unavailable;

    public static TestDispatcherProvider Instance { get; } = new();

    public IDispatcher? GetForCurrentThread() => unavailable ? null : current ??= new TestDispatcher();

    public static Task RunWithoutDispatcherAsync(Action action) =>
        Task.Factory.StartNew(
            () =>
            {
                unavailable = true;
                try
                {
                    action();
                }
                finally
                {
                    unavailable = false;
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
}
