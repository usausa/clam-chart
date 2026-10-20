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
    }

    internal override void OnDetached()
    {
        foreach (var series in attached)
        {
            series.Source.Deactivate();
        }
    }

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
            var reader = new ChartValueReader(new ChartValueOptions
            {
                Accessors = series.ValueAccessors ?? ValueAccessors,
                XKey = series.XKey ?? XKey,
                YKey = series.YKey,
                ValueSelector = series.ValueSelector
            });
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

        var culture = Culture ?? CultureInfo.CurrentCulture;
        var options = CreateLayoutOptions(EffectiveDensity, culture);
        ChartLayout.Update(width, height, EffectiveContentPadding, ChartStyle, culture, options, layoutSeries, EnsureRenderer().Measurer);
        ReportInvertedBounds(options);
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
        painter.Render(canvas, ChartLayout, Interpolator, attached, colors, ChartStyle, interaction, width, height);
    }

    private static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamChartView)bindable).InvalidateData();

    private CartesianRenderer EnsureRenderer() => renderer ??= new CartesianRenderer(ChartStyle.FontFamily);

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
