namespace ClamChart.Rendering;

internal static class CompactNumberFormat
{
    private const double CompactThreshold = 10000;

    private static readonly string[] Suffixes = ["K", "M", "B", "T"];

    public static string Format(double value, IFormatProvider provider)
    {
        if (!Double.IsFinite(value))
        {
            return value.ToString(provider);
        }

        var magnitude = Math.Abs(value);
        if (magnitude < 0.005)
        {
            return 0d.ToString("0", provider);
        }

        if (magnitude < CompactThreshold)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("#,0.##", provider);
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

        return String.Concat((value < 0 ? -rounded : rounded).ToString(rounded >= 100 ? "#,0" : "0.##", provider), Suffixes[unit]);
    }

    private static double Round(double value) =>
        Math.Round(value, value >= 100 ? 0 : value >= 10 ? 1 : 2, MidpointRounding.AwayFromZero);
}
