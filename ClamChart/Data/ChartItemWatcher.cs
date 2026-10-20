namespace ClamChart.Data;

using System.Collections;
using System.ComponentModel;

internal sealed class ChartItemWatcher(Action<object> changed)
{
    private readonly HashSet<INotifyPropertyChanged> items = [with(ReferenceEqualityComparer.Instance)];
    private readonly HashSet<INotifyPropertyChanged> current = [with(ReferenceEqualityComparer.Instance)];

    public int Count => items.Count;

    public void Sync(IEnumerable? source)
    {
        current.Clear();
        if (source is not (null or IEnumerable<double> or IEnumerable<double?>))
        {
            foreach (var item in source)
            {
                if (item is INotifyPropertyChanged notifier)
                {
                    current.Add(notifier);
                }
            }
        }

        foreach (var item in items)
        {
            if (!current.Contains(item))
            {
                item.PropertyChanged -= OnPropertyChanged;
            }
        }

        foreach (var item in current)
        {
            if (!items.Contains(item))
            {
                item.PropertyChanged += OnPropertyChanged;
            }
        }

        items.Clear();
        items.UnionWith(current);
        current.Clear();
    }

    public void Clear()
    {
        foreach (var item in items)
        {
            item.PropertyChanged -= OnPropertyChanged;
        }

        items.Clear();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not null)
        {
            changed(sender);
        }
    }
}
