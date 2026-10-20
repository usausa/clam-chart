namespace ClamChart.Data;

internal static class ChartDateTime
{
    public static double ToLocalTicks(DateTime value) => (value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value).Ticks;

    public static double ToLocalTicks(DateTimeOffset value) => value.LocalDateTime.Ticks;

    public static long ClampTicks(double ticks) => Double.IsNaN(ticks) ? 0 : Math.Min((long)Math.Clamp(ticks, 0, DateTime.MaxValue.Ticks), DateTime.MaxValue.Ticks);

    public static DateTime FromTicks(double ticks) => new(ClampTicks(ticks));
}
