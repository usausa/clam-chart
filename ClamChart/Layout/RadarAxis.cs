namespace ClamChart.Layout;

internal readonly record struct RadarAxis(string? Text, double DirectionX, double DirectionY, ChartRect LabelBounds, double LabelMaxWidth);
