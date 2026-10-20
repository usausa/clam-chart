namespace ClamChart.Animation;

using ClamChart.Layout;

internal sealed class SparklineInterpolator : IDisposable
{
    private SeriesInterpolation current = new();
    private SeriesInterpolation next = new();
    private SparklineScale from;
    private SparklineScale to;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool HasState { get; private set; }

    public bool IsMorphing { get; private set; }

    public bool IsEntrance => Entrance != ChartEntranceAnimation.None;

    public double Progress { get; private set; } = 1;

    public SeriesGeometry Display => current.Display;

    public SparklineScale Scale { get; private set; } = new(0, 1, 0);

    public ChartEntranceAnimation Entrance { get; private set; } = ChartEntranceAnimation.None;

    public double OverlayAlpha => IsEntrance ? Ease(ChartProgress.Delayed(Progress, ChartProgress.OverlayDelay)) : 1;

    public void Dispose()
    {
        current.Dispose();
        next.Dispose();
    }

    public double Ease(double progress) => Easing.Ease(progress);

    public int ToDisplayIndex(int target) => IsMorphing ? current.GetUnionIndex(target) : target;

    public bool Begin(SeriesGeometry? target, SparklineScale scale, ReadOnlySpan<int> map, bool identity, bool collapse)
    {
        Entrance = ChartEntranceAnimation.None;
        if (!HasState || (target is null))
        {
            Complete(target, scale);
            return false;
        }

        from = Scale;
        to = scale;
        next.BuildMorph(current, map, identity, target, collapse);
        (current, next) = (next, current);
        IsMorphing = current.IsMoving || !from.Equals(to);
        if (!IsMorphing)
        {
            Complete(target, scale);
            return false;
        }

        Apply(0);
        return true;
    }

    public void Retarget(SeriesGeometry? target, SparklineScale scale)
    {
        Scale = scale;
        HasState = target is not null;
        if (target is not null)
        {
            current.SetIdentity(target, 1);
        }
    }

    public void BeginEntrance(SeriesGeometry? target, SparklineScale scale, ChartEntranceAnimation mode)
    {
        Complete(target, scale);
        Entrance = mode;
        Progress = 0;
    }

    public void Apply(double progress)
    {
        Progress = progress;
        if (!IsMorphing)
        {
            return;
        }

        var eased = Ease(progress);
        current.Apply(eased);
        Scale = new SparklineScale(ChartProgress.Lerp(from.Minimum, to.Minimum, eased), ChartProgress.Lerp(from.Maximum, to.Maximum, eased), ChartProgress.Lerp(from.Last, to.Last, eased));
    }

    public void Complete(SeriesGeometry? target, SparklineScale scale)
    {
        IsMorphing = false;
        Entrance = ChartEntranceAnimation.None;
        Progress = 1;
        Scale = scale;
        HasState = target is not null;
        if (target is not null)
        {
            current.SetIdentity(target, 1);
        }
    }
}
