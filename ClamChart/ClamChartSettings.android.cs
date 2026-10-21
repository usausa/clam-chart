namespace ClamChart;

using Android.Animation;
using Android.Provider;

public static partial class ClamChartSettings
{
    static partial void ApplySystemAnimationScale(ref double scale)
    {
        if (!ValueAnimator.AreAnimatorsEnabled())
        {
            scale = 0;
            return;
        }

        scale *= OperatingSystem.IsAndroidVersionAtLeast(33)
            ? ValueAnimator.DurationScale
            : Settings.Global.GetFloat(Android.App.Application.Context.ContentResolver, Settings.Global.AnimatorDurationScale, 1f);
    }
}
