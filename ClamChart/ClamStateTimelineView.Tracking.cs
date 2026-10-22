namespace ClamChart;

using System.Windows.Input;

using ClamChart.Data;
using ClamChart.Layout;
using ClamChart.Platform;

using SkiaSharp;

public partial class ClamStateTimelineView
{
    public static readonly BindableProperty TrackballModeProperty = BindableProperty.Create(nameof(TrackballMode), typeof(ChartTrackballMode), typeof(ClamStateTimelineView), ChartTrackballMode.None, propertyChanged: OnInputModeChanged);
    public static readonly BindableProperty TooltipVisibleProperty = BindableProperty.Create(nameof(TooltipVisible), typeof(bool), typeof(ClamStateTimelineView), true, propertyChanged: OnTooltipVisibleChanged);
    public static readonly BindableProperty TrackedItemProperty = BindableProperty.Create(nameof(TrackedItem), typeof(object), typeof(ClamStateTimelineView), defaultBindingMode: BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackedIndexProperty = BindableProperty.Create(nameof(TrackedIndex), typeof(int), typeof(ClamStateTimelineView), -1, BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackingChangedCommandProperty = BindableProperty.Create(nameof(TrackingChangedCommand), typeof(ICommand), typeof(ClamStateTimelineView));

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

    internal override ChartTrackballMode InputTrackballMode => EffectiveDensity == ChartDensity.Minimal ? ChartTrackballMode.None : TrackballMode;

    internal string? TooltipHeader => interaction.TooltipHeader;

    internal IReadOnlyList<string?> TooltipTexts => interaction.TrackedTexts;

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
        if (ResolveTracking())
        {
            UpdateTrackingTexts();
            UpdateTrackedItem();
            RaiseTracking(TrackingChanged);
            PerformHaptic(ChartHaptic.Tick);
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
        var view = (ClamStateTimelineView)bindable;
        view.UpdateTrackingTexts();
        view.InvalidateRender();
    }

    private double GetTime(double x)
    {
        var plot = ChartLayout.Plot;
        var scale = Interpolator.DisplayScale;
        return plot.Width > 0 ? scale.Minimum + ((x - plot.X) / plot.Width * scale.Span) : Double.NaN;
    }

    private int FindSegment(Point point, bool nearest)
    {
        var layout = ChartLayout;
        var row = nearest ? layout.FindNearestRow(point.Y) : layout.FindRow(point.Y);
        var x = nearest ? Math.Clamp(point.X, layout.Plot.X, layout.Plot.Right) : point.X;
        return (x >= layout.Plot.X) && (x <= layout.Plot.Right) ? layout.FindSegment(row, GetTime(x), layout.Axis.Scale.Maximum) : -1;
    }

    private void RefreshTracking()
    {
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
        var previous = interaction.TrackedSegment;
        var segment = FindSegment(interaction.TrackPoint, true);
        interaction.TrackedSegment = segment;
        return segment != previous;
    }

    private void UpdateTrackingTexts()
    {
        var texts = interaction.TrackedTexts;
        var colors = interaction.TrackedColors;
        texts.Clear();
        colors.Clear();
        interaction.TooltipHeader = null;
        var index = interaction.TrackedSegment;
        if (!interaction.IsTracking || !TooltipVisible || (index < 0))
        {
            return;
        }

        var layout = ChartLayout;
        var segment = layout.Segments[index];
        var end = StateTimelineLayout.GetEnd(segment, layout.Axis.Scale.Maximum);
        interaction.TooltipHeader = layout.Axis.GetTrackingTimeText(segment.Start, layout.TimePrecision) + " – " + layout.Axis.GetTrackingTimeText(end, layout.TimePrecision);
        var name = layout.RowNames[segment.Row];
        texts.Add(String.IsNullOrEmpty(name) ? layout.GetText(index) : name + " " + layout.GetText(index));
        colors.Add(layout.GetColor(index));
        texts.Add(StateTimelineLayout.FormatDuration(end - segment.Start));
        colors.Add(SKColors.Transparent);
    }

    private int GetTrackedIndex() => interaction.TrackedSegment >= 0 ? ChartLayout.Segments[interaction.TrackedSegment].DataIndex : -1;

    private void UpdateTrackedItem()
    {
        var index = GetTrackedIndex();
        TrackedItem = GetItem(index);
        TrackedIndex = index;
    }

    private void RaiseTracking(EventHandler<ChartTrackingEventArgs>? handler)
    {
        if ((handler is null) && (TrackingChangedCommand is null))
        {
            return;
        }

        var index = GetTrackedIndex();
        var time = interaction.IsTracking ? GetTime(Math.Clamp(interaction.TrackPoint.X, ChartLayout.Plot.X, ChartLayout.Plot.Right)) : Double.NaN;
        var args = new ChartTrackingEventArgs(index, GetItem(index), Double.IsFinite(time) ? ChartDateTime.FromTicks(time) : null, []);
        handler?.Invoke(this, args);
        ExecuteCommand(TrackingChangedCommand, args);
    }
}
