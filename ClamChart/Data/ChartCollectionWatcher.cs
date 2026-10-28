namespace ClamChart.Data;

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

internal sealed class ChartCollectionWatcher(Action<NotifyCollectionChangedEventArgs> changed, bool watchItems = false)
{
    private static readonly NotifyCollectionChangedEventArgs ItemChangedArgs = new(NotifyCollectionChangedAction.Reset);

    private readonly List<INotifyPropertyChanged> items = [];

    private INotifyCollectionChanged? source;
    private bool active;
    private bool subscribed;

    public void Watch(object? value)
    {
        Unsubscribe();
        source = value as INotifyCollectionChanged;
        Subscribe();
    }

    public void Activate()
    {
        active = true;
        Subscribe();
    }

    public void Deactivate()
    {
        Unsubscribe();
        active = false;
    }

    private void Subscribe()
    {
        if (active && !subscribed && (source is not null))
        {
            source.CollectionChanged += OnCollectionChanged;
            subscribed = true;
            if (watchItems)
            {
                WatchItems();
            }
        }
    }

    private void Unsubscribe()
    {
        if (subscribed && (source is not null))
        {
            source.CollectionChanged -= OnCollectionChanged;
        }

        subscribed = false;
        UnwatchItems();
    }

    private void WatchItems()
    {
        if (source is not IEnumerable enumerable)
        {
            return;
        }

        foreach (var item in enumerable)
        {
            if (item is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += OnItemPropertyChanged;
                items.Add(notify);
            }
        }
    }

    private void UnwatchItems()
    {
        foreach (var item in items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        items.Clear();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (watchItems)
        {
            UnwatchItems();
            WatchItems();
        }

        changed(e);
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => changed(ItemChangedArgs);
}
