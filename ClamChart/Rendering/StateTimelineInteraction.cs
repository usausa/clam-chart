namespace ClamChart.Rendering;

using SkiaSharp;

internal sealed class StateTimelineInteraction
{
    public bool IsTracking { get; set; }

    public int TrackedSegment { get; set; } = -1;

    public Point TrackPoint { get; set; }

    public bool TooltipVisible { get; set; }

    public string? TooltipHeader { get; set; }

    public List<string?> TrackedTexts { get; } = [];

    public List<SKColor> TrackedColors { get; } = [];

    public void ClearTracking()
    {
        IsTracking = false;
        TrackedSegment = -1;
        TooltipHeader = null;
        TrackedTexts.Clear();
        TrackedColors.Clear();
    }
}
