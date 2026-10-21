namespace ClamChart.Layout;

internal static class AxisRangeResolver
{
    private const double Limit = 1e300;

    private const double Epsilon = 1e-300;

    private const double MinimumSpan = 1e-290;

    private const double RelativeSpan = 1e-12;

    private const int MaxIterations = 64;

    private const double TimePadding = TimeSpan.TicksPerMinute;

    public static bool HasInvertedBounds(AxisRangeOptions options) =>
        (options.Minimum is { } minimum) && (options.Maximum is { } maximum) && (maximum <= minimum);

    public static AxisScale Resolve(double dataMinimum, double dataMaximum, AxisRangeOptions options, int maxDivisions)
    {
        var (minimum, maximum) = GetFixedBounds(options);
        var (low, high) = GetBounds(dataMinimum, dataMaximum, options, minimum, maximum);
        var niceMinimum = options.NiceRange && (minimum is null);
        var niceMaximum = options.NiceRange && (maximum is null);
        if ((ClampOption(options.Interval) is not { } interval) || (interval <= 0))
        {
            return NiceScale.Create(low, high, maxDivisions, niceMinimum, niceMaximum);
        }

        return new AxisScale(niceMinimum ? NiceScale.Floor(low, interval) : low, niceMaximum ? NiceScale.Ceiling(high, interval) : high, interval);
    }

    public static AxisScale ResolveAligned(double dataMinimum, double dataMaximum, AxisRangeOptions options, AxisScale primary)
    {
        var (minimum, maximum) = GetFixedBounds(options);
        var (low, high) = GetBounds(dataMinimum, dataMaximum, options, minimum, maximum);
        var steps = SnapIndex(primary.Span / primary.Interval);
        if (!Double.IsFinite(steps) || (steps <= 0))
        {
            return new AxisScale(low, high, high - low);
        }

        if (((minimum is not null) && (maximum is not null)) || !options.NiceRange)
        {
            return new AxisScale(low, high, (high - low) / steps);
        }

        if (minimum is not null)
        {
            var step = NiceScale.Step((high - low) / steps);
            return new AxisScale(low, low + (step * steps), step);
        }

        if (maximum is not null)
        {
            var step = NiceScale.Step((high - low) / steps);
            return new AxisScale(high - (step * steps), high, step);
        }

        var offset = SnapIndex((NiceScale.Ceiling(primary.Minimum, primary.Interval) - primary.Minimum) / primary.Interval);
        var interval = NiceScale.Step((high - low) / steps);
        for (var i = 0; i < MaxIterations; i++)
        {
            var index = Math.Floor((low / interval) + offset + NiceScale.Tolerance);
            var end = NiceScale.Multiple(index - offset + steps, interval);
            if (end >= high - (interval * NiceScale.Tolerance))
            {
                return new AxisScale(NiceScale.Multiple(index - offset, interval), end, interval);
            }

            interval = NiceScale.NextStep(interval);
        }

        return new AxisScale(low, high, (high - low) / steps);
    }

    public static (double Minimum, double Maximum)? GetWindow(double dataMaximum, AxisRangeOptions options)
    {
        if (options.Window is not { } window)
        {
            return null;
        }

        var end = ClampOption(options.Maximum) ?? ClampTime(dataMaximum);
        return Double.IsNaN(end) || (end <= 0) ? null : (Math.Max(0, end - window), end);
    }

    public static (double Minimum, double Maximum) ResolveTime(double dataMinimum, double dataMaximum, AxisRangeOptions options)
    {
        if (GetWindow(dataMaximum, options) is { } window)
        {
            return window;
        }

        var (minimum, maximum) = GetFixedBounds(options);
        var low = minimum ?? ClampTime(dataMinimum);
        var high = maximum ?? ClampTime(dataMaximum);
        if (Double.IsNaN(low) && Double.IsNaN(high))
        {
            return (0, TimePadding * 2);
        }

        if (minimum is not null)
        {
            return (low, high > low ? high : low + GetTimePadding(low));
        }

        if (maximum is not null)
        {
            return (low < high ? low : Math.Max(0, high - GetTimePadding(high)), high);
        }

        low = Double.IsNaN(low) ? high : low;
        high = Double.IsNaN(high) ? low : high;
        return high > low ? (low, high) : (Math.Max(0, low - GetTimePadding(low)), high + GetTimePadding(low));
    }

    public static AxisScale ResolveCategory(int count, ChartLabelPlacement placement) =>
        (placement == ChartLabelPlacement.OnTicks) && (count > 1) ? new AxisScale(0, count - 1, 1) : new AxisScale(-0.5, Math.Max(count, 1) - 0.5, 1);

    public static (double? Minimum, double? Maximum) GetFixedBounds(AxisRangeOptions options)
    {
        var minimum = ClampOption(options.Minimum);
        var maximum = ClampOption(options.Maximum);
        return (minimum is { } low) && (maximum is { } high) && (high <= low) ? (null, null) : (minimum, maximum);
    }

    private static (double Low, double High) GetBounds(double dataMinimum, double dataMaximum, AxisRangeOptions options, double? minimum, double? maximum)
    {
        var low = Min(ClampValue(dataMinimum), ClampOption(options.SoftMinimum));
        var high = Max(ClampValue(dataMaximum), ClampOption(options.SoftMaximum));
        if (options.IncludeZero)
        {
            low = Min(low, 0);
            high = Max(high, 0);
        }

        if (Double.IsNaN(low))
        {
            low = high;
        }
        else if (Double.IsNaN(high))
        {
            high = low;
        }
        else if (low > high)
        {
            (low, high) = (high, low);
        }

        if (minimum is { } fixedMinimum)
        {
            if (maximum is { } fixedMaximum)
            {
                return (fixedMinimum, fixedMaximum);
            }

            return (fixedMinimum, HasSpan(fixedMinimum, high) ? high : fixedMinimum + GetPadding(fixedMinimum));
        }

        if (maximum is { } onlyMaximum)
        {
            return (HasSpan(low, onlyMaximum) ? low : onlyMaximum - GetPadding(onlyMaximum), onlyMaximum);
        }

        if (HasSpan(low, high))
        {
            return (low, high);
        }

        if (Double.IsNaN(low) || (low == 0))
        {
            return (0, 1);
        }

        var padding = GetPadding(low);
        return (low - padding, low + padding);
    }

    private static bool HasSpan(double low, double high) =>
        (high - low) > Math.Max(MinimumSpan, Math.Max(Math.Abs(low), Math.Abs(high)) * RelativeSpan);

    private static double GetPadding(double value) => value == 0 ? 1 : Math.Abs(value) / 10;

    private static double SnapIndex(double value)
    {
        var index = Math.Round(value);
        return Math.Abs(value - index) <= NiceScale.Tolerance ? index : value;
    }

    private static double Min(double value, double? other) =>
        (other is { } candidate) && (Double.IsNaN(value) || (candidate < value)) ? candidate : value;

    private static double Max(double value, double? other) =>
        (other is { } candidate) && (Double.IsNaN(value) || (candidate > value)) ? candidate : value;

    private static double ClampValue(double value)
    {
        if (!Double.IsFinite(value))
        {
            return Double.NaN;
        }

        return Math.Abs(value) < Epsilon ? 0 : Math.Clamp(value, -Limit, Limit);
    }

    private static double? ClampOption(double? value) => (value is { } number) && Double.IsFinite(number) ? ClampValue(number) : null;

    private static double ClampTime(double value) => Double.IsFinite(value) ? Math.Clamp(value, 0, DateTime.MaxValue.Ticks) : Double.NaN;

    private static double GetTimePadding(double ticks)
    {
        var value = (long)ticks;
        if ((value % TimeSpan.TicksPerDay) == 0)
        {
            return TimeSpan.TicksPerDay;
        }

        return (value % TimeSpan.TicksPerHour) == 0 ? TimeSpan.TicksPerHour : TimeSpan.TicksPerMinute;
    }
}
