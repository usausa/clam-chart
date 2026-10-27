namespace ClamChart;

using System.Windows.Input;

using ClamChart.Layout;
using ClamChart.Platform;

public partial class ClamRadarChartView
{
    public static readonly BindableProperty TrackballModeProperty = BindableProperty.Create(nameof(TrackballMode), typeof(ChartTrackballMode), typeof(ClamRadarChartView), ChartTrackballMode.None, propertyChanged: OnInputModeChanged);
    public static readonly BindableProperty TooltipVisibleProperty = BindableProperty.Create(nameof(TooltipVisible), typeof(bool), typeof(ClamRadarChartView), true, propertyChanged: OnTooltipVisibleChanged);
    public static readonly BindableProperty TrackedItemProperty = BindableProperty.Create(nameof(TrackedItem), typeof(object), typeof(ClamRadarChartView), defaultBindingMode: BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackedIndexProperty = BindableProperty.Create(nameof(TrackedIndex), typeof(int), typeof(ClamRadarChartView), -1, BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackingChangedCommandProperty = BindableProperty.Create(nameof(TrackingChangedCommand), typeof(ICommand), typeof(ClamRadarChartView));
    public static readonly BindableProperty TooltipFormatterProperty = BindableProperty.Create(nameof(TooltipFormatter), typeof(Func<ChartTrackingInfo, string>), typeof(ClamRadarChartView), propertyChanged: OnTooltipFormatterChanged);

    private readonly Dictionary<long, string> trackingTexts = [];

    public event EventHandler<ChartTrackingEventArgs>? TrackingStarted;

    public event EventHandler<ChartTrackingEventArgs>? TrackingChanged;

    public event EventHandler<ChartTrackingEventArgs>? TrackingEnded;

    public ChartTrackballMode TrackballMode
    {
        get => (ChartTrackballMode)GetValue(TrackballModeProperty);
        set => SetValue(TrackballModeProperty, value);
    }

    public bool TooltipVisible
    {
        get => (bool)GetValue(TooltipVisibleProperty);
        set => SetValue(TooltipVisibleProperty, value);
    }

    public object? TrackedItem
    {
        get => GetValue(TrackedItemProperty);
        set => SetValue(TrackedItemProperty, value);
    }

    public int TrackedIndex
    {
        get => (int)GetValue(TrackedIndexProperty);
        set => SetValue(TrackedIndexProperty, value);
    }

    public ICommand? TrackingChangedCommand
    {
        get => (ICommand?)GetValue(TrackingChangedCommandProperty);
        set => SetValue(TrackingChangedCommandProperty, value);
    }

    public Func<ChartTrackingInfo, string>? TooltipFormatter
    {
        get => (Func<ChartTrackingInfo, string>?)GetValue(TooltipFormatterProperty);
        set => SetValue(TooltipFormatterProperty, value);
    }

    internal override ChartTrackballMode InputTrackballMode => EffectiveDensity == ChartDensity.Minimal ? ChartTrackballMode.None : TrackballMode;

    internal override void OnTrackStarted(Point point)
    {
        interaction.IsTracking = true;
        interaction.TrackPoint = point;
        ResolveTracking();
        UpdateTrackingTexts();
        UpdateTrackedItem();
        RaiseTracking(TrackingStarted);
        PerformHaptic(ChartHaptic.Tick);
        InvalidateCanvas();
    }

    internal override void OnTrackMoved(Point point)
    {
        interaction.TrackPoint = point;
        var axis = interaction.TrackedAxis;
        if (ResolveTracking())
        {
            UpdateTrackingTexts();
            UpdateTrackedItem();
            RaiseTracking(TrackingChanged);
            if (axis != interaction.TrackedAxis)
            {
                PerformHaptic(ChartHaptic.Tick);
            }
        }

        InvalidateCanvas();
    }

    internal override void OnTrackEnded()
    {
        interaction.ClearTracking();
        UpdateTrackedItem();
        RaiseTracking(TrackingEnded);
        InvalidateCanvas();
    }

    private static void OnTooltipVisibleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamRadarChartView)bindable;
        view.UpdateTrackingTexts();
        view.InvalidateRender();
    }

    private static void OnTooltipFormatterChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamRadarChartView)bindable;
        view.trackingTexts.Clear();
        view.UpdateTrackingTexts();
        view.InvalidateRender();
    }

    private void RefreshTracking()
    {
        trackingTexts.Clear();
        if (!interaction.IsTracking)
        {
            return;
        }

        var changed = ResolveTracking();
        UpdateTrackingTexts();
        if (changed)
        {
            UpdateTrackedItem();
            RaiseTracking(TrackingChanged);
        }
    }

    private bool ResolveTracking()
    {
        var layout = ChartLayout;
        var point = interaction.TrackPoint;
        var axis = layout.FindAxis(point.X, point.Y);
        if (axis < 0)
        {
            axis = interaction.TrackedAxis < layout.AxisCount ? interaction.TrackedAxis : -1;
        }

        var changed = axis != interaction.TrackedAxis;
        interaction.TrackedAxis = axis;
        var indices = interaction.TrackedIndices;
        while (indices.Count > layoutSeries.Count)
        {
            indices.RemoveAt(indices.Count - 1);
        }

        while (indices.Count < layoutSeries.Count)
        {
            indices.Add(-1);
            changed = true;
        }

        for (var s = 0; s < indices.Count; s++)
        {
            var index = (axis >= 0) && (s < layout.SeriesCount) && layoutSeries[s].IsVisible && Double.IsFinite(layout.GetValue(s, axis)) ? layout.GetDataIndex(s, axis) : -1;
            changed |= indices[s] != index;
            indices[s] = index;
        }

        return changed;
    }

    private int FindTrackedLead()
    {
        var indices = interaction.TrackedIndices;
        for (var s = 0; s < indices.Count; s++)
        {
            if (indices[s] >= 0)
            {
                return s;
            }
        }

        return -1;
    }

    private void UpdateTrackingTexts()
    {
        var texts = interaction.TrackedTexts;
        texts.Clear();
        interaction.TooltipHeader = null;
        if (!interaction.IsTracking || !TooltipVisible || (FindTrackedLead() < 0))
        {
            return;
        }

        var indices = interaction.TrackedIndices;
        for (var s = 0; s < indices.Count; s++)
        {
            texts.Add(indices[s] >= 0 ? GetTrackingText(s, indices[s]) : null);
        }

        interaction.TooltipHeader = ChartLayout.GetAxisText(interaction.TrackedAxis);
    }

    internal string GetTrackingText(int seriesIndex, int index)
    {
        var key = ((long)seriesIndex << 32) | (uint)index;
        if (!trackingTexts.TryGetValue(key, out var text))
        {
            text = FormatTrackingText(seriesIndex, index);
            trackingTexts.Add(key, text);
        }

        return text;
    }

    private string FormatTrackingText(int seriesIndex, int index)
    {
        if (TooltipFormatter is { } formatter)
        {
            return formatter(CreateTrackingInfo(seriesIndex, index));
        }

        var item = layoutSeries[seriesIndex];
        var value = RadarLayout.FormatValue(item.Data.Y[index], item, ChartLayout.ValueFormatting);
        return String.IsNullOrEmpty(item.Title) ? value : item.Title + " " + value;
    }

    private ChartTrackingInfo CreateTrackingInfo(int seriesIndex, int index)
    {
        var series = attached[seriesIndex];
        var data = series.Snapshot;
        return new ChartTrackingInfo(series, seriesIndex, index, data.GetItem(index), data.X[index], data.XKind, data.GetCategory(index), data.Y[index]);
    }

    private void UpdateTrackedItem()
    {
        var lead = FindTrackedLead();
        var index = lead >= 0 ? interaction.TrackedIndices[lead] : -1;
        TrackedItem = lead >= 0 ? attached[lead].Snapshot.GetItem(index) : null;
        TrackedIndex = index;
    }

    private void RaiseTracking(EventHandler<ChartTrackingEventArgs>? handler)
    {
        if ((handler is null) && (TrackingChangedCommand is null))
        {
            return;
        }

        var args = CreateTrackingEventArgs();
        handler?.Invoke(this, args);
        ExecuteCommand(TrackingChangedCommand, args);
    }

    private ChartTrackingEventArgs CreateTrackingEventArgs()
    {
        var lead = FindTrackedLead();
        if (lead < 0)
        {
            return new ChartTrackingEventArgs(-1, null, null, []);
        }

        var indices = interaction.TrackedIndices;
        var count = 0;
        foreach (var index in indices)
        {
            if (index >= 0)
            {
                count++;
            }
        }

        var points = new ChartTrackingInfo[count];
        var position = 0;
        for (var s = 0; s < indices.Count; s++)
        {
            if (indices[s] >= 0)
            {
                points[position++] = CreateTrackingInfo(s, indices[s]);
            }
        }

        var data = attached[lead].Snapshot;
        var leadIndex = indices[lead];
        return new ChartTrackingEventArgs(leadIndex, data.GetItem(leadIndex), data.GetXValue(leadIndex), points);
    }
}
