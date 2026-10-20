namespace ClamChart.Layout;

internal readonly record struct RadialBarRow(int DataIndex, int Slot, string? Label, double Ratio, double Percent, Color? Color, string? PercentText, string? ValueText) : IGaugeRow;
