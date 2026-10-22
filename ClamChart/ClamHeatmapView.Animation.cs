namespace ClamChart;

using ClamChart.Animation;

public partial class ClamHeatmapView
{
    private const string DataAnimationName = "ClamHeatmapViewData";

    private const string SelectionAnimationName = "ClamHeatmapViewSelection";

    private const double SelectionMilliseconds = 150;

    private readonly ChartAnimator dataAnimator;
    private readonly ChartAnimator selectionAnimator;
    private bool sourceReplaced;
    private bool entranceShown;
    private bool entranceRequested;

    internal HeatmapInterpolator Interpolator { get; } = new();

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
        Interpolator.Complete(ChartLayout);
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
    }

    private bool PrepareTransition()
    {
        Interpolator.Easing = AnimationEasing;
        var allowed = !Interpolator.IsEntrance && (ChartLayout.CellCount > 0) && UpdateAnimationEnabled && CanAnimate;
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
        if (TryStartEntrance())
        {
            return;
        }

        if (Interpolator.IsEntrance)
        {
            Interpolator.Retarget(ChartLayout);
        }
        else if (captured && Interpolator.Begin(ChartLayout))
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

    private bool TryStartEntrance()
    {
        if ((entranceShown && !entranceRequested) || (RenderedPointCount == 0))
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

        InvalidateCanvas();
    }

    private void OnSelectionProgress(double progress)
    {
        interaction.SelectionProgress = progress;
        InvalidateCanvas();
    }
}
