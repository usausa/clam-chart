namespace ClamChart;

using ClamChart.Layout;

public sealed class CategoryAxis : ChartAxis
{
    public static readonly BindableProperty LabelPlacementProperty = BindableProperty.Create(nameof(LabelPlacement), typeof(ChartLabelPlacement), typeof(CategoryAxis), ChartLabelPlacement.BetweenTicks, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty CategoryWidthProperty = BindableProperty.Create(nameof(CategoryWidth), typeof(double?), typeof(CategoryAxis), validateValue: static (_, value) => ValidatePositive(value, nameof(CategoryWidth)), propertyChanged: OnCategoryWidthChanged);
    public static readonly BindableProperty LabelMaxLengthProperty = BindableProperty.Create(nameof(LabelMaxLength), typeof(int?), typeof(CategoryAxis), validateValue: static (_, value) => ValidateAtLeastOne(value, nameof(LabelMaxLength)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty LabelFormatterProperty = BindableProperty.Create(nameof(LabelFormatter), typeof(Func<string, string>), typeof(CategoryAxis), propertyChanged: OnLayoutChanged);

    public ChartLabelPlacement LabelPlacement
    {
        get => (ChartLabelPlacement)GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public double? CategoryWidth
    {
        get => (double?)GetValue(CategoryWidthProperty);
        set => SetValue(CategoryWidthProperty, value);
    }

    public int? LabelMaxLength
    {
        get => (int?)GetValue(LabelMaxLengthProperty);
        set => SetValue(LabelMaxLengthProperty, value);
    }

    public Func<string, string>? LabelFormatter
    {
        get => (Func<string, string>?)GetValue(LabelFormatterProperty);
        set => SetValue(LabelFormatterProperty, value);
    }

    internal override ChartAxisKind Kind => ChartAxisKind.Category;

    internal override AxisLayoutOptions CreateOptions(AxisContext context) =>
        CreateCommonOptions(context) with
        {
            LabelPlacement = LabelPlacement,
            LabelMaxLength = LabelMaxLength,
            CategoryFormatter = LabelFormatter
        };

    private static void OnCategoryWidthChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartAxis)bindable).Owner?.InvalidateSize();
}
