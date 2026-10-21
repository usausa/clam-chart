namespace ClamChart.Animation;

internal sealed class ChartAnimator
{
    private readonly ChartViewBase owner;
    private readonly string name;
    private readonly Action<double> progressChanged;
    private readonly Action<double> frame;
    private readonly Action<double, bool> finished;
    private readonly Func<bool> repeat;
    private bool looping;
    private bool repeating;

    public bool IsRunning { get; private set; }

    public double Progress { get; private set; } = 1;

    public ChartAnimator(ChartViewBase owner, string name, Action<double> progressChanged)
    {
        this.owner = owner;
        this.name = name;
        this.progressChanged = progressChanged;
        frame = OnFrame;
        finished = OnFinished;
        repeat = OnRepeat;
    }

    public bool Start(double length, bool loop = false)
    {
        Stop();
        var driver = owner.AnimationDriver;
        if ((length < 1) || !driver.CanStart)
        {
            return false;
        }

        IsRunning = true;
        Progress = 0;
        looping = loop;
        repeating = false;
        driver.Start(name, (uint)Math.Round(length), frame, finished, loop ? repeat : null);
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

    private bool OnRepeat()
    {
        repeating = IsRunning && looping;
        return repeating;
    }

    private void OnFinished(double value, bool cancelled)
    {
        if (repeating)
        {
            repeating = false;
            return;
        }

        if (IsRunning)
        {
            IsRunning = false;
            Progress = 1;
            progressChanged(1);
        }
    }
}
