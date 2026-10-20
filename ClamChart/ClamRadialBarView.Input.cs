namespace ClamChart;

using System.Windows.Input;

using ClamChart.Layout;

public partial class ClamRadialBarView
{
    public static readonly BindableProperty LegendItemTappedCommandProperty = BindableProperty.Create(nameof(LegendItemTappedCommand), typeof(ICommand), typeof(ClamRadialBarView));

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

        return ChartLayout.HitTest(point.X, point.Y).Kind switch
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

        var hit = ChartLayout.HitTest(start.X, start.Y);
        if (ChartLayout.HitTest(end.X, end.Y) != hit)
        {
            return;
        }

        if (hit.Kind == ChartHitKind.Legend)
        {
            TapLegend(hit.Index);
        }
        else if (hit.Kind == ChartHitKind.Item)
        {
            TapRing(hit.Index, start);
        }
        else
        {
            TapChart(start);
        }
    }

    private void TapRing(int rowIndex, Point position)
    {
        var row = ChartLayout.Rows[rowIndex];
        var generation = InputGeneration;
        var args = HasItemTapHandlers ? new ChartItemEventArgs(null, -1, row.DataIndex, snapshot.GetItem(row.DataIndex), row.Label, snapshot.Y[row.DataIndex], position) : null;
        if ((args is not null) && InvokeItemTapped(args))
        {
            return;
        }

        if (generation != InputGeneration)
        {
            return;
        }

        if (SelectionMode != ChartSelectionMode.None)
        {
            ToggleSelection(row.DataIndex);
        }

        if (args is not null)
        {
            ExecuteCommand(ItemTappedCommand, args);
        }
    }

    private void TapLegend(int rowIndex)
    {
        var row = ChartLayout.Rows[rowIndex];
        var generation = InputGeneration;
        var args = HasLegendHandlers ? new ChartLegendEventArgs(null, snapshot.GetItem(row.DataIndex), row.DataIndex) : null;
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

        if (SelectionMode != ChartSelectionMode.None)
        {
            ToggleSelection(row.DataIndex);
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
}
