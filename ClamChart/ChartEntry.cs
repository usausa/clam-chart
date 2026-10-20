namespace ClamChart;

public sealed record ChartEntry(string? Label, double Value, Color? Color = null, double? Maximum = null, double? Target = null);
