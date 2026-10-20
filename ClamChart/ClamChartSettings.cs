namespace ClamChart;

public static partial class ClamChartSettings
{
    public static bool AnimationEnabled { get; set; } = true;

    public static double AnimationScale { get; set => field = (Double.IsFinite(value) && (value >= 0)) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be a finite value of zero or more."); } = 1;

    public static bool RespectSystemAnimationScale { get; set; } = true;

    internal static double ResolveAnimationScale()
    {
        if (!AnimationEnabled)
        {
            return 0;
        }

        var scale = AnimationScale;
        if (RespectSystemAnimationScale)
        {
            ApplySystemAnimationScale(ref scale);
        }

        return scale;
    }

    static partial void ApplySystemAnimationScale(ref double scale);
}
