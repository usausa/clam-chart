namespace ClamChart;

using System.Collections.ObjectModel;
using System.Collections.Specialized;

using ClamChart.Data;

public partial class ClamChartView
{
    public static readonly BindableProperty SeriesProperty = BindableProperty.Create(nameof(Series), typeof(IList<ChartSeries>), typeof(ClamChartView), defaultValueCreator: static _ => new ObservableCollection<ChartSeries>(), validateValue: static (_, value) => ValidateSeries(value, nameof(Series)), propertyChanged: OnSeriesChanged);

    private readonly List<ChartSeries> attached = [];
    private readonly List<ChartSeries> ordered = [];
    private INotifyCollectionChanged? observedSeries;

#pragma warning disable CA2227
    public IList<ChartSeries> Series
    {
        get => (IList<ChartSeries>)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }
#pragma warning restore CA2227

    private static bool ValidateSeries(object? value, string name) => value is not null ? true : throw new ArgumentNullException(name);

    private static void OnSeriesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ClamChartView)bindable).ObserveSeries((IList<ChartSeries>)newValue);

    private void ObserveSeries(IEnumerable<ChartSeries> list)
    {
        if (observedSeries is not null)
        {
            observedSeries.CollectionChanged -= OnSeriesCollectionChanged;
        }

        observedSeries = list as INotifyCollectionChanged;
        if (observedSeries is not null)
        {
            observedSeries.CollectionChanged += OnSeriesCollectionChanged;
        }

        SyncSeries();
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncSeries();

    private void SyncSeries()
    {
        InvalidateData();
        seriesChanged = true;
        ordered.Clear();
        var list = Series;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < list.Count; i++)
        {
            var series = list[i];
            if (!ordered.Contains(series))
            {
                ordered.Add(series);
            }
        }

        foreach (var series in attached)
        {
            if (!ordered.Contains(series))
            {
                Detach(series);
            }
        }

        foreach (var series in ordered)
        {
            if (!attached.Contains(series))
            {
                Attach(series);
            }
        }

        attached.Clear();
        attached.AddRange(ordered);
    }

    private void Attach(ChartSeries series)
    {
        if (series is RadarSeries)
        {
            throw new InvalidOperationException("The radar series can only be used by ClamRadarChartView.");
        }

        if ((series.Owner is not null) && (series.Owner != this))
        {
            throw new InvalidOperationException("The series is already used by another chart.");
        }

        series.Owner = this;
        AddLogicalChild(series);
        if (Handler is not null)
        {
            series.Source.Activate();
        }
    }

    private void Detach(ChartSeries series)
    {
        series.Source.Deactivate();
        series.Owner = null;
        series.OtherColorReported = false;
        RemoveLogicalChild(series);
        series.ReplaceSnapshot(DataSnapshot.Empty);
    }
}
