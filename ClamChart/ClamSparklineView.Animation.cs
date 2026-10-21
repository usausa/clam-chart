namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Rendering;

public partial class ClamSparklineView
{
    private const string DataAnimationName = "ClamSparklineViewData";

    internal SparklineInterpolator Interpolator => core.Interpolator;

    internal override bool IsAnimating => core.IsAnimating;

    internal override double EffectMilliseconds => LiveIndicatorVisible && !ChartLayout.IsBar && (ChartLayout.LastIndex >= 0) ? LiveIndicator.Milliseconds : 0;

    internal override void StartEntranceAnimation()
    {
        core.RequestEntrance();
        EnsureLayout();
        if (core.TryStartEntrance(ResolveEntrance(), ResolveAnimationLength(AnimationDuration)))
        {
            InvalidateCanvas();
        }
    }

    internal override void FinishAnimations()
    {
        core.Finish();
        InvalidateCanvas();
    }

    private bool CanAnimate => (ResolveAnimationLength(AnimationDuration) >= 1) && AnimationDriver.CanStart;

    private ChartEntranceAnimation ResolveEntrance() => EntranceAnimation == ChartEntranceAnimation.Auto ? ChartEntranceAnimation.None : EntranceAnimation;
}
