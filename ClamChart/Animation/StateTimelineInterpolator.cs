namespace ClamChart.Animation;

using ClamChart.Layout;

internal sealed class StateTimelineInterpolator
{
    private AxisScale from = new(0, 1, 1);
    private AxisScale to = new(0, 1, 1);
    private bool captured;
    private ChartEntranceAnimation entrance;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool IsMorphing { get; private set; }

    public bool IsEntrance { get; private set; }

    public double Progress { get; private set; } = 1;

    public AxisScale DisplayScale { get; private set; } = new(0, 1, 1);

    public ChartEntranceAnimation Entrance => IsEntrance ? entrance : ChartEntranceAnimation.None;

    public double RevealRatio => Entrance == ChartEntranceAnimation.Reveal ? Ease(Progress) : 1;

    public double Alpha => Entrance == ChartEntranceAnimation.Fade ? Ease(Progress) : 1;

    public double Scale => Entrance == ChartEntranceAnimation.Grow ? Ease(Progress) : 1;

    public double Ease(double progress) => Easing.Ease(progress);

    public void Capture()
    {
        from = DisplayScale;
        captured = true;
    }

    public bool Begin(StateTimelineLayout target)
    {
        IsEntrance = false;
        var scale = target.Axis.Scale;
        if (!captured || (from.Minimum.Equals(scale.Minimum) && from.Maximum.Equals(scale.Maximum)))
        {
            Complete(target);
            return false;
        }

        captured = false;
        to = scale;
        IsMorphing = true;
        Apply(0);
        return true;
    }

    public void BeginEntrance(StateTimelineLayout target, ChartEntranceAnimation animation)
    {
        Complete(target);
        entrance = animation == ChartEntranceAnimation.Auto ? ChartEntranceAnimation.Reveal : animation;
        IsEntrance = true;
        Progress = 0;
    }

    public void Retarget(StateTimelineLayout target)
    {
        var progress = Progress;
        var animation = entrance;
        Complete(target);
        entrance = animation;
        IsEntrance = true;
        Progress = progress;
    }

    public void Apply(double progress)
    {
        Progress = progress;
        if (!IsMorphing)
        {
            return;
        }

        var eased = Ease(progress);
        DisplayScale = new AxisScale(ChartProgress.Lerp(from.Minimum, to.Minimum, eased), ChartProgress.Lerp(from.Maximum, to.Maximum, eased), to.Interval);
    }

    public void Complete(StateTimelineLayout target)
    {
        IsMorphing = false;
        IsEntrance = false;
        Progress = 1;
        captured = false;
        DisplayScale = target.Axis.Scale;
    }
}
