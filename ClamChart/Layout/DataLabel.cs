namespace ClamChart.Layout;

internal readonly record struct DataLabel(int SeriesIndex, int Index, string Text, double Width, bool Low = false);
