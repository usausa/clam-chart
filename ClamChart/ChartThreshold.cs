namespace ClamChart;

public sealed class ChartThreshold : Element
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(ChartThreshold), Double.NegativeInfinity);
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(ChartThreshold));
    public static readonly BindableProperty LevelProperty = BindableProperty.Create(nameof(Level), typeof(ChartThresholdLevel), typeof(ChartThreshold));
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(ChartThreshold));

#pragma warning disable CA1721
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
#pragma warning restore CA1721

    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ChartThresholdLevel Level
    {
        get => (ChartThresholdLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}
