namespace ClamChart;

public sealed class ChartReferenceLine : ChartAnnotation
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(ChartReferenceLine), Double.NaN, validateValue: static (_, value) => ValidateValue(value, nameof(Value)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty TimeProperty = BindableProperty.Create(nameof(Time), typeof(DateTime?), typeof(ChartReferenceLine), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty DashPatternProperty = BindableProperty.Create(nameof(DashPattern), typeof(DoubleCollection), typeof(ChartReferenceLine), propertyChanged: OnRenderChanged);

#pragma warning disable CA1721
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
#pragma warning restore CA1721

    public DateTime? Time
    {
        get => (DateTime?)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

#pragma warning disable CA2227
    public DoubleCollection? DashPattern
    {
        get => (DoubleCollection?)GetValue(DashPatternProperty);
        set => SetValue(DashPatternProperty, value);
    }
#pragma warning restore CA2227
}
