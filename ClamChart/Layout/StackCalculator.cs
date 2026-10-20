namespace ClamChart.Layout;

using ClamChart.Data;

internal sealed class StackCalculator : IDisposable
{
    private readonly Dictionary<double, int> slots = [];
    private readonly List<(ChartSeriesKind Kind, ChartAxisTarget Axis, string Group)> groups = [];
    private readonly List<(ChartAxisTarget Axis, string Group, int Slot)> barGroups = [];
    private double[] positive = [];
    private double[] negative = [];
    private double[] positiveTotal = [];
    private double[] negativeTotal = [];
    private int[] positiveSeries = [];
    private int[] positivePoint = [];
    private int[] negativeSeries = [];
    private int[] negativePoint = [];

    public int BarSlotCount { get; private set; }

    public void Dispose()
    {
        Release();
    }

    public void Calculate(IReadOnlyList<CartesianSeries> series, IReadOnlyList<SeriesGeometry> geometries, ChartStackMode mode)
    {
        for (var s = 0; s < series.Count; s++)
        {
            Fill(series[s], geometries[s]);
        }

        groups.Clear();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var s = 0; s < series.Count; s++)
        {
            if ((GetGroup(series[s]) is { } group) && !groups.Contains(group))
            {
                groups.Add(group);
            }
        }

        foreach (var group in groups)
        {
            Stack(series, geometries, group, mode);
        }

        AssignBarSlots(series, geometries);
    }

    private static (ChartSeriesKind Kind, ChartAxisTarget Axis, string Group)? GetGroup(CartesianSeries series) =>
        series.IsVisible && (series.Kind != ChartSeriesKind.Line) && !String.IsNullOrEmpty(series.StackGroup) ? (series.Kind, series.Axis, series.StackGroup) : null;

    private static void Fill(CartesianSeries series, SeriesGeometry geometry)
    {
        var y = series.Data.Y;
        var start = geometry.Start;
        var end = geometry.End;
        var values = geometry.Value;
        var outer = geometry.Outer;
        var baseline = series.Kind == ChartSeriesKind.Area ? series.BaselineValue ?? 0 : 0;
        for (var i = 0; i < end.Length; i++)
        {
            var value = y[i];
            end[i] = value;
            start[i] = Double.IsNaN(value) || (series.Kind == ChartSeriesKind.Line) ? value : baseline;
            values[i] = value;
            outer[i] = true;
        }
    }

    private static double ToPercent(double value, double total) => total > 0 ? value / total * 100 : 0;

    private void Stack(IReadOnlyList<CartesianSeries> series, IReadOnlyList<SeriesGeometry> geometries, (ChartSeriesKind Kind, ChartAxisTarget Axis, string Group) group, ChartStackMode mode)
    {
        slots.Clear();
        for (var s = 0; s < series.Count; s++)
        {
            if (GetGroup(series[s]) == group)
            {
                foreach (var x in geometries[s].X)
                {
                    if (Double.IsFinite(x))
                    {
                        slots.TryAdd(x, slots.Count);
                    }
                }
            }
        }

        Prepare(slots.Count);
        if (mode == ChartStackMode.Percent)
        {
            for (var s = 0; s < series.Count; s++)
            {
                if (GetGroup(series[s]) == group)
                {
                    AddTotals(series[s].Data.Y, geometries[s].X);
                }
            }
        }

        for (var s = 0; s < series.Count; s++)
        {
            if (GetGroup(series[s]) == group)
            {
                Accumulate(series[s].Data.Y, geometries[s], s, mode);
            }
        }

        for (var slot = 0; slot < slots.Count; slot++)
        {
            if (positiveSeries[slot] >= 0)
            {
                geometries[positiveSeries[slot]].Outer[positivePoint[slot]] = true;
            }

            if (negativeSeries[slot] >= 0)
            {
                geometries[negativeSeries[slot]].Outer[negativePoint[slot]] = true;
            }
        }
    }

    private void AddTotals(ReadOnlySpan<double> y, ReadOnlySpan<double> x)
    {
        for (var i = 0; i < x.Length; i++)
        {
            if (Double.IsFinite(x[i]) && Double.IsFinite(y[i]))
            {
                var slot = slots[x[i]];
                if (y[i] > 0)
                {
                    positiveTotal[slot] += y[i];
                }
                else
                {
                    negativeTotal[slot] -= y[i];
                }
            }
        }
    }

    private void Accumulate(ReadOnlySpan<double> y, SeriesGeometry geometry, int seriesIndex, ChartStackMode mode)
    {
        geometry.IsStacked = true;
        var x = geometry.X;
        var start = geometry.Start;
        var end = geometry.End;
        var values = geometry.Value;
        var outer = geometry.Outer;
        for (var i = 0; i < x.Length; i++)
        {
            outer[i] = false;
            var value = y[i];
            if (!Double.IsFinite(x[i]) || !Double.IsFinite(value))
            {
                start[i] = Double.NaN;
                end[i] = Double.NaN;
                values[i] = Double.NaN;
                continue;
            }

            var slot = slots[x[i]];
            if (value >= 0)
            {
                if (mode == ChartStackMode.Percent)
                {
                    value = ToPercent(value, positiveTotal[slot]);
                }

                start[i] = positive[slot];
                positive[slot] += value;
                end[i] = positive[slot];
                if (value > 0)
                {
                    positiveSeries[slot] = seriesIndex;
                    positivePoint[slot] = i;
                }
            }
            else
            {
                if (mode == ChartStackMode.Percent)
                {
                    value = ToPercent(value, negativeTotal[slot]);
                }

                start[i] = negative[slot];
                negative[slot] += value;
                end[i] = negative[slot];
                negativeSeries[slot] = seriesIndex;
                negativePoint[slot] = i;
            }

            values[i] = value;
        }
    }

    private void AssignBarSlots(IReadOnlyList<CartesianSeries> series, IReadOnlyList<SeriesGeometry> geometries)
    {
        BarSlotCount = 0;
        barGroups.Clear();
        for (var s = 0; s < series.Count; s++)
        {
            var item = series[s];
            if ((item.Kind != ChartSeriesKind.Bar) || !item.IsVisible)
            {
                continue;
            }

            if (String.IsNullOrEmpty(item.StackGroup))
            {
                geometries[s].BarSlot = BarSlotCount++;
                continue;
            }

            var slot = FindBarGroup(item.Axis, item.StackGroup);
            if (slot < 0)
            {
                slot = BarSlotCount++;
                barGroups.Add((item.Axis, item.StackGroup, slot));
            }

            geometries[s].BarSlot = slot;
        }
    }

    private int FindBarGroup(ChartAxisTarget axis, string group)
    {
        foreach (var (entryAxis, entryGroup, slot) in barGroups)
        {
            if ((entryAxis == axis) && (entryGroup == group))
            {
                return slot;
            }
        }

        return -1;
    }

    private void Prepare(int count)
    {
        if (positive.Length < count)
        {
            Release();
            positive = DataSnapshot.Rent<double>(count);
            negative = DataSnapshot.Rent<double>(count);
            positiveTotal = DataSnapshot.Rent<double>(count);
            negativeTotal = DataSnapshot.Rent<double>(count);
            positiveSeries = DataSnapshot.Rent<int>(count);
            positivePoint = DataSnapshot.Rent<int>(count);
            negativeSeries = DataSnapshot.Rent<int>(count);
            negativePoint = DataSnapshot.Rent<int>(count);
        }

        Array.Clear(positive, 0, count);
        Array.Clear(negative, 0, count);
        Array.Clear(positiveTotal, 0, count);
        Array.Clear(negativeTotal, 0, count);
        Array.Fill(positiveSeries, -1, 0, count);
        Array.Fill(negativeSeries, -1, 0, count);
    }

    private void Release()
    {
        DataSnapshot.Return(positive);
        DataSnapshot.Return(negative);
        DataSnapshot.Return(positiveTotal);
        DataSnapshot.Return(negativeTotal);
        DataSnapshot.Return(positiveSeries);
        DataSnapshot.Return(positivePoint);
        DataSnapshot.Return(negativeSeries);
        DataSnapshot.Return(negativePoint);
        positive = [];
        negative = [];
        positiveTotal = [];
        negativeTotal = [];
        positiveSeries = [];
        positivePoint = [];
        negativeSeries = [];
        negativePoint = [];
    }
}
