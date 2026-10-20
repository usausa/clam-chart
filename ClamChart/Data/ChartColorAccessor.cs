namespace ClamChart.Data;

internal sealed class ChartColorAccessor<TItem>(Func<TItem, Color?> getter) : ChartAccessor
    where TItem : class
{
    public override ChartValueKind Kind => ChartValueKind.Color;

    public override Type ItemType => typeof(TItem);

    public override Type ValueType => typeof(Color);

    public override Color? GetColor(object? item) => item is null ? null : getter(Cast<TItem>(item));
}
