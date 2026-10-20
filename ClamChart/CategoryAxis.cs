namespace ClamChart;

using ClamChart.Layout;

public sealed class CategoryAxis : ChartAxis
{
    public static readonly BindableProperty LabelPlacementProperty = BindableProperty.Create(nameof(LabelPlacement), typeof(ChartLabelPlacement), typeof(CategoryAxis), ChartLabelPlacement.BetweenTicks, propertyChanged: OnLayoutChanged);

    public ChartLabelPlacement LabelPlacement
    {
        get => (ChartLabelPlacement)GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public Func<string, string>? LabelFormatter
    {
        get;
        set
        {
            field = value;
            Owner?.InvalidateLayout();
        }
    }

    internal override ChartAxisKind Kind => ChartAxisKind.Category;

    internal override AxisLayoutOptions CreateOptions(AxisContext context) =>
        CreateCommonOptions(context) with
        {
            LabelPlacement = LabelPlacement,
            CategoryFormatter = LabelFormatter
        };
}
