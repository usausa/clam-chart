namespace ClamChart;

using ClamChart.Layout;
using ClamChart.Rendering;

public abstract class ChartAxis : Element
{
    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(nameof(IsVisible), typeof(bool?), typeof(ChartAxis), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty LabelFontSizeProperty = BindableProperty.Create(nameof(LabelFontSize), typeof(double), typeof(ChartAxis), 11d, validateValue: static (_, value) => ValidatePositive(value, nameof(LabelFontSize)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty LineColorProperty = BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(ChartAxis), Color.FromArgb("#C3C2B7"), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty GridLineColorProperty = BindableProperty.Create(nameof(GridLineColor), typeof(Color), typeof(ChartAxis), Color.FromArgb("#E1E0D9"), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty GridLineThicknessProperty = BindableProperty.Create(nameof(GridLineThickness), typeof(double), typeof(ChartAxis), 0d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(GridLineThickness)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(ChartAxis), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty TitleOrientationProperty = BindableProperty.Create(nameof(TitleOrientation), typeof(ChartOrientation), typeof(ChartAxis), ChartOrientation.Horizontal, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty LabelsVisibleProperty = BindableProperty.Create(nameof(LabelsVisible), typeof(bool), typeof(ChartAxis), true, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty LabelFormatProperty = BindableProperty.Create(nameof(LabelFormat), typeof(string), typeof(ChartAxis), propertyChanged: OnLabelFormatChanged);
    public static readonly BindableProperty LabelConverterProperty = BindableProperty.Create(nameof(LabelConverter), typeof(IValueConverter), typeof(ChartAxis), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty MaxLabelCountProperty = BindableProperty.Create(nameof(MaxLabelCount), typeof(int?), typeof(ChartAxis), validateValue: static (_, value) => ValidateAtLeastOne(value, nameof(MaxLabelCount)), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty GridLinesVisibleProperty = BindableProperty.Create(nameof(GridLinesVisible), typeof(bool?), typeof(ChartAxis), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(ChartAxisPosition), typeof(ChartAxis), ChartAxisPosition.Auto, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty IsInversedProperty = BindableProperty.Create(nameof(IsInversed), typeof(bool), typeof(ChartAxis), false, propertyChanged: OnLayoutChanged);

    public bool? IsVisible
    {
        get => (bool?)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ChartOrientation TitleOrientation
    {
        get => (ChartOrientation)GetValue(TitleOrientationProperty);
        set => SetValue(TitleOrientationProperty, value);
    }

    public bool LabelsVisible
    {
        get => (bool)GetValue(LabelsVisibleProperty);
        set => SetValue(LabelsVisibleProperty, value);
    }

    public string? LabelFormat
    {
        get => (string?)GetValue(LabelFormatProperty);
        set => SetValue(LabelFormatProperty, value);
    }

    public IValueConverter? LabelConverter
    {
        get => (IValueConverter?)GetValue(LabelConverterProperty);
        set => SetValue(LabelConverterProperty, value);
    }

    public int? MaxLabelCount
    {
        get => (int?)GetValue(MaxLabelCountProperty);
        set => SetValue(MaxLabelCountProperty, value);
    }

    public bool? GridLinesVisible
    {
        get => (bool?)GetValue(GridLinesVisibleProperty);
        set => SetValue(GridLinesVisibleProperty, value);
    }

    public ChartAxisPosition Position
    {
        get => (ChartAxisPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public bool IsInversed
    {
        get => (bool)GetValue(IsInversedProperty);
        set => SetValue(IsInversedProperty, value);
    }

    internal ChartViewBase? Owner { get; set; }

    internal bool InvertedBoundsReported { get; set; }

    internal ChartTextFormat Format { get; private set; } = ChartTextFormat.Default;

    internal abstract ChartAxisKind Kind { get; }

    internal ChartAxis()
    {
    }

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public Color GridLineColor
    {
        get => (Color)GetValue(GridLineColorProperty);
        set => SetValue(GridLineColorProperty, value);
    }

    public double GridLineThickness
    {
        get => (double)GetValue(GridLineThicknessProperty);
        set => SetValue(GridLineThicknessProperty, value);
    }

    internal abstract AxisLayoutOptions CreateOptions(AxisContext context);

    internal AxisLayoutOptions CreateCommonOptions(AxisContext context)
    {
        var visible = IsVisible ?? context.Visible;
        return new AxisLayoutOptions
        {
            Kind = Kind,
            IsVisible = visible,
            LabelsVisible = LabelsVisible,
            Title = context.TitleVisible ? Title : null,
            TitleOrientation = TitleOrientation,
            MaxLabelCount = MaxLabelCount ?? context.MaxLabelCount,
            IsInversed = IsInversed,
            Position = Position,
            LabelFormat = Format,
            LabelConverter = LabelConverter,
            GridLinesVisible = GridLinesVisible ?? (context.ValueAxis && visible),
            LabelFontSize = LabelFontSize,
            LineColor = LineColor,
            GridLineColor = GridLineColor,
            GridLineThickness = GridLineThickness
        };
    }

    internal static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartAxis)bindable).Owner?.InvalidateLayout();

    internal void ResetBounds()
    {
        InvertedBoundsReported = false;
        Owner?.InvalidateLayout();
    }

    internal static bool ValidateFinite(object? value, string name) =>
        (value is not double number) || Double.IsFinite(number) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be finite.");

    internal static bool ValidatePositive(object? value, string name) =>
        (value is not double number) || (Double.IsFinite(number) && (number > 0)) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite value greater than zero.");

    internal static bool ValidateAtLeastOne(object? value, string name) =>
        (value is not int count) || (count >= 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be 1 or more.");

    private static void OnLabelFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var axis = (ChartAxis)bindable;
        axis.Format = ChartTextFormat.Parse((string?)newValue);
        axis.Owner?.InvalidateLayout();
    }
}
