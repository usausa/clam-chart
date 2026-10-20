namespace ClamChart.Data;

using System.Collections;

internal static class DataSnapshotBuilder
{
    private const int InitialCapacity = 16;

    public static DataSnapshot Build(IEnumerable? source, ChartValueReader reader, ChartEmptyPointMode emptyPointMode)
    {
        if (source is null)
        {
            return DataSnapshot.Empty;
        }

        if (reader.ReadsValuesDirectly)
        {
            if (source is IReadOnlyList<double> values)
            {
                return BuildValues(values, emptyPointMode);
            }

            if (source is IReadOnlyList<double?> nullableValues)
            {
                return BuildValues(nullableValues, emptyPointMode);
            }
        }

        return BuildItems(source, reader, emptyPointMode);
    }

    private static DataSnapshot BuildValues(IReadOnlyList<double> values, ChartEmptyPointMode emptyPointMode)
    {
        var count = values.Count;
        var x = DataSnapshot.Rent<double>(count);
        var y = DataSnapshot.Rent<double>(count);
        for (var i = 0; i < count; i++)
        {
            x[i] = i;
            y[i] = values[i];
        }

        ApplyEmptyPoints(x, y, count, emptyPointMode);
        return new DataSnapshot(count, ChartXKind.Index, x, y, null, null, null, null);
    }

    private static DataSnapshot BuildValues(IReadOnlyList<double?> values, ChartEmptyPointMode emptyPointMode)
    {
        var count = values.Count;
        var x = DataSnapshot.Rent<double>(count);
        var y = DataSnapshot.Rent<double>(count);
        for (var i = 0; i < count; i++)
        {
            x[i] = i;
            y[i] = values[i] ?? Double.NaN;
        }

        ApplyEmptyPoints(x, y, count, emptyPointMode);
        return new DataSnapshot(count, ChartXKind.Index, x, y, null, null, null, null);
    }

    private static DataSnapshot BuildItems(IEnumerable source, ChartValueReader reader, ChartEmptyPointMode emptyPointMode)
    {
        var items = Collect(source, out var count);
        var sample = FindSample(items, count);
        var kind = reader.ResolveXKind(sample);
        var entries = sample is ChartEntry;
        var x = DataSnapshot.Rent<double>(count);
        var y = DataSnapshot.Rent<double>(count);
        var categories = kind == ChartXKind.Category ? DataSnapshot.Rent<string?>(count) : null;
        var labels = (reader.HasLabelSource || entries) ? DataSnapshot.Rent<string?>(count) : null;
        var colors = (reader.HasColorSource || entries) ? DataSnapshot.Rent<Color?>(count) : null;
        var snapshot = new DataSnapshot(count, kind, x, y, items, categories, labels, colors);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var item = items[i];
                x[i] = reader.ReadX(item, i, kind);
                y[i] = reader.ReadY(item);
                categories?[i] = reader.ReadCategory(item);
                labels?[i] = reader.ReadLabel(item);
                colors?[i] = reader.ReadColor(item);
            }
        }
        catch
        {
            snapshot.Dispose();
            throw;
        }

        ApplyEmptyPoints(x, y, count, emptyPointMode);
        return snapshot;
    }

    private static object?[] Collect(IEnumerable source, out int count)
    {
        if (source is IList list)
        {
            count = list.Count;
            var buffer = DataSnapshot.Rent<object?>(count);
            for (var i = 0; i < count; i++)
            {
                buffer[i] = list[i];
            }

            return buffer;
        }

        var items = DataSnapshot.Rent<object?>(InitialCapacity);
        count = 0;
        foreach (var item in source)
        {
            if (count == items.Length)
            {
                var larger = DataSnapshot.Rent<object?>(items.Length * 2);
                Array.Copy(items, larger, count);
                DataSnapshot.Return(items);
                items = larger;
            }

            items[count++] = item;
        }

        return items;
    }

    private static object? FindSample(object?[] items, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (items[i] is { } item)
            {
                return item;
            }
        }

        return null;
    }

    private static void ApplyEmptyPoints(double[] x, double[] y, int count, ChartEmptyPointMode emptyPointMode)
    {
        if (emptyPointMode == ChartEmptyPointMode.Zero)
        {
            for (var i = 0; i < count; i++)
            {
                if (Double.IsNaN(y[i]))
                {
                    y[i] = 0;
                }
            }
        }
        else if (emptyPointMode == ChartEmptyPointMode.Interpolate)
        {
            var previous = -1;
            for (var i = 0; i < count; i++)
            {
                if (Double.IsNaN(y[i]) || Double.IsNaN(x[i]))
                {
                    continue;
                }

                if ((previous >= 0) && (i - previous > 1))
                {
                    Interpolate(x, y, previous, i);
                }

                previous = i;
            }
        }
    }

    private static void Interpolate(double[] x, double[] y, int start, int end)
    {
        var span = x[end] - x[start];
        for (var i = start + 1; i < end; i++)
        {
            if (Double.IsNaN(x[i]))
            {
                continue;
            }

            var ratio = Math.Abs(span) > 0 ? (x[i] - x[start]) / span : (double)(i - start) / (end - start);
            y[i] = y[start] + ((y[end] - y[start]) * ratio);
        }
    }
}
