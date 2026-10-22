namespace ClamChart.Data;

using System.Buffers;
using System.Runtime.CompilerServices;

internal sealed class DataSnapshot : IDisposable
{
    private double[]? x;
    private double[]? y;
    private object?[]? items;
    private string?[]? categories;
    private string?[]? labels;
    private Color?[]? colors;
    private double[]? maximums;
    private double[]? targets;
    private double[]? lows;
    private double[]? sizes;

    public static DataSnapshot Empty { get; } = new(0, ChartXKind.Index, null, null, null, null, null, null);

    public int Count { get; private set; }

    public ChartXKind XKind { get; }

    public ReadOnlySpan<double> X => x.AsSpan(0, Count);

    public ReadOnlySpan<double> Y => y.AsSpan(0, Count);

    public ReadOnlySpan<double> Low => lows is null ? default : lows.AsSpan(0, Count);

    public ReadOnlySpan<double> Size => sizes is null ? default : sizes.AsSpan(0, Count);

    public bool HasCategories => categories is not null;

    public bool HasLabels => labels is not null;

    public bool HasColors => colors is not null;

    public DataSnapshot(int count, ChartXKind xKind, double[]? x, double[]? y, object?[]? items, string?[]? categories, string?[]? labels, Color?[]? colors, double[]? maximums = null, double[]? targets = null, double[]? lows = null, double[]? sizes = null)
    {
        Count = count;
        XKind = xKind;
        this.x = x;
        this.y = y;
        this.items = items;
        this.categories = categories;
        this.labels = labels;
        this.colors = colors;
        this.maximums = maximums;
        this.targets = targets;
        this.lows = lows;
        this.sizes = sizes;
    }

    public void Dispose()
    {
        Count = 0;
        Return(ref x);
        Return(ref y);
        Return(ref items);
        Return(ref categories);
        Return(ref labels);
        Return(ref colors);
        Return(ref maximums);
        Return(ref targets);
        Return(ref lows);
        Return(ref sizes);
    }

    public object? GetItem(int index) => items is null ? Y[index] : items[index];

    public object? GetXValue(int index) => XKind switch
    {
        ChartXKind.Category => GetCategory(index),
        ChartXKind.Time => ChartDateTime.FromTicks(X[index]),
        _ => X[index]
    };

    public int IndexOf(object? item)
    {
        if (items is not null)
        {
            for (var i = 0; i < Count; i++)
            {
                if (Equals(items[i], item))
                {
                    return i;
                }
            }
        }
        else if (item is double value)
        {
            var values = Y;
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i].Equals(value))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public string? GetCategory(int index) => categories?[index];

    public string? GetLabel(int index) => labels?[index];

    public Color? GetColor(int index) => colors?[index];

    public double GetMaximum(int index) => maximums is null ? Double.NaN : maximums[index];

    public double GetTarget(int index) => targets is null ? Double.NaN : targets[index];

    internal static T[] Rent<T>(int length) => length == 0 ? [] : ArrayPool<T>.Shared.Rent(length);

    internal static void Return<T>(T[] array)
    {
        if (array.Length > 0)
        {
            ArrayPool<T>.Shared.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }

    private static void Return<T>(ref T[]? array)
    {
        if (array is not null)
        {
            Return(array);
            array = null;
        }
    }
}
