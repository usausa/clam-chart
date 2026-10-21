namespace ClamChart;

using ClamChart.Animation;

public abstract partial class ChartViewBase
{
    private const string EffectAnimationName = "ClamChartEffect";

    private ChartAnimator? effectAnimator;

    internal virtual double EffectMilliseconds => 0;

    internal bool IsEffectAnimating => effectAnimator is { IsRunning: true };

    internal double EffectPhase => !IsSnapshot && (effectAnimator is { IsRunning: true } animator) ? animator.Progress : Double.NaN;

    internal void StopEffect()
    {
        if (effectAnimator is { IsRunning: true } animator)
        {
            animator.Stop();
            InvalidateCanvas();
        }
    }

    private void UpdateEffect()
    {
        var milliseconds = IsBusy || windowStopped ? 0 : EffectMilliseconds;
        if (milliseconds <= 0)
        {
            effectAnimator?.Stop();
            return;
        }

        if (IsEffectAnimating)
        {
            return;
        }

        var length = ResolveAnimationLength(milliseconds);
        if (length < 1)
        {
            return;
        }

        effectAnimator ??= new ChartAnimator(this, EffectAnimationName, OnEffectProgress);
        effectAnimator.Start(length, true);
    }

    private void OnEffectProgress(double progress) => InvalidateCanvas();
}
