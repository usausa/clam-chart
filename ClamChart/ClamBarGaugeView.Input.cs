namespace ClamChart;

public partial class ClamBarGaugeView
{
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

        return HasItemTapHandlers ? ChartTouchMode.HitOnly : ChartTouchMode.PassThrough;
    }

    internal override bool IsInputTarget(Point point) =>
        (EffectiveDensity != ChartDensity.Minimal) && HasItemTapHandlers && (ChartLayout.FindRow(point.X, point.Y) >= 0);

    internal override void OnInputTap(Point start, Point end)
    {
        if (EffectiveDensity == ChartDensity.Minimal)
        {
            RaiseChartTapped(start);
            return;
        }

        var row = ChartLayout.FindRow(start.X, start.Y);
        if (ChartLayout.FindRow(end.X, end.Y) != row)
        {
            return;
        }

        if (row >= 0)
        {
            TapRow(row, start);
        }
        else
        {
            RaiseChartTapped(start);
        }
    }

    private void TapRow(int rowIndex, Point position)
    {
        if (!HasItemTapHandlers)
        {
            return;
        }

        var row = ChartLayout.Rows[rowIndex];
        var generation = InputGeneration;
        var args = new ChartItemEventArgs(null, -1, row.DataIndex, snapshot.GetItem(row.DataIndex), row.Label, snapshot.Y[row.DataIndex], position);
        if (!InvokeItemTapped(args) && (generation == InputGeneration))
        {
            ExecuteCommand(ItemTappedCommand, args);
        }
    }
}
