namespace ClamChart;

public partial class ClamStatView
{
    public static readonly BindableProperty AccentTextColorProperty = BindableProperty.Create(nameof(AccentTextColor), typeof(Color), typeof(ClamStatView), Color.FromArgb("#202226"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty GradientStrengthProperty = BindableProperty.Create(nameof(GradientStrength), typeof(double), typeof(ClamStatView), -0.7d, validateValue: static (_, value) => ChartAxis.ValidateFinite(value, nameof(GradientStrength)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty PositiveTextColorProperty = BindableProperty.Create(nameof(PositiveTextColor), typeof(Color), typeof(ClamStatView), Color.FromArgb("#56A64B"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty NegativeTextColorProperty = BindableProperty.Create(nameof(NegativeTextColor), typeof(Color), typeof(ClamStatView), Color.FromArgb("#E02F44"), propertyChanged: OnStyleRenderChanged);

    public Color AccentTextColor
    {
        get => (Color)GetValue(AccentTextColorProperty);
        set => SetValue(AccentTextColorProperty, value);
    }

    public double GradientStrength
    {
        get => (double)GetValue(GradientStrengthProperty);
        set => SetValue(GradientStrengthProperty, value);
    }

    public Color PositiveTextColor
    {
        get => (Color)GetValue(PositiveTextColorProperty);
        set => SetValue(PositiveTextColorProperty, value);
    }

    public Color NegativeTextColor
    {
        get => (Color)GetValue(NegativeTextColorProperty);
        set => SetValue(NegativeTextColorProperty, value);
    }

    private protected override ChartStyle ApplyStyle(ChartStyle style) =>
        style with
        {
            PositiveTextColor = IsSet(PositiveTextColorProperty) ? PositiveTextColor : style.PositiveTextColor,
            NegativeTextColor = IsSet(NegativeTextColorProperty) ? NegativeTextColor : style.NegativeTextColor
        };
}
