namespace ClamChart;

using ClamChart.Layout;

public class LineSeries : ChartSeries
{
    public static readonly BindableProperty ThicknessProperty = BindableProperty.Create(nameof(Thickness), typeof(double), typeof(LineSeries), 2d, validateValue: static (_, value) => ValidateSize(value, nameof(Thickness)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty InterpolationProperty = BindableProperty.Create(nameof(Interpolation), typeof(ChartLineInterpolation), typeof(LineSeries), ChartLineInterpolation.Linear, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty DashPatternProperty = BindableProperty.Create(nameof(DashPattern), typeof(DoubleCollection), typeof(LineSeries), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty MarkerModeProperty = BindableProperty.Create(nameof(MarkerMode), typeof(ChartMarkerMode), typeof(LineSeries), ChartMarkerMode.Auto, propertyChanged: OnRenderChanged);
    public static readonly BindableProperty MarkerSizeProperty = BindableProperty.Create(nameof(MarkerSize), typeof(double), typeof(LineSeries), 8d, validateValue: static (_, value) => ValidateSize(value, nameof(MarkerSize)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty MarkerShapeProperty = BindableProperty.Create(nameof(MarkerShape), typeof(ChartMarkerShape), typeof(LineSeries), ChartMarkerShape.Circle, propertyChanged: OnRenderChanged);
    public static readonly BindableProperty GlowRadiusProperty = BindableProperty.Create(nameof(GlowRadius), typeof(double), typeof(LineSeries), 0d, validateValue: static (_, value) => ValidateSize(value, nameof(GlowRadius)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty LiveIndicatorVisibleProperty = BindableProperty.Create(nameof(LiveIndicatorVisible), typeof(bool), typeof(LineSeries), false, propertyChanged: OnLayoutChanged);

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
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

    public double GlowRadius
    {
        get => (double)GetValue(GlowRadiusProperty);
        set => SetValue(GlowRadiusProperty, value);
    }

    public bool LiveIndicatorVisible
    {
        get => (bool)GetValue(LiveIndicatorVisibleProperty);
        set => SetValue(LiveIndicatorVisibleProperty, value);
    }

    internal override ChartSeriesKind Kind => ChartSeriesKind.Line;

    internal override CartesianSeries CreateLayoutSeries() =>
        base.CreateLayoutSeries() with
        {
            Interpolation = Interpolation
        };
}
