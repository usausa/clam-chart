namespace ClamChart.Rendering;

internal static class CompactNumberFormat
{
    private const double CompactThreshold = 10000;

    private const double ZeroThreshold = 0.005;

    private const string ZeroFormat = "0";

    private const string PlainFormat = "#,0.##";

    private static readonly string[] Suffixes = ["K", "M", "B", "T"];

    public static string Format(double value, IFormatProvider provider)
    {
        if (!Double.IsFinite(value))
        {
            return value.ToString(provider);
        }

        var (number, format, suffix) = Resolve(value);
        var text = number.ToString(format, provider);
        return suffix is null ? text : String.Concat(text, suffix);
    }

    public static bool TryFormat(double value, Span<char> destination, IFormatProvider provider, out int written)
    {
        if (!Double.IsFinite(value))
        {
            return value.TryFormat(destination, out written, default, provider);
        }

        var (number, format, suffix) = Resolve(value);
        if (!number.TryFormat(destination, out written, format, provider))
        {
            return false;
        }

        if (suffix is null)
        {
            return true;
        }

        if (!suffix.AsSpan().TryCopyTo(destination[written..]))
        {
            written = 0;
            return false;
        }

        written += suffix.Length;
        return true;
    }

    private static (double Number, string Format, string? Suffix) Resolve(double value)
    {
        var magnitude = Math.Abs(value);
        if (magnitude < ZeroThreshold)
        {
            return (0, ZeroFormat, null);
        }

        if (magnitude < CompactThreshold)
        {
            return (Math.Round(value, 2, MidpointRounding.AwayFromZero), PlainFormat, null);
        }

        var unit = 0;
        var scaled = magnitude / 1000;
        while ((scaled >= 1000) && (unit < Suffixes.Length - 1))
        {
            scaled /= 1000;
            unit++;
        }

        var rounded = Round(scaled);
        if ((rounded >= 1000) && (unit < Suffixes.Length - 1))
        {
            unit++;
            rounded = Round(rounded / 1000);
        }

        return (value < 0 ? -rounded : rounded, rounded >= 100 ? "#,0" : "0.##", Suffixes[unit]);
    }

    private static double Round(double value) =>
        Math.Round(value, value >= 100 ? 0 : value >= 10 ? 1 : 2, MidpointRounding.AwayFromZero);
}
