namespace ClamChart;

public partial class ClamSparklineView
{
    public static readonly BindableProperty AreaOpacityProperty = BindableProperty.Create(nameof(AreaOpacity), typeof(double), typeof(ClamSparklineView), 0.1d, validateValue: static (_, value) => ValidateOpacity(value, nameof(AreaOpacity)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty EmphasisDimmedOpacityProperty = BindableProperty.Create(nameof(EmphasisDimmedOpacity), typeof(double), typeof(ClamSparklineView), 0.35d, validateValue: static (_, value) => ValidateOpacity(value, nameof(EmphasisDimmedOpacity)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty BaselineColorProperty = BindableProperty.Create(nameof(BaselineColor), typeof(Color), typeof(ClamSparklineView), Color.FromArgb("#C3C2B7"), propertyChanged: OnStyleRenderChanged);

    public double AreaOpacity
    {
        get => (double)GetValue(AreaOpacityProperty);
        set => SetValue(AreaOpacityProperty, value);
    }

    public double EmphasisDimmedOpacity
    {
        get => (double)GetValue(EmphasisDimmedOpacityProperty);
        set => SetValue(EmphasisDimmedOpacityProperty, value);
    }

    public Color BaselineColor
    {
        get => (Color)GetValue(BaselineColorProperty);
        set => SetValue(BaselineColorProperty, value);
    }

    private protected override ChartStyle ApplyStyle(ChartStyle style) =>
        style with
        {
            EmphasisDimmedOpacity = IsSet(EmphasisDimmedOpacityProperty) ? EmphasisDimmedOpacity : style.EmphasisDimmedOpacity,
            AxisLineColor = IsSet(BaselineColorProperty) ? BaselineColor : style.AxisLineColor
        };
}
