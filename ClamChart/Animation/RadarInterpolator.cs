namespace ClamChart.Animation;

using ClamChart.Layout;

internal sealed class RadarInterpolator
{
    private double[] from = [];
    private double[] to = [];
    private double[] values = [];
    private double[] fromOpacity = [];
    private double[] toOpacity = [];
    private double[] opacity = [];
    private string?[] names = [];
    private AxisScale fromScale = new(0, 1, 1);
    private AxisScale toScale = new(0, 1, 1);
    private int seriesCount;
    private int axisCount;
    private bool captured;
    private ChartEntranceAnimation entrance;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool IsMorphing { get; private set; }

    public bool IsEntrance { get; private set; }

    public double Progress { get; private set; } = 1;

    public int Version { get; private set; }

    public AxisScale Scale { get; private set; } = new(0, 1, 1);

    public ChartEntranceAnimation Entrance => IsEntrance ? entrance : ChartEntranceAnimation.None;

    public double Growth => Entrance == ChartEntranceAnimation.Grow ? Ease(Progress) : 1;

    public double EntranceAlpha => Entrance == ChartEntranceAnimation.Fade ? Ease(Progress) : 1;

    public double RevealAngle => Entrance == ChartEntranceAnimation.Reveal ? 360 * Ease(Progress) : 360;

    public double OverlayAlpha => IsEntrance ? Ease(ChartProgress.Delayed(Progress, ChartProgress.OverlayDelay)) : 1;

    public double Ease(double progress) => Easing.Ease(progress);

    public double GetValue(int seriesIndex, int axis) =>
        (seriesIndex < seriesCount) && (axis < axisCount) ? values[(seriesIndex * axisCount) + axis] : Double.NaN;

    public double GetOpacity(int seriesIndex) => seriesIndex < seriesCount ? opacity[seriesIndex] : 0;

    public void Capture()
    {
        var size = seriesCount * axisCount;
        Ensure(ref from, size);
        Ensure(ref fromOpacity, seriesCount);
        Array.Copy(values, from, size);
        Array.Copy(opacity, fromOpacity, seriesCount);
        fromScale = Scale;
        captured = true;
    }

    public bool Begin(RadarLayout target, IReadOnlyList<CartesianSeries> series)
    {
        IsEntrance = false;
        if (!captured || !HasSameShape(target))
        {
            Complete(target, series);
            return false;
        }

        captured = false;
        var size = seriesCount * axisCount;
        Ensure(ref to, size);
        Ensure(ref toOpacity, seriesCount);
        var moving = !fromScale.Equals(target.Scale);
        for (var s = 0; s < seriesCount; s++)
        {
            for (var k = 0; k < axisCount; k++)
            {
                var slot = (s * axisCount) + k;
                to[slot] = target.GetValue(s, k);
                moving |= !from[slot].Equals(to[slot]);
            }

            toOpacity[s] = series[s].IsVisible ? 1 : 0;
            moving |= !fromOpacity[s].Equals(toOpacity[s]);
        }

        toScale = target.Scale;
        if (!moving)
        {
            Complete(target, series);
            return false;
        }

        IsMorphing = true;
        Apply(0);
        return true;
    }

    public void BeginEntrance(RadarLayout target, IReadOnlyList<CartesianSeries> series, ChartEntranceAnimation mode)
    {
        Complete(target, series);
        entrance = mode == ChartEntranceAnimation.Auto ? ChartEntranceAnimation.Grow : mode;
        IsEntrance = true;
        Progress = 0;
    }

    public void Retarget(RadarLayout target, IReadOnlyList<CartesianSeries> series)
    {
        var progress = Progress;
        var mode = entrance;
        Complete(target, series);
        entrance = mode;
        IsEntrance = true;
        Progress = progress;
    }

    public void Apply(double progress)
    {
        Progress = progress;
        Version++;
        if (!IsMorphing)
        {
            return;
        }

        var eased = Ease(progress);
        var size = seriesCount * axisCount;
        for (var i = 0; i < size; i++)
        {
            values[i] = Double.IsFinite(from[i]) && Double.IsFinite(to[i]) ? ChartProgress.Lerp(from[i], to[i], eased) : to[i];
        }

        for (var s = 0; s < seriesCount; s++)
        {
            opacity[s] = ChartProgress.Lerp(fromOpacity[s], toOpacity[s], eased);
        }

        Scale = new AxisScale(ChartProgress.Lerp(fromScale.Minimum, toScale.Minimum, eased), ChartProgress.Lerp(fromScale.Maximum, toScale.Maximum, eased), toScale.Interval);
    }

    public void Complete(RadarLayout target, IReadOnlyList<CartesianSeries> series)
    {
        IsMorphing = false;
        IsEntrance = false;
        Progress = 1;
        captured = false;
        Version++;
        seriesCount = Math.Min(target.SeriesCount, series.Count);
        axisCount = target.AxisCount;
        var size = seriesCount * axisCount;
        Ensure(ref values, size);
        Ensure(ref opacity, seriesCount);
        Ensure(ref names, axisCount);
        for (var s = 0; s < seriesCount; s++)
        {
            for (var k = 0; k < axisCount; k++)
            {
                values[(s * axisCount) + k] = target.GetValue(s, k);
            }

            opacity[s] = series[s].IsVisible ? 1 : 0;
        }

        for (var k = 0; k < axisCount; k++)
        {
            names[k] = target.Axes[k].Text;
        }

        Scale = target.Scale;
    }

    private static void Ensure<T>(ref T[] buffer, int count)
    {
        if (buffer.Length < count)
        {
            buffer = new T[count];
        }
    }

    private bool HasSameShape(RadarLayout target)
    {
        if ((target.SeriesCount != seriesCount) || (target.AxisCount != axisCount))
        {
            return false;
        }

        for (var k = 0; k < axisCount; k++)
        {
            if (!String.Equals(names[k], target.Axes[k].Text, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
