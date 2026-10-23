namespace ClamChart;

using System.Globalization;
using System.Windows.Input;

using ClamChart.Layout;
using ClamChart.Platform;
using ClamChart.Rendering;

public partial class ClamChartView
{
    public static readonly BindableProperty TrackballModeProperty = BindableProperty.Create(nameof(TrackballMode), typeof(ChartTrackballMode), typeof(ClamChartView), ChartTrackballMode.None, propertyChanged: OnInputModeChanged);
    public static readonly BindableProperty TrackballSnapEnabledProperty = BindableProperty.Create(nameof(TrackballSnapEnabled), typeof(bool), typeof(ClamChartView), true, propertyChanged: OnTrackballSnapEnabledChanged);
    public static readonly BindableProperty TooltipVisibleProperty = BindableProperty.Create(nameof(TooltipVisible), typeof(bool), typeof(ClamChartView), true, propertyChanged: OnTooltipVisibleChanged);
    public static readonly BindableProperty TrackedItemProperty = BindableProperty.Create(nameof(TrackedItem), typeof(object), typeof(ClamChartView), defaultBindingMode: BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackedIndexProperty = BindableProperty.Create(nameof(TrackedIndex), typeof(int), typeof(ClamChartView), -1, BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackingChangedCommandProperty = BindableProperty.Create(nameof(TrackingChangedCommand), typeof(ICommand), typeof(ClamChartView));

    private readonly Dictionary<long, string> trackingTexts = [];
    private readonly Dictionary<double, string> trackingHeaders = [];

    public event EventHandler<ChartTrackingEventArgs>? TrackingStarted;

    public event EventHandler<ChartTrackingEventArgs>? TrackingChanged;

    public event EventHandler<ChartTrackingEventArgs>? TrackingEnded;

    public ChartTrackballMode TrackballMode
    {
        get => (ChartTrackballMode)GetValue(TrackballModeProperty);
        set => SetValue(TrackballModeProperty, value);
    }

    public bool TrackballSnapEnabled
    {
        get => (bool)GetValue(TrackballSnapEnabledProperty);
        set => SetValue(TrackballSnapEnabledProperty, value);
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
        get;
        set
        {
            field = value;
            trackingTexts.Clear();
            UpdateTrackingTexts();
            InvalidateRender();
        }
    }

    internal override ChartTrackballMode InputTrackballMode => EffectiveDensity == ChartDensity.Minimal ? ChartTrackballMode.None : TrackballMode;

    internal string? TooltipHeader => interaction.TooltipHeader;

    internal Point TooltipPoint => interaction.TrackPoint;

    internal override void OnTrackStarted(Point point)
    {
        interaction.IsFollowing = false;
        interaction.IsTracking = true;
        interaction.TrackPoint = point;
        ResolveTracking();
        UpdateTrackingTexts();
        UpdateTrackedItem();
        RaiseTracking(TrackingStarted);
        ShareTracking();
        PerformHaptic(ChartHaptic.Tick);
        InvalidateCanvas();
    }

    internal override void OnTrackMoved(Point point)
    {
        interaction.TrackPoint = point;
        var lead = (interaction.LeadSeries, interaction.LeadIndex);
        if (ResolveTracking())
        {
            UpdateTrackingTexts();
            UpdateTrackedItem();
            RaiseTracking(TrackingChanged);
            if (lead != (interaction.LeadSeries, interaction.LeadIndex))
            {
                PerformHaptic(ChartHaptic.Tick);
            }
        }

        ShareTracking();
        InvalidateCanvas();
    }

    internal override void OnTrackEnded()
    {
        interaction.ClearTracking();
        UpdateTrackedItem();
        RaiseTracking(TrackingEnded);
        ShareTracking();
        InvalidateCanvas();
    }

    private static void OnTrackballSnapEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        view.interaction.Snaps = (bool)newValue;
        if (view.interaction.IsFollowing)
        {
            view.PlaceFollowingPoint();
        }
        else if (view.interaction.IsTracking)
        {
            view.ShareTracking();
        }

        view.InvalidateCanvas();
    }

    private static void OnTooltipVisibleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        view.UpdateTrackingTexts();
        view.InvalidateRender();
    }

    private void RefreshTracking()
    {
        trackingTexts.Clear();
        trackingHeaders.Clear();
        if (!interaction.IsTracking)
        {
            return;
        }

        var changed = ResolveTracking();
        UpdateTrackingTexts();
        if (interaction.IsFollowing)
        {
            return;
        }

        if (changed)
        {
            UpdateTrackedItem();
            RaiseTracking(TrackingChanged);
        }

        if (changed || !interaction.Snaps)
        {
            ShareTracking();
        }
    }

    private bool ResolveTracking()
    {
        if (interaction.IsFollowing)
        {
            return ResolveFollowing();
        }

        var layout = ChartLayout;
        var plot = layout.PlotBounds;
        var point = interaction.TrackPoint;
        interaction.IsCrosshair = IsScatterOnly();
        if (interaction.IsCrosshair)
        {
            return ResolveCrosshair(point);
        }

        var along = layout.Orientation == ChartOrientation.Horizontal ? Math.Clamp(point.Y, plot.Y, plot.Bottom) : Math.Clamp(point.X, plot.X, plot.Right);
        return ResolveTrackingAt(layout.XAxis.ToValue(along));
    }

    private bool ResolveTrackingAt(double value)
    {
        var layout = ChartLayout;
        var (leadSeries, leadIndex) = Double.IsNaN(value) ? (-1, -1) : FindSnapPoint(value);
        var changed = (leadSeries != interaction.LeadSeries) || (leadIndex != interaction.LeadIndex);
        interaction.LeadSeries = leadSeries;
        interaction.LeadIndex = leadIndex;

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

        var x = leadSeries >= 0 ? layout.Geometries[leadSeries].X[leadIndex] : Double.NaN;
        for (var s = 0; s < indices.Count; s++)
        {
            var index = (leadSeries >= 0) && layoutSeries[s].IsVisible ? FindPointAt(s, x) : -1;
            changed |= indices[s] != index;
            indices[s] = index;
        }

        return changed;
    }

    private bool IsScatterOnly()
    {
        var scatter = false;
        foreach (var series in layoutSeries)
        {
            if (!series.IsVisible)
            {
                continue;
            }

            if (series.Kind != ChartSeriesKind.Scatter)
            {
                return false;
            }

            scatter = true;
        }

        return scatter;
    }

    private bool ResolveCrosshair(Point point)
    {
        var (leadSeries, leadIndex) = CartesianHitMap.FindNearestPoint(ChartLayout, layoutSeries, point.X, point.Y, Double.PositiveInfinity);
        var changed = (leadSeries != interaction.LeadSeries) || (leadIndex != interaction.LeadIndex);
        interaction.LeadSeries = leadSeries;
        interaction.LeadIndex = leadIndex;
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
            var index = s == leadSeries ? leadIndex : -1;
            changed |= indices[s] != index;
            indices[s] = index;
        }

        return changed;
    }

    private void UpdateTrackingTexts()
    {
        var texts = interaction.TrackedTexts;
        texts.Clear();
        interaction.TooltipHeader = null;
        if (!interaction.IsTracking || (interaction.IsFollowing && (TrackingGroupMode != ChartTrackingGroupMode.Tooltip)) || !TooltipVisible || (interaction.LeadSeries < 0))
        {
            return;
        }

        var indices = interaction.TrackedIndices;
        for (var s = 0; s < indices.Count; s++)
        {
            texts.Add(indices[s] >= 0 ? GetTrackingText(s, indices[s]) : null);
        }

        var x = ChartLayout.Geometries[interaction.LeadSeries].X[interaction.LeadIndex];
        if (!trackingHeaders.TryGetValue(x, out var header))
        {
            header = ChartLayout.GetTrackingLabel(x, layoutSeries);
            trackingHeaders.Add(x, header);
        }

        interaction.TooltipHeader = header;
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
        var axis = ChartLayout.GetValueAxis(item.Axis).Options;
        var culture = Culture ?? CultureInfo.CurrentCulture;
        var value = CartesianLayout.FormatValue(item.Data.Y[index], item, axis, false, culture);
        if (item.Kind == ChartSeriesKind.RangeBar)
        {
            var geometry = ChartLayout.Geometries[seriesIndex];
            value = CartesianLayout.FormatValue(geometry.Start[index], item, axis, false, culture) + " – " + CartesianLayout.FormatValue(geometry.End[index], item, axis, false, culture);
        }
        else if (item.IsBubble && (index < item.Data.Size.Length) && Double.IsFinite(item.Data.Size[index]))
        {
            value += " (" + ChartTextFormat.FormatValue(item.Data.Size[index], null, null, ChartTextFormat.Default, culture) + ")";
        }

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
        var lead = interaction.LeadSeries;
        var index = lead >= 0 ? interaction.LeadIndex : -1;
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
        var lead = interaction.LeadSeries;
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
        return new ChartTrackingEventArgs(interaction.LeadIndex, data.GetItem(interaction.LeadIndex), data.GetXValue(interaction.LeadIndex), points);
    }
}
