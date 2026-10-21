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

public partial class ClamRadialBarView : ChartViewBase
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ClamRadialBarView), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamRadialBarView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty LabelKeyProperty = BindableProperty.Create(nameof(LabelKey), typeof(string), typeof(ClamRadialBarView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ValueKeyProperty = BindableProperty.Create(nameof(ValueKey), typeof(string), typeof(ClamRadialBarView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty MaximumKeyProperty = BindableProperty.Create(nameof(MaximumKey), typeof(string), typeof(ClamRadialBarView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string), typeof(ClamRadialBarView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty TrackItemChangesProperty = BindableProperty.Create(nameof(TrackItemChanges), typeof(bool), typeof(ClamRadialBarView), true, propertyChanged: OnTrackItemChangesChanged);
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(double), typeof(ClamRadialBarView), 0d, validateValue: static (_, value) => ValidateFinite(value, nameof(Minimum)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(double), typeof(ClamRadialBarView), 100d, validateValue: static (_, value) => ValidateFinite(value, nameof(Maximum)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty StartAngleProperty = BindableProperty.Create(nameof(StartAngle), typeof(double), typeof(ClamRadialBarView), 0d, validateValue: static (_, value) => ValidateFinite(value, nameof(StartAngle)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty SweepAngleProperty = BindableProperty.Create(nameof(SweepAngle), typeof(double), typeof(ClamRadialBarView), 360d, validateValue: static (_, value) => ValidateSweepAngle(value, nameof(SweepAngle)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty InnerRadiusProperty = BindableProperty.Create(nameof(InnerRadius), typeof(double), typeof(ClamRadialBarView), 0.4, validateValue: static (_, value) => ValidateInnerRadius(value, nameof(InnerRadius)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty RingThicknessProperty = BindableProperty.Create(nameof(RingThickness), typeof(double?), typeof(ClamRadialBarView), validateValue: static (_, value) => (value is null) || ValidatePositive(value, nameof(RingThickness)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty RingSpacingProperty = BindableProperty.Create(nameof(RingSpacing), typeof(double), typeof(ClamRadialBarView), 4d, validateValue: static (_, value) => ValidateNonNegative(value, nameof(RingSpacing)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CapStyleProperty = BindableProperty.Create(nameof(CapStyle), typeof(ChartCapStyle), typeof(ClamRadialBarView), ChartCapStyle.Flat, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty DisplayModeProperty = BindableProperty.Create(nameof(DisplayMode), typeof(ChartRadialBarMode), typeof(ClamRadialBarView), ChartRadialBarMode.Basic, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty SegmentCountProperty = BindableProperty.Create(nameof(SegmentCount), typeof(int), typeof(ClamRadialBarView), 20, validateValue: static (_, value) => ValidateSegmentCount(value, nameof(SegmentCount)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty TrackVisibleProperty = BindableProperty.Create(nameof(TrackVisible), typeof(bool), typeof(ClamRadialBarView), true, propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty OverflowModeProperty = BindableProperty.Create(nameof(OverflowMode), typeof(ChartOverflowMode), typeof(ClamRadialBarView), ChartOverflowMode.Clamp, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ThresholdsProperty = BindableProperty.Create(nameof(Thresholds), typeof(IList<ChartThreshold>), typeof(ClamRadialBarView), defaultValueCreator: static _ => new ObservableCollection<ChartThreshold>(), validateValue: static (_, value) => ValidateThresholds(value, nameof(Thresholds)), propertyChanged: OnThresholdsChanged);
    public static readonly BindableProperty CenterValueModeProperty = BindableProperty.Create(nameof(CenterValueMode), typeof(ChartCenterValueMode), typeof(ClamRadialBarView), ChartCenterValueMode.Auto, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CenterTextProperty = BindableProperty.Create(nameof(CenterText), typeof(string), typeof(ClamRadialBarView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CenterLabelProperty = BindableProperty.Create(nameof(CenterLabel), typeof(string), typeof(ClamRadialBarView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ClamRadialBarView), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ClamRadialBarView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendVisibleProperty = BindableProperty.Create(nameof(LegendVisible), typeof(bool?), typeof(ClamRadialBarView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendPositionProperty = BindableProperty.Create(nameof(LegendPosition), typeof(ChartLegendPosition), typeof(ClamRadialBarView), ChartLegendPosition.Right, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendValueModeProperty = BindableProperty.Create(nameof(LegendValueMode), typeof(ChartLegendValueMode), typeof(ClamRadialBarView), ChartLegendValueMode.None, propertyChanged: OnLayoutInputChanged);

    private readonly ChartDataSource source;
    private readonly ChartCollectionWatcher thresholdsWatcher;
    private readonly PieInteraction interaction = new();
    private readonly List<GaugeEntry> entries = [];
    private RadialBarRenderer? renderer;
    private DataSnapshot snapshot = DataSnapshot.Empty;
    private ChartTextFormat valueFormat = ChartTextFormat.Default;
    private bool otherColorReported;
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

    public double StartAngle
    {
        get => (double)GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    public double SweepAngle
    {
        get => (double)GetValue(SweepAngleProperty);
        set => SetValue(SweepAngleProperty, value);
    }

    public double InnerRadius
    {
        get => (double)GetValue(InnerRadiusProperty);
        set => SetValue(InnerRadiusProperty, value);
    }

    public double? RingThickness
    {
        get => (double?)GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    public double RingSpacing
    {
        get => (double)GetValue(RingSpacingProperty);
        set => SetValue(RingSpacingProperty, value);
    }

    public ChartCapStyle CapStyle
    {
        get => (ChartCapStyle)GetValue(CapStyleProperty);
        set => SetValue(CapStyleProperty, value);
    }

    public ChartRadialBarMode DisplayMode
    {
        get => (ChartRadialBarMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public int SegmentCount
    {
        get => (int)GetValue(SegmentCountProperty);
        set => SetValue(SegmentCountProperty, value);
    }

    public bool TrackVisible
    {
        get => (bool)GetValue(TrackVisibleProperty);
        set => SetValue(TrackVisibleProperty, value);
    }

    public ChartOverflowMode OverflowMode
    {
        get => (ChartOverflowMode)GetValue(OverflowModeProperty);
        set => SetValue(OverflowModeProperty, value);
    }

#pragma warning disable CA2227
    public IList<ChartThreshold> Thresholds
    {
        get => (IList<ChartThreshold>)GetValue(ThresholdsProperty);
        set => SetValue(ThresholdsProperty, value);
    }
#pragma warning restore CA2227

    public ChartCenterValueMode CenterValueMode
    {
        get => (ChartCenterValueMode)GetValue(CenterValueModeProperty);
        set => SetValue(CenterValueModeProperty, value);
    }

    public string? CenterText
    {
        get => (string?)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public string? CenterLabel
    {
        get => (string?)GetValue(CenterLabelProperty);
        set => SetValue(CenterLabelProperty, value);
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

    public ChartLegendValueMode LegendValueMode
    {
        get => (ChartLegendValueMode)GetValue(LegendValueModeProperty);
        set => SetValue(LegendValueModeProperty, value);
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

    internal RadialBarLayout ChartLayout { get; } = new();

    internal override int RenderedPointCount => ChartLayout.Rows.Count;

    private bool IsSelectable => (SelectionMode != ChartSelectionMode.None) && (EffectiveDensity != ChartDensity.Minimal);

    public ClamRadialBarView()
    {
        source = new ChartDataSource(InvalidateData);
        dataAnimator = new ChartAnimator(this, DataAnimationName, OnDataProgress);
        selectionAnimator = new ChartAnimator(this, SelectionAnimationName, OnSelectionProgress);
        CenterCount = new ChartCountUp(this, CenterAnimationName);
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

    internal override SkeletonShape SkeletonShape => SkeletonShape.Ring;

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
            entries.Add(new GaugeEntry(i, data.GetLabel(i), values[i], data.GetMaximum(i), Double.NaN, data.GetColor(i)));
        }

        ResolveSelection(SelectedItem, SelectedIndex, false, false);
    }

    internal override void UpdateLayout(float width, float height)
    {
        var captured = PrepareTransition();
        var culture = Culture ?? CultureInfo.CurrentCulture;
        ChartLayout.Update(width, height, EffectiveContentPadding, ChartStyle, culture, CreateLayoutOptions(culture), CollectionsMarshal.AsSpan(entries), EnsureRenderer().Measurer);
        CompleteTransition(captured);
    }

    internal override string? CreateAutomationSummary(ChartSummaryText text)
    {
        foreach (var row in ChartLayout.Rows)
        {
            if ((row.ValueText ?? row.PercentText) is { } value)
            {
                text.AddText(String.IsNullOrEmpty(row.Label) ? value : row.Label + " " + value);
            }
            else if (!String.IsNullOrEmpty(row.Label))
            {
                text.AddText(row.Label);
            }
        }

        return text.Build();
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
        ReportPaletteOverflow(palette);
        Interpolator.Easing = AnimationEasing;
        CenterCount.Easing = AnimationEasing;
        painter.Render(canvas, ChartLayout, Interpolator, interaction, CenterCount, countedKey, new RadialBarRenderState(palette, Thresholds, TrackVisible), ChartStyle);
    }

    private static bool ValidateFinite(object? value, string name) =>
        (value is double number) && Double.IsFinite(number) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number.");

    private static bool ValidateSweepAngle(object? value, string name) =>
        (value is double angle) && (angle > 0) && (angle <= 360) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be greater than 0 and at most 360.");

    private static bool ValidateInnerRadius(object? value, string name) =>
        (value is double ratio) && (ratio >= 0) && (ratio <= 0.95) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be from 0 to 0.95.");

    private static bool ValidatePositive(object? value, string name) =>
        (value is double number) && Double.IsFinite(number) && (number > 0) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a positive finite value.");

    private static bool ValidateNonNegative(object? value, string name) =>
        (value is double number) && Double.IsFinite(number) && (number >= 0) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite value of zero or more.");

    private static bool ValidateSegmentCount(object? value, string name) =>
        (value is int count) && (count >= 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be 1 or more.");

    private static bool ValidateThresholds(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamRadialBarView)bindable).InvalidateData();

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamRadialBarView)bindable;
        view.source.SetSource((IEnumerable?)newValue);
        view.sourceReplaced = true;
        view.InvalidateData();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamRadialBarView)bindable).source.SetTrackItemChanges((bool)newValue);

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamRadialBarView)bindable;
        view.valueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private static void OnThresholdsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamRadialBarView)bindable;
        view.thresholdsWatcher.Watch(newValue);
        view.InvalidateLayout();
    }

    private void OnThresholdsCollectionChanged(NotifyCollectionChangedEventArgs e) => InvalidateLayout();

    private RadialBarRenderer EnsureRenderer() => renderer ??= new RadialBarRenderer(ChartStyle.FontFamily);

    private RadialBarLayoutOptions CreateLayoutOptions(CultureInfo culture)
    {
        var density = EffectiveDensity;
        return new RadialBarLayoutOptions
        {
            Minimum = Minimum,
            Maximum = Maximum,
            StartAngle = StartAngle,
            SweepAngle = SweepAngle,
            InnerRadius = InnerRadius,
            RingThickness = RingThickness,
            RingSpacing = RingSpacing,
            CapStyle = CapStyle,
            DisplayMode = DisplayMode,
            SegmentCount = SegmentCount,
            OverflowMode = OverflowMode,
            Formatting = new ChartValueFormatting(ValueFormatter, ValueConverter, valueFormat, culture),
            LegendVisible = (density != ChartDensity.Minimal) && (LegendVisible ?? (density == ChartDensity.Full)),
            LegendPosition = LegendPosition,
            LegendMaxRows = density == ChartDensity.Full ? LegendLayout.MaxRows : 1,
            LegendValueMode = LegendValueMode,
            CenterValueMode = CenterValueMode,
            CenterText = CenterText,
            CenterLabel = CenterLabel,
            Selectable = IsSelectable
        };
    }

    private void ReplaceSnapshot(DataSnapshot value)
    {
        var previous = snapshot;
        snapshot = value;
        if (!ReferenceEquals(previous, value) && !ReferenceEquals(previous, DataSnapshot.Empty))
        {
            previous.Dispose();
        }
    }

    private void ReportPaletteOverflow(IReadOnlyList<Color> palette)
    {
        if (otherColorReported)
        {
            return;
        }

        var rows = ChartLayout.Rows;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if ((row.Color is null) && (row.Slot >= palette.Count))
            {
                otherColorReported = true;
                ChartDiagnostics.Warn($"The ring at index {row.DataIndex} has no color in the palette and is drawn with OtherColor.");
                return;
            }
        }
    }
}
