namespace ClamChart.Tests;

using ClamChart.Animation;

internal sealed class TestAnimationDriver : IChartAnimationDriver
{
    private readonly Dictionary<string, (Action<double> Frame, Action<double, bool> Finished, Func<bool>? Repeat)> running = [];

    public List<(string Name, uint Length)> Starts { get; } = [];

    public List<string> Aborts { get; } = [];

    public bool CanStart { get; set; } = true;

    public bool IsRunning(string name) => running.ContainsKey(name);

    public void Start(string name, uint length, Action<double> frame, Action<double, bool> finished, Func<bool>? repeat = null)
    {
        Starts.Add((name, length));
        running[name] = (frame, finished, repeat);
    }

    public void Abort(string name)
    {
        Aborts.Add(name);
        if (running.Remove(name, out var entry))
        {
            entry.Finished(1, true);
        }
    }

    public void Frame(string name, double progress) => running[name].Frame(progress);

    public void Cancel(string name)
    {
        if (running.Remove(name, out var entry))
        {
            entry.Frame(1);
            entry.Finished(1, true);
        }
    }

    public void Finish(string name)
    {
        if (!running.TryGetValue(name, out var entry))
        {
            return;
        }

        entry.Frame(1);
        if (entry.Repeat?.Invoke() != true)
        {
            running.Remove(name);
        }

        entry.Finished(1, false);
    }
}
