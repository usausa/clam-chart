namespace ClamChart;

public partial class ClamHeatmapView
{
    public static readonly BindableProperty LabelFontSizeProperty = BindableProperty.Create(nameof(LabelFontSize), typeof(double), typeof(ClamHeatmapView), 11d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(LabelFontSize)), propertyChanged: OnStyleChanged);

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    private protected override ChartStyle ApplyStyle(ChartStyle style) =>
        style with
        {
            AxisLabelFontSize = IsSet(LabelFontSizeProperty) ? LabelFontSize : style.AxisLabelFontSize,
            HeatmapColorScale = ColorScale
        };
}
