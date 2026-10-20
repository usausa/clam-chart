namespace ClamChart.Layout;

internal readonly record struct AxisLabel(double Value, string Text, double Width, double MaxWidth, ChartRect Bounds);
