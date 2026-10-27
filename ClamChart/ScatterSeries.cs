namespace ClamChart;

using ClamChart.Layout;

public class ScatterSeries : ChartSeries
{
    public static readonly BindableProperty MarkerSizeProperty = BindableProperty.Create(nameof(MarkerSize), typeof(double), typeof(ScatterSeries), 8d, validateValue: static (_, value) => ValidateSize(value, nameof(MarkerSize)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty MarkerShapeProperty = BindableProperty.Create(nameof(MarkerShape), typeof(ChartMarkerShape), typeof(ScatterSeries), ChartMarkerShape.Circle, propertyChanged: OnRenderChanged);
    public static readonly BindableProperty FillOpacityProperty = BindableProperty.Create(nameof(FillOpacity), typeof(double?), typeof(ScatterSeries), validateValue: static (_, value) => ValidateOpacity(value, nameof(FillOpacity)), propertyChanged: OnRenderChanged);

    public double MarkerSize
    {
        get => (double)GetValue(MarkerSizeProperty);
        set => SetValue(MarkerSizeProperty, value);
    }

    public ChartMarkerShape MarkerShape
    {
        get => (ChartMarkerShape)GetValue(MarkerShapeProperty);
        set => SetValue(MarkerShapeProperty, value);
    }

    public double? FillOpacity
    {
        get => (double?)GetValue(FillOpacityProperty);
        set => SetValue(FillOpacityProperty, value);
    }

    internal override ChartSeriesKind Kind => ChartSeriesKind.Scatter;

    private static bool ValidateOpacity(object? value, string name) =>
        (value is not double opacity) || ((opacity >= 0) && (opacity <= 1)) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be from 0 to 1.");
}
