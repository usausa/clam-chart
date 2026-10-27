namespace ClamChart;

using ClamChart.Layout;

public sealed class RadarSeries : ChartSeries
{
    public static readonly BindableProperty ThicknessProperty = BindableProperty.Create(nameof(Thickness), typeof(double), typeof(RadarSeries), 2d, validateValue: static (_, value) => ValidateSize(value, nameof(Thickness)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty FillOpacityProperty = BindableProperty.Create(nameof(FillOpacity), typeof(double), typeof(RadarSeries), 0.15, validateValue: static (_, value) => ValidateOpacity(value, nameof(FillOpacity)), propertyChanged: OnRenderChanged);

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public double FillOpacity
    {
        get => (double)GetValue(FillOpacityProperty);
        set => SetValue(FillOpacityProperty, value);
    }

    internal override ChartSeriesKind Kind => ChartSeriesKind.Radar;

    private static bool ValidateOpacity(object? value, string name) =>
        (value is double opacity) && (opacity >= 0) && (opacity <= 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be between 0 and 1.");
}
