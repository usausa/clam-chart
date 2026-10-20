namespace ClamChart.Layout;

using System.Globalization;

using ClamChart.Rendering;

internal readonly record struct StatDelta(string? Text, int Direction, StatDeltaTone Tone)
{
    private const string PercentFormat = "P1";

    private const double ZeroThreshold = 0.005;

    public bool IsVisible => Text is not null;

    public static StatDelta Create(double value, double? previous, double? delta, ChartDeltaMode mode, ChartTextFormat format, bool increaseIsGood, CultureInfo culture)
    {
        if (!Double.IsFinite(value) || ((delta ?? Compute(value, previous, mode)) is not { } amount) || !Double.IsFinite(amount))
        {
            return default;
        }

        var direction = format.IsDefault ? GetDisplayedSign(amount, mode) : Math.Sign(amount);
        var tone = direction == 0 ? StatDeltaTone.Neutral : (direction > 0) == increaseIsGood ? StatDeltaTone.Good : StatDeltaTone.Bad;
        var text = format.IsDefault ? FormatDefault(amount, direction, mode, culture) : format.Format(amount, culture);
        return new StatDelta(text, direction, tone);
    }

    private static double? Compute(double value, double? previous, ChartDeltaMode mode)
    {
        if ((previous is not { } baseline) || !Double.IsFinite(baseline))
        {
            return null;
        }

        if (mode == ChartDeltaMode.Absolute)
        {
            return value - baseline;
        }

        return baseline != 0 ? (value - baseline) / Math.Abs(baseline) : null;
    }

    private static int GetDisplayedSign(double amount, ChartDeltaMode mode)
    {
        var magnitude = mode == ChartDeltaMode.Percent ? Math.Round(Math.Abs(amount) * 100, 1, MidpointRounding.AwayFromZero) : Math.Abs(amount);
        var zero = mode == ChartDeltaMode.Percent ? magnitude == 0 : magnitude < ZeroThreshold;
        return zero ? 0 : Math.Sign(amount);
    }

    private static string FormatDefault(double amount, int direction, ChartDeltaMode mode, CultureInfo culture)
    {
        var magnitude = Math.Abs(amount);
        var text = mode == ChartDeltaMode.Percent ? magnitude.ToString(PercentFormat, culture) : CompactNumberFormat.Format(magnitude, culture);
        return direction switch
        {
            > 0 => String.Concat(culture.NumberFormat.PositiveSign, text),
            < 0 => String.Concat(culture.NumberFormat.NegativeSign, text),
            _ => text
        };
    }
}
