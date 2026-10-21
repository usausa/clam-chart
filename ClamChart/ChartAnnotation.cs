namespace ClamChart;

public abstract class ChartAnnotation : Element
{
    public static readonly BindableProperty AxisProperty = BindableProperty.Create(nameof(Axis), typeof(ChartAnnotationAxis), typeof(ChartAnnotation), ChartAnnotationAxis.Y, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty YAxisProperty = BindableProperty.Create(nameof(YAxis), typeof(ChartAxisTarget), typeof(ChartAnnotation), ChartAxisTarget.Primary, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(ChartAnnotation), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(ChartAnnotation), propertyChanged: OnRenderChanged);

    public ChartAnnotationAxis Axis
    {
        get => (ChartAnnotationAxis)GetValue(AxisProperty);
        set => SetValue(AxisProperty, value);
    }

    public ChartAxisTarget YAxis
    {
        get => (ChartAxisTarget)GetValue(YAxisProperty);
        set => SetValue(YAxisProperty, value);
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    internal ChartViewBase? Owner { get; set; }

    private protected ChartAnnotation()
    {
    }

    internal static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartAnnotation)bindable).Owner?.InvalidateLayout();

    internal static void OnRenderChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartAnnotation)bindable).Owner?.InvalidateRender();

    internal static bool ValidateValue(object? value, string name) =>
        (value is double number) && !Double.IsInfinity(number) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number or NaN.");
}
