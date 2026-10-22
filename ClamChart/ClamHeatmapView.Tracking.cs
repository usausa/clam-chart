namespace ClamChart;

using System.Windows.Input;

using ClamChart.Platform;

public partial class ClamHeatmapView
{
    public static readonly BindableProperty TrackballModeProperty = BindableProperty.Create(nameof(TrackballMode), typeof(ChartTrackballMode), typeof(ClamHeatmapView), ChartTrackballMode.None, propertyChanged: OnInputModeChanged);
    public static readonly BindableProperty TooltipVisibleProperty = BindableProperty.Create(nameof(TooltipVisible), typeof(bool), typeof(ClamHeatmapView), true, propertyChanged: OnTooltipVisibleChanged);
    public static readonly BindableProperty TrackedItemProperty = BindableProperty.Create(nameof(TrackedItem), typeof(object), typeof(ClamHeatmapView), defaultBindingMode: BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackedIndexProperty = BindableProperty.Create(nameof(TrackedIndex), typeof(int), typeof(ClamHeatmapView), -1, BindingMode.OneWayToSource);
    public static readonly BindableProperty TrackingChangedCommandProperty = BindableProperty.Create(nameof(TrackingChangedCommand), typeof(ICommand), typeof(ClamHeatmapView));

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
        var view = (ClamHeatmapView)bindable;
        view.UpdateTrackingTexts();
        view.InvalidateRender();
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
        var layout = ChartLayout;
        var point = interaction.TrackPoint;
        var previous = interaction.TrackedCell;
        var cell = layout.FindNearestCell(point.X, point.Y);
        if ((cell < 0) && (previous >= 0) && (previous < layout.CellCount) && layout.IsPresent(previous))
        {
            cell = previous;
        }

        interaction.TrackedCell = cell;
        return cell != previous;
    }

    private void UpdateTrackingTexts()
    {
        var texts = interaction.TrackedTexts;
        texts.Clear();
        interaction.TooltipHeader = null;
        var cell = interaction.TrackedCell;
        if (!interaction.IsTracking || !TooltipVisible || (cell < 0))
        {
            return;
        }

        var layout = ChartLayout;
        interaction.TooltipHeader = layout.GetCellText(cell);
        var value = layout.GetValue(cell);
        if (Double.IsFinite(value))
        {
            texts.Add(layout.FormatValue(value));
        }
    }

    private int GetTrackedIndex() => interaction.TrackedCell >= 0 ? ChartLayout.GetDataIndex(interaction.TrackedCell) : -1;

    private void UpdateTrackedItem()
    {
        var index = GetTrackedIndex();
        TrackedItem = index >= 0 ? snapshot.GetItem(index) : null;
        TrackedIndex = index;
    }

    private void RaiseTracking(EventHandler<ChartTrackingEventArgs>? handler)
    {
        if ((handler is null) && (TrackingChangedCommand is null))
        {
            return;
        }

        var cell = interaction.TrackedCell;
        var index = GetTrackedIndex();
        var args = cell >= 0 ? new ChartTrackingEventArgs(index, index >= 0 ? snapshot.GetItem(index) : null, ChartLayout.GetCellX(cell), []) : new ChartTrackingEventArgs(-1, null, null, []);
        handler?.Invoke(this, args);
        ExecuteCommand(TrackingChangedCommand, args);
    }
}
