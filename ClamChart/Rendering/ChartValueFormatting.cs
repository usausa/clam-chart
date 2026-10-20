namespace ClamChart.Rendering;

using System.Globalization;

internal readonly record struct ChartValueFormatting(Func<double, string>? Formatter, IValueConverter? Converter, ChartTextFormat TextFormat, CultureInfo Culture)
{
    public static ChartValueFormatting Default { get; } = new(null, null, ChartTextFormat.Default, CultureInfo.InvariantCulture);

    public bool IsDefault => (Formatter is null) && (Converter is null) && TextFormat.IsDefault;

    public string Format(double value) => ChartTextFormat.FormatValue(value, Formatter, Converter, TextFormat, Culture);

    public bool TryFormat(double value, Span<char> destination, out int written)
    {
        if ((Formatter is not null) || (Converter is not null))
        {
            written = 0;
            return false;
        }

        return TextFormat.TryFormat(value, destination, Culture, out written);
    }
}
