namespace ClamChart;

using ClamChart.Animation;
using ClamChart.Rendering;

public partial class ClamStatView
{
    private const string ValueAnimationName = "ClamStatViewValue";

    private const string TrendAnimationName = "ClamStatViewTrend";

    private bool entranceShown;
    private bool entranceRequested;

    internal ChartCountUp CountUp { get; }

    internal SparklineInterpolator TrendInterpolator => trend.Interpolator;

    internal override bool IsAnimating => CountUp.IsRunning || trend.IsAnimating;

    internal override double EffectMilliseconds => AlertAnimationEnabled && critical && (ColorMode != ChartStatColorMode.None) ? AlertPulse.Milliseconds : 0;

    internal override void StartEntranceAnimation()
    {
        entranceRequested = true;
        trend.RequestEntrance();
        EnsureLayout();
        var started = TryStartEntrance();
        if (trend.TryStartEntrance(ResolveTrendEntrance(), ResolveAnimationLength(AnimationDuration)) || started)
        {
            InvalidateCanvas();
        }
    }

    internal override void FinishAnimations()
    {
        CountUp.Complete(Value);
        trend.Finish();
        InvalidateCanvas();
    }

    private bool CanAnimate => (ResolveAnimationLength(AnimationDuration) >= 1) && AnimationDriver.CanStart;

    private ChartEntranceAnimation ResolveTrendEntrance() => EntranceAnimation == ChartEntranceAnimation.Auto ? ChartEntranceAnimation.Reveal : EntranceAnimation;

    private void AnimateValue(double value)
    {
        if (released)
        {
            return;
        }

        if (!entranceShown)
        {
            CountUp.Complete(value);
            return;
        }

        if (CountUp.IsEntrance)
        {
            CountUp.Retarget(value, formatting.IsDefault);
            return;
        }

        if (UpdateAnimationEnabled && CanAnimate)
        {
            CountUp.Start(CountUp.Current, value, ResolveAnimationLength(AnimationDuration), false, formatting.IsDefault);
        }
        else
        {
            CountUp.Complete(value);
        }
    }

    private bool TryStartEntrance()
    {
        var value = Value;
        if ((entranceShown && !entranceRequested) || !Double.IsFinite(value))
        {
            return false;
        }

        var length = ResolveAnimationLength(AnimationDuration);
        if ((EntranceAnimation == ChartEntranceAnimation.None) || (length < 1))
        {
            entranceShown = true;
            entranceRequested = false;
            CountUp.Complete(value);
            return false;
        }

        if (!AnimationDriver.CanStart)
        {
            return false;
        }

        entranceShown = true;
        entranceRequested = false;
        return CountUp.Start(0, value, length, true, formatting.IsDefault);
    }
}
