namespace ClamChart.Data;

internal abstract class ChartAccessor
{
    public abstract ChartValueKind Kind { get; }

    public abstract Type ItemType { get; }

    public abstract Type ValueType { get; }

    public virtual double GetNumber(object? item) => throw CreateKindException();

    public virtual double GetTime(object? item) => throw CreateKindException();

    public virtual string? GetText(object? item) => throw CreateKindException();

    public virtual Color? GetColor(object? item) => throw CreateKindException();

    protected static TItem Cast<TItem>(object item)
        where TItem : class =>
        item as TItem ?? throw new InvalidOperationException($"The item of type {item.GetType().Name} does not match the accessor item type {typeof(TItem).Name}.");

    private InvalidOperationException CreateKindException() => new($"The accessor returns {ValueType.Name}.");
}
