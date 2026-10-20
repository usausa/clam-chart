namespace ClamChart.Rendering;

internal sealed class PieInteraction
{
    public int SelectedKey { get; private set; } = -1;

    public int PreviousKey { get; private set; } = -1;

    public bool HasSelection => SelectedKey >= 0;

    public double SelectionProgress { get; set; } = 1;

    public double ExplodeOffset { get; set; }

    public bool Select(int key)
    {
        if (SelectedKey == key)
        {
            return false;
        }

        PreviousKey = SelectedKey;
        SelectedKey = key;
        return true;
    }

    public bool ClearSelection() => Select(-1);
}
