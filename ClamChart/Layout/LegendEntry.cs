namespace ClamChart.Layout;

internal readonly record struct LegendEntry(int SeriesIndex, string Text, LegendMarkerKind Marker, string? Value = null);
