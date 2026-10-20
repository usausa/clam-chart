namespace ClamChart.Data;

internal readonly record struct ChartChange(ChartChangeKind Kind, int Index, int Count, int OldIndex = -1);
