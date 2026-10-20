namespace ClamChart;

using System.Collections;

using ClamChart.Data;

public sealed class ChartAccessorCollection<TItem> : IChartAccessorProvider, IEnumerable<string>
    where TItem : class
{
    private readonly Dictionary<string, ChartAccessor> accessors = [with(StringComparer.Ordinal)];

    public int Count => accessors.Count;

    public void Add(string key, Func<TItem, double> getter) => accessors.Add(key, new ChartNumberAccessor<TItem>(getter));

    public void Add(string key, Func<TItem, double?> getter) => accessors.Add(key, new ChartNullableNumberAccessor<TItem>(getter));

    public void Add(string key, Func<TItem, DateTime> getter) => accessors.Add(key, new ChartDateTimeAccessor<TItem>(getter));

    public void Add(string key, Func<TItem, DateTimeOffset> getter) => accessors.Add(key, new ChartDateTimeOffsetAccessor<TItem>(getter));

    public void Add(string key, Func<TItem, string?> getter) => accessors.Add(key, new ChartTextAccessor<TItem>(getter));

    public void Add(string key, Func<TItem, Color?> getter) => accessors.Add(key, new ChartColorAccessor<TItem>(getter));

    public bool ContainsKey(string key) => accessors.ContainsKey(key);

    public IEnumerator<string> GetEnumerator() => accessors.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    ChartAccessor? IChartAccessorProvider.FindAccessor(string key) => accessors.GetValueOrDefault(key);
}
