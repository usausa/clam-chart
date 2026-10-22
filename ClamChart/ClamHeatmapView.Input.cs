namespace ClamChart;

public partial class ClamHeatmapView
{
    private bool HasItemHandlers => (SelectionMode != ChartSelectionMode.None) || HasItemTapHandlers;

    internal override ChartTouchMode ResolveTouchMode()
    {
        if (EffectiveDensity == ChartDensity.Minimal)
        {
            return base.ResolveTouchMode();
        }

        if ((TrackballMode != ChartTrackballMode.None) || HasChartTapHandlers)
        {
            return ChartTouchMode.Interactive;
        }

        return HasItemHandlers ? ChartTouchMode.HitOnly : ChartTouchMode.PassThrough;
    }

    internal override bool IsInputTarget(Point point) =>
        (EffectiveDensity != ChartDensity.Minimal) && (HasItemHandlers || (TrackballMode != ChartTrackballMode.None)) && (FindItem(point) >= 0);

    internal override void OnInputTap(Point start, Point end)
    {
        if (EffectiveDensity == ChartDensity.Minimal)
        {
            RaiseChartTapped(start);
            return;
        }

        var cell = ChartLayout.FindCell(start.X, start.Y);
        if (ChartLayout.FindCell(end.X, end.Y) != cell)
        {
            return;
        }

        var index = cell >= 0 ? ChartLayout.GetDataIndex(cell) : -1;
        if (index >= 0)
        {
            TapItem(index, start);
        }
        else
        {
            TapChart(start);
        }
    }

    private int FindItem(Point point)
    {
        var cell = ChartLayout.FindCell(point.X, point.Y);
        return cell >= 0 ? ChartLayout.GetDataIndex(cell) : -1;
    }

    private void TapItem(int index, Point position)
    {
        var generation = InputGeneration;
        var args = HasItemTapHandlers ? new ChartItemEventArgs(null, -1, index, snapshot.GetItem(index), snapshot.GetXValue(index), snapshot.Y[index], position) : null;
        if ((args is not null) && InvokeItemTapped(args))
        {
            return;
        }

        if (generation != InputGeneration)
        {
            return;
        }

        if (IsSelectable)
        {
            ToggleSelection(index);
        }

        if (args is not null)
        {
            ExecuteCommand(ItemTappedCommand, args);
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
