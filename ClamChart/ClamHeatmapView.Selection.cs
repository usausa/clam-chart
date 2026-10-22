namespace ClamChart;

using System.Windows.Input;

using ClamChart.Platform;

public partial class ClamHeatmapView
{
    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(nameof(SelectionMode), typeof(ChartSelectionMode), typeof(ClamHeatmapView), ChartSelectionMode.None, propertyChanged: OnSelectionModeChanged);
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(ClamHeatmapView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnSelectedItemChanged);
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(ClamHeatmapView), -1, BindingMode.TwoWay, propertyChanged: OnSelectedIndexChanged);
    public static readonly BindableProperty SelectionChangedCommandProperty = BindableProperty.Create(nameof(SelectionChangedCommand), typeof(ICommand), typeof(ClamHeatmapView));

    private bool synchronizingSelection;
    private bool selectionFound;

    public event EventHandler<ChartSelectionChangedEventArgs>? SelectionChanged;

    public ChartSelectionMode SelectionMode
    {
        get => (ChartSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    private static void OnSelectionModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamHeatmapView)bindable;
        if ((ChartSelectionMode)newValue == ChartSelectionMode.None)
        {
            view.ClearSelection();
        }
        else
        {
            view.ResolveSelection(view.SelectedItem, view.SelectedIndex, true, true);
        }
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamHeatmapView)bindable;
        if (!view.synchronizingSelection)
        {
            view.selectionFound = false;
            view.ResolveSelection(oldValue, view.SelectedIndex, true, true);
        }
    }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamHeatmapView)bindable;
        if (!view.synchronizingSelection)
        {
            var oldItem = view.SelectedItem;
            view.selectionFound = false;
            view.SynchronizeSelection(null, (int)newValue);
            view.ResolveSelection(oldItem, (int)oldValue, true, true);
        }
    }

    private void ResolveSelection(object? oldItem, int oldIndex, bool indexChanges, bool render)
    {
        if (SelectionMode == ChartSelectionMode.None)
        {
            ClearSelectionState(render);
        }
        else
        {
            var index = FindSelection();
            if (index >= 0)
            {
                selectionFound = true;
                SynchronizeSelection(SelectedItem ?? snapshot.GetItem(index), index);
                SetSelectionState(index, render);
            }
            else if (selectionFound)
            {
                selectionFound = false;
                SynchronizeSelection(null, -1);
                ClearSelectionState(render);
            }
            else
            {
                SynchronizeSelection(SelectedItem, SelectedItem is null ? SelectedIndex : -1);
                ClearSelectionState(render);
            }
        }

        if (render)
        {
            InvalidateRender();
        }

        NotifySelection(oldItem, oldIndex, indexChanges);
    }

    private int FindSelection()
    {
        if (SelectedItem is { } item)
        {
            return snapshot.IndexOf(item);
        }

        return (SelectedIndex >= 0) && (SelectedIndex < snapshot.Count) ? SelectedIndex : -1;
    }

    private void ToggleSelection(int index)
    {
        var oldItem = SelectedItem;
        var oldIndex = SelectedIndex;
        if (interaction.SelectedIndex == index)
        {
            selectionFound = false;
            SynchronizeSelection(null, -1);
            ClearSelectionState(true);
        }
        else
        {
            selectionFound = true;
            SynchronizeSelection(snapshot.GetItem(index), index);
            SetSelectionState(index, true);
            PerformHaptic(ChartHaptic.Selection);
        }

        InvalidateRender();
        NotifySelection(oldItem, oldIndex, true);
    }

    private void ClearSelection()
    {
        if ((SelectedItem is null) && (SelectedIndex < 0) && !interaction.HasSelection)
        {
            return;
        }

        var oldItem = SelectedItem;
        var oldIndex = SelectedIndex;
        selectionFound = false;
        SynchronizeSelection(null, -1);
        ClearSelectionState(true);
        InvalidateRender();
        NotifySelection(oldItem, oldIndex, true);
    }

    private void SynchronizeSelection(object? item, int index)
    {
        synchronizingSelection = true;
        try
        {
            SelectedIndex = index;
            SelectedItem = item;
        }
        finally
        {
            synchronizingSelection = false;
        }
    }

    private void NotifySelection(object? oldItem, int oldIndex, bool indexChanges)
    {
        var item = SelectedItem;
        var index = SelectedIndex;
        if (Equals(oldItem, item) && (!indexChanges || (oldIndex == index)))
        {
            return;
        }

        if ((SelectionChanged is null) && (SelectionChangedCommand is null))
        {
            return;
        }

        var args = new ChartSelectionChangedEventArgs(oldItem, item, oldIndex, index);
        SelectionChanged?.Invoke(this, args);
        ExecuteCommand(SelectionChangedCommand, args);
    }
}
