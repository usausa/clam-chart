namespace ClamChart;

using System.Windows.Input;

using ClamChart.Platform;

public partial class ClamChartView
{
    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(nameof(SelectionMode), typeof(ChartSelectionMode), typeof(ClamChartView), ChartSelectionMode.None, propertyChanged: OnSelectionModeChanged);
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(ClamChartView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnSelectedItemChanged);
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(ClamChartView), -1, BindingMode.TwoWay, propertyChanged: OnSelectedIndexChanged);
    public static readonly BindableProperty SelectedSeriesProperty = BindableProperty.Create(nameof(SelectedSeries), typeof(ChartSeries), typeof(ClamChartView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnSelectedSeriesChanged);
    public static readonly BindableProperty SelectionChangedCommandProperty = BindableProperty.Create(nameof(SelectionChangedCommand), typeof(ICommand), typeof(ClamChartView));

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

    public ChartSeries? SelectedSeries
    {
        get => (ChartSeries?)GetValue(SelectedSeriesProperty);
        set => SetValue(SelectedSeriesProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    private static void OnSelectionModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
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
        var view = (ClamChartView)bindable;
        if (!view.synchronizingSelection)
        {
            view.selectionFound = false;
            view.ResolveSelection(oldValue, view.SelectedIndex, true, true);
        }
    }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        if (!view.synchronizingSelection)
        {
            var oldItem = view.SelectedItem;
            view.selectionFound = false;
            view.SynchronizeSelection(null, (int)newValue, view.SelectedSeries);
            view.ResolveSelection(oldItem, (int)oldValue, true, true);
        }
    }

    private static void OnSelectedSeriesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ClamChartView)bindable;
        if (!view.synchronizingSelection)
        {
            var oldItem = view.SelectedItem;
            view.selectionFound = false;
            view.SynchronizeSelection(null, view.SelectedIndex, (ChartSeries?)newValue);
            view.ResolveSelection(oldItem, view.SelectedIndex, true, true);
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
            var (seriesIndex, index) = FindSelection();
            if (seriesIndex >= 0)
            {
                var series = attached[seriesIndex];
                selectionFound = true;
                SynchronizeSelection(SelectedItem ?? series.Snapshot.GetItem(index), index, SelectionMode == ChartSelectionMode.Item ? series : null);
                SetSelectionState(seriesIndex, index, SelectionMode == ChartSelectionMode.Category, render);
            }
            else if (selectionFound)
            {
                selectionFound = false;
                SynchronizeSelection(null, -1, null);
                ClearSelectionState(render);
            }
            else
            {
                SynchronizeSelection(SelectedItem, SelectedItem is null ? SelectedIndex : -1, SelectedSeries);
                ClearSelectionState(render);
            }
        }

        if (render)
        {
            InvalidateRender();
        }

        NotifySelection(oldItem, oldIndex, indexChanges);
    }

    private (int Series, int Index) FindSelection()
    {
        if (SelectedItem is { } item)
        {
            if ((SelectionMode == ChartSelectionMode.Item) && (SelectedSeries is { } preferred))
            {
                var preferredIndex = attached.IndexOf(preferred);
                var found = preferredIndex >= 0 ? attached[preferredIndex].Snapshot.IndexOf(item) : -1;
                if (found >= 0)
                {
                    return (preferredIndex, found);
                }
            }

            var hidden = (Series: -1, Index: -1);
            for (var s = 0; s < attached.Count; s++)
            {
                var index = attached[s].Snapshot.IndexOf(item);
                if (index < 0)
                {
                    continue;
                }

                if (attached[s].IsVisible)
                {
                    return (s, index);
                }

                if (hidden.Series < 0)
                {
                    hidden = (s, index);
                }
            }

            return hidden;
        }

        if (SelectedIndex < 0)
        {
            return (-1, -1);
        }

        var target = (SelectionMode == ChartSelectionMode.Item) && (SelectedSeries is { } series) ? attached.IndexOf(series) : FindFirstVisibleSeries();
        return (target >= 0) && (SelectedIndex < attached[target].Snapshot.Count) ? (target, SelectedIndex) : (-1, -1);
    }

    private int FindFirstVisibleSeries()
    {
        for (var s = 0; s < attached.Count; s++)
        {
            if (attached[s].IsVisible)
            {
                return s;
            }
        }

        return -1;
    }

    private void ToggleSelection(int seriesIndex, int index)
    {
        if (SelectionMode == ChartSelectionMode.Category)
        {
            (seriesIndex, index) = FindSnapPoint(ChartLayout.Geometries[seriesIndex].X[index]);
            if (seriesIndex < 0)
            {
                return;
            }
        }

        var oldItem = SelectedItem;
        var oldIndex = SelectedIndex;
        if (IsSelected(seriesIndex, index))
        {
            selectionFound = false;
            SynchronizeSelection(null, -1, null);
            ClearSelectionState(true);
        }
        else
        {
            var series = attached[seriesIndex];
            selectionFound = true;
            SynchronizeSelection(series.Snapshot.GetItem(index), index, SelectionMode == ChartSelectionMode.Item ? series : null);
            SetSelectionState(seriesIndex, index, SelectionMode == ChartSelectionMode.Category, true);
            PerformHaptic(ChartHaptic.Selection);
        }

        InvalidateRender();
        NotifySelection(oldItem, oldIndex, true);
    }

    private bool IsSelected(int seriesIndex, int index)
    {
        if (!interaction.HasSelection)
        {
            return false;
        }

        if (!interaction.CategorySelection)
        {
            return (interaction.SelectedSeries == seriesIndex) && (interaction.SelectedIndex == index);
        }

        var geometries = ChartLayout.Geometries;
        var selected = interaction.SelectedSeries;
        return (selected < geometries.Count) && (interaction.SelectedIndex < geometries[selected].Count) && geometries[selected].X[interaction.SelectedIndex].Equals(geometries[seriesIndex].X[index]);
    }

    private void ClearSelection()
    {
        if ((SelectedItem is null) && (SelectedIndex < 0) && (SelectedSeries is null) && !interaction.HasSelection)
        {
            return;
        }

        var oldItem = SelectedItem;
        var oldIndex = SelectedIndex;
        selectionFound = false;
        SynchronizeSelection(null, -1, null);
        ClearSelectionState(true);
        InvalidateRender();
        NotifySelection(oldItem, oldIndex, true);
    }

    private void SynchronizeSelection(object? item, int index, ChartSeries? series)
    {
        synchronizingSelection = true;
        try
        {
            SelectedSeries = series;
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
