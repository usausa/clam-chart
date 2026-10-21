namespace ClamChart;

using System.Collections.ObjectModel;
using System.Collections.Specialized;

using ClamChart.Data;
using ClamChart.Layout;

public partial class ClamChartView
{
    public static readonly BindableProperty ThresholdsProperty = BindableProperty.Create(nameof(Thresholds), typeof(IList<ChartThreshold>), typeof(ClamChartView), defaultValueCreator: static _ => new ObservableCollection<ChartThreshold>(), validateValue: static (_, value) => ValidateList(value, nameof(Thresholds)), propertyChanged: OnThresholdsChanged);
    public static readonly BindableProperty ThresholdsModeProperty = BindableProperty.Create(nameof(ThresholdsMode), typeof(ChartThresholdsMode), typeof(ClamChartView), ChartThresholdsMode.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty AnnotationsProperty = BindableProperty.Create(nameof(Annotations), typeof(IList<ChartAnnotation>), typeof(ClamChartView), defaultValueCreator: static _ => new ObservableCollection<ChartAnnotation>(), validateValue: static (_, value) => ValidateList(value, nameof(Annotations)), propertyChanged: OnAnnotationsChanged);

    private readonly List<ChartAnnotation> attachedAnnotations = [];
    private readonly List<ChartAnnotation> orderedAnnotations = [];
    private readonly List<CartesianAnnotation> layoutAnnotations = [];
    private readonly ChartCollectionWatcher thresholdsWatcher;
    private INotifyCollectionChanged? observedAnnotations;

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

#pragma warning disable CA2227
    public IList<ChartAnnotation> Annotations
    {
        get => (IList<ChartAnnotation>)GetValue(AnnotationsProperty);
        set => SetValue(AnnotationsProperty, value);
    }
#pragma warning restore CA2227

    private static bool ValidateList(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static void OnThresholdsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        view.thresholdsWatcher.Watch(newValue);
        view.InvalidateLayout();
    }

    private static void OnAnnotationsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamChartView)bindable).ObserveAnnotations((IList<ChartAnnotation>)newValue);

    private void OnThresholdsCollectionChanged(NotifyCollectionChangedEventArgs e) => InvalidateLayout();

    private void ObserveAnnotations(IEnumerable<ChartAnnotation> list)
    {
        if (observedAnnotations is not null)
        {
            observedAnnotations.CollectionChanged -= OnAnnotationsCollectionChanged;
        }

        observedAnnotations = list as INotifyCollectionChanged;
        if (observedAnnotations is not null)
        {
            observedAnnotations.CollectionChanged += OnAnnotationsCollectionChanged;
        }

        SyncAnnotations();
    }

    private void OnAnnotationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncAnnotations();

    private void SyncAnnotations()
    {
        orderedAnnotations.Clear();
        var list = Annotations;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < list.Count; i++)
        {
            var annotation = list[i];
            if (!orderedAnnotations.Contains(annotation))
            {
                orderedAnnotations.Add(annotation);
            }
        }

        foreach (var annotation in attachedAnnotations)
        {
            if (!orderedAnnotations.Contains(annotation))
            {
                annotation.Owner = null;
                RemoveLogicalChild(annotation);
            }
        }

        foreach (var annotation in orderedAnnotations)
        {
            if (!attachedAnnotations.Contains(annotation))
            {
                if ((annotation.Owner is not null) && (annotation.Owner != this))
                {
                    throw new InvalidOperationException("The annotation is already used by another chart.");
                }

                annotation.Owner = this;
                AddLogicalChild(annotation);
            }
        }

        attachedAnnotations.Clear();
        attachedAnnotations.AddRange(orderedAnnotations);
        InvalidateLayout();
    }

    private void PrepareAnnotations()
    {
        layoutAnnotations.Clear();
        for (var i = 0; i < attachedAnnotations.Count; i++)
        {
            switch (attachedAnnotations[i])
            {
                case ChartReferenceLine line:
                {
                    var ticks = line.Time is { } time ? ChartDateTime.ToLocalTicks(time) : Double.NaN;
                    layoutAnnotations.Add(new CartesianAnnotation(i, false, line.Axis, line.YAxis, line.Value, line.Value, ticks, ticks));
                    break;
                }

                case ChartReferenceBand band:
                {
                    var start = band.StartTime is { } startTime ? ChartDateTime.ToLocalTicks(startTime) : Double.NaN;
                    var end = band.EndTime is { } endTime ? ChartDateTime.ToLocalTicks(endTime) : Double.NaN;
                    layoutAnnotations.Add(new CartesianAnnotation(i, true, band.Axis, band.YAxis, band.Start, band.End, start, end));
                    break;
                }
            }
        }
    }
}
