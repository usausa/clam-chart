namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Data;

public partial class ClamSparklineView
{
    private const string DataAnimationName = "ClamSparklineViewData";

    private readonly ChartAnimator dataAnimator;
    private int[]? pendingMap;
    private bool pendingPositional;
    private bool pendingReset;
    private bool sourceReplaced;
    private bool entranceShown;
    private bool entranceRequested;

    internal SparklineInterpolator Interpolator { get; } = new();

    internal override bool IsAnimating => dataAnimator.IsRunning;

    internal override void StartEntranceAnimation()
    {
        entranceRequested = true;
        EnsureLayout();
        if (TryStartEntrance())
        {
            InvalidateSurface();
        }
    }

    internal override void FinishAnimations()
    {
        dataAnimator.Stop();
        Interpolator.Complete(live ? target : null, ChartLayout.Scale);
        InvalidateSurface();
    }

    private bool CanAnimate => (ResolveAnimationLength(AnimationDuration) >= 1) && AnimationDriver.CanStart;

    private void RecordChange(bool continuous, int oldCount, int newCount)
    {
        if (sourceReplaced || !continuous || source.Changes.IsReset)
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
        if (!ChangeMap.TryBuild(source.Changes, oldCount, newCount, map.AsSpan(0, oldCount)))
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

    private bool PrepareTransition()
    {
        Interpolator.Easing = AnimationEasing;
        var allowed = !Interpolator.IsEntrance && !pendingReset && live && Interpolator.HasState && UpdateAnimationEnabled && CanAnimate;
        if (!allowed && !Interpolator.IsEntrance)
        {
            dataAnimator.Stop();
        }

        return allowed;
    }

    private void CompleteTransition(bool captured)
    {
        var current = live ? target : null;
        if (!TryStartEntrance())
        {
            if (Interpolator.IsEntrance)
            {
                Interpolator.Retarget(current, ChartLayout.Scale);
            }
            else if (captured && Interpolator.Begin(current, ChartLayout.Scale, pendingMap, pendingPositional || (pendingMap is null), ChartLayout.IsBar))
            {
                if (!dataAnimator.Start(ResolveAnimationLength(AnimationDuration)))
                {
                    Interpolator.Complete(current, ChartLayout.Scale);
                }
            }
            else
            {
                Interpolator.Complete(current, ChartLayout.Scale);
            }
        }

        ReleasePendingMap();
        pendingPositional = false;
        pendingReset = false;
    }

    private bool TryStartEntrance()
    {
        if ((entranceShown && !entranceRequested) || (ChartLayout.FirstIndex < 0))
        {
            return false;
        }

        var mode = EntranceAnimation;
        var length = ResolveAnimationLength(AnimationDuration);
        if ((mode is ChartEntranceAnimation.Auto or ChartEntranceAnimation.None) || (length < 1))
        {
            entranceShown = true;
            entranceRequested = false;
            return false;
        }

        if (!AnimationDriver.CanStart)
        {
            return false;
        }

        entranceShown = true;
        entranceRequested = false;
        dataAnimator.Stop();
        Interpolator.BeginEntrance(live ? target : null, ChartLayout.Scale, mode);
        if (!dataAnimator.Start(length))
        {
            Interpolator.Complete(live ? target : null, ChartLayout.Scale);
        }

        return true;
    }

    private void OnDataProgress(double progress)
    {
        Interpolator.Apply(progress);
        if (!dataAnimator.IsRunning)
        {
            Interpolator.Complete(live ? target : null, ChartLayout.Scale);
        }

        InvalidateSurface();
    }
}
