namespace ClamChart.Rendering;

internal sealed class HeatmapInteraction
{
    public int SelectedIndex { get; private set; } = -1;

    public int PreviousIndex { get; private set; } = -1;

    public bool HasSelection => SelectedIndex >= 0;

    public double SelectionProgress { get; set; } = 1;

    public bool IsTracking { get; set; }

    public int TrackedCell { get; set; } = -1;

    public Point TrackPoint { get; set; }

    public bool TooltipVisible { get; set; }

    public string? TooltipHeader { get; set; }

    public List<string?> TrackedTexts { get; } = [];

    public bool Select(int index)
    {
        if (SelectedIndex == index)
        {
            return false;
        }

        PreviousIndex = SelectedIndex;
        SelectedIndex = index;
        return true;
    }

    public bool ClearSelection() => Select(-1);

    public void ClearTracking()
    {
        IsTracking = false;
        TrackedCell = -1;
        TooltipHeader = null;
        TrackedTexts.Clear();
    }
}
