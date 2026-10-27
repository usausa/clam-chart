namespace ClamChart;

public partial class ClamRadialBarView
{
    public static readonly BindableProperty UnlitColorProperty = BindableProperty.Create(nameof(UnlitColor), typeof(Color), typeof(ClamRadialBarView), Color.FromArgb("#4D24292E"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty GradientShiftProperty = BindableProperty.Create(nameof(GradientShift), typeof(double), typeof(ClamRadialBarView), 5d, validateValue: static (_, value) => ChartAxis.ValidateFinite(value, nameof(GradientShift)), propertyChanged: OnStyleRenderChanged);

    public Color UnlitColor
    {
        get => (Color)GetValue(UnlitColorProperty);
        set => SetValue(UnlitColorProperty, value);
    }

    public double GradientShift
    {
        get => (double)GetValue(GradientShiftProperty);
        set => SetValue(GradientShiftProperty, value);
    }
}
