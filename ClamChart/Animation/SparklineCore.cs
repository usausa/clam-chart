namespace ClamChart.Animation;

using System.Collections;
using System.Collections.Specialized;

using ClamChart.Data;
using ClamChart.Layout;

internal sealed class SparklineCore : IDisposable
{
    private const int InitialCapacity = 16;

    private readonly ChartViewBase owner;
    private readonly ChartAnimator animator;
    private readonly SeriesGeometry target = new();
    private readonly Func<ChartValueReader>? readerFactory;
    private ChartValueReader? reader;
    private IReadOnlyList<double>? direct;
    private double[] buffer = [];
    private bool live;
    private int[]? pendingMap;
    private bool pendingPositional;
    private bool pendingReset;
    private bool sourceReplaced;
    private bool entranceShown;
    private bool entranceRequested;
    private bool released;

    public ChartDataSource Source { get; }

    public SparklineLayout Layout { get; } = new();

    public SparklineInterpolator Interpolator { get; } = new();

    public bool IsAnimating => animator.IsRunning;

    public SparklineValues Values => new(direct, live ? target : null);

    private SeriesGeometry? Current => live ? target : null;

    public SparklineCore(ChartViewBase owner, string animationName, Action changed, Func<ChartValueReader>? readerFactory = null)
    {
        this.owner = owner;
        this.readerFactory = readerFactory;
        Source = new ChartDataSource(changed);
        animator = new ChartAnimator(owner, animationName, OnProgress);
    }

    public void Dispose()
    {
        if (released)
        {
            return;
        }

        released = true;
        ReleasePendingMap();
        Interpolator.Dispose();
        target.Dispose();
        DataSnapshot.Return(buffer);
        buffer = [];
        direct = null;
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public void SetValues(IEnumerable<double>? values)
    {
        Source.SetTrackItemChanges(false);
        Source.SetSource(values);
        sourceReplaced = true;
    }

    public void SetItems(IEnumerable? items, bool trackItemChanges)
    {
        Source.SetSource(items);
        Source.SetTrackItemChanges(trackItemChanges);
        sourceReplaced = true;
    }

    public void ResetReader() => reader = null;

    public void Update(IEnumerable<double>? values, IEnumerable? items)
    {
        var wasLive = live;
        var oldCount = target.Count;
        if ((values is IReadOnlyList<double> list) && (values is not INotifyCollectionChanged))
        {
            direct = list;
            live = false;
        }
        else
        {
            direct = null;
            live = true;
            if (values is not null)
            {
                ReadValues(values);
            }
            else
            {
                ReadItems(items);
            }
        }

        RecordChange(wasLive && live, oldCount, live ? target.Count : 0);
        Source.Changes.Clear();
    }

    private void ReadValues(IEnumerable<double> values)
    {
        if (values is IReadOnlyList<double> list)
        {
            var count = list.Count;
            target.Reset(count);
            for (var i = 0; i < count; i++)
            {
                SetPoint(i, list[i]);
            }

            return;
        }

        var collected = 0;
        foreach (var value in values)
        {
            Store(ref collected, value);
        }

        CopyBuffer(collected);
    }

    private void ReadItems(IEnumerable? items)
    {
        if ((items is null) || (readerFactory is null))
        {
            target.Reset(0);
            return;
        }

        reader ??= readerFactory();
        if (items is IList list)
        {
            var count = list.Count;
            target.Reset(count);
            for (var i = 0; i < count; i++)
            {
                SetPoint(i, reader.ReadY(list[i]));
            }

            return;
        }

        var collected = 0;
        foreach (var item in items)
        {
            Store(ref collected, reader.ReadY(item));
        }

        CopyBuffer(collected);
    }

    private void Store(ref int count, double value)
    {
        if (count == buffer.Length)
        {
            var larger = DataSnapshot.Rent<double>(Math.Max(InitialCapacity, buffer.Length * 2));
            Array.Copy(buffer, larger, count);
            DataSnapshot.Return(buffer);
            buffer = larger;
        }

        buffer[count++] = value;
    }

    private void CopyBuffer(int count)
    {
        target.Reset(count);
        for (var i = 0; i < count; i++)
        {
            SetPoint(i, buffer[i]);
        }
    }

    private void SetPoint(int index, double value)
    {
        target.X[index] = index;
        target.Start[index] = 0;
        target.End[index] = value;
        target.Value[index] = value;
        target.Outer[index] = false;
    }

    //--------------------------------------------------------------------------------
    // Layout
    //--------------------------------------------------------------------------------

    public bool PrepareTransition(Easing easing, bool updateEnabled, bool canAnimate)
    {
        Interpolator.Easing = easing;
        var allowed = !Interpolator.IsEntrance && !pendingReset && live && Interpolator.HasState && updateEnabled && canAnimate;
        if (!allowed && !Interpolator.IsEntrance)
        {
            animator.Stop();
        }

        return allowed;
    }

    public void UpdateLayout(double width, double height, Thickness padding, ChartStyle style, SparklineLayoutOptions options) =>
        Layout.Update(width, height, padding, style, options, Values);

    public void CompleteTransition(bool captured, ChartEntranceAnimation entrance, double length)
    {
        var current = Current;
        if (!TryStartEntrance(entrance, length))
        {
            if (Interpolator.IsEntrance)
            {
                Interpolator.Retarget(current, Layout.Scale);
            }
            else if (captured && Interpolator.Begin(current, Layout.Scale, pendingMap, pendingPositional || (pendingMap is null), Layout.IsBar))
            {
                if (!animator.Start(length))
                {
                    Interpolator.Complete(current, Layout.Scale);
                }
            }
            else
            {
                Interpolator.Complete(current, Layout.Scale);
            }
        }

        ReleasePendingMap();
        pendingPositional = false;
        pendingReset = false;
    }

    //--------------------------------------------------------------------------------
    // Animation
    //--------------------------------------------------------------------------------

    public void RequestEntrance() => entranceRequested = true;

    public bool TryStartEntrance(ChartEntranceAnimation mode, double length)
    {
        if ((entranceShown && !entranceRequested) || (Layout.FirstIndex < 0))
        {
            return false;
        }

        if ((mode == ChartEntranceAnimation.None) || (length < 1))
        {
            entranceShown = true;
            entranceRequested = false;
            return false;
        }

        if (!owner.AnimationDriver.CanStart)
        {
            return false;
        }

        entranceShown = true;
        entranceRequested = false;
        animator.Stop();
        Interpolator.BeginEntrance(Current, Layout.Scale, mode);
        if (!animator.Start(length))
        {
            Interpolator.Complete(Current, Layout.Scale);
        }

        return true;
    }

    public void Finish()
    {
        animator.Stop();
        Interpolator.Complete(Current, Layout.Scale);
    }

    private void RecordChange(bool continuous, int oldCount, int newCount)
    {
        if (sourceReplaced || !continuous || Source.Changes.IsReset)
        {
            sourceReplaced = false;
            pendingReset = true;
            pendingPositional = false;
            ReleasePendingMap();
            return;
        }

        if (pendingReset || pendingPositional)
        {
            return;
        }

        var map = DataSnapshot.Rent<int>(oldCount);
        if (!ChangeMap.TryBuild(Source.Changes, oldCount, newCount, map.AsSpan(0, oldCount)))
        {
            DataSnapshot.Return(map);
            ReleasePendingMap();
            pendingPositional = true;
            return;
        }

        if (pendingMap is { } previous)
        {
            ChangeMap.Compose(previous, map.AsSpan(0, oldCount));
            DataSnapshot.Return(map);
        }
        else
        {
            pendingMap = map;
        }
    }

    private void ReleasePendingMap()
    {
        if (pendingMap is { } map)
        {
            DataSnapshot.Return(map);
            pendingMap = null;
        }
    }

    private void OnProgress(double progress)
    {
        Interpolator.Apply(progress);
        if (!animator.IsRunning)
        {
            Interpolator.Complete(Current, Layout.Scale);
        }

        owner.InvalidateSurface();
    }
}
