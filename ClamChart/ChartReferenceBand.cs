namespace ClamChart;

public sealed class ChartReferenceBand : ChartAnnotation
{
    public static readonly BindableProperty StartProperty = BindableProperty.Create(nameof(Start), typeof(double), typeof(ChartReferenceBand), Double.NaN, validateValue: static (_, value) => ValidateValue(value, nameof(Start)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty EndProperty = BindableProperty.Create(nameof(End), typeof(double), typeof(ChartReferenceBand), Double.NaN, validateValue: static (_, value) => ValidateValue(value, nameof(End)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty StartTimeProperty = BindableProperty.Create(nameof(StartTime), typeof(DateTime?), typeof(ChartReferenceBand), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty EndTimeProperty = BindableProperty.Create(nameof(EndTime), typeof(DateTime?), typeof(ChartReferenceBand), propertyChanged: OnLayoutChanged);

    public double Start
    {
        get => (double)GetValue(StartProperty);
        set => SetValue(StartProperty, value);
    }

    public double End
    {
        get => (double)GetValue(EndProperty);
        set => SetValue(EndProperty, value);
    }

    public DateTime? StartTime
    {
        get => (DateTime?)GetValue(StartTimeProperty);
        set => SetValue(StartTimeProperty, value);
    }

    public DateTime? EndTime
    {
        get => (DateTime?)GetValue(EndTimeProperty);
        set => SetValue(EndTimeProperty, value);
    }
}
