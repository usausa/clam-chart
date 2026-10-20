namespace ClamChart.Data;

internal sealed class ChartTextAccessor<TItem>(Func<TItem, string?> getter) : ChartAccessor
    where TItem : class
{
    public override ChartValueKind Kind => ChartValueKind.Text;

    public override Type ItemType => typeof(TItem);

    public override Type ValueType => typeof(string);

    public override string? GetText(object? item) => item is null ? null : getter(Cast<TItem>(item));
}
