namespace ClamChart.Layout;

internal readonly record struct StateTimelineEntry(int DataIndex, string? Row, double Start, double End, string? Text, double Number)
{
    public bool HasState => (Text is not null) || Double.IsFinite(Number);
}
