namespace ClamChart;

using ClamChart.Layout;

public sealed class BarSeries : ChartSeries
{
    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(BarSeries), 0d, validateValue: static (_, value) => ValidateSize(value, nameof(CornerRadius)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty MaxBarWidthProperty = BindableProperty.Create(nameof(MaxBarWidth), typeof(double), typeof(BarSeries), 24d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(MaxBarWidth)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty BarWidthRatioProperty = BindableProperty.Create(nameof(BarWidthRatio), typeof(double), typeof(BarSeries), CartesianSeries.DefaultBarWidthRatio, validateValue: static (_, value) => ValidateRatio(value, nameof(BarWidthRatio)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty StackGroupProperty = BindableProperty.Create(nameof(StackGroup), typeof(string), typeof(BarSeries), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty NegativeColorProperty = BindableProperty.Create(nameof(NegativeColor), typeof(Color), typeof(BarSeries), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty TrackVisibleProperty = BindableProperty.Create(nameof(TrackVisible), typeof(bool), typeof(BarSeries), false, propertyChanged: OnRenderChanged);

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double MaxBarWidth
    {
        get => (double)GetValue(MaxBarWidthProperty);
        set => SetValue(MaxBarWidthProperty, value);
    }

    public double BarWidthRatio
    {
        get => (double)GetValue(BarWidthRatioProperty);
        set => SetValue(BarWidthRatioProperty, value);
    }

    public string? StackGroup
    {
        get => (string?)GetValue(StackGroupProperty);
        set => SetValue(StackGroupProperty, value);
    }

    public Color? NegativeColor
    {
        get => (Color?)GetValue(NegativeColorProperty);
        set => SetValue(NegativeColorProperty, value);
    }

    public bool TrackVisible
    {
        get => (bool)GetValue(TrackVisibleProperty);
        set => SetValue(TrackVisibleProperty, value);
    }

    internal override ChartSeriesKind Kind => ChartSeriesKind.Bar;

    internal override CartesianSeries CreateLayoutSeries() =>
        base.CreateLayoutSeries() with
        {
            StackGroup = StackGroup,
            BarWidthRatio = BarWidthRatio,
            MaxBarWidth = MaxBarWidth
        };
}
