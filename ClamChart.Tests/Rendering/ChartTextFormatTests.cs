namespace ClamChart.Tests.Rendering;

public sealed class ChartTextFormatTests
{
    [Fact]
    public void EmptyFormatsAreTheCompactDefault()
    {
        // Act & Assert
        Assert.True(ChartTextFormat.Parse(null).IsDefault);
        Assert.True(ChartTextFormat.Parse(String.Empty).IsDefault);
        Assert.Equal("12.9K", ChartTextFormat.Default.Format(12900, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormatsWithABraceAreCompositeAndOthersAreFormatStrings()
    {
        // Act & Assert
        Assert.Equal("1,234.5", ChartTextFormat.Parse("N1").Format(1234.5, CultureInfo.InvariantCulture));
        Assert.Equal("1,235 yen", ChartTextFormat.Parse("{0:N0} yen").Format(1234.6, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ValuesPreferTheFormatterThenTheConverterThenTheFormat()
    {
        // Arrange
        var culture = CultureInfo.InvariantCulture;
        var format = ChartTextFormat.Parse("{0:N0}%");
        var converter = new CultureConverter();

        // Act & Assert
        Assert.Equal("40%", ChartTextFormat.FormatValue(40, null, null, format, culture));
        Assert.Equal("40@ja-JP", ChartTextFormat.FormatValue(40, null, converter, format, CultureInfo.GetCultureInfo("ja-JP")));
        Assert.Equal("<40>", ChartTextFormat.FormatValue(40, static value => $"<{value.ToString(CultureInfo.InvariantCulture)}>", converter, format, culture));
    }

    [Fact]
    public void TimesUseTheDefaultFormatUnlessAnotherIsGiven()
    {
        // Arrange
        var culture = CultureInfo.InvariantCulture;
        double ticks = new DateTime(2026, 10, 19, 9, 5, 0).Ticks;

        // Act & Assert
        Assert.Equal("09:05", ChartTextFormat.FormatTime(ticks, null, null, ChartTextFormat.Default, "HH:mm", culture));
        Assert.Equal("10-19", ChartTextFormat.FormatTime(ticks, null, null, ChartTextFormat.Parse("MM-dd"), "HH:mm", culture));
        Assert.Equal("at 9:05", ChartTextFormat.FormatTime(ticks, null, null, ChartTextFormat.Parse("at {0:H:mm}"), "HH:mm", culture));
        Assert.Equal("10/19/2026 09:05:00@", ChartTextFormat.FormatTime(ticks, null, new CultureConverter(), ChartTextFormat.Default, "HH:mm", culture));
        Assert.Equal("ticks", ChartTextFormat.FormatTime(ticks, static _ => "ticks", new CultureConverter(), ChartTextFormat.Default, "HH:mm", culture));
        Assert.Equal("0001", ChartTextFormat.FormatTime(-1, null, null, ChartTextFormat.Default, "yyyy", culture));
    }

    [Fact]
    public void CategoriesUseACompositeFormatButNotANumberFormat()
    {
        // Arrange
        var culture = CultureInfo.InvariantCulture;

        // Act & Assert
        Assert.Equal("Jan", ChartTextFormat.FormatCategory("Jan", null, null, ChartTextFormat.Default, culture));
        Assert.Equal("[Jan]", ChartTextFormat.FormatCategory("Jan", null, null, ChartTextFormat.Parse("[{0}]"), culture));
        Assert.Equal("Jan", ChartTextFormat.FormatCategory("Jan", null, null, ChartTextFormat.Parse("N0"), culture));
        Assert.Equal("Jan@", ChartTextFormat.FormatCategory("Jan", null, new CultureConverter(), ChartTextFormat.Default, culture));
        Assert.Equal("JAN", ChartTextFormat.FormatCategory("Jan", static x => x.ToUpperInvariant(), new CultureConverter(), ChartTextFormat.Default, culture));
    }

    [Fact]
    public void ConverterResultsThatAreNotTextBecomeEmpty()
    {
        // Act & Assert
        Assert.Equal(String.Empty, ChartTextFormat.FormatValue(40, null, new NumberConverter(), ChartTextFormat.Default, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormatsWithTheSameTextAreEqual()
    {
        // Arrange
        var first = ChartTextFormat.Parse("{0:N0}%");
        var second = ChartTextFormat.Parse("{0:N0}%");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, ChartTextFormat.Parse("N0"));
        Assert.NotEqual(first, ChartTextFormat.Default);
    }

    [Theory]
    [InlineData(null, 12900d, "12.9K")]
    [InlineData("N1", 1234.56, "1,234.6")]
    [InlineData("{0:N0} pts", 1234.56, "1,235 pts")]
    public void FormattingIntoABufferMatchesTheText(string? format, double value, string expected)
    {
        // Arrange
        var parsed = ChartTextFormat.Parse(format);
        Span<char> buffer = stackalloc char[32];

        // Act
        var formatted = parsed.TryFormat(value, buffer, CultureInfo.InvariantCulture, out var written);

        // Assert
        Assert.True(formatted);
        Assert.Equal(expected, buffer[..written].ToString());
        Assert.Equal(expected, parsed.Format(value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ValueFormattingUsesTheBufferOnlyWithoutDelegates()
    {
        // Arrange
        var plain = new ChartValueFormatting(null, null, ChartTextFormat.Parse("N0"), CultureInfo.InvariantCulture);
        var formatted = plain with { Formatter = static value => $"#{value}" };
        var converted = plain with { Converter = new CultureConverter() };
        Span<char> buffer = stackalloc char[32];

        // Act & Assert
        Assert.True(plain.TryFormat(1234, buffer, out var written));
        Assert.Equal("1,234", buffer[..written].ToString());
        Assert.False(plain.IsDefault);
        Assert.True(ChartValueFormatting.Default.IsDefault);
        Assert.False(formatted.TryFormat(1234, buffer, out _));
        Assert.Equal("#1234", formatted.Format(1234));
        Assert.False(converted.TryFormat(1234, buffer, out _));
        Assert.Equal("1234@", converted.Format(1234));
    }

    private sealed class CultureConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            $"{System.Convert.ToString(value, CultureInfo.InvariantCulture)}@{culture.Name}";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class NumberConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
