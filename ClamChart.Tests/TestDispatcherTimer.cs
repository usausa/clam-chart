namespace ClamChart.Tests;

using Microsoft.Maui.Dispatching;

internal sealed class TestDispatcherTimer : IDispatcherTimer
{
    public event EventHandler? Tick;

    public TimeSpan Interval { get; set; }

    public bool IsRepeating { get; set; } = true;

    public bool IsRunning { get; private set; }

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;

    public void Fire() => Tick?.Invoke(this, EventArgs.Empty);
}
