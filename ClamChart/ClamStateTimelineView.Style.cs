namespace ClamChart;

public partial class ClamStateTimelineView
{
    public static readonly BindableProperty SeparatorColorProperty = BindableProperty.Create(nameof(SeparatorColor), typeof(Color), typeof(ClamStateTimelineView), Color.FromArgb("#E1E0D9"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SeparatorThicknessProperty = BindableProperty.Create(nameof(SeparatorThickness), typeof(double), typeof(ClamStateTimelineView), 0d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(SeparatorThickness)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty LabelFontSizeProperty = BindableProperty.Create(nameof(LabelFontSize), typeof(double), typeof(ClamStateTimelineView), 11d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(LabelFontSize)), propertyChanged: OnStyleChanged);

    public Color SeparatorColor
    {
        get => (Color)GetValue(SeparatorColorProperty);
        set => SetValue(SeparatorColorProperty, value);
    }

    public double SeparatorThickness
    {
        get => (double)GetValue(SeparatorThicknessProperty);
        set => SetValue(SeparatorThicknessProperty, value);
    }

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    private protected override ChartStyle ApplyStyle(ChartStyle style) =>
        style with
        {
            GridLineColor = IsSet(SeparatorColorProperty) ? SeparatorColor : style.GridLineColor,
            GridLineThickness = IsSet(SeparatorThicknessProperty) ? SeparatorThickness : style.GridLineThickness,
            AxisLabelFontSize = IsSet(LabelFontSizeProperty) ? LabelFontSize : style.AxisLabelFontSize
        };
}
