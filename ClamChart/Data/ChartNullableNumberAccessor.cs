namespace ClamChart.Data;

internal sealed class ChartNullableNumberAccessor<TItem>(Func<TItem, double?> getter) : ChartAccessor
    where TItem : class
{
    public override ChartValueKind Kind => ChartValueKind.Number;

    public override Type ItemType => typeof(TItem);

    public override Type ValueType => typeof(double?);

    public override double GetNumber(object? item) => item is null ? Double.NaN : getter(Cast<TItem>(item)) ?? Double.NaN;
}
