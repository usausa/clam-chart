namespace ClamChart;

public sealed class ChartValueMapping : Element
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(object), typeof(ChartValueMapping));
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(ChartValueMapping));
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(ChartValueMapping));
    public static readonly BindableProperty LevelProperty = BindableProperty.Create(nameof(Level), typeof(ChartThresholdLevel), typeof(ChartValueMapping));

#pragma warning disable CA1721
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
#pragma warning restore CA1721

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

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
}
