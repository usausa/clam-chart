namespace ClamChart.Layout;

internal readonly record struct BarGaugeRow(int DataIndex, int Slot, string? Label, string? ValueText, double Ratio, double Percent, double TargetRatio, Color? Color);
