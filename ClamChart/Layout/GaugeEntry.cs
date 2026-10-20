namespace ClamChart.Layout;

internal readonly record struct GaugeEntry(int DataIndex, string? Label, double Value, double Maximum, double Target, Color? Color);
