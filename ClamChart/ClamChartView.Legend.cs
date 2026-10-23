namespace ClamChart;

using System.Windows.Input;

using ClamChart.Layout;

public partial class ClamChartView
{
    public static readonly BindableProperty LegendModeProperty = BindableProperty.Create(nameof(LegendMode), typeof(ChartLegendMode), typeof(ClamChartView), ChartLegendMode.List, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendValuesProperty = BindableProperty.Create(nameof(LegendValues), typeof(ChartLegendValues), typeof(ClamChartView), ChartLegendValues.None, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendWidthProperty = BindableProperty.Create(nameof(LegendWidth), typeof(double), typeof(ClamChartView), LegendLayout.MaxSideWidth, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(LegendWidth)), propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendToggleEnabledProperty = BindableProperty.Create(nameof(LegendToggleEnabled), typeof(bool), typeof(ClamChartView), false);
    public static readonly BindableProperty LegendSortEnabledProperty = BindableProperty.Create(nameof(LegendSortEnabled), typeof(bool), typeof(ClamChartView), false);
    public static readonly BindableProperty LegendSortByProperty = BindableProperty.Create(nameof(LegendSortBy), typeof(ChartLegendSortKey), typeof(ClamChartView), ChartLegendSortKey.Name, BindingMode.TwoWay, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendSortModeProperty = BindableProperty.Create(nameof(LegendSortMode), typeof(ChartSortMode), typeof(ClamChartView), ChartSortMode.None, BindingMode.TwoWay, propertyChanged: OnLayoutInputChanged);
    public static readonly BindableProperty LegendItemTappedCommandProperty = BindableProperty.Create(nameof(LegendItemTappedCommand), typeof(ICommand), typeof(ClamChartView));

    public event EventHandler<ChartLegendEventArgs>? LegendItemTapped;

    public ChartLegendMode LegendMode
    {
        get => (ChartLegendMode)GetValue(LegendModeProperty);
        set => SetValue(LegendModeProperty, value);
    }

    public ChartLegendValues LegendValues
    {
        get => (ChartLegendValues)GetValue(LegendValuesProperty);
        set => SetValue(LegendValuesProperty, value);
    }

    public double LegendWidth
    {
        get => (double)GetValue(LegendWidthProperty);
        set => SetValue(LegendWidthProperty, value);
    }

    public bool LegendToggleEnabled
    {
        get => (bool)GetValue(LegendToggleEnabledProperty);
        set => SetValue(LegendToggleEnabledProperty, value);
    }

    public bool LegendSortEnabled
    {
        get => (bool)GetValue(LegendSortEnabledProperty);
        set => SetValue(LegendSortEnabledProperty, value);
    }

    public ChartLegendSortKey LegendSortBy
    {
        get => (ChartLegendSortKey)GetValue(LegendSortByProperty);
        set => SetValue(LegendSortByProperty, value);
    }

    public ChartSortMode LegendSortMode
    {
        get => (ChartSortMode)GetValue(LegendSortModeProperty);
        set => SetValue(LegendSortModeProperty, value);
    }

    public ICommand? LegendItemTappedCommand
    {
        get => (ICommand?)GetValue(LegendItemTappedCommandProperty);
        set => SetValue(LegendItemTappedCommandProperty, value);
    }

    private void TapLegendHeader(int column)
    {
        if (!LegendSortEnabled || (ChartLayout.GetLegendSortKey(column) is not { } key))
        {
            return;
        }

        if ((key != LegendSortBy) || (LegendSortMode == ChartSortMode.None))
        {
            LegendSortBy = key;
            LegendSortMode = ChartSortMode.Descending;
        }
        else
        {
            LegendSortMode = LegendSortMode == ChartSortMode.Descending ? ChartSortMode.Ascending : ChartSortMode.None;
        }
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
