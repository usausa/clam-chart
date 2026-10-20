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
