namespace ClamChart;

using System.Windows.Input;

using ClamChart.Layout;

public partial class ClamPieChartView
{
    public static readonly BindableProperty LegendToggleEnabledProperty = BindableProperty.Create(nameof(LegendToggleEnabled), typeof(bool), typeof(ClamPieChartView), false, propertyChanged: OnLegendToggleEnabledChanged);
    public static readonly BindableProperty LegendItemTappedCommandProperty = BindableProperty.Create(nameof(LegendItemTappedCommand), typeof(ICommand), typeof(ClamPieChartView));

    private readonly HashSet<string> hiddenLabels = [with(StringComparer.Ordinal)];

    public event EventHandler<ChartLegendEventArgs>? LegendItemTapped;

    public bool LegendToggleEnabled
    {
        get => (bool)GetValue(LegendToggleEnabledProperty);
        set => SetValue(LegendToggleEnabledProperty, value);
    }

    public ICommand? LegendItemTappedCommand
    {
        get => (ICommand?)GetValue(LegendItemTappedCommandProperty);
        set => SetValue(LegendItemTappedCommandProperty, value);
    }

    private bool HasLegendHandlers => LegendToggleEnabled || (LegendItemTapped is not null) || (LegendItemTappedCommand is not null);

    private bool HasItemHandlers => (SelectionMode != ChartSelectionMode.None) || HasItemTapHandlers;

    internal override ChartTouchMode ResolveTouchMode()
    {
        if (EffectiveDensity == ChartDensity.Minimal)
        {
            return base.ResolveTouchMode();
        }

        if (HasChartTapHandlers)
        {
            return ChartTouchMode.Interactive;
        }

        return HasItemHandlers || HasLegendHandlers ? ChartTouchMode.HitOnly : ChartTouchMode.PassThrough;
    }

    internal override bool IsInputTarget(Point point)
    {
        if (EffectiveDensity == ChartDensity.Minimal)
        {
            return false;
        }

        return HitTest(point).Kind switch
        {
            ChartHitKind.Legend => HasLegendHandlers || (SelectionMode != ChartSelectionMode.None),
            ChartHitKind.Item => HasItemHandlers,
            _ => false
        };
    }

    internal override void OnInputTap(Point start, Point end)
    {
        if (EffectiveDensity == ChartDensity.Minimal)
        {
            RaiseChartTapped(start);
            return;
        }

        var hit = HitTest(start);
        if (HitTest(end) != hit)
        {
            return;
        }

        if (hit.Kind == ChartHitKind.Legend)
        {
            TapLegend(hit.Index);
        }
        else if (hit.Kind == ChartHitKind.Item)
        {
            TapSlice(hit.Index, start);
        }
        else
        {
            TapChart(start);
        }
    }

    private ChartHit HitTest(Point point)
    {
        var selectable = IsSelectable;
        var exploded = selectable ? ChartLayout.FindSlice(interaction.SelectedKey) : -1;
        return ChartLayout.HitTest(point.X, point.Y, exploded, selectable ? ExplodeOffset : 0);
    }

    private void TapSlice(int sliceIndex, Point position)
    {
        var slice = ChartLayout.Slices[sliceIndex];
        var generation = InputGeneration;
        var args = HasItemTapHandlers ? new ChartItemEventArgs(null, -1, slice.DataIndex, GetItem(slice), slice.Label, slice.Value, position) : null;
        if ((args is not null) && InvokeItemTapped(args))
        {
            return;
        }

        if (generation != InputGeneration)
        {
            return;
        }

        if ((SelectionMode != ChartSelectionMode.None) && !slice.IsOthers)
        {
            ToggleSelection(slice.DataIndex);
        }

        if (args is not null)
        {
            ExecuteCommand(ItemTappedCommand, args);
        }
    }

    private static void OnLegendToggleEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamPieChartView)bindable;
        if (!(bool)newValue && (view.hiddenLabels.Count > 0))
        {
            view.hiddenLabels.Clear();
            view.hiddenChanged = true;
            view.InvalidateLayout();
        }
    }

    private void TapLegend(int legendIndex)
    {
        var slice = ChartLayout.GetLegendSlice(legendIndex);
        var hidden = ChartLayout.IsHiddenLegendSlice(legendIndex);
        var generation = InputGeneration;
        var args = (LegendItemTapped is not null) || (LegendItemTappedCommand is not null) ? new ChartLegendEventArgs(null, GetItem(slice), slice.DataIndex) : null;
        if (args is not null)
        {
            LegendItemTapped?.Invoke(this, args);
            if (args.Handled)
            {
                return;
            }
        }

        if (generation != InputGeneration)
        {
            return;
        }

        if (LegendToggleEnabled)
        {
            ToggleSlice(slice, hidden);
        }
        else if ((SelectionMode != ChartSelectionMode.None) && !slice.IsOthers)
        {
            ToggleSelection(slice.DataIndex);
        }

        if (args is not null)
        {
            ExecuteCommand(LegendItemTappedCommand, args);
        }
    }

    private void ToggleSlice(PieSlice slice, bool hidden)
    {
        if (slice.IsOthers || String.IsNullOrEmpty(slice.Label))
        {
            return;
        }

        if (hidden)
        {
            hiddenLabels.Remove(slice.Label);
        }
        else
        {
            if (!HasOtherVisibleSlice(slice.Label))
            {
                return;
            }

            hiddenLabels.Add(slice.Label);
            if (interaction.SelectedKey == slice.DataIndex)
            {
                ClearSelection();
            }
        }

        hiddenChanged = true;
        InvalidateLayout();
    }

    private bool HasOtherVisibleSlice(string label)
    {
        var values = snapshot.Y;
        for (var i = 0; i < values.Length; i++)
        {
            var name = snapshot.GetLabel(i);
            if (Double.IsFinite(values[i]) && (values[i] > 0) && !String.Equals(name, label, StringComparison.Ordinal) && (String.IsNullOrEmpty(name) || !hiddenLabels.Contains(name)))
            {
                return true;
            }
        }

        return false;
    }

    private void TapChart(Point position)
    {
        var generation = InputGeneration;
        var args = HasChartTapHandlers ? new ChartTappedEventArgs(position) : null;
        if ((args is not null) && InvokeChartTapped(args))
        {
            return;
        }

        if (generation != InputGeneration)
        {
            return;
        }

        ClearSelection();
        if (args is not null)
        {
            ExecuteCommand(ChartTappedCommand, args);
        }
    }

    private object? GetItem(PieSlice slice) => slice.IsOthers ? null : snapshot.GetItem(slice.DataIndex);
}
