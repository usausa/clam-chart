namespace ClamChart.Layout;

internal readonly record struct LegendCell(int Row, string Text, ChartRect Bounds, double MaxWidth);
