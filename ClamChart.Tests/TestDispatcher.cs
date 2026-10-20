namespace ClamChart.Tests;

using Microsoft.Maui.Dispatching;

internal sealed class TestDispatcher : IDispatcher
{
    private readonly int threadId = Environment.CurrentManagedThreadId;
    private readonly Queue<Action> actions = new();

    public static TestDispatcher Current => (TestDispatcher)TestDispatcherProvider.Instance.GetForCurrentThread()!;

    public bool IsDispatchRequired => Environment.CurrentManagedThreadId != threadId;

    public int PendingCount => actions.Count;

    public bool Dispatch(Action action)
    {
        actions.Enqueue(action);
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

    public TestDispatcherTimer? LastTimer { get; private set; }

    public IDispatcherTimer CreateTimer() => LastTimer = new TestDispatcherTimer();

    public void RunPending()
    {
        while (actions.TryDequeue(out var action))
        {
            action();
        }
    }
}
