namespace ClamChart.Layout;

internal static class ThresholdResolver
{
    public static ChartThreshold? Resolve(IList<ChartThreshold> thresholds, double value)
    {
        if (!Double.IsFinite(value))
        {
            return null;
        }

        ChartThreshold? active = null;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < thresholds.Count; i++)
        {
            var threshold = thresholds[i];
            if ((threshold.Value <= value) && ((active is null) || (threshold.Value >= active.Value)))
            {
                active = threshold;
            }
        }

        return active;
    }

    public static Color? GetColor(ChartThreshold? threshold, ChartStyle style)
    {
        if (threshold is null)
        {
            return null;
        }

        return threshold.Color ?? threshold.Level switch
        {
            ChartThresholdLevel.Good => style.GoodColor,
            ChartThresholdLevel.Warning => style.WarningColor,
            ChartThresholdLevel.Serious => style.SeriousColor,
            ChartThresholdLevel.Critical => style.CriticalColor,
            _ => null
        };
    }
}
