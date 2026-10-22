namespace ClamChart;

using ClamChart.Animation;

public partial class ClamRadarChartView
{
    private const string DataAnimationName = "ClamRadarChartViewData";

    private const string SelectionAnimationName = "ClamRadarChartViewSelection";

    private const double SelectionMilliseconds = 150;

    private readonly ChartAnimator dataAnimator;
    private readonly ChartAnimator selectionAnimator;
    private bool dataChanged;
    private bool seriesChanged = true;
    private bool entranceShown;
    private bool entranceRequested;

    internal RadarInterpolator Interpolator { get; } = new();

    internal override bool IsAnimating => dataAnimator.IsRunning || selectionAnimator.IsRunning;

    internal override void StartEntranceAnimation()
    {
        entranceRequested = true;
        EnsureLayout();
        if (TryStartEntrance())
        {
            InvalidateCanvas();
        }
    }

    internal override void FinishAnimations()
    {
        dataAnimator.Stop();
        selectionAnimator.Stop();
        Interpolator.Complete(ChartLayout, layoutSeries);
        interaction.SelectionProgress = 1;
        InvalidateCanvas();
    }

    private bool CanAnimate => (ResolveAnimationLength(AnimationDuration) >= 1) && AnimationDriver.CanStart;

    private void RecordChange(ChartSeries series, int oldCount, int newCount)
    {
        dataChanged = true;
        if (series.SourceReplaced)
        {
            series.SourceReplaced = false;
            entranceRequested |= entranceShown && (oldCount != newCount) && (newCount > 0);
        }
    }

    private bool PrepareTransition()
    {
        Interpolator.Easing = AnimationEasing;
        var visibility = HasVisibilityChange();
        var allowed = !seriesChanged && !Interpolator.IsEntrance && (layoutSeries.Count == attached.Count) && CanAnimate && (UpdateAnimationEnabled || (visibility && !dataChanged));
        if (allowed)
        {
            Interpolator.Capture();
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
                Interpolator.Retarget(ChartLayout, layoutSeries);
            }
            else if (captured && Interpolator.Begin(ChartLayout, layoutSeries))
            {
                if (!dataAnimator.Start(ResolveAnimationLength(AnimationDuration)))
                {
                    Interpolator.Complete(ChartLayout, layoutSeries);
                }
            }
            else
            {
                Interpolator.Complete(ChartLayout, layoutSeries);
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
        Interpolator.BeginEntrance(ChartLayout, layoutSeries, EntranceAnimation);
        if (!dataAnimator.Start(length))
        {
            Interpolator.Complete(ChartLayout, layoutSeries);
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
            Interpolator.Complete(ChartLayout, layoutSeries);
        }

        InvalidateCanvas();
    }

    private void OnSelectionProgress(double progress)
    {
        interaction.SelectionProgress = progress;
        InvalidateCanvas();
    }
}
