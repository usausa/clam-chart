namespace ClamChart;

using ClamChart.Layout;

public partial class ClamChartView
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

        var hit = HitTest(point);
        return hit.Kind switch
        {
            ChartHitKind.Legend => HasLegendHandlers,
            ChartHitKind.Item => HasItemHandlers || (TrackballMode != ChartTrackballMode.None),
            ChartHitKind.Plot => (SelectionMode == ChartSelectionMode.Category) && (FindColumnPoint(point).Series >= 0),
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

        if (SelectionMode == ChartSelectionMode.Category)
        {
            var (series, index) = FindColumnPoint(start);
            if (series >= 0)
            {
                TapItem(series, index, start);
                return;
            }
        }

        TapChart(start);
    }

    private ChartHit HitTest(Point point) => CartesianHitMap.HitTest(ChartLayout, layoutSeries, point.X, point.Y);

    private (int Series, int Index) FindColumnPoint(Point point)
    {
        var layout = ChartLayout;
        var plot = layout.PlotBounds;
        if (!plot.Contains(point.X, point.Y))
        {
            return (-1, -1);
        }

        return FindSnapPoint(layout.XAxis.ToValue(layout.Orientation == ChartOrientation.Horizontal ? point.Y : point.X));
    }

    private (int Series, int Index) FindSnapPoint(double value)
    {
        var layout = ChartLayout;
        var series = -1;
        var index = -1;
        var nearest = Double.PositiveInfinity;
        for (var s = 0; s < layoutSeries.Count; s++)
        {
            if (!layoutSeries[s].IsVisible)
            {
                continue;
            }

            var candidate = FindPointAt(s, value);
            if (candidate < 0)
            {
                continue;
            }

            var distance = Math.Abs(layout.Geometries[s].X[candidate] - value);
            if (distance < nearest)
            {
                nearest = distance;
                series = s;
                index = candidate;
            }
        }

        return (series, index);
    }

    private int FindPointAt(int seriesIndex, double value)
    {
        var index = CartesianHitMap.FindNearestIndexAt(ChartLayout, seriesIndex, value);
        if (index < 0)
        {
            return -1;
        }

        var geometry = ChartLayout.Geometries[seriesIndex];
        return Double.IsFinite(geometry.X[index]) && Double.IsFinite(geometry.End[index]) ? index : -1;
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
