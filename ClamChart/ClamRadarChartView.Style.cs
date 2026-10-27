namespace ClamChart;

public partial class ClamRadarChartView
{
    public static readonly BindableProperty WebColorProperty = BindableProperty.Create(nameof(WebColor), typeof(Color), typeof(ClamRadarChartView), Color.FromArgb("#E1E0D9"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty WebThicknessProperty = BindableProperty.Create(nameof(WebThickness), typeof(double), typeof(ClamRadarChartView), 0d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(WebThickness)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty LabelFontSizeProperty = BindableProperty.Create(nameof(LabelFontSize), typeof(double), typeof(ClamRadarChartView), 11d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(LabelFontSize)), propertyChanged: OnStyleChanged);

    public Color WebColor
    {
        get => (Color)GetValue(WebColorProperty);
        set => SetValue(WebColorProperty, value);
    }

    public double WebThickness
    {
        get => (double)GetValue(WebThicknessProperty);
        set => SetValue(WebThicknessProperty, value);
    }

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    private protected override ChartStyle ApplyStyle(ChartStyle style) =>
        style with
        {
            GridLineColor = IsSet(WebColorProperty) ? WebColor : style.GridLineColor,
            GridLineThickness = IsSet(WebThicknessProperty) ? WebThickness : style.GridLineThickness,
            AxisLabelFontSize = IsSet(LabelFontSizeProperty) ? LabelFontSize : style.AxisLabelFontSize
        };
}
