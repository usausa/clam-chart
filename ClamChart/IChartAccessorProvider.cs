namespace ClamChart;

using ClamChart.Data;

public interface IChartAccessorProvider
{
    internal ChartAccessor? FindAccessor(string key);
}
