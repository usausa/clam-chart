namespace ClamChart.Layout;

internal readonly record struct StateTimelineSegment(int Row, double Start, double End, int State, int DataIndex, double Value);
