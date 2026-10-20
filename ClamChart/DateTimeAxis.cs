namespace ClamChart;

using ClamChart.Data;
using ClamChart.Layout;

public sealed class DateTimeAxis : ChartAxis
{
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(DateTime?), typeof(DateTimeAxis), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(DateTime?), typeof(DateTimeAxis), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty IntervalTypeProperty = BindableProperty.Create(nameof(IntervalType), typeof(ChartDateTimeIntervalType), typeof(DateTimeAxis), ChartDateTimeIntervalType.Auto, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty IntervalProperty = BindableProperty.Create(nameof(Interval), typeof(double?), typeof(DateTimeAxis), validateValue: static (_, value) => ValidatePositive(value, nameof(Interval)), propertyChanged: OnLayoutChanged);

    private Func<double, string>? tickFormatter;

    public DateTime? Minimum
    {
        get => (DateTime?)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public DateTime? Maximum
    {
        get => (DateTime?)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public ChartDateTimeIntervalType IntervalType
    {
        get => (ChartDateTimeIntervalType)GetValue(IntervalTypeProperty);
        set => SetValue(IntervalTypeProperty, value);
    }

    public double? Interval
    {
        get => (double?)GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    public Func<DateTime, string>? LabelFormatter
    {
        get;
        set
        {
            field = value;
            tickFormatter = value is null ? null : ticks => value(ChartDateTime.FromTicks(ticks));
            Owner?.InvalidateLayout();
        }
    }

    internal override ChartAxisKind Kind => ChartAxisKind.DateTime;

    internal override AxisLayoutOptions CreateOptions(AxisContext context) =>
        CreateCommonOptions(context) with
        {
            Range = new AxisRangeOptions { Minimum = ToTicks(Minimum), Maximum = ToTicks(Maximum) },
            TimeInterval = (IntervalType != ChartDateTimeIntervalType.Auto) && (Interval is { } interval) ? TimeInterval.FromUnits(IntervalType, interval) : null,
            MinimumTimeUnit = IntervalType,
            FirstDayOfWeek = context.Culture.DateTimeFormat.FirstDayOfWeek,
            LabelFormatter = tickFormatter
        };

    private static double? ToTicks(DateTime? value) => value is { } time ? ChartDateTime.ToLocalTicks(time) : null;

    private static void OnBoundsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartAxis)bindable).ResetBounds();
}
