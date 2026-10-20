namespace ClamChart;

using ClamChart.Layout;

public class LineSeries : ChartSeries
{
    public static readonly BindableProperty ThicknessProperty = BindableProperty.Create(nameof(Thickness), typeof(double?), typeof(LineSeries), validateValue: static (_, value) => ValidateSize(value, nameof(Thickness)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty InterpolationProperty = BindableProperty.Create(nameof(Interpolation), typeof(ChartLineInterpolation), typeof(LineSeries), ChartLineInterpolation.Linear, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty DashPatternProperty = BindableProperty.Create(nameof(DashPattern), typeof(DoubleCollection), typeof(LineSeries), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty MarkerModeProperty = BindableProperty.Create(nameof(MarkerMode), typeof(ChartMarkerMode), typeof(LineSeries), ChartMarkerMode.Auto, propertyChanged: OnRenderChanged);
    public static readonly BindableProperty MarkerSizeProperty = BindableProperty.Create(nameof(MarkerSize), typeof(double?), typeof(LineSeries), validateValue: static (_, value) => ValidateSize(value, nameof(MarkerSize)), propertyChanged: OnRenderChanged);

    public double? Thickness
    {
        get => (double?)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public ChartLineInterpolation Interpolation
    {
        get => (ChartLineInterpolation)GetValue(InterpolationProperty);
        set => SetValue(InterpolationProperty, value);
    }

#pragma warning disable CA2227
    public DoubleCollection? DashPattern
    {
        get => (DoubleCollection?)GetValue(DashPatternProperty);
        set => SetValue(DashPatternProperty, value);
    }
#pragma warning restore CA2227

    public ChartMarkerMode MarkerMode
    {
        get => (ChartMarkerMode)GetValue(MarkerModeProperty);
        set => SetValue(MarkerModeProperty, value);
    }

    public double? MarkerSize
    {
        get => (double?)GetValue(MarkerSizeProperty);
        set => SetValue(MarkerSizeProperty, value);
    }

    internal override ChartSeriesKind Kind => ChartSeriesKind.Line;

    internal override CartesianSeries CreateLayoutSeries() =>
        base.CreateLayoutSeries() with
        {
            Interpolation = Interpolation
        };
}
