namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Data;

public partial class ClamBarGaugeView
{
    private const string DataAnimationName = "ClamBarGaugeViewData";

    private const double StaggerMilliseconds = 40;

    private readonly ChartAnimator dataAnimator;
    private int[]? pendingMap;
    private bool pendingPositional;
    private bool sourceReplaced;
    private bool entranceShown;
    private bool entranceRequested;

    internal BarGaugeInterpolator Interpolator { get; } = new();

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
        Interpolator.Complete(ChartLayout);
        InvalidateSurface();
    }

    private bool CanAnimate => (ResolveAnimationLength(AnimationDuration) >= 1) && AnimationDriver.CanStart;

    private void RecordChange(int oldCount, int newCount)
    {
        if (sourceReplaced)
        {
            sourceReplaced = false;
            entranceRequested |= entranceShown && (oldCount != newCount) && (newCount > 0);
        }

        if (pendingPositional)
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
        var allowed = !Interpolator.IsEntrance && (ChartLayout.Rows.Count > 0) && UpdateAnimationEnabled && CanAnimate;
        if (!allowed && !Interpolator.IsEntrance)
        {
            dataAnimator.Stop();
        }

        return allowed;
    }

    private void CompleteTransition(bool captured)
    {
        if (!TryStartEntrance())
        {
            if (Interpolator.IsEntrance)
            {
                Interpolator.Retarget(ChartLayout);
            }
            else if (captured && (ChartLayout.Rows.Count > 0) && Interpolator.Begin(ChartLayout, pendingMap, pendingPositional || (pendingMap is null), snapshot.Count))
            {
                if (!dataAnimator.Start(ResolveAnimationLength(AnimationDuration)))
                {
                    Interpolator.Complete(ChartLayout);
                }
            }
            else
            {
                Interpolator.Complete(ChartLayout);
            }
        }

        ReleasePendingMap();
        pendingPositional = false;
    }

    private bool TryStartEntrance()
    {
        if ((entranceShown && !entranceRequested) || (ChartLayout.Rows.Count == 0))
        {
            return false;
        }

        var length = ResolveAnimationLength(AnimationDuration);
        if ((EntranceAnimation == ChartEntranceAnimation.None) || (length < 1))
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
        Interpolator.BeginEntrance(ChartLayout, EntranceAnimation, ChartProgress.GetStagger(ChartLayout.Rows.Count, StaggerMilliseconds, length));
        if (!dataAnimator.Start(length))
        {
            Interpolator.Complete(ChartLayout);
        }

        return true;
    }

    private void OnDataProgress(double progress)
    {
        Interpolator.Apply(progress);
        if (!dataAnimator.IsRunning)
        {
            Interpolator.Complete(ChartLayout);
        }

        InvalidateSurface();
    }
}
