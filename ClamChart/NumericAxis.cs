namespace ClamChart;

using ClamChart.Layout;

public sealed class NumericAxis : ChartAxis
{
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(double?), typeof(NumericAxis), validateValue: static (_, value) => ValidateFinite(value, nameof(Minimum)), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(double?), typeof(NumericAxis), validateValue: static (_, value) => ValidateFinite(value, nameof(Maximum)), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty SoftMinimumProperty = BindableProperty.Create(nameof(SoftMinimum), typeof(double?), typeof(NumericAxis), validateValue: static (_, value) => ValidateFinite(value, nameof(SoftMinimum)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty SoftMaximumProperty = BindableProperty.Create(nameof(SoftMaximum), typeof(double?), typeof(NumericAxis), validateValue: static (_, value) => ValidateFinite(value, nameof(SoftMaximum)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty IntervalProperty = BindableProperty.Create(nameof(Interval), typeof(double?), typeof(NumericAxis), validateValue: static (_, value) => ValidatePositive(value, nameof(Interval)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty IncludeZeroProperty = BindableProperty.Create(nameof(IncludeZero), typeof(bool?), typeof(NumericAxis), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty NiceRangeProperty = BindableProperty.Create(nameof(NiceRange), typeof(bool), typeof(NumericAxis), true, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty TickAlignmentProperty = BindableProperty.Create(nameof(TickAlignment), typeof(ChartTickAlignment), typeof(NumericAxis), ChartTickAlignment.AlignToPrimary, propertyChanged: OnLayoutChanged);

    public double? Minimum
    {
        get => (double?)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double? Maximum
    {
        get => (double?)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double? SoftMinimum
    {
        get => (double?)GetValue(SoftMinimumProperty);
        set => SetValue(SoftMinimumProperty, value);
    }

    public double? SoftMaximum
    {
        get => (double?)GetValue(SoftMaximumProperty);
        set => SetValue(SoftMaximumProperty, value);
    }

    public double? Interval
    {
        get => (double?)GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    public bool? IncludeZero
    {
        get => (bool?)GetValue(IncludeZeroProperty);
        set => SetValue(IncludeZeroProperty, value);
    }

    public bool NiceRange
    {
        get => (bool)GetValue(NiceRangeProperty);
        set => SetValue(NiceRangeProperty, value);
    }

    public ChartTickAlignment TickAlignment
    {
        get => (ChartTickAlignment)GetValue(TickAlignmentProperty);
        set => SetValue(TickAlignmentProperty, value);
    }

    public Func<double, string>? LabelFormatter
    {
        get;
        set
        {
            field = value;
            Owner?.InvalidateLayout();
        }
    }

    internal override ChartAxisKind Kind => ChartAxisKind.Numeric;

    internal override AxisLayoutOptions CreateOptions(AxisContext context) =>
        CreateCommonOptions(context) with
        {
            Range = new AxisRangeOptions
            {
                Minimum = Minimum,
                Maximum = Maximum,
                SoftMinimum = SoftMinimum,
                SoftMaximum = SoftMaximum,
                Interval = Interval,
                IncludeZero = IncludeZero ?? context.IncludeZero,
                NiceRange = NiceRange
            },
            TickAlignment = TickAlignment,
            LabelFormatter = LabelFormatter
        };

    private static void OnBoundsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartAxis)bindable).ResetBounds();
}
