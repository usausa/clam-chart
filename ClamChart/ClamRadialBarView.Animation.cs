namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;

public partial class ClamRadialBarView
{
    private const string DataAnimationName = "ClamRadialBarViewData";

    private const string SelectionAnimationName = "ClamRadialBarViewSelection";

    private const string CenterAnimationName = "ClamRadialBarViewCenter";

    private const double SelectionMilliseconds = 150;

    private const double StaggerMilliseconds = 40;

    private readonly ChartAnimator dataAnimator;
    private readonly ChartAnimator selectionAnimator;
    private int[]? pendingMap;
    private bool pendingPositional;
    private bool sourceReplaced;
    private bool entranceShown;
    private bool entranceRequested;
    private int countedKey = RadialBarLayout.DefaultCenterKey;

    internal GaugeInterpolator<RadialBarRow> Interpolator { get; } = new();

    internal ChartCountUp CenterCount { get; }

    internal override bool IsAnimating => dataAnimator.IsRunning || selectionAnimator.IsRunning || CenterCount.IsRunning;

    internal override void StartEntranceAnimation()
    {
        entranceRequested = true;
        EnsureLayout();
        if (TryStartEntrance())
        {
            SyncCenterCount(true, false);
            InvalidateCanvas();
        }
    }

    internal override void FinishAnimations()
    {
        dataAnimator.Stop();
        selectionAnimator.Stop();
        CenterCount.Complete(CenterCount.Target);
        Interpolator.Complete(ChartLayout.Rows);
        interaction.SelectionProgress = 1;
        InvalidateCanvas();
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
        var entrance = TryStartEntrance();
        if (!entrance)
        {
            if (Interpolator.IsEntrance)
            {
                Interpolator.Retarget(ChartLayout.Rows);
            }
            else if (captured && (ChartLayout.Rows.Count > 0) && Interpolator.Begin(ChartLayout.Rows, pendingMap, pendingPositional || (pendingMap is null), snapshot.Count))
            {
                if (!dataAnimator.Start(ResolveAnimationLength(AnimationDuration)))
                {
                    Interpolator.Complete(ChartLayout.Rows);
                }
            }
            else
            {
                Interpolator.Complete(ChartLayout.Rows);
            }
        }

        SyncCenterCount(entrance, captured);
        ReleasePendingMap();
        pendingPositional = false;
    }

    private void SyncCenterCount(bool entrance, bool animate)
    {
        var key = ChartLayout.GetCenterKey(interaction.SelectedKey);
        var center = ChartLayout.GetCenter(interaction.SelectedKey);
        var number = center.IsVisible ? center.Number : Double.NaN;
        var round = !center.IsPercent && ChartLayout.ValueFormatting.IsDefault;
        var length = ResolveAnimationLength(AnimationDuration);
        if (entrance)
        {
            CenterCount.Start(0, number, length, true, round);
        }
        else if (CenterCount.IsEntrance && Interpolator.IsEntrance && (key == countedKey))
        {
            CenterCount.Retarget(number, round);
        }
        else if (animate && (key == countedKey))
        {
            CenterCount.Start(CenterCount.Current, number, length, false, round);
        }
        else
        {
            CenterCount.Complete(number);
        }

        countedKey = key;
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
        Interpolator.BeginEntrance(ChartLayout.Rows, EntranceAnimation, ChartProgress.GetStagger(ChartLayout.Rows.Count, StaggerMilliseconds, length));
        if (!dataAnimator.Start(length))
        {
            Interpolator.Complete(ChartLayout.Rows);
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
            Interpolator.Complete(ChartLayout.Rows);
        }

        InvalidateCanvas();
    }

    private void OnSelectionProgress(double progress)
    {
        interaction.SelectionProgress = progress;
        InvalidateCanvas();
    }
}
