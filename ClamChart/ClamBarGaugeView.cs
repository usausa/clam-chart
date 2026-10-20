namespace ClamChart;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.InteropServices;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;

public partial class ClamBarGaugeView : ChartViewBase
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ClamBarGaugeView), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamBarGaugeView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty LabelKeyProperty = BindableProperty.Create(nameof(LabelKey), typeof(string), typeof(ClamBarGaugeView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ValueKeyProperty = BindableProperty.Create(nameof(ValueKey), typeof(string), typeof(ClamBarGaugeView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty MaximumKeyProperty = BindableProperty.Create(nameof(MaximumKey), typeof(string), typeof(ClamBarGaugeView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty TargetKeyProperty = BindableProperty.Create(nameof(TargetKey), typeof(string), typeof(ClamBarGaugeView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string), typeof(ClamBarGaugeView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty TrackItemChangesProperty = BindableProperty.Create(nameof(TrackItemChanges), typeof(bool), typeof(ClamBarGaugeView), true, propertyChanged: OnTrackItemChangesChanged);
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(double), typeof(ClamBarGaugeView), 0d, validateValue: static (_, value) => ValidateFinite(value, nameof(Minimum)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(double), typeof(ClamBarGaugeView), 100d, validateValue: static (_, value) => ValidateFinite(value, nameof(Maximum)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(ChartOrientation), typeof(ClamBarGaugeView), ChartOrientation.Horizontal, propertyChanged: OnSizeInputChanged);
    public static readonly BindableProperty DisplayModeProperty = BindableProperty.Create(nameof(DisplayMode), typeof(ChartBarGaugeMode), typeof(ClamBarGaugeView), ChartBarGaugeMode.Basic, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty SegmentWidthProperty = BindableProperty.Create(nameof(SegmentWidth), typeof(double), typeof(ClamBarGaugeView), 12d, validateValue: static (_, value) => ValidatePositive(value, nameof(SegmentWidth)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty BarThicknessProperty = BindableProperty.Create(nameof(BarThickness), typeof(double), typeof(ClamBarGaugeView), 8d, validateValue: static (_, value) => ValidatePositive(value, nameof(BarThickness)), propertyChanged: OnSizeInputChanged);
    public static readonly BindableProperty ItemSpacingProperty = BindableProperty.Create(nameof(ItemSpacing), typeof(double), typeof(ClamBarGaugeView), 12d, validateValue: static (_, value) => ValidateNonNegative(value, nameof(ItemSpacing)), propertyChanged: OnSizeInputChanged);
    public static readonly BindableProperty LabelPositionProperty = BindableProperty.Create(nameof(LabelPosition), typeof(ChartBarGaugeLabelPosition), typeof(ClamBarGaugeView), ChartBarGaugeLabelPosition.Top, propertyChanged: OnSizeInputChanged);
    public static readonly BindableProperty ValueVisibleProperty = BindableProperty.Create(nameof(ValueVisible), typeof(bool), typeof(ClamBarGaugeView), true, propertyChanged: OnSizeInputChanged);
    public static readonly BindableProperty ValueModeProperty = BindableProperty.Create(nameof(ValueMode), typeof(ChartBarGaugeValueMode), typeof(ClamBarGaugeView), ChartBarGaugeValueMode.Percent, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ClamBarGaugeView), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ClamBarGaugeView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty TrackVisibleProperty = BindableProperty.Create(nameof(TrackVisible), typeof(bool), typeof(ClamBarGaugeView), true, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty TargetVisibleProperty = BindableProperty.Create(nameof(TargetVisible), typeof(bool), typeof(ClamBarGaugeView), true, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty ThresholdsProperty = BindableProperty.Create(nameof(Thresholds), typeof(IList<ChartThreshold>), typeof(ClamBarGaugeView), defaultValueCreator: static _ => new ObservableCollection<ChartThreshold>(), validateValue: static (_, value) => ValidateThresholds(value, nameof(Thresholds)), propertyChanged: OnThresholdsChanged);
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(ClamBarGaugeView), propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty SortModeProperty = BindableProperty.Create(nameof(SortMode), typeof(ChartSortMode), typeof(ClamBarGaugeView), ChartSortMode.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty MaxItemsProperty = BindableProperty.Create(nameof(MaxItems), typeof(int?), typeof(ClamBarGaugeView), validateValue: static (_, value) => ValidateMaxItems(value, nameof(MaxItems)), propertyChanged: OnSizeInputChanged);
    public static readonly BindableProperty AutoHeightProperty = BindableProperty.Create(nameof(AutoHeight), typeof(bool), typeof(ClamBarGaugeView), true, propertyChanged: OnAutoHeightChanged);

    private readonly ChartDataSource source;
    private readonly ChartCollectionWatcher thresholdsWatcher;
    private readonly List<BarGaugeEntry> entries = [];
    private BarGaugeRenderer? renderer;
    private DataSnapshot snapshot = DataSnapshot.Empty;
    private ChartTextFormat valueFormat = ChartTextFormat.Default;
    private bool released;

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

    public string? LabelKey
    {
        get => (string?)GetValue(LabelKeyProperty);
        set => SetValue(LabelKeyProperty, value);
    }

    public string? ValueKey
    {
        get => (string?)GetValue(ValueKeyProperty);
        set => SetValue(ValueKeyProperty, value);
    }

    public string? MaximumKey
    {
        get => (string?)GetValue(MaximumKeyProperty);
        set => SetValue(MaximumKeyProperty, value);
    }

    public string? TargetKey
    {
        get => (string?)GetValue(TargetKeyProperty);
        set => SetValue(TargetKeyProperty, value);
    }

    public string? ColorKey
    {
        get => (string?)GetValue(ColorKeyProperty);
        set => SetValue(ColorKeyProperty, value);
    }

    public bool TrackItemChanges
    {
        get => (bool)GetValue(TrackItemChangesProperty);
        set => SetValue(TrackItemChangesProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public ChartOrientation Orientation
    {
        get => (ChartOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public ChartBarGaugeMode DisplayMode
    {
        get => (ChartBarGaugeMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public double SegmentWidth
    {
        get => (double)GetValue(SegmentWidthProperty);
        set => SetValue(SegmentWidthProperty, value);
    }

    public double BarThickness
    {
        get => (double)GetValue(BarThicknessProperty);
        set => SetValue(BarThicknessProperty, value);
    }

    public double ItemSpacing
    {
        get => (double)GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public ChartBarGaugeLabelPosition LabelPosition
    {
        get => (ChartBarGaugeLabelPosition)GetValue(LabelPositionProperty);
        set => SetValue(LabelPositionProperty, value);
    }

    public bool ValueVisible
    {
        get => (bool)GetValue(ValueVisibleProperty);
        set => SetValue(ValueVisibleProperty, value);
    }

    public ChartBarGaugeValueMode ValueMode
    {
        get => (ChartBarGaugeValueMode)GetValue(ValueModeProperty);
        set => SetValue(ValueModeProperty, value);
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

    public bool TrackVisible
    {
        get => (bool)GetValue(TrackVisibleProperty);
        set => SetValue(TrackVisibleProperty, value);
    }

    public bool TargetVisible
    {
        get => (bool)GetValue(TargetVisibleProperty);
        set => SetValue(TargetVisibleProperty, value);
    }

#pragma warning disable CA2227
    public IList<ChartThreshold> Thresholds
    {
        get => (IList<ChartThreshold>)GetValue(ThresholdsProperty);
        set => SetValue(ThresholdsProperty, value);
    }
#pragma warning restore CA2227

    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ChartSortMode SortMode
    {
        get => (ChartSortMode)GetValue(SortModeProperty);
        set => SetValue(SortModeProperty, value);
    }

    public int? MaxItems
    {
        get => (int?)GetValue(MaxItemsProperty);
        set => SetValue(MaxItemsProperty, value);
    }

    public bool AutoHeight
    {
        get => (bool)GetValue(AutoHeightProperty);
        set => SetValue(AutoHeightProperty, value);
    }

    public Func<object, double?>? ValueSelector
    {
        get;
        set
        {
            field = value;
            InvalidateData();
        }
    }

    public Func<object, string?>? LabelSelector
    {
        get;
        set
        {
            field = value;
            InvalidateData();
        }
    }

    public Func<double, string>? ValueFormatter
    {
        get;
        set
        {
            field = value;
            InvalidateLayout();
        }
    }

    internal BarGaugeLayout ChartLayout { get; } = new();

    internal override int RenderedPointCount => ChartLayout.Rows.Count;

    private bool IsAutoHeight => AutoHeight && (Orientation == ChartOrientation.Horizontal) && (HeightRequest < 0);

    public ClamBarGaugeView()
    {
        source = new ChartDataSource(InvalidateData);
        dataAnimator = new ChartAnimator(this, DataAnimationName, OnDataProgress);
        thresholdsWatcher = new ChartCollectionWatcher(OnThresholdsCollectionChanged);
        thresholdsWatcher.Watch(Thresholds);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || released)
        {
            return;
        }

        released = true;
        ReleasePendingMap();
        thresholdsWatcher.Deactivate();
        ReplaceSnapshot(DataSnapshot.Empty);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName is nameof(ChartStyle) or nameof(ContentPadding) or nameof(Density))
        {
            InvalidateHeight();
        }
        else if (propertyName == nameof(HeightRequest))
        {
            InvalidateLayout();
        }
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var size = base.MeasureOverride(widthConstraint, heightConstraint);
        if (!IsAutoHeight)
        {
            return size;
        }

        var width = Double.IsFinite(widthConstraint) ? widthConstraint : Width;
        var padding = ContentPadding ?? GetDensityPadding(Density == ChartDensity.Auto ? ResolveWidthDensity(width) : Density);
        var height = BarGaugeLayout.MeasureHeight(CollectionsMarshal.AsSpan(entries), ChartStyle, LabelPosition, ValueVisible, BarThickness, ItemSpacing, MaxItems, EnsureRenderer().Measurer);
        return new Size(size.Width, padding.VerticalThickness + height);
    }

    internal override ChartDensity ResolveDensity() =>
        (Density == ChartDensity.Auto) && IsAutoHeight ? ResolveWidthDensity(Width) : base.ResolveDensity();

    internal override void OnAttached()
    {
        source.Activate();
        thresholdsWatcher.Activate();
    }

    internal override void OnDetached()
    {
        source.Deactivate();
        thresholdsWatcher.Deactivate();
    }

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData()
    {
        var reader = new ChartValueReader(new ChartValueOptions
        {
            Accessors = ValueAccessors,
            YKey = ValueKey,
            LabelKey = LabelKey,
            ColorKey = ColorKey,
            MaximumKey = MaximumKey,
            TargetKey = TargetKey,
            ValueSelector = ValueSelector,
            LabelSelector = LabelSelector
        });
        var data = DataSnapshotBuilder.Build(source.Source, reader, ChartEmptyPointMode.Gap);
        RecordChange(snapshot.Count, data.Count);
        ReplaceSnapshot(data);
        source.Changes.Clear();
        entries.Clear();
        var values = data.Y;
        for (var i = 0; i < data.Count; i++)
        {
            entries.Add(new BarGaugeEntry(i, data.GetLabel(i), values[i], data.GetMaximum(i), data.GetTarget(i), data.GetColor(i)));
        }

        InvalidateHeight();
    }

    internal override void UpdateLayout(float width, float height)
    {
        var captured = PrepareTransition();
        var culture = Culture ?? CultureInfo.CurrentCulture;
        ChartLayout.Update(width, height, ResolvePadding(), ChartStyle, culture, CreateLayoutOptions(culture), CollectionsMarshal.AsSpan(entries), EnsureRenderer().Measurer);
        CompleteTransition(captured);
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        if ((ChartLayout.Rows.Count == 0) && (Interpolator.Count == 0))
        {
            if (!String.IsNullOrEmpty(EmptyText))
            {
                painter.DrawEmptyText(canvas, EmptyText, ChartLayout.Bounds, ChartStyle);
            }

            return;
        }

        var palette = Palette ?? ChartStyle.Palette;
        var color = Color ?? (palette.Count > 0 ? palette[0] : ChartStyle.OtherColor);
        Interpolator.Easing = AnimationEasing;
        painter.Render(canvas, ChartLayout, Interpolator, new BarGaugeRenderState(color, Thresholds, TrackVisible, TargetVisible), ChartStyle);
    }

    private static bool ValidateFinite(object? value, string name) =>
        (value is double number) && Double.IsFinite(number) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number.");

    private static bool ValidatePositive(object? value, string name) =>
        (value is double number) && Double.IsFinite(number) && (number > 0) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a positive finite value.");

    private static bool ValidateNonNegative(object? value, string name) =>
        (value is double number) && Double.IsFinite(number) && (number >= 0) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite value of zero or more.");

    private static bool ValidateMaxItems(object? value, string name) =>
        (value is not int count) || (count >= 0) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be zero or more.");

    private static bool ValidateThresholds(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamBarGaugeView)bindable).InvalidateData();

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamBarGaugeView)bindable;
        view.source.SetSource((IEnumerable?)newValue);
        view.sourceReplaced = true;
        view.InvalidateData();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamBarGaugeView)bindable).source.SetTrackItemChanges((bool)newValue);

    private static void OnSizeInputChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamBarGaugeView)bindable;
        view.InvalidateLayout();
        view.InvalidateHeight();
    }

    private static void OnAutoHeightChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamBarGaugeView)bindable;
        view.InvalidateLayout();
        view.InvalidateMeasure();
    }

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamBarGaugeView)bindable;
        view.valueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private static void OnThresholdsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamBarGaugeView)bindable;
        view.thresholdsWatcher.Watch(newValue);
        view.InvalidateLayout();
    }

    private void OnThresholdsCollectionChanged(NotifyCollectionChangedEventArgs e) => InvalidateLayout();

    private void InvalidateHeight()
    {
        if (AutoHeight)
        {
            InvalidateMeasure();
        }
    }

    private BarGaugeRenderer EnsureRenderer() => renderer ??= new BarGaugeRenderer(ChartStyle.FontFamily);

    private ChartDensity ResolveWidthDensity(double width) =>
        DensityResolver.Resolve(width, Double.PositiveInfinity, ChartStyle.CompactSize, ChartStyle.MinimalSize);

    private Thickness ResolvePadding() => EffectiveContentPadding;

    private BarGaugeLayoutOptions CreateLayoutOptions(CultureInfo culture) => new()
    {
        Orientation = Orientation,
        DisplayMode = DisplayMode,
        LabelPosition = LabelPosition,
        ValueVisible = ValueVisible,
        ValueMode = ValueMode,
        Formatting = new ChartValueFormatting(ValueFormatter, ValueConverter, valueFormat, culture),
        Minimum = Minimum,
        Maximum = Maximum,
        BarThickness = BarThickness,
        ItemSpacing = ItemSpacing,
        SegmentWidth = SegmentWidth,
        SortMode = SortMode,
        MaxItems = MaxItems
    };

    private void ReplaceSnapshot(DataSnapshot value)
    {
        var previous = snapshot;
        snapshot = value;
        if (!ReferenceEquals(previous, value) && !ReferenceEquals(previous, DataSnapshot.Empty))
        {
            previous.Dispose();
        }
    }
}
