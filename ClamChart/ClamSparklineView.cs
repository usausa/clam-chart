namespace ClamChart;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public partial class ClamSparklineView : ChartViewBase
{
    public static readonly BindableProperty ValuesProperty = BindableProperty.Create(nameof(Values), typeof(IEnumerable<double>), typeof(ClamSparklineView), propertyChanged: OnSourceChanged);
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ClamSparklineView), propertyChanged: OnSourceChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamSparklineView), propertyChanged: OnReaderChanged);
    public static readonly BindableProperty ValueKeyProperty = BindableProperty.Create(nameof(ValueKey), typeof(string), typeof(ClamSparklineView), propertyChanged: OnReaderChanged);
    public static readonly BindableProperty TrackItemChangesProperty = BindableProperty.Create(nameof(TrackItemChanges), typeof(bool), typeof(ClamSparklineView), true, propertyChanged: OnTrackItemChangesChanged);
    public static readonly BindableProperty KindProperty = BindableProperty.Create(nameof(Kind), typeof(ChartSparklineKind), typeof(ClamSparklineView), ChartSparklineKind.Line, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(ClamSparklineView), propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty NegativeColorProperty = BindableProperty.Create(nameof(NegativeColor), typeof(Color), typeof(ClamSparklineView), propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty ThicknessProperty = BindableProperty.Create(nameof(Thickness), typeof(double?), typeof(ClamSparklineView), validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(Thickness)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty SmoothProperty = BindableProperty.Create(nameof(Smooth), typeof(bool), typeof(ClamSparklineView), false, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty MarkersProperty = BindableProperty.Create(nameof(Markers), typeof(ChartSparklineMarkers), typeof(ClamSparklineView), ChartSparklineMarkers.Last, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(double?), typeof(ClamSparklineView), validateValue: static (_, value) => ValidateFinite(value, nameof(Minimum)), propertyChanged: OnRangeChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(double?), typeof(ClamSparklineView), validateValue: static (_, value) => ValidateFinite(value, nameof(Maximum)), propertyChanged: OnRangeChanged);
    public static readonly BindableProperty ReferenceValueProperty = BindableProperty.Create(nameof(ReferenceValue), typeof(double?), typeof(ClamSparklineView), validateValue: static (_, value) => ValidateFinite(value, nameof(ReferenceValue)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty NormalRangeMinimumProperty = BindableProperty.Create(nameof(NormalRangeMinimum), typeof(double?), typeof(ClamSparklineView), validateValue: static (_, value) => ValidateFinite(value, nameof(NormalRangeMinimum)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty NormalRangeMaximumProperty = BindableProperty.Create(nameof(NormalRangeMaximum), typeof(double?), typeof(ClamSparklineView), validateValue: static (_, value) => ValidateFinite(value, nameof(NormalRangeMaximum)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty EmphasisModeProperty = BindableProperty.Create(nameof(EmphasisMode), typeof(ChartSparklineEmphasis), typeof(ClamSparklineView), ChartSparklineEmphasis.None, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty ThresholdsProperty = BindableProperty.Create(nameof(Thresholds), typeof(IList<ChartThreshold>), typeof(ClamSparklineView), defaultValueCreator: static _ => new ObservableCollection<ChartThreshold>(), validateValue: static (_, value) => ValidateThresholds(value, nameof(Thresholds)), propertyChanged: OnThresholdsChanged);
    public static readonly BindableProperty ThresholdsModeProperty = BindableProperty.Create(nameof(ThresholdsMode), typeof(ChartThresholdsMode), typeof(ClamSparklineView), ChartThresholdsMode.None, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty EdgeToEdgeProperty = BindableProperty.Create(nameof(EdgeToEdge), typeof(bool), typeof(ClamSparklineView), false, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LiveIndicatorVisibleProperty = BindableProperty.Create(nameof(LiveIndicatorVisible), typeof(bool), typeof(ClamSparklineView), false, propertyChanged: OnLayoutInputChanged);

    private readonly SparklineCore core;
    private readonly ChartCollectionWatcher thresholdsWatcher;
    private SparklineRenderer? renderer;
    private bool invertedBoundsReported;
    private bool released;

    public IEnumerable<double>? Values
    {
        get => (IEnumerable<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
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

    public string? ValueKey
    {
        get => (string?)GetValue(ValueKeyProperty);
        set => SetValue(ValueKeyProperty, value);
    }

    public bool TrackItemChanges
    {
        get => (bool)GetValue(TrackItemChangesProperty);
        set => SetValue(TrackItemChangesProperty, value);
    }

    public ChartSparklineKind Kind
    {
        get => (ChartSparklineKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public Color? NegativeColor
    {
        get => (Color?)GetValue(NegativeColorProperty);
        set => SetValue(NegativeColorProperty, value);
    }

    public double? Thickness
    {
        get => (double?)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public bool Smooth
    {
        get => (bool)GetValue(SmoothProperty);
        set => SetValue(SmoothProperty, value);
    }

    public ChartSparklineMarkers Markers
    {
        get => (ChartSparklineMarkers)GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

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

    public double? ReferenceValue
    {
        get => (double?)GetValue(ReferenceValueProperty);
        set => SetValue(ReferenceValueProperty, value);
    }

    public double? NormalRangeMinimum
    {
        get => (double?)GetValue(NormalRangeMinimumProperty);
        set => SetValue(NormalRangeMinimumProperty, value);
    }

    public double? NormalRangeMaximum
    {
        get => (double?)GetValue(NormalRangeMaximumProperty);
        set => SetValue(NormalRangeMaximumProperty, value);
    }

    public ChartSparklineEmphasis EmphasisMode
    {
        get => (ChartSparklineEmphasis)GetValue(EmphasisModeProperty);
        set => SetValue(EmphasisModeProperty, value);
    }

#pragma warning disable CA2227
    public IList<ChartThreshold> Thresholds
    {
        get => (IList<ChartThreshold>)GetValue(ThresholdsProperty);
        set => SetValue(ThresholdsProperty, value);
    }
#pragma warning restore CA2227

    public ChartThresholdsMode ThresholdsMode
    {
        get => (ChartThresholdsMode)GetValue(ThresholdsModeProperty);
        set => SetValue(ThresholdsModeProperty, value);
    }

    public bool EdgeToEdge
    {
        get => (bool)GetValue(EdgeToEdgeProperty);
        set => SetValue(EdgeToEdgeProperty, value);
    }

    public bool LiveIndicatorVisible
    {
        get => (bool)GetValue(LiveIndicatorVisibleProperty);
        set => SetValue(LiveIndicatorVisibleProperty, value);
    }

    public Func<object, double?>? ValueSelector
    {
        get;
        set
        {
            field = value;
            core.ResetReader();
            InvalidateData();
        }
    }

    internal SparklineLayout ChartLayout => core.Layout;

    internal override int RenderedPointCount => ChartLayout.Count;

    public ClamSparklineView()
    {
        core = new SparklineCore(this, DataAnimationName, InvalidateData, CreateReader);
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
        core.Dispose();
    }

    internal override ChartDensity ResolveDensity() => ChartDensity.Minimal;

    internal override void OnAttached()
    {
        core.Source.Activate();
        thresholdsWatcher.Activate();
    }

    internal override void OnDetached()
    {
        core.Source.Deactivate();
        thresholdsWatcher.Deactivate();
    }

    internal override SkeletonShape SkeletonShape => Kind is ChartSparklineKind.Bar or ChartSparklineKind.WinLoss ? SkeletonShape.Bars : SkeletonShape.Line;

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData() => core.Update(Values, ItemsSource);

    internal override string? CreateAutomationSummary(ChartSummaryText text)
    {
        var layout = ChartLayout;
        if ((layout.LastIndex < 0) || (layout.HighIndex < 0) || (layout.LowIndex < 0))
        {
            return null;
        }

        var values = core.Values;
        var count = 0;
        for (var i = 0; i < values.Count; i++)
        {
            if (Double.IsFinite(values[i]))
            {
                count++;
            }
        }

        text.AddCount(count, false);
        text.AddValue(ChartSummaryWord.Last, CompactNumberFormat.Format(values[layout.LastIndex], text.Culture));
        text.AddValue(ChartSummaryWord.Maximum, CompactNumberFormat.Format(values[layout.HighIndex], text.Culture));
        text.AddValue(ChartSummaryWord.Minimum, CompactNumberFormat.Format(values[layout.LowIndex], text.Culture));
        return text.Build();
    }

    internal override void UpdateLayout(float width, float height)
    {
        var captured = core.PrepareTransition(AnimationEasing, UpdateAnimationEnabled, CanAnimate);
        var options = new SparklineLayoutOptions
        {
            Kind = Kind,
            Thickness = Thickness,
            Markers = Markers,
            Minimum = Minimum,
            Maximum = Maximum,
            ReferenceValue = ReferenceValue,
            NormalRangeMinimum = NormalRangeMinimum,
            NormalRangeMaximum = NormalRangeMaximum,
            Thresholds = Thresholds,
            EdgeToEdge = EdgeToEdge,
            LiveIndicator = LiveIndicatorVisible
        };
        core.UpdateLayout(width, height, EffectiveContentPadding, ChartStyle, options);
        if (ChartLayout.HasInvertedBounds && !invertedBoundsReported)
        {
            invertedBoundsReported = true;
            ChartDiagnostics.Warn("The sparkline Maximum is not greater than Minimum, so the automatic range is used.");
        }

        core.CompleteTransition(captured, ResolveEntrance(), ResolveAnimationLength(AnimationDuration));
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        if (ChartLayout.FirstIndex < 0)
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
        var line = color.ToSKColor();
        var weak = EmphasisMode == ChartSparklineEmphasis.Last ? ChartStyle.EmphasisDimmedOpacity : 1;
        var paint = new SparklinePaint(line, (NegativeColor ?? ChartStyle.CriticalColor).ToSKColor(), ChartColor.Fade(line, ChartStyle.AreaFillOpacity), weak);
        painter.Render(canvas, ChartLayout, Interpolator, core.Values, paint, ChartStyle, Smooth, ThresholdsMode, LiveIndicatorVisible, EffectPhase);
    }

    private static bool ValidateFinite(object? value, string name) =>
        (value is not double number) || Double.IsFinite(number) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number.");

    private static bool ValidateThresholds(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static void OnThresholdsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamSparklineView)bindable;
        view.thresholdsWatcher.Watch(newValue);
        view.InvalidateLayout();
    }

    private void OnThresholdsCollectionChanged(NotifyCollectionChangedEventArgs e) => InvalidateLayout();

    private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamSparklineView)bindable).ReplaceSource();

    private static void OnReaderChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamSparklineView)bindable;
        view.core.ResetReader();
        view.InvalidateData();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamSparklineView)bindable;
        if (view.Values is null)
        {
            view.core.Source.SetTrackItemChanges((bool)newValue);
        }
    }

    private static void OnRangeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamSparklineView)bindable;
        view.invertedBoundsReported = false;
        view.InvalidateLayout();
    }

    private SparklineRenderer EnsureRenderer() => renderer ??= new SparklineRenderer(ChartStyle.FontFamily);

    private void ReplaceSource()
    {
        if (Values is { } values)
        {
            core.SetValues(values);
        }
        else
        {
            core.SetItems(ItemsSource, TrackItemChanges);
        }

        InvalidateData();
    }

    private ChartValueReader CreateReader() => new(new ChartValueOptions { Accessors = ValueAccessors, YKey = ValueKey, ValueSelector = ValueSelector });
}
