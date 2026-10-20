namespace ClamChart;

using ClamChart.Layout;

public sealed class AreaSeries : LineSeries
{
    public static readonly BindableProperty FillOpacityProperty = BindableProperty.Create(nameof(FillOpacity), typeof(double?), typeof(AreaSeries), validateValue: static (_, value) => ValidateOpacity(value, nameof(FillOpacity)), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty FillModeProperty = BindableProperty.Create(nameof(FillMode), typeof(ChartFillMode), typeof(AreaSeries), ChartFillMode.Solid, propertyChanged: OnRenderChanged);
    public static readonly BindableProperty BaselineValueProperty = BindableProperty.Create(nameof(BaselineValue), typeof(double?), typeof(AreaSeries), validateValue: static (_, value) => ChartAxis.ValidateFinite(value, nameof(BaselineValue)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty StackGroupProperty = BindableProperty.Create(nameof(StackGroup), typeof(string), typeof(AreaSeries), propertyChanged: OnLayoutChanged);

    public double? FillOpacity
    {
        get => (double?)GetValue(FillOpacityProperty);
        set => SetValue(FillOpacityProperty, value);
    }

    public ChartFillMode FillMode
    {
        get => (ChartFillMode)GetValue(FillModeProperty);
        set => SetValue(FillModeProperty, value);
    }

    public double? BaselineValue
    {
        get => (double?)GetValue(BaselineValueProperty);
        set => SetValue(BaselineValueProperty, value);
    }

    public string? StackGroup
    {
        get => (string?)GetValue(StackGroupProperty);
        set => SetValue(StackGroupProperty, value);
    }

    internal override ChartSeriesKind Kind => ChartSeriesKind.Area;

    internal override CartesianSeries CreateLayoutSeries() =>
        base.CreateLayoutSeries() with
        {
            StackGroup = StackGroup,
            BaselineValue = BaselineValue
        };

    private static bool ValidateOpacity(object? value, string name) =>
        (value is not double opacity) || (Double.IsFinite(opacity) && (opacity >= 0) && (opacity <= 1)) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be between 0 and 1.");
}
