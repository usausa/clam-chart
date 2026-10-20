namespace ClamChart;

public sealed class ChartSelectionChangedEventArgs(object? oldItem, object? newItem, int oldIndex, int newIndex) : EventArgs
{
    public object? OldItem { get; } = oldItem;

    public object? NewItem { get; } = newItem;

    public int OldIndex { get; } = oldIndex;

    public int NewIndex { get; } = newIndex;
}
