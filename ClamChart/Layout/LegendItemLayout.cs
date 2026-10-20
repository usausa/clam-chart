namespace ClamChart.Layout;

internal readonly record struct LegendItemLayout(int SeriesIndex, ChartRect Bounds, ChartRect Marker, ChartRect Text, double TextMaxWidth, ChartRect Value = default);
