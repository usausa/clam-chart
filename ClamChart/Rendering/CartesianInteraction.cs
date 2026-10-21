namespace ClamChart.Rendering;

internal sealed class CartesianInteraction
{
    public int SelectedSeries { get; private set; } = -1;

    public int SelectedIndex { get; private set; } = -1;

    public bool CategorySelection { get; private set; }

    public bool HasSelection => (SelectedSeries >= 0) && (SelectedIndex >= 0);

    public int PreviousSeries { get; private set; } = -1;

    public int PreviousIndex { get; private set; } = -1;

    public bool PreviousCategory { get; private set; }

    public double SelectionProgress { get; set; } = 1;

    public bool IsTracking { get; set; }

    public bool IsCrosshair { get; set; }

    public int LeadSeries { get; set; } = -1;

    public int LeadIndex { get; set; } = -1;

    public Point TrackPoint { get; set; }

    public bool TooltipVisible { get; set; }

    public string? TooltipHeader { get; set; }

    public List<int> TrackedIndices { get; } = [];

    public List<string?> TrackedTexts { get; } = [];

    public bool Select(int seriesIndex, int index, bool category)
    {
        if ((SelectedSeries == seriesIndex) && (SelectedIndex == index) && (CategorySelection == category))
        {
            return false;
        }

        PreviousSeries = SelectedSeries;
        PreviousIndex = SelectedIndex;
        PreviousCategory = CategorySelection;
        SelectedSeries = seriesIndex;
        SelectedIndex = index;
        CategorySelection = category;
        return true;
    }

    public bool ClearSelection() => Select(-1, -1, false);

    public void ClearTracking()
    {
        IsTracking = false;
        IsCrosshair = false;
        LeadSeries = -1;
        LeadIndex = -1;
        TooltipHeader = null;
        TrackedIndices.Clear();
        TrackedTexts.Clear();
    }
}
