namespace ClamChart;

public partial class ClamChartView
{
    public static readonly BindableProperty AxisTitleFontSizeProperty = BindableProperty.Create(nameof(AxisTitleFontSize), typeof(double), typeof(ClamChartView), 11d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(AxisTitleFontSize)), propertyChanged: OnStyleChanged);

    public double AxisTitleFontSize
    {
        get => (double)GetValue(AxisTitleFontSizeProperty);
        set => SetValue(AxisTitleFontSizeProperty, value);
    }

    private protected override ChartStyle ApplyStyle(ChartStyle style) =>
        style with
        {
            AxisTitleFontSize = IsSet(AxisTitleFontSizeProperty) ? AxisTitleFontSize : style.AxisTitleFontSize
        };
}
