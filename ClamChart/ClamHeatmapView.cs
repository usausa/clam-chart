namespace ClamChart;

using System.Collections;
using System.Globalization;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;

public partial class ClamHeatmapView : ChartViewBase
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ClamHeatmapView), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamHeatmapView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty RowKeyProperty = BindableProperty.Create(nameof(RowKey), typeof(string), typeof(ClamHeatmapView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ColumnKeyProperty = BindableProperty.Create(nameof(ColumnKey), typeof(string), typeof(ClamHeatmapView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty DateKeyProperty = BindableProperty.Create(nameof(DateKey), typeof(string), typeof(ClamHeatmapView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty ValueKeyProperty = BindableProperty.Create(nameof(ValueKey), typeof(string), typeof(ClamHeatmapView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty TrackItemChangesProperty = BindableProperty.Create(nameof(TrackItemChanges), typeof(bool), typeof(ClamHeatmapView), true, propertyChanged: OnTrackItemChangesChanged);
    public static readonly BindableProperty LayoutModeProperty = BindableProperty.Create(nameof(LayoutMode), typeof(ChartHeatmapLayoutMode), typeof(ClamHeatmapView), ChartHeatmapLayoutMode.Matrix, propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(double?), typeof(ClamHeatmapView), validateValue: static (_, value) => ChartAxis.ValidateFinite(value, nameof(Minimum)), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(double?), typeof(ClamHeatmapView), validateValue: static (_, value) => ChartAxis.ValidateFinite(value, nameof(Maximum)), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty ColorScaleProperty = BindableProperty.Create(nameof(ColorScale), typeof(IReadOnlyList<Color>), typeof(ClamHeatmapView), validateValue: static (_, value) => ValidateColorScale(value, nameof(ColorScale)), propertyChanged: OnRenderInputChanged);
    public static readonly BindableProperty CellGapProperty = BindableProperty.Create(nameof(CellGap), typeof(double), typeof(ClamHeatmapView), 1d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(CellGap)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendVisibleProperty = BindableProperty.Create(nameof(LegendVisible), typeof(bool?), typeof(ClamHeatmapView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendPositionProperty = BindableProperty.Create(nameof(LegendPosition), typeof(ChartLegendPosition), typeof(ClamHeatmapView), ChartLegendPosition.Bottom, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ClamHeatmapView), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ClamHeatmapView), propertyChanged: OnLayoutInputChanged);

    private readonly ChartDataSource source;
    private readonly HeatmapInteraction interaction = new();
    private HeatmapRenderer? renderer;
    private DataSnapshot snapshot = DataSnapshot.Empty;
    private ChartTextFormat valueFormat = ChartTextFormat.Default;
    private bool invertedBoundsReported;
    private bool datesReported;
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

    public string? ColumnKey
    {
        get => (string?)GetValue(ColumnKeyProperty);
        set => SetValue(ColumnKeyProperty, value);
    }

    public string? DateKey
    {
        get => (string?)GetValue(DateKeyProperty);
        set => SetValue(DateKeyProperty, value);
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

    public ChartHeatmapLayoutMode LayoutMode
    {
        get => (ChartHeatmapLayoutMode)GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
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

    public IReadOnlyList<Color>? ColorScale
    {
        get => (IReadOnlyList<Color>?)GetValue(ColorScaleProperty);
        set => SetValue(ColorScaleProperty, value);
    }

    public double CellGap
    {
        get => (double)GetValue(CellGapProperty);
        set => SetValue(CellGapProperty, value);
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

    public Func<object, string?>? RowSelector
    {
        get;
        set
        {
            field = value;
            InvalidateData();
        }
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

    public Func<double, string>? ValueFormatter
    {
        get;
        set
        {
            field = value;
            InvalidateLayout();
        }
    }

    internal HeatmapLayout ChartLayout { get; } = new();

    internal override int RenderedPointCount
    {
        get
        {
            var layout = ChartLayout;
            var count = 0;
            for (var cell = 0; cell < layout.CellCount; cell++)
            {
                if (layout.IsPresent(cell) && (layout.GetDataIndex(cell) >= 0))
                {
                    count++;
                }
            }

            return count;
        }
    }

    private bool IsSelectable => (SelectionMode != ChartSelectionMode.None) && (EffectiveDensity != ChartDensity.Minimal);

    public ClamHeatmapView()
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
        ReplaceSnapshot(DataSnapshot.Empty);
    }

    internal override void OnAttached() => source.Activate();

    internal override void OnDetached() => source.Deactivate();

    internal override SkeletonShape SkeletonShape => SkeletonShape.Grid;

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData()
    {
        var calendar = LayoutMode == ChartHeatmapLayoutMode.Calendar;
        var reader = new ChartValueReader(new ChartValueOptions
        {
            Accessors = ValueAccessors,
            XKey = calendar ? DateKey : ColumnKey,
            YKey = ValueKey,
            LabelKey = calendar ? null : RowKey,
            ValueSelector = ValueSelector,
            LabelSelector = calendar ? null : RowSelector
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
        ChartLayout.Update(width, height, EffectiveContentPadding, ChartStyle, culture, CreateLayoutOptions(culture), snapshot, EnsureRenderer().Measurer);
        ReportProblems();
        CompleteTransition(captured);
        RefreshTracking();
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        if (RenderedPointCount == 0)
        {
            if (!String.IsNullOrEmpty(EmptyText))
            {
                painter.DrawEmptyText(canvas, EmptyText, ChartLayout.Bounds, ChartStyle);
            }

            return;
        }

        interaction.TooltipVisible = TooltipVisible;
        Interpolator.Easing = AnimationEasing;
        painter.Render(canvas, ChartLayout, Interpolator, ColorScale ?? ChartStyle.HeatmapColorScale, interaction, ChartStyle, width, height);
    }

    internal override string? CreateAutomationSummary(ChartSummaryText text)
    {
        var layout = ChartLayout;
        var count = 0;
        var minimum = -1;
        var maximum = -1;
        for (var cell = 0; cell < layout.CellCount; cell++)
        {
            var value = layout.GetValue(cell);
            if (!layout.IsPresent(cell) || !Double.IsFinite(value))
            {
                continue;
            }

            count++;
            if ((minimum < 0) || (value < layout.GetValue(minimum)))
            {
                minimum = cell;
            }

            if ((maximum < 0) || (value > layout.GetValue(maximum)))
            {
                maximum = cell;
            }
        }

        if (count == 0)
        {
            return null;
        }

        text.AddCount(count, true);
        text.AddValue(ChartSummaryWord.Maximum, layout.FormatValue(layout.GetValue(maximum)), layout.GetCellText(maximum));
        text.AddValue(ChartSummaryWord.Minimum, layout.FormatValue(layout.GetValue(minimum)), layout.GetCellText(minimum));
        return text.Build();
    }

    private static bool ValidateColorScale(object? value, string name) =>
        (value is not IReadOnlyList<Color> colors) || (colors.Count >= 2) ? true : throw new ArgumentOutOfRangeException(name, value, "The list must have two or more colors.");

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamHeatmapView)bindable).InvalidateData();

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamHeatmapView)bindable;
        view.source.SetSource((IEnumerable?)newValue);
        view.sourceReplaced = true;
        view.InvalidateData();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamHeatmapView)bindable).source.SetTrackItemChanges((bool)newValue);

    private static void OnBoundsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamHeatmapView)bindable;
        view.invertedBoundsReported = false;
        view.InvalidateLayout();
    }

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamHeatmapView)bindable;
        view.valueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private HeatmapRenderer EnsureRenderer() => renderer ??= new HeatmapRenderer(ChartStyle.FontFamily);

    private HeatmapLayoutOptions CreateLayoutOptions(CultureInfo culture)
    {
        var density = EffectiveDensity;
        return new HeatmapLayoutOptions
        {
            LayoutMode = LayoutMode,
            HasRows = (RowKey is not null) || (RowSelector is not null),
            Minimum = Minimum,
            Maximum = Maximum,
            CellGap = CellGap,
            LabelsVisible = density != ChartDensity.Minimal,
            LegendVisible = LegendVisible ?? (density == ChartDensity.Full),
            LegendPosition = LegendPosition,
            FirstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek,
            ValueFormat = valueFormat,
            ValueConverter = ValueConverter,
            ValueFormatter = ValueFormatter
        };
    }

    private void ReportProblems()
    {
        if (!invertedBoundsReported && (Minimum is { } minimum) && (Maximum is { } maximum) && (maximum <= minimum))
        {
            invertedBoundsReported = true;
            ChartDiagnostics.Warn("The heatmap Maximum is not greater than Minimum, so the automatic range is used.");
        }

        if (!datesReported && (LayoutMode == ChartHeatmapLayoutMode.Calendar) && (snapshot.Count > 0) && !ChartLayout.HasDates)
        {
            datesReported = true;
            ChartDiagnostics.Warn("The calendar heatmap needs dates; set DateKey to a date or use ChartTimePoint items.");
        }
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
}
