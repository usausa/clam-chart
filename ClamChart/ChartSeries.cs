namespace ClamChart;

using System.Collections;

using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

public abstract class ChartSeries : Element
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(ChartSeries), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ChartSeries), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ChartSeries), propertyChanged: OnDataChanged);
    public static readonly BindableProperty XKeyProperty = BindableProperty.Create(nameof(XKey), typeof(string), typeof(ChartSeries), propertyChanged: OnDataChanged);
    public static readonly BindableProperty YKeyProperty = BindableProperty.Create(nameof(YKey), typeof(string), typeof(ChartSeries), propertyChanged: OnDataChanged);
    public static readonly BindableProperty YAxisProperty = BindableProperty.Create(nameof(YAxis), typeof(ChartAxisTarget), typeof(ChartSeries), ChartAxisTarget.Primary, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(ChartSeries), propertyChanged: OnRenderChanged);
    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(ChartSeries), true, BindingMode.TwoWay, propertyChanged: OnVisibilityChanged);
    public static readonly BindableProperty LegendVisibleProperty = BindableProperty.Create(nameof(LegendVisible), typeof(bool), typeof(ChartSeries), true, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ChartSeries), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ChartSeries), propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty EmptyPointModeProperty = BindableProperty.Create(nameof(EmptyPointMode), typeof(ChartEmptyPointMode), typeof(ChartSeries), ChartEmptyPointMode.Gap, propertyChanged: OnDataChanged);
    public static readonly BindableProperty TrackItemChangesProperty = BindableProperty.Create(nameof(TrackItemChanges), typeof(bool), typeof(ChartSeries), true, propertyChanged: OnTrackItemChangesChanged);

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IChartAccessorProvider? ValueAccessors
    {
        get => (IChartAccessorProvider?)GetValue(ValueAccessorsProperty);
        set => SetValue(ValueAccessorsProperty, value);
    }

    public string? XKey
    {
        get => (string?)GetValue(XKeyProperty);
        set => SetValue(XKeyProperty, value);
    }

    public string? YKey
    {
        get => (string?)GetValue(YKeyProperty);
        set => SetValue(YKeyProperty, value);
    }

    public ChartAxisTarget YAxis
    {
        get => (ChartAxisTarget)GetValue(YAxisProperty);
        set => SetValue(YAxisProperty, value);
    }

    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public bool LegendVisible
    {
        get => (bool)GetValue(LegendVisibleProperty);
        set => SetValue(LegendVisibleProperty, value);
    }

    public string? ValueFormat
    {
        get => (string?)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public IValueConverter? ValueConverter
    {
        get => (IValueConverter?)GetValue(ValueConverterProperty);
        set => SetValue(ValueConverterProperty, value);
    }

    public ChartEmptyPointMode EmptyPointMode
    {
        get => (ChartEmptyPointMode)GetValue(EmptyPointModeProperty);
        set => SetValue(EmptyPointModeProperty, value);
    }

    public bool TrackItemChanges
    {
        get => (bool)GetValue(TrackItemChangesProperty);
        set => SetValue(TrackItemChangesProperty, value);
    }

    public Func<object, double?>? ValueSelector
    {
        get;
        set
        {
            field = value;
            Owner?.InvalidateData();
        }
    }

    public Func<double, string>? ValueFormatter
    {
        get;
        set
        {
            field = value;
            Owner?.InvalidateLayout();
        }
    }

    internal ChartViewBase? Owner { get; set; }

    internal ChartDataSource Source { get; }

    internal DataSnapshot Snapshot { get; private set; } = DataSnapshot.Empty;

    internal bool OtherColorReported { get; set; }

    internal bool SourceReplaced { get; set; }

    internal bool VisibilityChanged { get; set; }

    internal ChartTextFormat Format { get; private set; } = ChartTextFormat.Default;

    internal abstract ChartSeriesKind Kind { get; }

    internal ChartSeries()
    {
        Source = new ChartDataSource(OnSourceChanged);
    }

    internal virtual ChartValueOptions CreateValueOptions(IChartAccessorProvider? accessors, string? xKey) =>
        new()
        {
            Accessors = ValueAccessors ?? accessors,
            XKey = XKey ?? xKey,
            YKey = YKey,
            ValueSelector = ValueSelector
        };

    internal virtual CartesianSeries CreateLayoutSeries() =>
        new()
        {
            Data = Snapshot,
            Kind = Kind,
            Axis = YAxis,
            IsVisible = IsVisible,
            Title = Title,
            LegendVisible = LegendVisible,
            ValueFormat = Format,
            ValueConverter = ValueConverter,
            ValueFormatter = ValueFormatter
        };

    internal void ReplaceSnapshot(DataSnapshot value)
    {
        var previous = Snapshot;
        Snapshot = value;
        if (!ReferenceEquals(previous, value) && !ReferenceEquals(previous, DataSnapshot.Empty))
        {
            previous.Dispose();
        }
    }

    internal static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartSeries)bindable).Owner?.InvalidateLayout();

    internal static void OnRenderChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartSeries)bindable).Owner?.InvalidateRender();

    internal static void OnDataChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartSeries)bindable).Owner?.InvalidateData();

    internal static bool ValidateSize(object? value, string name) =>
        (value is not double size) || (Double.IsFinite(size) && (size >= 0)) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite value of zero or more.");

    internal static bool ValidateRatio(object? value, string name) =>
        (value is double ratio) && (ratio > 0) && (ratio <= 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be greater than 0 and not more than 1.");

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var series = (ChartSeries)bindable;
        series.Format = ChartTextFormat.Parse((string?)newValue);
        series.Owner?.InvalidateLayout();
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var series = (ChartSeries)bindable;
        series.Source.SetSource((IEnumerable?)newValue);
        series.SourceReplaced = true;
        series.Owner?.InvalidateData();
    }

    private static void OnVisibilityChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var series = (ChartSeries)bindable;
        series.VisibilityChanged = true;
        series.Owner?.InvalidateLayout();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChartSeries)bindable).Source.SetTrackItemChanges((bool)newValue);

    private void OnSourceChanged() => Owner?.InvalidateData();
}
