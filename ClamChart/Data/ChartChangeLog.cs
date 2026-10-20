namespace ClamChart.Data;

using System.Collections.Specialized;

internal sealed class ChartChangeLog
{
    public const int Limit = 64;

    private readonly List<ChartChange> changes = [];

    public bool IsReset { get; private set; } = true;

    public IReadOnlyList<ChartChange> Changes => changes;

    public void Record(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Add(new ChartChange(ChartChangeKind.Add, e.NewStartingIndex, e.NewItems?.Count ?? 0));
                break;
            case NotifyCollectionChangedAction.Remove:
                Add(new ChartChange(ChartChangeKind.Remove, e.OldStartingIndex, e.OldItems?.Count ?? 0));
                break;
            case NotifyCollectionChangedAction.Replace:
                Add(new ChartChange(ChartChangeKind.Replace, e.NewStartingIndex, e.NewItems?.Count ?? 0));
                break;
            case NotifyCollectionChangedAction.Move when e.OldStartingIndex >= 0:
                Add(new ChartChange(ChartChangeKind.Move, e.NewStartingIndex, e.NewItems?.Count ?? 0, e.OldStartingIndex));
                break;
            default:
                MarkReset();
                break;
        }
    }

    public void RecordItemChanged(int index) => Add(new ChartChange(ChartChangeKind.Replace, index, 1));

    public void MarkReset()
    {
        IsReset = true;
        changes.Clear();
    }

    public void Clear()
    {
        IsReset = false;
        changes.Clear();
    }

    private void Add(ChartChange change)
    {
        if (IsReset)
        {
            return;
        }

        if ((change.Index < 0) || (change.Count <= 0) || (changes.Count >= Limit))
        {
            MarkReset();
            return;
        }

        changes.Add(change);
    }
}
