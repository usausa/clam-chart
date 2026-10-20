namespace ClamChart.Rendering;

using System.Globalization;
using System.Text;

using ClamChart.Data;

internal sealed class ChartTextFormat : IEquatable<ChartTextFormat>
{
    private readonly string? format;
    private readonly CompositeFormat? composite;

    public static ChartTextFormat Default { get; } = new(null, null);

    public bool IsDefault => ReferenceEquals(this, Default);

    private ChartTextFormat(string? format, CompositeFormat? composite)
    {
        this.format = format;
        this.composite = composite;
    }

    public static ChartTextFormat Parse(string? format)
    {
        if (String.IsNullOrEmpty(format))
        {
            return Default;
        }

        return format.Contains('{', StringComparison.Ordinal) ? new ChartTextFormat(null, CompositeFormat.Parse(format)) : new ChartTextFormat(format, null);
    }

    public static string FormatValue(double value, Func<double, string>? formatter, IValueConverter? converter, ChartTextFormat format, CultureInfo culture)
    {
        if (formatter is not null)
        {
            return formatter(value);
        }

        if (converter is not null)
        {
            return converter.Convert(value, typeof(string), null, culture) as string ?? String.Empty;
        }

        return format.Format(value, culture);
    }

    public static string FormatTime(double ticks, Func<double, string>? formatter, IValueConverter? converter, ChartTextFormat format, string defaultFormat, CultureInfo culture)
    {
        if (formatter is not null)
        {
            return formatter(ticks);
        }

        var time = ChartDateTime.FromTicks(ticks);
        if (converter is not null)
        {
            return converter.Convert(time, typeof(string), null, culture) as string ?? String.Empty;
        }

        if (format.IsDefault)
        {
            return time.ToString(defaultFormat, culture);
        }

        return format.composite is null ? time.ToString(format.format, culture) : String.Format(culture, format.composite, time);
    }

    public static string FormatCategory(string text, Func<string, string>? formatter, IValueConverter? converter, ChartTextFormat format, CultureInfo culture)
    {
        if (formatter is not null)
        {
            return formatter(text);
        }

        if (converter is not null)
        {
            return converter.Convert(text, typeof(string), null, culture) as string ?? String.Empty;
        }

        return format.composite is null ? text : String.Format(culture, format.composite, text);
    }

    public string Format(double value, IFormatProvider provider)
    {
        if (IsDefault)
        {
            return CompactNumberFormat.Format(value, provider);
        }

        return composite is null ? value.ToString(format, provider) : String.Format(provider, composite, value);
    }

    public bool TryFormat(double value, Span<char> destination, IFormatProvider provider, out int written)
    {
        if (IsDefault)
        {
            return CompactNumberFormat.TryFormat(value, destination, provider, out written);
        }

        return composite is null ? value.TryFormat(destination, out written, format, provider) : destination.TryWrite(provider, composite, out written, value);
    }

    public bool Equals(ChartTextFormat? other) => (other is not null) && (format == other.format) && (composite?.Format == other.composite?.Format);

    public override bool Equals(object? obj) => (obj is ChartTextFormat other) && Equals(other);

    public override int GetHashCode() => HashCode.Combine(format, composite?.Format);
}
