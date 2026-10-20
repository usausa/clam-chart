namespace ClamChart;

using System.Windows.Input;

using ClamChart.Layout;

public partial class ClamPieChartView
{
    public static readonly BindableProperty LegendItemTappedCommandProperty = BindableProperty.Create(nameof(LegendItemTappedCommand), typeof(ICommand), typeof(ClamPieChartView));

    public event EventHandler<ChartLegendEventArgs>? LegendItemTapped;

    public ICommand? LegendItemTappedCommand
    {
        get => (ICommand?)GetValue(LegendItemTappedCommandProperty);
        set => SetValue(LegendItemTappedCommandProperty, value);
    }

    private bool HasLegendHandlers => (LegendItemTapped is not null) || (LegendItemTappedCommand is not null);

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

    private void TapLegend(int sliceIndex)
    {
        var slice = ChartLayout.Slices[sliceIndex];
        var generation = InputGeneration;
        var args = HasLegendHandlers ? new ChartLegendEventArgs(null, GetItem(slice), slice.DataIndex) : null;
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

        if ((SelectionMode != ChartSelectionMode.None) && !slice.IsOthers)
        {
            ToggleSelection(slice.DataIndex);
        }

        if (args is not null)
        {
            ExecuteCommand(LegendItemTappedCommand, args);
        }
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
