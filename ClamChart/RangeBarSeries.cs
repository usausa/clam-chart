namespace ClamChart;

using ClamChart.Data;
using ClamChart.Layout;

public sealed class RangeBarSeries : ChartSeries
{
    public static readonly BindableProperty LowKeyProperty = BindableProperty.Create(nameof(LowKey), typeof(string), typeof(RangeBarSeries), propertyChanged: OnDataChanged);
    public static readonly BindableProperty HighKeyProperty = BindableProperty.Create(nameof(HighKey), typeof(string), typeof(RangeBarSeries), propertyChanged: OnDataChanged);
    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(RangeBarSeries), 0d, validateValue: static (_, value) => ValidateSize(value, nameof(CornerRadius)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty MaxBarWidthProperty = BindableProperty.Create(nameof(MaxBarWidth), typeof(double), typeof(RangeBarSeries), 24d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(MaxBarWidth)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty BarWidthRatioProperty = BindableProperty.Create(nameof(BarWidthRatio), typeof(double), typeof(RangeBarSeries), CartesianSeries.DefaultBarWidthRatio, validateValue: static (_, value) => ValidateRatio(value, nameof(BarWidthRatio)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty TrackVisibleProperty = BindableProperty.Create(nameof(TrackVisible), typeof(bool), typeof(RangeBarSeries), false, propertyChanged: OnRenderChanged);
    public static readonly BindableProperty LowSelectorProperty = BindableProperty.Create(nameof(LowSelector), typeof(Func<object, double?>), typeof(RangeBarSeries), propertyChanged: OnDataChanged);
    public static readonly BindableProperty HighSelectorProperty = BindableProperty.Create(nameof(HighSelector), typeof(Func<object, double?>), typeof(RangeBarSeries), propertyChanged: OnDataChanged);

    public string? LowKey
    {
        get => (string?)GetValue(LowKeyProperty);
        set => SetValue(LowKeyProperty, value);
    }

    public string? HighKey
    {
        get => (string?)GetValue(HighKeyProperty);
        set => SetValue(HighKeyProperty, value);
    }

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

    public bool TrackVisible
    {
        get => (bool)GetValue(TrackVisibleProperty);
        set => SetValue(TrackVisibleProperty, value);
    }

    public Func<object, double?>? LowSelector
    {
        get => (Func<object, double?>?)GetValue(LowSelectorProperty);
        set => SetValue(LowSelectorProperty, value);
    }

    public Func<object, double?>? HighSelector
    {
        get => (Func<object, double?>?)GetValue(HighSelectorProperty);
        set => SetValue(HighSelectorProperty, value);
    }

    internal override ChartSeriesKind Kind => ChartSeriesKind.RangeBar;

    internal override CartesianSeries CreateLayoutSeries() =>
        base.CreateLayoutSeries() with
        {
            BarWidthRatio = BarWidthRatio,
            MaxBarWidth = MaxBarWidth
        };

    internal override ChartValueOptions CreateValueOptions(IChartAccessorProvider? accessors, string? xKey) =>
        base.CreateValueOptions(accessors, xKey) with
        {
            YKey = HighKey,
            ValueSelector = HighSelector,
            LowKey = LowKey,
            LowSelector = LowSelector
        };
}
