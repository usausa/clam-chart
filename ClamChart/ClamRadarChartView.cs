namespace ClamChart;

using System.Globalization;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

[ContentProperty(nameof(Series))]
public partial class ClamRadarChartView : ChartViewBase
{
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamRadarChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty XKeyProperty = BindableProperty.Create(nameof(XKey), typeof(string), typeof(ClamRadarChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(double?), typeof(ClamRadarChartView), validateValue: static (_, value) => ChartAxis.ValidateFinite(value, nameof(Minimum)), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(double?), typeof(ClamRadarChartView), validateValue: static (_, value) => ChartAxis.ValidateFinite(value, nameof(Maximum)), propertyChanged: OnBoundsChanged);
    public static readonly BindableProperty GridShapeProperty = BindableProperty.Create(nameof(GridShape), typeof(ChartRadarGridShape), typeof(ClamRadarChartView), ChartRadarGridShape.Polygon, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LevelCountProperty = BindableProperty.Create(nameof(LevelCount), typeof(int), typeof(ClamRadarChartView), 5, validateValue: static (_, value) => ValidateLevelCount(value, nameof(LevelCount)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendVisibleProperty = BindableProperty.Create(nameof(LegendVisible), typeof(bool?), typeof(ClamRadarChartView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendPositionProperty = BindableProperty.Create(nameof(LegendPosition), typeof(ChartLegendPosition), typeof(ClamRadarChartView), ChartLegendPosition.Bottom, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ClamRadarChartView), propertyChanged: OnValueFormatChanged);
    public static readonly BindableProperty ValueConverterProperty = BindableProperty.Create(nameof(ValueConverter), typeof(IValueConverter), typeof(ClamRadarChartView), propertyChanged: OnLayoutInputChanged);

    private readonly List<CartesianSeries> layoutSeries = [];
    private readonly List<SKColor> colors = [];
    private readonly RadarInteraction interaction = new();
    private RadarRenderer? renderer;
    private ChartTextFormat valueFormat = ChartTextFormat.Default;
    private bool invertedBoundsReported;
    private bool released;

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

    public ChartRadarGridShape GridShape
    {
        get => (ChartRadarGridShape)GetValue(GridShapeProperty);
        set => SetValue(GridShapeProperty, value);
    }

    public int LevelCount
    {
        get => (int)GetValue(LevelCountProperty);
        set => SetValue(LevelCountProperty, value);
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

    public Func<double, string>? ValueFormatter
    {
        get;
        set
        {
            field = value;
            InvalidateLayout();
        }
    }

    internal RadarLayout ChartLayout { get; } = new();

    internal override int RenderedPointCount
    {
        get
        {
            var count = 0;
            foreach (var series in attached)
            {
                if (series.IsVisible)
                {
                    count += series.Snapshot.Count;
                }
            }

            return count;
        }
    }

    public ClamRadarChartView()
    {
        dataAnimator = new ChartAnimator(this, DataAnimationName, OnDataProgress);
        selectionAnimator = new ChartAnimator(this, SelectionAnimationName, OnSelectionProgress);
        ObserveSeries(Series);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || released)
        {
            return;
        }

        released = true;
        foreach (var series in attached)
        {
            series.ReplaceSnapshot(DataSnapshot.Empty);
        }
    }

    internal override void OnAttached()
    {
        foreach (var series in attached)
        {
            series.Source.Activate();
        }
    }

    internal override void OnDetached()
    {
        foreach (var series in attached)
        {
            series.Source.Deactivate();
        }
    }

    internal override SkeletonShape SkeletonShape => SkeletonShape.Ring;

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData()
    {
        foreach (var series in attached)
        {
            var reader = new ChartValueReader(series.CreateValueOptions(ValueAccessors, XKey));
            var snapshot = DataSnapshotBuilder.Build(series.ItemsSource, reader, series.EmptyPointMode);
            RecordChange(series, series.Snapshot.Count, snapshot.Count);
            series.ReplaceSnapshot(snapshot);
            series.Source.Changes.Clear();
        }

        ResolveSelection(SelectedItem, SelectedIndex, false, false);
    }

    internal override void UpdateLayout(float width, float height)
    {
        var captured = PrepareTransition();
        layoutSeries.Clear();
        foreach (var series in attached)
        {
            layoutSeries.Add(series.CreateLayoutSeries());
        }

        var culture = Culture ?? CultureInfo.CurrentCulture;
        ChartLayout.Update(width, height, EffectiveContentPadding, ChartStyle, culture, CreateLayoutOptions(), layoutSeries, EnsureRenderer().Measurer);
        ReportInvertedBounds();
        CompleteTransition(captured);
        RefreshTracking();
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        if (!HasVisibleData())
        {
            if (!String.IsNullOrEmpty(EmptyText))
            {
                painter.DrawEmptyText(canvas, EmptyText, ChartLayout.Bounds, ChartStyle);
            }

            return;
        }

        UpdateColors();
        interaction.TooltipVisible = TooltipVisible;
        Interpolator.Easing = AnimationEasing;
        painter.Render(canvas, ChartLayout, Interpolator, attached, colors, interaction, ChartStyle, width, height);
    }

    internal override string? CreateAutomationSummary(ChartSummaryText text)
    {
        var layout = ChartLayout;
        var named = CountVisibleSeries() > 1;
        for (var s = 0; (s < layoutSeries.Count) && (s < layout.SeriesCount); s++)
        {
            var item = layoutSeries[s];
            if (!item.IsVisible)
            {
                continue;
            }

            var count = 0;
            var minimum = -1;
            var maximum = -1;
            for (var k = 0; k < layout.AxisCount; k++)
            {
                var value = layout.GetValue(s, k);
                if (!Double.IsFinite(value))
                {
                    continue;
                }

                count++;
                if ((minimum < 0) || (value < layout.GetValue(s, minimum)))
                {
                    minimum = k;
                }

                if ((maximum < 0) || (value > layout.GetValue(s, maximum)))
                {
                    maximum = k;
                }
            }

            if (count == 0)
            {
                continue;
            }

            text.BeginGroup(!String.IsNullOrEmpty(item.Title) ? item.Title : named ? text.SeriesName(s + 1) : null);
            text.AddCount(count, true);
            text.AddValue(ChartSummaryWord.Maximum, RadarLayout.FormatValue(layout.GetValue(s, maximum), item, layout.ValueFormatting), layout.GetAxisText(maximum));
            text.AddValue(ChartSummaryWord.Minimum, RadarLayout.FormatValue(layout.GetValue(s, minimum), item, layout.ValueFormatting), layout.GetAxisText(minimum));
        }

        return text.Build();
    }

    private static bool ValidateLevelCount(object? value, string name) =>
        (value is int count) && (count >= 1) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be 1 or more.");

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamRadarChartView)bindable).InvalidateData();

    private static void OnBoundsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamRadarChartView)bindable;
        view.invertedBoundsReported = false;
        view.InvalidateLayout();
    }

    private static void OnValueFormatChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamRadarChartView)bindable;
        view.valueFormat = ChartTextFormat.Parse((string?)newValue);
        view.InvalidateLayout();
    }

    private RadarRenderer EnsureRenderer() => renderer ??= new RadarRenderer(ChartStyle.FontFamily);

    private RadarLayoutOptions CreateLayoutOptions()
    {
        var density = EffectiveDensity;
        var full = density == ChartDensity.Full;
        var minimal = density == ChartDensity.Minimal;
        return new RadarLayoutOptions
        {
            Minimum = Minimum,
            Maximum = Maximum,
            GridShape = GridShape,
            LevelCount = LevelCount,
            LabelsVisible = !minimal,
            RingMargin = minimal ? ChartStyle.LineThickness / 2 : (ChartStyle.MarkerSize / 2) + ChartStyle.MarkerKnockout,
            LegendVisible = LegendVisible ?? ((attached.Count >= 2) && full),
            LegendPosition = LegendPosition,
            LegendMaxRows = full ? LegendLayout.MaxRows : 1,
            ValueFormat = valueFormat,
            ValueConverter = ValueConverter,
            ValueFormatter = ValueFormatter
        };
    }

    private void ReportInvertedBounds()
    {
        if (invertedBoundsReported || (Minimum is not { } minimum) || (Maximum is not { } maximum) || (maximum > minimum))
        {
            return;
        }

        invertedBoundsReported = true;
        ChartDiagnostics.Warn("The radar chart Maximum is not greater than Minimum, so the automatic range is used.");
    }

    private int CountVisibleSeries()
    {
        var count = 0;
        foreach (var series in layoutSeries)
        {
            if (series.IsVisible)
            {
                count++;
            }
        }

        return count;
    }

    private bool HasVisibleData()
    {
        foreach (var series in attached)
        {
            if (series.IsVisible && (series.Snapshot.Count > 0))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateColors()
    {
        colors.Clear();
        var palette = Palette ?? ChartStyle.Palette;
        for (var i = 0; i < attached.Count; i++)
        {
            var series = attached[i];
            if (series.Color is { } color)
            {
                colors.Add(color.ToSKColor());
            }
            else if (i < palette.Count)
            {
                colors.Add(palette[i].ToSKColor());
            }
            else
            {
                colors.Add(ChartStyle.OtherColor.ToSKColor());
                if (!series.OtherColorReported)
                {
                    series.OtherColorReported = true;
                    ChartDiagnostics.Warn($"The series at index {i} has no color in the palette and is drawn with OtherColor.");
                }
            }
        }
    }
}
