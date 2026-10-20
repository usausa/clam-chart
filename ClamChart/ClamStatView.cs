namespace ClamChart;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;

public partial class ClamStatView : ChartViewBase
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(ClamStatView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(ClamStatView), 0d, propertyChanged: OnValueChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ClamStatView), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ClamStatView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty UnitProperty = BindableProperty.Create(nameof(Unit), typeof(string), typeof(ClamStatView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueFontSizeProperty = BindableProperty.Create(nameof(ValueFontSize), typeof(double), typeof(ClamStatView), 32d, validateValue: static (_, value) => ValidateFontSize(value, nameof(ValueFontSize)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty MinValueFontSizeProperty = BindableProperty.Create(nameof(MinValueFontSize), typeof(double), typeof(ClamStatView), 16d, validateValue: static (_, value) => ValidateFontSize(value, nameof(MinValueFontSize)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty PreviousValueProperty = BindableProperty.Create(nameof(PreviousValue), typeof(double?), typeof(ClamStatView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty DeltaProperty = BindableProperty.Create(nameof(Delta), typeof(double?), typeof(ClamStatView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty DeltaModeProperty = BindableProperty.Create(nameof(DeltaMode), typeof(ChartDeltaMode), typeof(ClamStatView), ChartDeltaMode.Percent, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty DeltaFormatProperty = BindableProperty.Create(nameof(DeltaFormat), typeof(string), typeof(ClamStatView), propertyChanged: OnDeltaFormatChanged);
    public static readonly BindableProperty DeltaCaptionProperty = BindableProperty.Create(nameof(DeltaCaption), typeof(string), typeof(ClamStatView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty IncreaseIsGoodProperty = BindableProperty.Create(nameof(IncreaseIsGood), typeof(bool), typeof(ClamStatView), true, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty TrendValuesProperty = BindableProperty.Create(nameof(TrendValues), typeof(IEnumerable<double>), typeof(ClamStatView), propertyChanged: OnTrendValuesChanged);
    public static readonly BindableProperty TrendKindProperty = BindableProperty.Create(nameof(TrendKind), typeof(ChartSparklineKind), typeof(ClamStatView), ChartSparklineKind.Area, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty TrendPlacementProperty = BindableProperty.Create(nameof(TrendPlacement), typeof(ChartTrendPlacement), typeof(ClamStatView), ChartTrendPlacement.Bottom, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ThresholdsProperty = BindableProperty.Create(nameof(Thresholds), typeof(IList<ChartThreshold>), typeof(ClamStatView), defaultValueCreator: static _ => new ObservableCollection<ChartThreshold>(), validateValue: static (_, value) => ValidateThresholds(value, nameof(Thresholds)), propertyChanged: OnThresholdsChanged);
    public static readonly BindableProperty ColorModeProperty = BindableProperty.Create(nameof(ColorMode), typeof(ChartStatColorMode), typeof(ClamStatView), ChartStatColorMode.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty FillProperty = BindableProperty.Create(nameof(Fill), typeof(Color), typeof(ClamStatView), propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty LayoutModeProperty = BindableProperty.Create(nameof(LayoutMode), typeof(ChartStatLayoutMode), typeof(ClamStatView), ChartStatLayoutMode.Auto, propertyChanged: OnLayoutInputChanged);

    private const double TrendThickness = 1;

    private readonly SparklineCore trend;
    private readonly ChartCollectionWatcher thresholdsWatcher;
    private StatRenderer? renderer;
    private ChartTextFormat valueFormat = ChartTextFormat.Default;
    private ChartTextFormat deltaFormat = ChartTextFormat.Default;
    private ChartValueFormatting formatting = ChartValueFormatting.Default;
    private Color? stateColor;
    private bool released;

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

#pragma warning disable CA1721
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
#pragma warning restore CA1721

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

    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public double ValueFontSize
    {
        get => (double)GetValue(ValueFontSizeProperty);
        set => SetValue(ValueFontSizeProperty, value);
    }

    public double MinValueFontSize
    {
        get => (double)GetValue(MinValueFontSizeProperty);
        set => SetValue(MinValueFontSizeProperty, value);
    }

    public double? PreviousValue
    {
        get => (double?)GetValue(PreviousValueProperty);
        set => SetValue(PreviousValueProperty, value);
    }

    public double? Delta
    {
        get => (double?)GetValue(DeltaProperty);
        set => SetValue(DeltaProperty, value);
    }

    public ChartDeltaMode DeltaMode
    {
        get => (ChartDeltaMode)GetValue(DeltaModeProperty);
        set => SetValue(DeltaModeProperty, value);
    }

    public string? DeltaFormat
    {
        get => (string?)GetValue(DeltaFormatProperty);
        set => SetValue(DeltaFormatProperty, value);
    }

    public string? DeltaCaption
    {
        get => (string?)GetValue(DeltaCaptionProperty);
        set => SetValue(DeltaCaptionProperty, value);
    }

    public bool IncreaseIsGood
    {
        get => (bool)GetValue(IncreaseIsGoodProperty);
        set => SetValue(IncreaseIsGoodProperty, value);
    }

    public IEnumerable<double>? TrendValues
    {
        get => (IEnumerable<double>?)GetValue(TrendValuesProperty);
        set => SetValue(TrendValuesProperty, value);
    }

    public ChartSparklineKind TrendKind
    {
        get => (ChartSparklineKind)GetValue(TrendKindProperty);
        set => SetValue(TrendKindProperty, value);
    }

    public ChartTrendPlacement TrendPlacement
    {
        get => (ChartTrendPlacement)GetValue(TrendPlacementProperty);
        set => SetValue(TrendPlacementProperty, value);
    }

#pragma warning disable CA2227
    public IList<ChartThreshold> Thresholds
    {
        get => (IList<ChartThreshold>)GetValue(ThresholdsProperty);
        set => SetValue(ThresholdsProperty, value);
    }
#pragma warning restore CA2227

    public ChartStatColorMode ColorMode
    {
        get => (ChartStatColorMode)GetValue(ColorModeProperty);
        set => SetValue(ColorModeProperty, value);
    }

    public Color? Fill
    {
        get => (Color?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public ChartStatLayoutMode LayoutMode
    {
        get => (ChartStatLayoutMode)GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
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

    internal StatLayout ChartLayout { get; } = new();

    internal SparklineLayout TrendLayout => trend.Layout;

    internal override int RenderedPointCount => trend.Layout.Count;

    public ClamStatView()
    {
        trend = new SparklineCore(this, TrendAnimationName, InvalidateData);
        CountUp = new ChartCountUp(this, ValueAnimationName);
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
        thresholdsWatcher.Deactivate();
        trend.Dispose();
    }

    internal override void OnAttached()
    {
        trend.Source.Activate();
        thresholdsWatcher.Activate();
    }

    internal override void OnDetached()
    {
        trend.Source.Deactivate();
        thresholdsWatcher.Deactivate();
    }

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData() => trend.Update(TrendValues, null);

    internal override void UpdateLayout(float width, float height)
    {
        var captured = trend.PrepareTransition(AnimationEasing, UpdateAnimationEnabled, CanAnimate);
        var culture = Culture ?? CultureInfo.CurrentCulture;
        var value = Value;
        var finite = Double.IsFinite(value);
        var colorMode = ColorMode;
        formatting = new ChartValueFormatting(ValueFormatter, ValueConverter, valueFormat, culture);
        var state = ThresholdResolver.Resolve(Thresholds, value);
        stateColor = ThresholdResolver.GetColor(state, ChartStyle);
        var options = new StatLayoutOptions
        {
            Title = Title,
            ValueText = finite ? formatting.Format(value) : EmptyText,
            Unit = finite ? Unit : null,
            ValueFontSize = ValueFontSize,
            MinValueFontSize = MinValueFontSize,
            Delta = StatDelta.Create(value, PreviousValue, Delta, DeltaMode, deltaFormat, IncreaseIsGood, culture),
            Caption = DeltaCaption,
            IconLevel = colorMode != ChartStatColorMode.None ? state?.Level ?? ChartThresholdLevel.None : ChartThresholdLevel.None,
            ShowBand = (colorMode == ChartStatColorMode.Indicator) && (stateColor is not null),
            LayoutMode = LayoutMode,
            TrendPlacement = TrendPlacement,
            HasTrend = trend.Values.HasFinite()
        };
        ChartLayout.Update(width, height, ResolvePadding(colorMode), ChartStyle, options, EnsureRenderer().Measurer);

        var bounds = ChartLayout.TrendBounds;
        trend.UpdateLayout(width, height, new Thickness(bounds.X, bounds.Y, Math.Max(0, width - bounds.Right), Math.Max(0, height - bounds.Bottom)), ChartStyle, new SparklineLayoutOptions { Kind = TrendKind, Thickness = TrendThickness, Markers = ChartSparklineMarkers.None });
        var length = ResolveAnimationLength(AnimationDuration);
        trend.CompleteTransition(captured, ResolveTrendEntrance(), length);
        TryStartEntrance();
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        var palette = Palette ?? ChartStyle.Palette;
        CountUp.Easing = AnimationEasing;
        trend.Interpolator.Easing = AnimationEasing;
        var state = new StatRenderState(ColorMode, stateColor, Fill, palette.Count > 0 ? palette[0] : ChartStyle.OtherColor, CountUp, formatting, !Double.IsFinite(Value));
        painter.Render(canvas, ChartLayout, state, trend, ChartStyle);
    }

    private static bool ValidateFontSize(object? value, string name) =>
        (value is double size) && Double.IsFinite(size) && (size > 0) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a positive finite value.");

    private static bool ValidateThresholds(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStatView)bindable;
        view.InvalidateLayout();
        view.AnimateValue((double)newValue);
    }

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStatView)bindable;
        view.valueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private static void OnDeltaFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStatView)bindable;
        view.deltaFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private static void OnTrendValuesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStatView)bindable;
        view.trend.SetValues((IEnumerable<double>?)newValue);
        view.InvalidateData();
    }

    private static void OnThresholdsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStatView)bindable;
        view.thresholdsWatcher.Watch(newValue);
        view.InvalidateLayout();
    }

    private void OnThresholdsCollectionChanged(NotifyCollectionChangedEventArgs e) => InvalidateLayout();

    private StatRenderer EnsureRenderer() => renderer ??= new StatRenderer(ChartStyle.FontFamily);

    private Thickness ResolvePadding(ChartStatColorMode colorMode) =>
        ContentPadding ?? (colorMode == ChartStatColorMode.None ? EffectiveContentPadding : new Thickness(StatLayout.CardPadding));
}
