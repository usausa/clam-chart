namespace ClamChart.Data;

internal sealed class ChartValueReader
{
    private readonly ChartAccessor? x;
    private readonly ChartAccessor? y;
    private readonly ChartAccessor? label;
    private readonly ChartAccessor? color;
    private readonly Func<object, double?>? valueSelector;
    private readonly Func<object, string?>? labelSelector;

    public bool ReadsValuesDirectly => (valueSelector is null) && (y is null);

    public bool HasLabelSource => (labelSelector is not null) || (label is not null);

    public bool HasColorSource => color is not null;

    public ChartValueReader(ChartValueOptions options)
    {
        x = Resolve(options.Accessors, options.XKey, "X", static kind => kind != ChartValueKind.Color);
        y = Resolve(options.Accessors, options.YKey, "Y", static kind => kind == ChartValueKind.Number);
        label = Resolve(options.Accessors, options.LabelKey, "the label", static kind => kind == ChartValueKind.Text);
        color = Resolve(options.Accessors, options.ColorKey, "the color", static kind => kind == ChartValueKind.Color);
        valueSelector = options.ValueSelector;
        labelSelector = options.LabelSelector;
    }

    public ChartXKind ResolveXKind(object? sample)
    {
        if (x is not null)
        {
            return x.Kind switch
            {
                ChartValueKind.Number => ChartXKind.Number,
                ChartValueKind.Time => ChartXKind.Time,
                _ => ChartXKind.Category
            };
        }

        return sample switch
        {
            ChartPoint => ChartXKind.Number,
            ChartTimePoint => ChartXKind.Time,
            ChartEntry => ChartXKind.Category,
            _ => ChartXKind.Index
        };
    }

    public double ReadY(object? item)
    {
        if (item is null)
        {
            return Double.NaN;
        }

        if (valueSelector is not null)
        {
            return valueSelector(item) ?? Double.NaN;
        }

        if (y is not null)
        {
            return y.GetNumber(item);
        }

        return item switch
        {
            ChartPoint point => point.Y,
            ChartTimePoint point => point.Y,
            ChartEntry entry => entry.Value,
            double value => value,
            float value => value,
            int value => value,
            long value => value,
            short value => value,
            decimal value => (double)value,
            _ => throw new InvalidOperationException($"Items of type {item.GetType().Name} need a ValueSelector or ValueAccessors with a key.")
        };
    }

    public double ReadX(object? item, int index, ChartXKind kind)
    {
        if (kind is ChartXKind.Index or ChartXKind.Category)
        {
            return index;
        }

        if (item is null)
        {
            return Double.NaN;
        }

        if (x is not null)
        {
            return kind == ChartXKind.Number ? x.GetNumber(item) : x.GetTime(item);
        }

        return (kind, item) switch
        {
            (ChartXKind.Number, ChartPoint point) => point.X,
            (ChartXKind.Time, ChartTimePoint point) => ChartDateTime.ToLocalTicks(point.X),
            _ => throw CreateXMismatchException(item)
        };
    }

    public string? ReadCategory(object? item)
    {
        if (item is null)
        {
            return null;
        }

        if (x is not null)
        {
            return x.GetText(item);
        }

        return item is ChartEntry entry ? entry.Label : throw CreateXMismatchException(item);
    }

    public string? ReadLabel(object? item)
    {
        if (item is null)
        {
            return null;
        }

        if (labelSelector is not null)
        {
            return labelSelector(item);
        }

        return label is not null ? label.GetText(item) : (item as ChartEntry)?.Label;
    }

    public Color? ReadColor(object? item)
    {
        if (item is null)
        {
            return null;
        }

        return color is not null ? color.GetColor(item) : (item as ChartEntry)?.Color;
    }

    private static ChartAccessor? Resolve(IChartAccessorProvider? provider, string? key, string role, Func<ChartValueKind, bool> accepts)
    {
        if (key is null)
        {
            return null;
        }

        if (provider is null)
        {
            throw new InvalidOperationException($"The key \"{key}\" for {role} needs ValueAccessors.");
        }

        var accessor = provider.FindAccessor(key) ?? throw new InvalidOperationException($"No accessor is registered for the key \"{key}\".");
        if (!accepts(accessor.Kind))
        {
            throw new InvalidOperationException($"The accessor for the key \"{key}\" returns {accessor.ValueType.Name} and cannot be used for {role}.");
        }

        return accessor;
    }

    private static InvalidOperationException CreateXMismatchException(object item) =>
        new($"The item of type {item.GetType().Name} does not match the X values of the other items.");
}
