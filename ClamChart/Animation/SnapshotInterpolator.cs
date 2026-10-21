namespace ClamChart.Animation;

using ClamChart.Layout;

internal sealed class SnapshotInterpolator : IDisposable
{
    private readonly List<SeriesInterpolation> current = [];
    private readonly List<SeriesInterpolation> next = [];
    private readonly List<bool> visible = [];
    private CartesianLayout? layout;
    private AxisScale fromX;
    private AxisScale fromY;
    private AxisScale fromSecondary;
    private ChartEntranceAnimation entrance;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool IsMorphing { get; private set; }

    public bool IsEntrance { get; private set; }

    public double Progress { get; private set; } = 1;

    public double OverlayAlpha => IsEntrance ? Ease(ChartProgress.Delayed(Progress, ChartProgress.OverlayDelay)) : 1;

    public void Dispose()
    {
        foreach (var item in current)
        {
            item.Dispose();
        }

        foreach (var item in next)
        {
            item.Dispose();
        }

        current.Clear();
        next.Clear();
    }

    public double Ease(double progress) => Easing.Ease(progress);

    public SeriesGeometry GetGeometry(CartesianLayout target, int seriesIndex) =>
        IsMorphing && (seriesIndex < current.Count) ? current[seriesIndex].Display : target.Geometries[seriesIndex];

    public double GetOpacity(int seriesIndex, bool shown)
    {
        if (IsMorphing && (seriesIndex < current.Count))
        {
            return current[seriesIndex].Opacity;
        }

        return shown ? 1 : 0;
    }

    public int ToDisplayIndex(int seriesIndex, int index) =>
        IsMorphing && (seriesIndex < current.Count) ? current[seriesIndex].GetUnionIndex(index) : index;

    public int ToTargetIndex(int seriesIndex, int index) =>
        IsMorphing && (seriesIndex < current.Count) ? current[seriesIndex].GetTargetIndex(index) : index;

    public ChartEntranceAnimation GetEntrance(ChartSeriesKind kind)
    {
        if (!IsEntrance)
        {
            return ChartEntranceAnimation.None;
        }

        if (entrance != ChartEntranceAnimation.Auto)
        {
            return entrance;
        }

        return kind switch
        {
            ChartSeriesKind.Bar => ChartEntranceAnimation.Grow,
            ChartSeriesKind.Scatter => ChartEntranceAnimation.Fade,
            _ => ChartEntranceAnimation.Reveal
        };
    }

    public void Capture(CartesianLayout source, IReadOnlyList<CartesianSeries> series)
    {
        Ensure(series.Count);
        visible.Clear();
        for (var s = 0; s < series.Count; s++)
        {
            var shown = series[s].IsVisible;
            if (!IsMorphing)
            {
                current[s].SetIdentity(source.Geometries[s], shown ? 1 : 0);
            }

            visible.Add(IsMorphing ? current[s].Opacity > 0 : shown);
        }

        fromX = source.XAxis.DisplayScale;
        fromY = source.YAxis.DisplayScale;
        fromSecondary = source.HasSecondaryAxis ? source.SecondaryYAxis.DisplayScale : source.YAxis.DisplayScale;
    }

    public bool Begin(CartesianLayout target, IReadOnlyList<CartesianSeries> series, IReadOnlyList<int[]?> maps)
    {
        layout = target;
        IsEntrance = false;
        if (visible.Count != series.Count)
        {
            Complete(target);
            return false;
        }

        var moving = !fromX.Equals(target.XAxis.Scale) || !fromY.Equals(target.YAxis.Scale) || (target.HasSecondaryAxis && !fromSecondary.Equals(target.SecondaryYAxis.Scale));
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            var geometry = target.Geometries[s];
            var collapse = (item.Kind == ChartSeriesKind.Bar) || geometry.IsStacked || current[s].Display.IsStacked;
            var builder = next[s];
            var was = visible[s];
            if (was && item.IsVisible)
            {
                var map = s < maps.Count ? maps[s] : null;
                builder.BuildMorph(current[s], map, (map is null) || (map.Length == 0), geometry, collapse);
            }
            else if (was)
            {
                builder.BuildHide(current[s], collapse);
            }
            else if (item.IsVisible)
            {
                builder.BuildShow(geometry, collapse);
            }
            else
            {
                builder.SetIdentity(geometry, 0);
            }

            moving |= builder.IsMoving;
            next[s] = current[s];
            current[s] = builder;
        }

        IsMorphing = moving;
        if (!moving)
        {
            Complete(target);
            return false;
        }

        Apply(0);
        return true;
    }

    public void BeginEntrance(CartesianLayout target, ChartEntranceAnimation mode)
    {
        Complete(target);
        layout = target;
        entrance = mode;
        IsEntrance = true;
        Progress = 0;
    }

    public void Apply(double progress)
    {
        Progress = progress;
        if (!IsMorphing || (layout is null))
        {
            return;
        }

        var eased = Ease(progress);
        foreach (var item in current)
        {
            item.Apply(eased);
        }

        layout.XAxis.DisplayScale = Interpolate(fromX, layout.XAxis.Scale, eased);
        layout.YAxis.DisplayScale = Interpolate(fromY, layout.YAxis.Scale, eased);
        if (layout.HasSecondaryAxis)
        {
            layout.SecondaryYAxis.DisplayScale = Interpolate(fromSecondary, layout.SecondaryYAxis.Scale, eased);
        }
    }

    public void Complete(CartesianLayout target)
    {
        IsMorphing = false;
        IsEntrance = false;
        Progress = 1;
        target.XAxis.DisplayScale = target.XAxis.Scale;
        target.YAxis.DisplayScale = target.YAxis.Scale;
        target.SecondaryYAxis.DisplayScale = target.SecondaryYAxis.Scale;
    }

    private static AxisScale Interpolate(AxisScale from, AxisScale to, double progress) =>
        new(ChartProgress.Lerp(from.Minimum, to.Minimum, progress), ChartProgress.Lerp(from.Maximum, to.Maximum, progress), to.Interval);

    private void Ensure(int count)
    {
        while (current.Count < count)
        {
            current.Add(new SeriesInterpolation());
            next.Add(new SeriesInterpolation());
        }
    }
}
