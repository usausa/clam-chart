namespace ClamChart;

using System.Globalization;

using ClamChart.Animation;
using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

[ContentProperty(nameof(Series))]
public partial class ClamChartView : ChartViewBase
{
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(ChartOrientation), typeof(ClamChartView), ChartOrientation.Vertical, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IChartAccessorProvider), typeof(ClamChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty XKeyProperty = BindableProperty.Create(nameof(XKey), typeof(string), typeof(ClamChartView), propertyChanged: OnDataPropertyChanged);
    public static readonly BindableProperty StackModeProperty = BindableProperty.Create(nameof(StackMode), typeof(ChartStackMode), typeof(ClamChartView), ChartStackMode.Normal, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendVisibleProperty = BindableProperty.Create(nameof(LegendVisible), typeof(bool?), typeof(ClamChartView), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendPositionProperty = BindableProperty.Create(nameof(LegendPosition), typeof(ChartLegendPosition), typeof(ClamChartView), ChartLegendPosition.Bottom, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty DataLabelsModeProperty = BindableProperty.Create(nameof(DataLabelsMode), typeof(ChartDataLabelsMode), typeof(ClamChartView), ChartDataLabelsMode.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty DownsamplingProperty = BindableProperty.Create(nameof(Downsampling), typeof(ChartDownsampling), typeof(ClamChartView), ChartDownsampling.Auto, propertyChanged: OnRenderInputChanged);

    private readonly List<CartesianSeries> layoutSeries = [];
    private readonly List<SKColor> colors = [];
    private readonly CartesianInteraction interaction = new();
    private CartesianRenderer? renderer;
    private bool released;

    public ChartOrientation Orientation
    {
        get => (ChartOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
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

    public ChartStackMode StackMode
    {
        get => (ChartStackMode)GetValue(StackModeProperty);
        set => SetValue(StackModeProperty, value);
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

    public ChartDataLabelsMode DataLabelsMode
    {
        get => (ChartDataLabelsMode)GetValue(DataLabelsModeProperty);
        set => SetValue(DataLabelsModeProperty, value);
    }

    public ChartDownsampling Downsampling
    {
        get => (ChartDownsampling)GetValue(DownsamplingProperty);
        set => SetValue(DownsamplingProperty, value);
    }

    internal CartesianLayout ChartLayout { get; } = new();

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

    public ClamChartView()
    {
        dataAnimator = new ChartAnimator(this, DataAnimationName, OnDataProgress);
        selectionAnimator = new ChartAnimator(this, SelectionAnimationName, OnSelectionProgress);
        thresholdsWatcher = new ChartCollectionWatcher(OnThresholdsCollectionChanged, true);
        thresholdsWatcher.Watch(Thresholds);
        ObserveSeries(Series);
        ObserveAnnotations(Annotations);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || released)
        {
            return;
        }

        released = true;
        LeaveTrackingGroup(TrackingGroup);
        thresholdsWatcher.Deactivate();
        ChartLayout.Dispose();
        Interpolator.Dispose();
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

        thresholdsWatcher.Activate();
    }

    internal override void OnDetached()
    {
        foreach (var series in attached)
        {
            series.Source.Deactivate();
        }

        thresholdsWatcher.Deactivate();
    }

    internal override SkeletonShape SkeletonShape => HasBarSeries() ? (Orientation == ChartOrientation.Horizontal ? SkeletonShape.Rows : SkeletonShape.Bars) : SkeletonShape.Line;

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData()
    {
        for (var s = 0; s < attached.Count; s++)
        {
            var series = attached[s];
            var reader = new ChartValueReader(series.CreateValueOptions(ValueAccessors, XKey));
            var snapshot = DataSnapshotBuilder.Build(series.ItemsSource, reader, series.EmptyPointMode);
            RecordChange(s, series, series.Snapshot.Count, snapshot.Count);
            series.ReplaceSnapshot(snapshot);
            series.Source.Changes.Clear();
        }

        ValidateXValues();
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

        PrepareAnnotations();
        var culture = Culture ?? CultureInfo.CurrentCulture;
        var options = CreateLayoutOptions(EffectiveDensity, culture);
        ChartLayout.Update(width, height, EffectiveContentPadding, EffectiveStyle, culture, options, layoutSeries, EnsureRenderer().Measurer);
        ReportInvertedBounds(options);
        CompleteTransition(captured);
        RefreshTracking();
        UpdateCategoryExtent(width, height);
    }

    internal override void Render(SKCanvas canvas, float width, float height)
    {
        var painter = EnsureRenderer();
        if (!HasVisibleData())
        {
            if (!String.IsNullOrEmpty(EmptyText))
            {
                painter.DrawEmptyText(canvas, EmptyText, ChartLayout.Bounds, EffectiveStyle);
            }

            return;
        }

        UpdateColors();
        interaction.TooltipVisible = TooltipVisible;
        Interpolator.Easing = AnimationEasing;
        painter.Render(canvas, ChartLayout, Interpolator, attached, colors, EffectiveStyle, interaction, new CartesianRenderState(ThresholdsMode, attachedAnnotations, Downsampling, EffectPhase), width, height);
    }

    internal override string? CreateAutomationSummary(ChartSummaryText text)
    {
        var layout = ChartLayout;
        var named = CountVisibleSeries() > 1;
        for (var s = 0; (s < layoutSeries.Count) && (s < layout.Geometries.Count); s++)
        {
            var item = layoutSeries[s];
            if (!item.IsVisible)
            {
                continue;
            }

            var x = layout.Geometries[s].X;
            var y = item.Data.Y;
            var lows = item.Kind == ChartSeriesKind.RangeBar ? layout.Geometries[s].Start : y;
            var count = 0;
            var minimum = -1;
            var maximum = -1;
            for (var i = 0; i < y.Length; i++)
            {
                if (!Double.IsFinite(x[i]) || !Double.IsFinite(y[i]) || !Double.IsFinite(lows[i]))
                {
                    continue;
                }

                count++;
                if ((minimum < 0) || (lows[i] < lows[minimum]))
                {
                    minimum = i;
                }

                if ((maximum < 0) || (y[i] > y[maximum]))
                {
                    maximum = i;
                }
            }

            if (count == 0)
            {
                continue;
            }

            var axis = layout.GetValueAxis(item.Axis).Options;
            text.BeginGroup(!String.IsNullOrEmpty(item.Title) ? item.Title : named ? text.SeriesName(s + 1) : null);
            text.AddCount(count, false);
            text.AddValue(ChartSummaryWord.Maximum, CartesianLayout.FormatValue(y[maximum], item, axis, false, text.Culture), layout.GetTrackingLabel(x[maximum], layoutSeries));
            text.AddValue(ChartSummaryWord.Minimum, CartesianLayout.FormatValue(lows[minimum], item, axis, false, text.Culture), layout.GetTrackingLabel(x[minimum], layoutSeries));
        }

        return text.Build();
    }

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamChartView)bindable).InvalidateData();

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

    private CartesianRenderer EnsureRenderer() => renderer ??= new CartesianRenderer(EffectiveStyle.FontFamily);

    private bool HasBarSeries()
    {
        foreach (var series in attached)
        {
            if (series.Kind is ChartSeriesKind.Bar or ChartSeriesKind.RangeBar)
            {
                return true;
            }
        }

        return false;
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
        var palette = EffectiveStyle.Palette;
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
                colors.Add(EffectiveStyle.OtherColor.ToSKColor());
                if (!series.OtherColorReported)
                {
                    series.OtherColorReported = true;
                    ChartDiagnostics.Warn($"The series at index {i} has no color in the palette and is drawn with OtherColor.");
                }
            }
        }
    }
}
