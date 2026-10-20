namespace ClamChart;

using System.Windows.Input;

public partial class ClamChartView
{
    public static readonly BindableProperty LegendToggleEnabledProperty = BindableProperty.Create(nameof(LegendToggleEnabled), typeof(bool), typeof(ClamChartView), false);
    public static readonly BindableProperty LegendItemTappedCommandProperty = BindableProperty.Create(nameof(LegendItemTappedCommand), typeof(ICommand), typeof(ClamChartView));

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

    private void TapLegend(int seriesIndex)
    {
        var series = attached[seriesIndex];
        var generation = InputGeneration;
        var args = (LegendItemTapped is not null) || (LegendItemTappedCommand is not null) ? new ChartLegendEventArgs(series, null, seriesIndex) : null;
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

        if (LegendToggleEnabled && (!series.IsVisible || HasOtherVisibleSeries(series)))
        {
            series.IsVisible = !series.IsVisible;
        }

        if (args is not null)
        {
            ExecuteCommand(LegendItemTappedCommand, args);
        }
    }

    private bool HasOtherVisibleSeries(ChartSeries series)
    {
        foreach (var item in attached)
        {
            if ((item != series) && item.IsVisible)
            {
                return true;
            }
        }

        return false;
    }
}
