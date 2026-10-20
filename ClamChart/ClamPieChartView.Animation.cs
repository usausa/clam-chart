namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Data;

public partial class ClamPieChartView
{
    private const string DataAnimationName = "ClamPieChartViewData";

    private const string SelectionAnimationName = "ClamPieChartViewSelection";

    private const double SelectionMilliseconds = 150;

    private readonly ChartAnimator dataAnimator;
    private readonly ChartAnimator selectionAnimator;
    private int[]? pendingMap;
    private bool pendingPositional;
    private bool sourceReplaced;
    private bool entranceShown;
    private bool entranceRequested;

    internal PieInterpolator Interpolator { get; } = new();

    internal override bool IsAnimating => dataAnimator.IsRunning || selectionAnimator.IsRunning;

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
        selectionAnimator.Stop();
        Interpolator.Complete(ChartLayout);
        interaction.SelectionProgress = 1;
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
        var allowed = !Interpolator.IsEntrance && (ChartLayout.Slices.Count > 0) && UpdateAnimationEnabled && CanAnimate;
        if (allowed)
        {
            Interpolator.Capture(ChartLayout);
        }
        else if (!Interpolator.IsEntrance)
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
            else if (captured && (ChartLayout.Slices.Count > 0) && Interpolator.Begin(ChartLayout, pendingMap, pendingPositional || (pendingMap is null), snapshot.Count))
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
        if ((entranceShown && !entranceRequested) || (ChartLayout.Slices.Count == 0))
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
        Interpolator.BeginEntrance(ChartLayout, EntranceAnimation);
        if (!dataAnimator.Start(length))
        {
            Interpolator.Complete(ChartLayout);
        }

        return true;
    }

    private void SetSelectionState(int index, bool animate)
    {
        if (interaction.Select(index))
        {
            AnimateSelection(animate);
        }
    }

    private void ClearSelectionState(bool animate)
    {
        if (interaction.ClearSelection())
        {
            AnimateSelection(animate);
        }
    }

    private void AnimateSelection(bool animate)
    {
        interaction.SelectionProgress = 0;
        if (!animate || !selectionAnimator.Start(ResolveAnimationLength(SelectionMilliseconds)))
        {
            interaction.SelectionProgress = 1;
        }
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

    private void OnSelectionProgress(double progress)
    {
        interaction.SelectionProgress = progress;
        InvalidateSurface();
    }
}
