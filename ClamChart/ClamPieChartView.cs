namespace ClamChart;

using System.Collections;
using System.Globalization;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;

public partial class ClamPieChartView : ChartViewBase
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ClamPieChartView), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamPieChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ValueKeyProperty = BindableProperty.Create(nameof(ValueKey), typeof(string), typeof(ClamPieChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty LabelKeyProperty = BindableProperty.Create(nameof(LabelKey), typeof(string), typeof(ClamPieChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string), typeof(ClamPieChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty TrackItemChangesProperty = BindableProperty.Create(nameof(TrackItemChanges), typeof(bool), typeof(ClamPieChartView), true, propertyChanged: OnTrackItemChangesChanged);
    public static readonly BindableProperty InnerRadiusProperty = BindableProperty.Create(nameof(InnerRadius), typeof(double), typeof(ClamPieChartView), 0.6, validateValue: static (_, value) => ValidateInnerRadius(value, nameof(InnerRadius)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty StartAngleProperty = BindableProperty.Create(nameof(StartAngle), typeof(double), typeof(ClamPieChartView), 0d, validateValue: static (_, value) => ValidateFinite(value, nameof(StartAngle)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty SweepAngleProperty = BindableProperty.Create(nameof(SweepAngle), typeof(double), typeof(ClamPieChartView), 360d, validateValue: static (_, value) => ValidateSweepAngle(value, nameof(SweepAngle)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty SliceGapProperty = BindableProperty.Create(nameof(SliceGap), typeof(double?), typeof(ClamPieChartView), validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(SliceGap)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty SortModeProperty = BindableProperty.Create(nameof(SortMode), typeof(ChartSortMode), typeof(ClamPieChartView), ChartSortMode.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty MaxSlicesProperty = BindableProperty.Create(nameof(MaxSlices), typeof(int), typeof(ClamPieChartView), 8, validateValue: static (_, value) => ValidateMaxSlices(value, nameof(MaxSlices)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty OthersThresholdProperty = BindableProperty.Create(nameof(OthersThreshold), typeof(double), typeof(ClamPieChartView), 0d, validateValue: static (_, value) => ValidateThreshold(value, nameof(OthersThreshold)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty OthersLabelProperty = BindableProperty.Create(nameof(OthersLabel), typeof(string), typeof(ClamPieChartView), "Other", validateValue: static (_, value) => ValidateText(value, nameof(OthersLabel)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LabelsModeProperty = BindableProperty.Create(nameof(LabelsMode), typeof(ChartPieLabelsMode), typeof(ClamPieChartView), ChartPieLabelsMode.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LabelContentProperty = BindableProperty.Create(nameof(LabelContent), typeof(ChartPieLabelContent), typeof(ClamPieChartView), ChartPieLabelContent.Percent, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendVisibleProperty = BindableProperty.Create(nameof(LegendVisible), typeof(bool?), typeof(ClamPieChartView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendPositionProperty = BindableProperty.Create(nameof(LegendPosition), typeof(ChartLegendPosition), typeof(ClamPieChartView), ChartLegendPosition.Bottom, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendValueModeProperty = BindableProperty.Create(nameof(LegendValueMode), typeof(ChartLegendValueMode), typeof(ClamPieChartView), ChartLegendValueMode.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CenterValueModeProperty = BindableProperty.Create(nameof(CenterValueMode), typeof(ChartCenterValueMode), typeof(ClamPieChartView), ChartCenterValueMode.Auto, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CenterValueProperty = BindableProperty.Create(nameof(CenterValue), typeof(double?), typeof(ClamPieChartView), validateValue: static (_, value) => (value is null) || ValidateFinite(value, nameof(CenterValue)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CenterLabelProperty = BindableProperty.Create(nameof(CenterLabel), typeof(string), typeof(ClamPieChartView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty CenterValueFormatProperty = BindableProperty.Create(nameof(CenterValueFormat), typeof(string), typeof(ClamPieChartView), propertyChanged: OnCenterValueFormatChanged);
    public static readonly BindableProperty CenterValueConverterProperty = BindableProperty.Create(nameof(CenterValueConverter), typeof(IValueConverter), typeof(ClamPieChartView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ClamPieChartView), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ClamPieChartView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ExplodeOffsetProperty = BindableProperty.Create(nameof(ExplodeOffset), typeof(double), typeof(ClamPieChartView), 6d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(ExplodeOffset)), propertyChanged: OnLayoutInputChanged);

    private readonly ChartDataSource source;
    private readonly PieInteraction interaction = new();
    private PieRenderer? renderer;
    private DataSnapshot snapshot = DataSnapshot.Empty;
    private ChartTextFormat valueFormat = ChartTextFormat.Default;
    private ChartTextFormat centerValueFormat = ChartTextFormat.Default;
    private bool negativeReported;
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

    public string? ValueKey
    {
        get => (string?)GetValue(ValueKeyProperty);
        set => SetValue(ValueKeyProperty, value);
    }

    public string? LabelKey
    {
        get => (string?)GetValue(LabelKeyProperty);
        set => SetValue(LabelKeyProperty, value);
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

    public double InnerRadius
    {
        get => (double)GetValue(InnerRadiusProperty);
        set => SetValue(InnerRadiusProperty, value);
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

    public double? SliceGap
    {
        get => (double?)GetValue(SliceGapProperty);
        set => SetValue(SliceGapProperty, value);
    }

    public ChartSortMode SortMode
    {
        get => (ChartSortMode)GetValue(SortModeProperty);
        set => SetValue(SortModeProperty, value);
    }

    public int MaxSlices
    {
        get => (int)GetValue(MaxSlicesProperty);
        set => SetValue(MaxSlicesProperty, value);
    }

    public double OthersThreshold
    {
        get => (double)GetValue(OthersThresholdProperty);
        set => SetValue(OthersThresholdProperty, value);
    }

    public string OthersLabel
    {
        get => (string)GetValue(OthersLabelProperty);
        set => SetValue(OthersLabelProperty, value);
    }

    public ChartPieLabelsMode LabelsMode
    {
        get => (ChartPieLabelsMode)GetValue(LabelsModeProperty);
        set => SetValue(LabelsModeProperty, value);
    }

    public ChartPieLabelContent LabelContent
    {
        get => (ChartPieLabelContent)GetValue(LabelContentProperty);
        set => SetValue(LabelContentProperty, value);
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

    public ChartCenterValueMode CenterValueMode
    {
        get => (ChartCenterValueMode)GetValue(CenterValueModeProperty);
        set => SetValue(CenterValueModeProperty, value);
    }

    public double? CenterValue
    {
        get => (double?)GetValue(CenterValueProperty);
        set => SetValue(CenterValueProperty, value);
    }

    public string? CenterLabel
    {
        get => (string?)GetValue(CenterLabelProperty);
        set => SetValue(CenterLabelProperty, value);
    }

    public string? CenterValueFormat
    {
        get => (string?)GetValue(CenterValueFormatProperty);
        set => SetValue(CenterValueFormatProperty, value);
    }

    public IValueConverter? CenterValueConverter
    {
        get => (IValueConverter?)GetValue(CenterValueConverterProperty);
        set => SetValue(CenterValueConverterProperty, value);
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

    public double ExplodeOffset
    {
        get => (double)GetValue(ExplodeOffsetProperty);
        set => SetValue(ExplodeOffsetProperty, value);
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

    public Func<double, string>? CenterValueFormatter
    {
        get;
        set
        {
            field = value;
            InvalidateLayout();
        }
    }

    internal PieLayout ChartLayout { get; } = new();

    internal override int RenderedPointCount => ChartLayout.Slices.Count;

    private bool IsSelectable => (SelectionMode != ChartSelectionMode.None) && (EffectiveDensity != ChartDensity.Minimal);

    public ClamPieChartView()
    {
        source = new ChartDataSource(InvalidateData);
        dataAnimator = new ChartAnimator(this, DataAnimationName, OnDataProgress);
        selectionAnimator = new ChartAnimator(this, SelectionAnimationName, OnSelectionProgress);
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
        Interpolator.Dispose();
        ReplaceSnapshot(DataSnapshot.Empty);
    }

    internal override void OnAttached() => source.Activate();

    internal override void OnDetached() => source.Deactivate();

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
            ValueSelector = ValueSelector,
            LabelSelector = LabelSelector
        });
        var data = DataSnapshotBuilder.Build(source.Source, reader, ChartEmptyPointMode.Gap);
        RecordChange(snapshot.Count, data.Count);
        ReplaceSnapshot(data);
        source.Changes.Clear();
        ResolveSelection(SelectedItem, SelectedIndex, false, false);
    }

    internal override void UpdateLayout(float width, float height)
    {
        var captured = PrepareTransition();
        var culture = Culture ?? CultureInfo.CurrentCulture;
        ChartLayout.Update(width, height, EffectiveContentPadding, ChartStyle, culture, CreateLayoutOptions(), snapshot, EnsureRenderer().Measurer);
        if (ChartLayout.HasNegativeValues && !negativeReported)
        {
            negativeReported = true;
            ChartDiagnostics.Warn("The pie chart ignores negative values.");
        }

        CompleteTransition(captured);
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        if (ChartLayout.Slices.Count == 0)
        {
            if (!String.IsNullOrEmpty(EmptyText))
            {
                painter.DrawEmptyText(canvas, EmptyText, ChartLayout.Bounds, ChartStyle);
            }

            return;
        }

        var palette = Palette ?? ChartStyle.Palette;
        ReportPaletteOverflow(palette);
        interaction.ExplodeOffset = IsSelectable ? ExplodeOffset : 0;
        Interpolator.Easing = AnimationEasing;
        painter.Render(canvas, ChartLayout, Interpolator, interaction, palette, ChartStyle);
    }

    private static bool ValidateInnerRadius(object? value, string name) =>
        (value is double ratio) && (ratio >= 0) && (ratio <= 0.95) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be from 0 to 0.95.");

    private static bool ValidateSweepAngle(object? value, string name) =>
        (value is double angle) && (angle > 0) && (angle <= 360) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be greater than 0 and at most 360.");

    private static bool ValidateFinite(object? value, string name) =>
        (value is double number) && Double.IsFinite(number) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number.");

    private static bool ValidateMaxSlices(object? value, string name) =>
        (value is int count) && (count >= 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be 1 or more.");

    private static bool ValidateThreshold(object? value, string name) =>
        (value is double ratio) && (ratio >= 0) && (ratio < 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be 0 or more and less than 1.");

    private static bool ValidateText(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamPieChartView)bindable).InvalidateData();

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamPieChartView)bindable;
        view.source.SetSource((IEnumerable?)newValue);
        view.sourceReplaced = true;
        view.InvalidateData();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamPieChartView)bindable).source.SetTrackItemChanges((bool)newValue);

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamPieChartView)bindable;
        view.valueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private static void OnCenterValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamPieChartView)bindable;
        view.centerValueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private PieRenderer EnsureRenderer() => renderer ??= new PieRenderer(ChartStyle.FontFamily);

    private PieLayoutOptions CreateLayoutOptions()
    {
        var density = EffectiveDensity;
        var selectable = IsSelectable;
        var center = (CenterValueFormatter is not null) || (CenterValueConverter is not null) || !centerValueFormat.IsDefault;
        return new PieLayoutOptions
        {
            InnerRadius = InnerRadius,
            StartAngle = StartAngle,
            SweepAngle = SweepAngle,
            Gap = SliceGap ?? ChartStyle.SegmentGap,
            ExplodeMargin = selectable ? ExplodeOffset : 0,
            SortMode = SortMode,
            MaxSlices = MaxSlices,
            OthersThreshold = OthersThreshold,
            OthersLabel = OthersLabel,
            LabelsMode = density == ChartDensity.Minimal ? ChartPieLabelsMode.None : LabelsMode,
            LabelContent = LabelContent,
            LegendVisible = LegendVisible ?? (density == ChartDensity.Full),
            LegendPosition = LegendPosition,
            LegendMaxRows = density == ChartDensity.Full ? LegendLayout.MaxRows : 1,
            LegendValueMode = LegendValueMode,
            CenterValueMode = CenterValueMode,
            CenterValue = CenterValue,
            CenterLabel = CenterLabel,
            Selectable = selectable,
            ValueFormat = valueFormat,
            ValueConverter = ValueConverter,
            ValueFormatter = ValueFormatter,
            CenterFormat = center ? centerValueFormat : valueFormat,
            CenterConverter = center ? CenterValueConverter : ValueConverter,
            CenterFormatter = center ? CenterValueFormatter : ValueFormatter
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

        var slices = ChartLayout.Slices;
        for (var i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            if (!slice.IsOthers && (slice.Color is null) && (slice.ColorRank >= palette.Count))
            {
                otherColorReported = true;
                ChartDiagnostics.Warn($"The slice at index {slice.DataIndex} has no color in the palette and is drawn with OtherColor.");
                return;
            }
        }
    }
}
