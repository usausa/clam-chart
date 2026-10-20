namespace ClamChart.Animation;

internal sealed class ChartAnimator
{
    private readonly ChartViewBase owner;
    private readonly string name;
    private readonly Action<double> progressChanged;
    private readonly Action<double> frame;
    private readonly Action<double, bool> finished;

    public bool IsRunning { get; private set; }

    public double Progress { get; private set; } = 1;

    public ChartAnimator(ChartViewBase owner, string name, Action<double> progressChanged)
    {
        this.owner = owner;
        this.name = name;
        this.progressChanged = progressChanged;
        frame = OnFrame;
        finished = OnFinished;
    }

    public bool Start(double length)
    {
        Stop();
        var driver = owner.AnimationDriver;
        if ((length < 1) || !driver.CanStart)
        {
            return false;
        }

        IsRunning = true;
        Progress = 0;
        driver.Start(name, (uint)Math.Round(length), frame, finished);
        return IsRunning;
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        Progress = 1;
        owner.AnimationDriver.Abort(name);
    }

    private void OnFrame(double value)
    {
        if (IsRunning)
        {
            Progress = Math.Clamp(value, 0, 1);
            progressChanged(Progress);
        }
    }

    private void OnFinished(double value, bool cancelled)
    {
        if (IsRunning)
        {
            IsRunning = false;
            Progress = 1;
            progressChanged(1);
        }
    }
}
