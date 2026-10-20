namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Data;

public partial class ClamChartView
{
    private const string DataAnimationName = "ClamChartViewData";

    private const string SelectionAnimationName = "ClamChartViewSelection";

    private const double SelectionMilliseconds = 150;

    private readonly List<int[]?> pendingMaps = [];
    private readonly ChartAnimator dataAnimator;
    private readonly ChartAnimator selectionAnimator;
    private bool dataChanged;
    private bool seriesChanged = true;
    private bool entranceShown;
    private bool entranceRequested;

    internal SnapshotInterpolator Interpolator { get; } = new();

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

    private void RecordChange(int index, ChartSeries series, int oldCount, int newCount)
    {
        while (pendingMaps.Count <= index)
        {
            pendingMaps.Add(null);
        }

        dataChanged = true;
        if (series.SourceReplaced)
        {
            series.SourceReplaced = false;
            entranceRequested |= entranceShown && (oldCount != newCount) && (newCount > 0);
        }

        if (pendingMaps[index] is { Length: 0 })
        {
            return;
        }

        var map = DataSnapshot.Rent<int>(oldCount);
        if (!ChangeMap.TryBuild(series.Source.Changes, oldCount, newCount, map.AsSpan(0, oldCount)))
        {
            DataSnapshot.Return(map);
            if (pendingMaps[index] is { } stale)
            {
                DataSnapshot.Return(stale);
            }

            pendingMaps[index] = [];
            return;
        }

        if (pendingMaps[index] is { } previous)
        {
            ChangeMap.Compose(previous, map.AsSpan(0, oldCount));
            DataSnapshot.Return(map);
        }
        else
        {
            pendingMaps[index] = map;
        }
    }

    private bool PrepareTransition()
    {
        Interpolator.Easing = AnimationEasing;
        var visibility = HasVisibilityChange();
        var allowed = !seriesChanged && !Interpolator.IsEntrance && (layoutSeries.Count == attached.Count) && CanAnimate && (UpdateAnimationEnabled || (visibility && !dataChanged));
        if (allowed)
        {
            Interpolator.Capture(ChartLayout, layoutSeries);
        }
        else if (!Interpolator.IsEntrance)
        {
            dataAnimator.Stop();
        }

        return allowed;
    }

    private void CompleteTransition(bool captured)
    {
        if (!TryStartEntrance() && !Interpolator.IsEntrance)
        {
            if (captured && Interpolator.Begin(ChartLayout, layoutSeries, pendingMaps))
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

        for (var i = 0; i < pendingMaps.Count; i++)
        {
            if (pendingMaps[i] is { } map)
            {
                DataSnapshot.Return(map);
                pendingMaps[i] = null;
            }
        }

        dataChanged = false;
        seriesChanged = false;
    }

    private bool TryStartEntrance()
    {
        if ((entranceShown && !entranceRequested) || !HasVisibleData())
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

    private bool HasVisibilityChange()
    {
        var changed = false;
        foreach (var series in attached)
        {
            changed |= series.VisibilityChanged;
            series.VisibilityChanged = false;
        }

        return changed;
    }

    private void SetSelectionState(int seriesIndex, int index, bool category, bool animate)
    {
        if (interaction.Select(seriesIndex, index, category))
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
