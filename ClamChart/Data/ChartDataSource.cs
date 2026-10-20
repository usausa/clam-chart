namespace ClamChart.Data;

using System.Collections;
using System.Collections.Specialized;

internal sealed class ChartDataSource
{
    private readonly Action changed;
    private readonly ChartCollectionWatcher collectionWatcher;
    private readonly ChartItemWatcher itemWatcher;
    private bool active;

    public IEnumerable? Source { get; private set; }

    public bool TrackItemChanges { get; private set; } = true;

    public ChartChangeLog Changes { get; } = new();

    public int WatchedItemCount => itemWatcher.Count;

    public ChartDataSource(Action changed)
    {
        this.changed = changed;
        collectionWatcher = new ChartCollectionWatcher(OnCollectionChanged);
        itemWatcher = new ChartItemWatcher(OnItemChanged);
    }

    public void SetSource(IEnumerable? source)
    {
        Source = source;
        collectionWatcher.Watch(source);
        Changes.MarkReset();
        SyncItems();
    }

    public void SetTrackItemChanges(bool value)
    {
        TrackItemChanges = value;
        SyncItems();
    }

    public void Activate()
    {
        active = true;
        collectionWatcher.Activate();
        Changes.MarkReset();
        SyncItems();
    }

    public void Deactivate()
    {
        active = false;
        collectionWatcher.Deactivate();
        itemWatcher.Clear();
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        Changes.Record(e);
        SyncItems();
        changed();
    }

    private void OnItemChanged(object item)
    {
        if (Source is IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                {
                    Changes.RecordItemChanged(i);
                }
            }
        }
        else if (Source is not null)
        {
            var index = 0;
            foreach (var value in Source)
            {
                if (ReferenceEquals(value, item))
                {
                    Changes.RecordItemChanged(index);
                }

                index++;
            }
        }

        changed();
    }

    private void SyncItems()
    {
        if (active && TrackItemChanges)
        {
            itemWatcher.Sync(Source);
        }
        else
        {
            itemWatcher.Clear();
        }
    }
}
