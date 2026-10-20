namespace ClamChart.Data;

using System.Collections.Specialized;

internal sealed class ChartCollectionWatcher(Action<NotifyCollectionChangedEventArgs> changed)
{
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
        }
    }

    private void Unsubscribe()
    {
        if (subscribed && (source is not null))
        {
            source.CollectionChanged -= OnCollectionChanged;
        }

        subscribed = false;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => changed(e);
}
