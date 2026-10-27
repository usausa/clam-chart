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

public partial class ClamStateTimelineView : ChartViewBase
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ClamStateTimelineView), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamStateTimelineView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty RowKeyProperty = BindableProperty.Create(nameof(RowKey), typeof(string), typeof(ClamStateTimelineView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty StartKeyProperty = BindableProperty.Create(nameof(StartKey), typeof(string), typeof(ClamStateTimelineView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty EndKeyProperty = BindableProperty.Create(nameof(EndKey), typeof(string), typeof(ClamStateTimelineView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty StateKeyProperty = BindableProperty.Create(nameof(StateKey), typeof(string), typeof(ClamStateTimelineView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty TrackItemChangesProperty = BindableProperty.Create(nameof(TrackItemChanges), typeof(bool), typeof(ClamStateTimelineView), true, propertyChanged: OnTrackItemChangesChanged);
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(DateTime?), typeof(ClamStateTimelineView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(DateTime?), typeof(ClamStateTimelineView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueMappingsProperty = BindableProperty.Create(nameof(ValueMappings), typeof(IList<ChartValueMapping>), typeof(ClamStateTimelineView), defaultValueCreator: static _ => new ObservableCollection<ChartValueMapping>(), validateValue: static (_, value) => ValidateList(value, nameof(ValueMappings)), propertyChanged: OnValueMappingsChanged);
    public static readonly BindableProperty ThresholdsProperty = BindableProperty.Create(nameof(Thresholds), typeof(IList<ChartThreshold>), typeof(ClamStateTimelineView), defaultValueCreator: static _ => new ObservableCollection<ChartThreshold>(), validateValue: static (_, value) => ValidateList(value, nameof(Thresholds)), propertyChanged: OnThresholdsChanged);
    public static readonly BindableProperty MergeValuesProperty = BindableProperty.Create(nameof(MergeValues), typeof(bool), typeof(ClamStateTimelineView), true, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ShowValueProperty = BindableProperty.Create(nameof(ShowValue), typeof(ChartVisibilityMode), typeof(ClamStateTimelineView), ChartVisibilityMode.Auto, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty AlignValueProperty = BindableProperty.Create(nameof(AlignValue), typeof(ChartValueAlignment), typeof(ClamStateTimelineView), ChartValueAlignment.Left, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty RowHeightProperty = BindableProperty.Create(nameof(RowHeight), typeof(double), typeof(ClamStateTimelineView), 0.9, validateValue: static (_, value) => ValidateRatio(value, nameof(RowHeight)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty FillOpacityProperty = BindableProperty.Create(nameof(FillOpacity), typeof(double), typeof(ClamStateTimelineView), 0.7, validateValue: static (_, value) => ValidateRatio(value, nameof(FillOpacity)), propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty LineWidthProperty = BindableProperty.Create(nameof(LineWidth), typeof(double), typeof(ClamStateTimelineView), 0d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(LineWidth)), propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty LegendVisibleProperty = BindableProperty.Create(nameof(LegendVisible), typeof(bool?), typeof(ClamStateTimelineView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendPositionProperty = BindableProperty.Create(nameof(LegendPosition), typeof(ChartLegendPosition), typeof(ClamStateTimelineView), ChartLegendPosition.Bottom, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ClamStateTimelineView), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ClamStateTimelineView), propertyChanged: OnLayoutInputChanged);

    private readonly ChartDataSource source;
    private readonly ChartCollectionWatcher mappingsWatcher;
    private readonly ChartCollectionWatcher thresholdsWatcher;
    private readonly StateTimelineInteraction interaction = new();
    private readonly List<StateTimelineEntry> entries = [];
    private readonly List<object?> items = [];
    private StateTimelineRenderer? renderer;
    private ChartTextFormat valueFormat = ChartTextFormat.Default;
    private bool keysReported;
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

    public string? RowKey
    {
        get => (string?)GetValue(RowKeyProperty);
        set => SetValue(RowKeyProperty, value);
    }

    public string? StartKey
    {
        get => (string?)GetValue(StartKeyProperty);
        set => SetValue(StartKeyProperty, value);
    }

    public string? EndKey
    {
        get => (string?)GetValue(EndKeyProperty);
        set => SetValue(EndKeyProperty, value);
    }

    public string? StateKey
    {
        get => (string?)GetValue(StateKeyProperty);
        set => SetValue(StateKeyProperty, value);
    }

    public bool TrackItemChanges
    {
        get => (bool)GetValue(TrackItemChangesProperty);
        set => SetValue(TrackItemChangesProperty, value);
    }

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

#pragma warning disable CA2227
    public IList<ChartValueMapping> ValueMappings
    {
        get => (IList<ChartValueMapping>)GetValue(ValueMappingsProperty);
        set => SetValue(ValueMappingsProperty, value);
    }

    public IList<ChartThreshold> Thresholds
    {
        get => (IList<ChartThreshold>)GetValue(ThresholdsProperty);
        set => SetValue(ThresholdsProperty, value);
    }
#pragma warning restore CA2227

    public bool MergeValues
    {
        get => (bool)GetValue(MergeValuesProperty);
        set => SetValue(MergeValuesProperty, value);
    }

    public ChartVisibilityMode ShowValue
    {
        get => (ChartVisibilityMode)GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }

    public ChartValueAlignment AlignValue
    {
        get => (ChartValueAlignment)GetValue(AlignValueProperty);
        set => SetValue(AlignValueProperty, value);
    }

    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public double FillOpacity
    {
        get => (double)GetValue(FillOpacityProperty);
        set => SetValue(FillOpacityProperty, value);
    }

    public double LineWidth
    {
        get => (double)GetValue(LineWidthProperty);
        set => SetValue(LineWidthProperty, value);
    }

    public bool? LegendVisible
    {
        get => (bool?)GetValue(LegendVisibleProperty);
        set => SetValue(LegendVisibleProperty, value);
    }

    public ChartLegendPosition LegendPosition
    {
        get => (ChartLegendPosition)GetValue(LegendPositionProperty);
        set => SetValue(LegendPositionProperty, value);
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

    internal StateTimelineLayout ChartLayout { get; } = new();

    internal override int RenderedPointCount => ChartLayout.Segments.Count;

    internal override SkeletonShape SkeletonShape => SkeletonShape.Rows;

    public ClamStateTimelineView()
    {
        source = new ChartDataSource(InvalidateData);
        dataAnimator = new ChartAnimator(this, DataAnimationName, OnDataProgress);
        mappingsWatcher = new ChartCollectionWatcher(OnListChanged);
        mappingsWatcher.Watch(ValueMappings);
        thresholdsWatcher = new ChartCollectionWatcher(OnListChanged);
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
        mappingsWatcher.Deactivate();
        thresholdsWatcher.Deactivate();
    }

    internal override void OnAttached()
    {
        source.Activate();
        mappingsWatcher.Activate();
        thresholdsWatcher.Activate();
    }

    internal override void OnDetached()
    {
        source.Deactivate();
        mappingsWatcher.Deactivate();
        thresholdsWatcher.Deactivate();
    }

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData()
    {
        var oldCount = entries.Count;
        entries.Clear();
        items.Clear();
        var accessors = ValueAccessors;
        var row = ChartValueReader.Resolve(accessors, RowKey, "the row", static kind => kind == ChartValueKind.Text);
        var start = ChartValueReader.Resolve(accessors, StartKey, "the start", static kind => kind == ChartValueKind.Time);
        var end = ChartValueReader.Resolve(accessors, EndKey, "the end", static kind => kind == ChartValueKind.Time);
        var state = ChartValueReader.Resolve(accessors, StateKey, "the state", static kind => kind is ChartValueKind.Text or ChartValueKind.Number);
        if (source.Source is { } list)
        {
            foreach (var item in list)
            {
                if ((start is not null) && (state is not null) && (item is not null))
                {
                    var text = state.Kind == ChartValueKind.Text ? state.GetText(item) : null;
                    var number = state.Kind == ChartValueKind.Number ? state.GetNumber(item) : Double.NaN;
                    entries.Add(new StateTimelineEntry(items.Count, row?.GetText(item), start.GetTime(item), end?.GetTime(item) ?? Double.NaN, text, number));
                }

                items.Add(item);
            }
        }

        RecordChange(oldCount, entries.Count);
        source.Changes.Clear();
        ReportKeys(start, state);
    }

    internal override void UpdateLayout(float width, float height)
    {
        var captured = PrepareTransition();
        var culture = Culture ?? CultureInfo.CurrentCulture;
        ChartLayout.Update(width, height, EffectiveContentPadding, EffectiveStyle, culture, CreateLayoutOptions(culture), CollectionsMarshal.AsSpan(entries), EnsureRenderer().Measurer);
        CompleteTransition(captured);
        RefreshTracking();
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        if (ChartLayout.Segments.Count == 0)
        {
            if (!String.IsNullOrEmpty(EmptyText))
            {
                painter.DrawEmptyText(canvas, EmptyText, ChartLayout.Bounds, EffectiveStyle);
            }

            return;
        }

        interaction.TooltipVisible = TooltipVisible;
        Interpolator.Easing = AnimationEasing;
        ChartLayout.Axis.DisplayScale = Interpolator.DisplayScale;
        painter.Render(canvas, ChartLayout, Interpolator, interaction, new StateTimelineRenderState(FillOpacity, LineWidth, ShowValue, AlignValue), EffectiveStyle, width, height);
    }

    internal override string? CreateAutomationSummary(ChartSummaryText text)
    {
        var layout = ChartLayout;
        for (var row = 0; row < layout.RowCount; row++)
        {
            var last = layout.GetLastSegment(row);
            if (last < 0)
            {
                continue;
            }

            text.BeginGroup(layout.RowNames[row] ?? (layout.RowCount > 1 ? text.SeriesName(row + 1) : null));
            text.AddValue(ChartSummaryWord.Last, layout.GetText(last));
        }

        return text.Build();
    }

    internal object? GetItem(int index) => (index >= 0) && (index < items.Count) ? items[index] : null;

    private static bool ValidateList(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static bool ValidateRatio(object? value, string name) =>
        (value is double ratio) && (ratio >= 0) && (ratio <= 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be from 0 to 1.");

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamStateTimelineView)bindable).InvalidateData();

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStateTimelineView)bindable;
        view.source.SetSource((IEnumerable?)newValue);
        view.sourceReplaced = true;
        view.InvalidateData();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamStateTimelineView)bindable).source.SetTrackItemChanges((bool)newValue);

    private static void OnValueMappingsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStateTimelineView)bindable;
        view.mappingsWatcher.Watch(newValue);
        view.InvalidateLayout();
    }

    private static void OnThresholdsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStateTimelineView)bindable;
        view.thresholdsWatcher.Watch(newValue);
        view.InvalidateLayout();
    }

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamStateTimelineView)bindable;
        view.valueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private void OnListChanged(NotifyCollectionChangedEventArgs e) => InvalidateLayout();

    private StateTimelineRenderer EnsureRenderer() => renderer ??= new StateTimelineRenderer(EffectiveStyle.FontFamily);

    private StateTimelineLayoutOptions CreateLayoutOptions(CultureInfo culture)
    {
        var density = EffectiveDensity;
        return new StateTimelineLayoutOptions
        {
            Minimum = Minimum is { } minimum ? ChartDateTime.ToLocalTicks(minimum) : null,
            Maximum = Maximum is { } maximum ? ChartDateTime.ToLocalTicks(maximum) : null,
            MergeValues = MergeValues,
            RowHeight = RowHeight,
            LabelsVisible = density != ChartDensity.Minimal,
            LegendVisible = LegendVisible ?? (density == ChartDensity.Full),
            LegendPosition = LegendPosition,
            Mappings = ValueMappings,
            Thresholds = Thresholds,
            FirstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek,
            Formatting = new ChartValueFormatting(null, ValueConverter, valueFormat, culture)
        };
    }

    private void ReportKeys(ChartAccessor? start, ChartAccessor? state)
    {
        if (!keysReported && (items.Count > 0) && ((start is null) || (state is null)))
        {
            keysReported = true;
            ChartDiagnostics.Warn("The state timeline needs StartKey and StateKey.");
        }
    }
}
