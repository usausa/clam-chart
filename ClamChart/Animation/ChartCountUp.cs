namespace ClamChart.Animation;

internal sealed class ChartCountUp
{
    private const int MaxDigits = 2;

    private const double Tolerance = 1e-9;

    private readonly ChartViewBase owner;
    private readonly ChartAnimator animator;
    private double from = Double.NaN;
    private int digits = -1;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool IsRunning => animator.IsRunning;

    public bool IsEntrance { get; private set; }

    public double Target { get; private set; } = Double.NaN;

    public double Current => animator.IsRunning ? Interpolate(from, Target, Easing.Ease(animator.Progress), digits) : Target;

    public double OverlayAlpha => IsEntrance && animator.IsRunning ? Easing.Ease(ChartProgress.Delayed(animator.Progress, ChartProgress.OverlayDelay)) : 1;

    public ChartCountUp(ChartViewBase owner, string name)
    {
        this.owner = owner;
        animator = new ChartAnimator(owner, name, OnProgress);
    }

    public static double Interpolate(double from, double to, double progress, int digits)
    {
        var value = ChartProgress.Lerp(from, to, progress);
        return digits >= 0 ? Math.Round(value, digits, MidpointRounding.AwayFromZero) : value;
    }

    public static int GetDigits(double value)
    {
        if (!Double.IsFinite(value))
        {
            return 0;
        }

        var scaled = Math.Abs(value);
        for (var count = 0; count < MaxDigits; count++)
        {
            if (Math.Abs(scaled - Math.Round(scaled)) <= Tolerance * Math.Max(1, scaled))
            {
                return count;
            }

            scaled *= 10;
        }

        return MaxDigits;
    }

    public bool Start(double start, double target, double length, bool entrance, bool round)
    {
        from = start;
        Target = target;
        IsEntrance = entrance;
        digits = round ? Math.Max(GetDigits(start), GetDigits(target)) : -1;
        if (!Double.IsFinite(start) || !Double.IsFinite(target) || (Math.Abs(target - start) < Tolerance) || !animator.Start(length))
        {
            Complete(target);
            return false;
        }

        return true;
    }

    public void Retarget(double target, bool round)
    {
        if (!animator.IsRunning || !Double.IsFinite(target))
        {
            Complete(target);
            return;
        }

        Target = target;
        digits = round ? Math.Max(GetDigits(from), GetDigits(target)) : -1;
    }

    public void Complete(double target)
    {
        animator.Stop();
        from = target;
        Target = target;
        IsEntrance = false;
    }

    private void OnProgress(double progress)
    {
        if (!animator.IsRunning)
        {
            IsEntrance = false;
        }

        owner.InvalidateSurface();
    }
}
