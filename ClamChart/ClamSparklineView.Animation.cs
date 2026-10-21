namespace ClamChart;

using ClamChart.Animation;

public partial class ClamSparklineView
{
    private const string DataAnimationName = "ClamSparklineViewData";

    internal SparklineInterpolator Interpolator => core.Interpolator;

    internal override bool IsAnimating => core.IsAnimating;

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
