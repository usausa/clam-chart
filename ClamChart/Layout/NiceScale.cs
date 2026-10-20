namespace ClamChart.Layout;

internal static class NiceScale
{
    public const double Tolerance = 1e-9;

    private const int MaxIterations = 64;

    private const int MantissaDigits = 9;

    private const double MantissaNoise = 1e-14;

    private const int CleanDigits = 9;

    private static readonly double[] Powers =
    [
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
        1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22
    ];

    public static double Step(double rawStep)
    {
        if (!Double.IsFinite(rawStep) || (rawStep <= 0))
        {
            return 1;
        }

        var (mantissa, exponent) = Decompose(rawStep);
        var nice = mantissa switch
        {
            <= 1 + Tolerance => 1,
            <= 2 + Tolerance => 2,
            <= 2.5 + Tolerance => 2.5,
            <= 5 + Tolerance => 5,
            _ => 10
        };
        return Compose(nice, exponent);
    }

    public static double NextStep(double step)
    {
        var (mantissa, exponent) = Decompose(step);
        var next = mantissa switch
        {
            < 1.5 => 2,
            < 2.25 => 2.5,
            < 3.75 => 5,
            _ => 10
        };
        return Compose(next, exponent);
    }

    public static double Multiple(double index, double step)
    {
        if (index == 0)
        {
            return 0;
        }

        return TryDecomposeShort(step, out var mantissa, out var exponent) ? Compose(index * mantissa, exponent) : index * step;
    }

    public static double Floor(double value, double step) => Multiple(Math.Floor((value / step) + Tolerance), step);

    public static double Ceiling(double value, double step) => Multiple(Math.Ceiling((value / step) - Tolerance), step);

    public static double Snap(double value, double step)
    {
        if (!TryDecomposeShort(step, out _, out var exponent))
        {
            return value;
        }

        var index = Math.Round(value / step);
        return Math.Abs((value / step) - index) <= Tolerance ? Multiple(index, step) : Round(value, CleanDigits - exponent);
    }

    public static AxisScale Create(double minimum, double maximum, int maxDivisions, bool niceMinimum, bool niceMaximum)
    {
        var span = maximum - minimum;
        var divisions = Math.Clamp(maxDivisions, 1, TickGenerator.MaxTickCount);
        var step = Step(span / divisions);
        for (var i = 0; i < MaxIterations; i++)
        {
            var low = niceMinimum ? Floor(minimum, step) : minimum;
            var high = niceMaximum ? Ceiling(maximum, step) : maximum;
            if ((((high - low) / step) <= divisions + Tolerance) || (step >= span))
            {
                return new AxisScale(low, high, step);
            }

            step = NextStep(step);
        }

        return new AxisScale(minimum, maximum, span);
    }

    private static (double Mantissa, int Exponent) Decompose(double value)
    {
        var exponent = (int)Math.Floor(Math.Log10(value));
        var mantissa = Compose(value, -exponent);
        if (mantissa >= 10)
        {
            return (mantissa / 10, exponent + 1);
        }

        return mantissa < 1 ? (mantissa * 10, exponent - 1) : (mantissa, exponent);
    }

    private static bool TryDecomposeShort(double value, out double mantissa, out int exponent)
    {
        if (!Double.IsFinite(value) || (value <= 0))
        {
            mantissa = 0;
            exponent = 0;
            return false;
        }

        var (raw, power) = Decompose(value);
        for (var digits = 0; digits <= MantissaDigits; digits++)
        {
            var scaled = raw * Powers[digits];
            var rounded = Math.Round(scaled);
            if (Math.Abs(scaled - rounded) <= scaled * MantissaNoise)
            {
                mantissa = rounded;
                exponent = power - digits;
                return true;
            }
        }

        mantissa = 0;
        exponent = 0;
        return false;
    }

    private static double Round(double value, int digits)
    {
        if (digits < 0)
        {
            var unit = Pow10(-digits);
            return Math.Round(value / unit) * unit;
        }

        if (digits <= 15)
        {
            return Math.Round(value, digits);
        }

        return digits < Powers.Length ? Math.Round(value * Powers[digits]) / Powers[digits] : value;
    }

    private static double Compose(double mantissa, int exponent) => exponent >= 0 ? mantissa * Pow10(exponent) : mantissa / Pow10(-exponent);

    private static double Pow10(int exponent) => exponent < Powers.Length ? Powers[exponent] : Math.Pow(10, exponent);
}
