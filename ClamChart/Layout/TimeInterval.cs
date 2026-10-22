namespace ClamChart.Layout;

internal readonly record struct TimeInterval(ChartDateTimeIntervalType Unit, int Count)
{
    public long MinimumTicks => Count <= (DateTime.MaxValue.Ticks / UnitTicks) ? Math.Max(Count, 1) * UnitTicks : DateTime.MaxValue.Ticks;

    public TimeLabelPrecision LabelPrecision => Unit switch
    {
        ChartDateTimeIntervalType.Months => Count < 2 ? TimeLabelPrecision.Day : TimeLabelPrecision.Month,
        ChartDateTimeIntervalType.Years => TimeLabelPrecision.Month,
        _ => GetFixedPrecision(MinimumTicks)
    };

    public string DefaultFormat => GetDefaultFormat(LabelPrecision);

    private long UnitTicks => Unit switch
    {
        ChartDateTimeIntervalType.Minutes => TimeSpan.TicksPerMinute,
        ChartDateTimeIntervalType.Hours => TimeSpan.TicksPerHour,
        ChartDateTimeIntervalType.Days => TimeSpan.TicksPerDay,
        ChartDateTimeIntervalType.Weeks => TimeSpan.TicksPerDay * 7,
        ChartDateTimeIntervalType.Months => TimeSpan.TicksPerDay * 28,
        ChartDateTimeIntervalType.Years => TimeSpan.TicksPerDay * 365,
        _ => TimeSpan.TicksPerSecond
    };

    public string GetTickFormat(double value)
    {
        var precision = LabelPrecision;
        var ticks = (long)value;
        if ((precision > TimeLabelPrecision.Minute) || ((ticks / TimeSpan.TicksPerDay) == ((ticks - MinimumTicks) / TimeSpan.TicksPerDay)))
        {
            return DefaultFormat;
        }

        return (ticks % TimeSpan.TicksPerDay) == 0 ? GetDefaultFormat(TimeLabelPrecision.Day) : GetTrackingFormat(precision, true);
    }

    public static TimeInterval FromUnits(ChartDateTimeIntervalType unit, double count)
    {
        while ((unit is > ChartDateTimeIntervalType.Seconds and < ChartDateTimeIntervalType.Months) && (Math.Abs(count - Math.Round(count)) > NiceScale.Tolerance))
        {
            (unit, count) = unit switch
            {
                ChartDateTimeIntervalType.Weeks => (ChartDateTimeIntervalType.Days, count * 7),
                ChartDateTimeIntervalType.Days => (ChartDateTimeIntervalType.Hours, count * 24),
                ChartDateTimeIntervalType.Hours => (ChartDateTimeIntervalType.Minutes, count * 60),
                _ => (ChartDateTimeIntervalType.Seconds, count * 60)
            };
        }

        return new TimeInterval(unit, (int)Math.Clamp(Math.Round(count, MidpointRounding.AwayFromZero), 1, Int32.MaxValue));
    }

    public static string GetDefaultFormat(TimeLabelPrecision precision) => precision switch
    {
        TimeLabelPrecision.Second => "HH:mm:ss",
        TimeLabelPrecision.Minute => "HH:mm",
        TimeLabelPrecision.Day => "M/d",
        _ => "yyyy/M"
    };

    public static string GetTrackingFormat(TimeLabelPrecision precision, bool spansDays) => precision switch
    {
        TimeLabelPrecision.Second => spansDays ? "M/d HH:mm:ss" : "HH:mm:ss",
        TimeLabelPrecision.Minute => spansDays ? "M/d HH:mm" : "HH:mm",
        _ => GetDefaultFormat(precision)
    };

    public static TimeLabelPrecision GetValuePrecision(ReadOnlySpan<double> values, TimeLabelPrecision precision)
    {
        foreach (var value in values)
        {
            if (precision == TimeLabelPrecision.Second)
            {
                break;
            }

            if (Double.IsFinite(value) && (value >= 0) && (value <= DateTime.MaxValue.Ticks))
            {
                var current = GetValuePrecision((long)value);
                if (current < precision)
                {
                    precision = current;
                }
            }
        }

        return precision;
    }

    private static TimeLabelPrecision GetFixedPrecision(long ticks)
    {
        if ((ticks % TimeSpan.TicksPerMinute) != 0)
        {
            return TimeLabelPrecision.Second;
        }

        return (ticks % TimeSpan.TicksPerDay) != 0 ? TimeLabelPrecision.Minute : TimeLabelPrecision.Day;
    }

    private static TimeLabelPrecision GetValuePrecision(long ticks)
    {
        if ((ticks % TimeSpan.TicksPerMinute) != 0)
        {
            return TimeLabelPrecision.Second;
        }

        if ((ticks % TimeSpan.TicksPerDay) != 0)
        {
            return TimeLabelPrecision.Minute;
        }

        return new DateTime(ticks).Day != 1 ? TimeLabelPrecision.Day : TimeLabelPrecision.Month;
    }
}
