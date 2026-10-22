namespace ClamChart;

using ClamChart.Layout;

public partial class ClamRadarChartView
{
    private bool HasLegendHandlers => LegendToggleEnabled || (LegendItemTapped is not null) || (LegendItemTappedCommand is not null);

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
            ChartHitKind.Legend => HasLegendHandlers,
            ChartHitKind.Item => HasItemHandlers || (TrackballMode != ChartTrackballMode.None),
            ChartHitKind.Plot => (SelectionMode == ChartSelectionMode.Category) && (FindAxisPoint(point).Series >= 0),
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
            TapLegend(hit.SeriesIndex);
            return;
        }

        if (hit.Kind == ChartHitKind.Item)
        {
            TapItem(hit.SeriesIndex, hit.Index, start);
            return;
        }

        if ((hit.Kind == ChartHitKind.Plot) && (SelectionMode == ChartSelectionMode.Category))
        {
            var (series, index) = FindAxisPoint(start);
            if (series >= 0)
            {
                TapItem(series, index, start);
                return;
            }
        }

        TapChart(start);
    }

    private ChartHit HitTest(Point point) => ChartLayout.HitTest(point.X, point.Y, layoutSeries);

    private (int Series, int Index) FindAxisPoint(Point point)
    {
        var axis = ChartLayout.IsInside(point.X, point.Y) ? ChartLayout.FindAxis(point.X, point.Y) : -1;
        return axis >= 0 ? FindLeadPoint(axis) : (-1, -1);
    }

    private (int Series, int Index) FindLeadPoint(int axis)
    {
        var layout = ChartLayout;
        for (var s = 0; (s < layoutSeries.Count) && (s < layout.SeriesCount); s++)
        {
            if (layoutSeries[s].IsVisible && Double.IsFinite(layout.GetValue(s, axis)))
            {
                return (s, layout.GetDataIndex(s, axis));
            }
        }

        return (-1, -1);
    }

    private void TapItem(int seriesIndex, int index, Point position)
    {
        var generation = InputGeneration;
        var args = HasItemTapHandlers ? CreateItemEventArgs(seriesIndex, index, position) : null;
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
            ToggleSelection(seriesIndex, index);
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

    private ChartItemEventArgs CreateItemEventArgs(int seriesIndex, int index, Point position)
    {
        var series = attached[seriesIndex];
        var data = series.Snapshot;
        return new ChartItemEventArgs(series, seriesIndex, index, data.GetItem(index), data.GetXValue(index), data.Y[index], position);
    }
}
