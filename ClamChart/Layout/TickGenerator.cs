namespace ClamChart.Layout;

using ClamChart.Data;

internal static class TickGenerator
{
    public const int MaxTickCount = 1000;

    public const double HorizontalLabelSpacing = 64;

    public const double VerticalLabelSpacing = 40;

    private const int MaxYear = 9999;

    private static readonly TimeInterval[] TimeIntervals =
    [
        new(ChartDateTimeIntervalType.Seconds, 1),
        new(ChartDateTimeIntervalType.Seconds, 2),
        new(ChartDateTimeIntervalType.Seconds, 5),
        new(ChartDateTimeIntervalType.Seconds, 10),
        new(ChartDateTimeIntervalType.Seconds, 15),
        new(ChartDateTimeIntervalType.Seconds, 30),
        new(ChartDateTimeIntervalType.Minutes, 1),
        new(ChartDateTimeIntervalType.Minutes, 2),
        new(ChartDateTimeIntervalType.Minutes, 5),
        new(ChartDateTimeIntervalType.Minutes, 10),
        new(ChartDateTimeIntervalType.Minutes, 15),
        new(ChartDateTimeIntervalType.Minutes, 30),
        new(ChartDateTimeIntervalType.Hours, 1),
        new(ChartDateTimeIntervalType.Hours, 3),
        new(ChartDateTimeIntervalType.Hours, 6),
        new(ChartDateTimeIntervalType.Hours, 12),
        new(ChartDateTimeIntervalType.Days, 1),
        new(ChartDateTimeIntervalType.Days, 2),
        new(ChartDateTimeIntervalType.Weeks, 1),
        new(ChartDateTimeIntervalType.Weeks, 2),
        new(ChartDateTimeIntervalType.Months, 1),
        new(ChartDateTimeIntervalType.Months, 2),
        new(ChartDateTimeIntervalType.Months, 3),
        new(ChartDateTimeIntervalType.Months, 6),
        new(ChartDateTimeIntervalType.Years, 1),
        new(ChartDateTimeIntervalType.Years, 2),
        new(ChartDateTimeIntervalType.Years, 5),
        new(ChartDateTimeIntervalType.Years, 10),
        new(ChartDateTimeIntervalType.Years, 20),
        new(ChartDateTimeIntervalType.Years, 50),
        new(ChartDateTimeIntervalType.Years, 100),
        new(ChartDateTimeIntervalType.Years, 200),
        new(ChartDateTimeIntervalType.Years, 500),
        new(ChartDateTimeIntervalType.Years, 1000),
        new(ChartDateTimeIntervalType.Years, 2000),
        new(ChartDateTimeIntervalType.Years, 5000)
    ];

    public static int GetMaxDivisions(double length, double spacing, int? maxLabelCount)
    {
        if (maxLabelCount is { } count)
        {
            return Math.Clamp(count - 1, 1, MaxTickCount);
        }

        return Double.IsFinite(length) && (length > 0) && (spacing > 0) ? Math.Max(1, (int)Math.Min(Math.Floor(length / spacing), MaxTickCount)) : 1;
    }

    public static void Numeric(AxisScale scale, List<double> ticks)
    {
        ticks.Clear();
        var interval = scale.Interval;
        if (!(interval > 0) || !(scale.Maximum >= scale.Minimum))
        {
            return;
        }

        var (first, count) = GetMultiples(scale, interval);
        if (count > MaxTickCount)
        {
            interval = NiceScale.Multiple(Math.Ceiling(count / MaxTickCount), interval);
            (first, count) = GetMultiples(scale, interval);
        }

        for (var i = 0; i < count; i++)
        {
            ticks.Add(NiceScale.Multiple(first + i, interval));
        }
    }

    public static void Aligned(AxisScale primary, IReadOnlyList<double> primaryTicks, AxisScale secondary, List<double> ticks)
    {
        ticks.Clear();
        var span = primary.Span;
        if (!(span > 0))
        {
            return;
        }

        for (var i = 0; i < primaryTicks.Count; i++)
        {
            var fraction = (primaryTicks[i] - primary.Minimum) / span;
            var value = (secondary.Minimum * (1 - fraction)) + (secondary.Maximum * fraction);
            ticks.Add(NiceScale.Snap(value, secondary.Interval));
        }
    }

    public static void Category(int count, List<double> ticks)
    {
        ticks.Clear();
        for (var i = 0; i < count; i++)
        {
            ticks.Add(i);
        }
    }

    public static TimeInterval SelectTimeInterval(double span, double length, int maxDivisions, double gap, TimeLabelWidths labelWidths, ChartDateTimeIntervalType minimumUnit = ChartDateTimeIntervalType.Auto)
    {
        foreach (var interval in TimeIntervals)
        {
            if (interval.Unit < minimumUnit)
            {
                continue;
            }

            if (!(span > 0))
            {
                return interval;
            }

            var ticks = (double)interval.MinimumTicks;
            if (((span / ticks) <= maxDivisions + NiceScale.Tolerance) && ((length * ticks / span) >= labelWidths.Get(interval.LabelPrecision) + gap))
            {
                return interval;
            }
        }

        return TimeIntervals[^1];
    }

    public static void Time(double minimum, double maximum, TimeInterval interval, DayOfWeek firstDayOfWeek, List<double> ticks)
    {
        ticks.Clear();
        if (!(maximum >= minimum))
        {
            return;
        }

        var start = ChartDateTime.ClampTicks(minimum);
        var end = ChartDateTime.ClampTicks(maximum);
        var count = Math.Max(interval.Count, 1);
        switch (interval.Unit)
        {
            case ChartDateTimeIntervalType.Months:
                AddMonths(start, end, count, ticks);
                break;
            case ChartDateTimeIntervalType.Years:
                AddYears(start, end, count, ticks);
                break;
            default:
                var offset = interval.Unit == ChartDateTimeIntervalType.Weeks ? (((int)firstDayOfWeek + 6) % 7) * TimeSpan.TicksPerDay : 0;
                AddFixed(start, end, interval.MinimumTicks, offset, ticks);
                break;
        }
    }

    private static (double First, double Count) GetMultiples(AxisScale scale, double interval)
    {
        var first = Math.Ceiling((scale.Minimum / interval) - NiceScale.Tolerance);
        var last = Math.Floor((scale.Maximum / interval) + NiceScale.Tolerance);
        return (first, Math.Max(0, last - first + 1));
    }

    private static void AddFixed(long start, long end, long step, long offset, List<double> ticks)
    {
        var quotient = Math.DivRem(start - offset, step, out var remainder);
        var tick = offset + ((remainder > 0 ? quotient + 1 : quotient) * step);
        while ((tick <= end) && (ticks.Count < MaxTickCount))
        {
            ticks.Add(tick);
            tick += step;
        }
    }

    private static void AddMonths(long start, long end, int count, List<double> ticks)
    {
        var date = new DateTime(start);
        var month = ((long)date.Year * 12) + date.Month - 1;
        if (date != new DateTime(date.Year, date.Month, 1))
        {
            month++;
        }

        month = CeilingToMultiple(month, count);
        while (((month / 12) <= MaxYear) && (ticks.Count < MaxTickCount))
        {
            var tick = new DateTime((int)(month / 12), (int)(month % 12) + 1, 1).Ticks;
            if (tick > end)
            {
                break;
            }

            ticks.Add(tick);
            month += count;
        }
    }

    private static void AddYears(long start, long end, int count, List<double> ticks)
    {
        var date = new DateTime(start);
        var year = (long)date.Year;
        if (date != new DateTime(date.Year, 1, 1))
        {
            year++;
        }

        year = CeilingToMultiple(year, count);
        while ((year <= MaxYear) && (ticks.Count < MaxTickCount))
        {
            var tick = new DateTime((int)year, 1, 1).Ticks;
            if (tick > end)
            {
                break;
            }

            ticks.Add(tick);
            year += count;
        }
    }

    private static long CeilingToMultiple(long value, int multiple) => ((value + multiple - 1) / multiple) * multiple;
}
