namespace ClamChart;

using ClamChart.Data;

public partial class ClamStateTimelineView
{
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

        return HasItemTapHandlers ? ChartTouchMode.HitOnly : ChartTouchMode.PassThrough;
    }

    internal override bool IsInputTarget(Point point) =>
        (EffectiveDensity != ChartDensity.Minimal) && (HasItemTapHandlers || (TrackballMode != ChartTrackballMode.None)) && (FindSegment(point, false) >= 0);

    internal override void OnInputTap(Point start, Point end)
    {
        if (EffectiveDensity == ChartDensity.Minimal)
        {
            RaiseChartTapped(start);
            return;
        }

        var segment = FindSegment(start, false);
        if (FindSegment(end, false) != segment)
        {
            return;
        }

        if (segment >= 0)
        {
            TapItem(segment, start);
        }
        else
        {
            TapChart(start);
        }
    }

    private void TapItem(int segment, Point position)
    {
        if (!HasItemTapHandlers)
        {
            return;
        }

        var value = ChartLayout.Segments[segment];
        var args = new ChartItemEventArgs(null, -1, value.DataIndex, GetItem(value.DataIndex), ChartDateTime.FromTicks(value.Start), value.Value, position);
        if (!InvokeItemTapped(args))
        {
            ExecuteCommand(ItemTappedCommand, args);
        }
    }

    private void TapChart(Point position)
    {
        if (!HasChartTapHandlers)
        {
            return;
        }

        var args = new ChartTappedEventArgs(position);
        if (!InvokeChartTapped(args))
        {
            ExecuteCommand(ChartTappedCommand, args);
        }
    }
}
