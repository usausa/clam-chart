namespace ClamChart;

using System.Collections;
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

    private const int InitialCapacity = 16;

    private readonly ChartDataSource source;
    private readonly SeriesGeometry target = new();
    private SparklineRenderer? renderer;
    private ChartValueReader? reader;
    private IReadOnlyList<double>? direct;
    private double[] buffer = [];
    private bool live;
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

    public Func<object, double?>? ValueSelector
    {
        get;
        set
        {
            field = value;
            reader = null;
            InvalidateData();
        }
    }

    internal SparklineLayout ChartLayout { get; } = new();

    internal override int RenderedPointCount => ChartLayout.Count;

    private SparklineValues CurrentValues => new(direct, live ? target : null);

    public ClamSparklineView()
    {
        source = new ChartDataSource(InvalidateData);
        dataAnimator = new ChartAnimator(this, DataAnimationName, OnDataProgress);
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
        target.Dispose();
        DataSnapshot.Return(buffer);
        buffer = [];
        direct = null;
    }

    internal override ChartDensity ResolveDensity() => ChartDensity.Minimal;

    internal override void OnAttached() => source.Activate();

    internal override void OnDetached() => source.Deactivate();

    internal override void ResetRenderer()
    {
        renderer?.Dispose();
        renderer = null;
    }

    internal override void UpdateData()
    {
        var wasLive = live;
        var oldCount = target.Count;
        var values = Values;
        if ((values is IReadOnlyList<double> list) && (values is not INotifyCollectionChanged))
        {
            direct = list;
            live = false;
        }
        else
        {
            direct = null;
            live = true;
            if (values is not null)
            {
                ReadValues(values);
            }
            else
            {
                ReadItems(ItemsSource);
            }
        }

        RecordChange(wasLive && live, oldCount, live ? target.Count : 0);
        source.Changes.Clear();
    }

    internal override void UpdateLayout(float width, float height)
    {
        var captured = PrepareTransition();
        var options = new SparklineLayoutOptions
        {
            Kind = Kind,
            Thickness = Thickness,
            Markers = Markers,
            Minimum = Minimum,
            Maximum = Maximum,
            ReferenceValue = ReferenceValue
        };
        ChartLayout.Update(width, height, EffectiveContentPadding, ChartStyle, options, CurrentValues);
        if (ChartLayout.HasInvertedBounds && !invertedBoundsReported)
        {
            invertedBoundsReported = true;
            ChartDiagnostics.Warn("The sparkline Maximum is not greater than Minimum, so the automatic range is used.");
        }

        CompleteTransition(captured);
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
        painter.Render(canvas, ChartLayout, Interpolator, CurrentValues, color.ToSKColor(), (NegativeColor ?? ChartStyle.CriticalColor).ToSKColor(), ChartStyle, Smooth);
    }

    private static bool ValidateFinite(object? value, string name) =>
        (value is not double number) || Double.IsFinite(number) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number.");

    private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamSparklineView)bindable).ReplaceSource();

    private static void OnReaderChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamSparklineView)bindable;
        view.reader = null;
        view.InvalidateData();
    }

    private static void OnTrackItemChangesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamSparklineView)bindable;
        if (view.Values is null)
        {
            view.source.SetTrackItemChanges((bool)newValue);
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
            source.SetTrackItemChanges(false);
            source.SetSource(values);
        }
        else
        {
            source.SetSource(ItemsSource);
            source.SetTrackItemChanges(TrackItemChanges);
        }

        sourceReplaced = true;
        InvalidateData();
    }

    private void ReadValues(IEnumerable<double> values)
    {
        if (values is IReadOnlyList<double> list)
        {
            var count = list.Count;
            target.Reset(count);
            for (var i = 0; i < count; i++)
            {
                SetPoint(i, list[i]);
            }

            return;
        }

        var collected = 0;
        foreach (var value in values)
        {
            Store(ref collected, value);
        }

        CopyBuffer(collected);
    }

    private void ReadItems(IEnumerable? items)
    {
        if (items is null)
        {
            target.Reset(0);
            return;
        }

        reader ??= new ChartValueReader(new ChartValueOptions { Accessors = ValueAccessors, YKey = ValueKey, ValueSelector = ValueSelector });
        if (items is IList list)
        {
            var count = list.Count;
            target.Reset(count);
            for (var i = 0; i < count; i++)
            {
                SetPoint(i, reader.ReadY(list[i]));
            }

            return;
        }

        var collected = 0;
        foreach (var item in items)
        {
            Store(ref collected, reader.ReadY(item));
        }

        CopyBuffer(collected);
    }

    private void Store(ref int count, double value)
    {
        if (count == buffer.Length)
        {
            var larger = DataSnapshot.Rent<double>(Math.Max(InitialCapacity, buffer.Length * 2));
            Array.Copy(buffer, larger, count);
            DataSnapshot.Return(buffer);
            buffer = larger;
        }

        buffer[count++] = value;
    }

    private void CopyBuffer(int count)
    {
        target.Reset(count);
        for (var i = 0; i < count; i++)
        {
            SetPoint(i, buffer[i]);
        }
    }

    private void SetPoint(int index, double value)
    {
        target.X[index] = index;
        target.Start[index] = 0;
        target.End[index] = value;
        target.Value[index] = value;
        target.Outer[index] = false;
    }
}
