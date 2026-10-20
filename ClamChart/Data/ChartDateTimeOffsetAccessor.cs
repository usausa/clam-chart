namespace ClamChart.Data;

internal sealed class ChartDateTimeOffsetAccessor<TItem>(Func<TItem, DateTimeOffset> getter) : ChartAccessor
    where TItem : class
{
    public override ChartValueKind Kind => ChartValueKind.Time;

    public override Type ItemType => typeof(TItem);

    public override Type ValueType => typeof(DateTimeOffset);

    public override double GetTime(object? item) => item is null ? Double.NaN : ChartDateTime.ToLocalTicks(getter(Cast<TItem>(item)));
}
