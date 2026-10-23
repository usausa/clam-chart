namespace ClamChart.Layout;

internal static class CartesianHitMap
{
    public const double PointRadius = 22;

    public static ChartHit HitTest(CartesianLayout layout, IReadOnlyList<CartesianSeries> series, double x, double y)
    {
        var legend = HitLegend(layout.Legend, x, y);
        if (legend >= 0)
        {
            return new ChartHit(ChartHitKind.Legend, legend);
        }

        var column = HitLegendHeader(layout.Legend, x, y);
        if (column >= 0)
        {
            return new ChartHit(ChartHitKind.LegendHeader, -1, column);
        }

        var plot = layout.PlotBounds;
        if (plot.IsEmpty)
        {
            return ChartHit.None;
        }

        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        var along = horizontal ? y : x;
        var across = horizontal ? x : y;
        if ((x >= plot.X - PointRadius) && (x <= plot.Right + PointRadius) && (y >= plot.Y - PointRadius) && (y <= plot.Bottom + PointRadius))
        {
            var (scatter, scatterIndex) = FindNearestPoint(layout, series, x, y, PointRadius);
            if (scatter >= 0)
            {
                return new ChartHit(ChartHitKind.Item, scatter, scatterIndex);
            }

            var point = FindPoint(layout, series, along, across);
            if (point.Kind == ChartHitKind.Item)
            {
                return point;
            }
        }

        if (!plot.Contains(x, y))
        {
            return ChartHit.None;
        }

        var bar = FindBar(layout, series, along, across);
        if (bar.Kind == ChartHitKind.Item)
        {
            return bar;
        }

        var area = FindArea(layout, series, along, across);
        return area.Kind == ChartHitKind.Item ? area : new ChartHit(ChartHitKind.Plot);
    }

    public static (int Series, int Index) FindNearestPoint(CartesianLayout layout, IReadOnlyList<CartesianSeries> series, double x, double y, double radius)
    {
        var horizontal = layout.Orientation == ChartOrientation.Horizontal;
        var result = (-1, -1);
        var nearest = Double.PositiveInfinity;
        for (var s = series.Count - 1; s >= 0; s--)
        {
            var item = series[s];
            if (!item.IsVisible || (item.Kind != ChartSeriesKind.Scatter))
            {
                continue;
            }

            var geometry = layout.Geometries[s];
            var axis = layout.GetValueAxis(item.Axis);
            for (var i = 0; i < geometry.Count; i++)
            {
                var size = geometry.Size[i];
                if (!Double.IsFinite(geometry.X[i]) || !Double.IsFinite(geometry.End[i]) || (item.IsBubble && Double.IsNaN(size)))
                {
                    continue;
                }

                var along = layout.XAxis.ToPixel(geometry.X[i]);
                var across = axis.ToPixel(geometry.End[i]);
                var dx = (horizontal ? across : along) - x;
                var dy = (horizontal ? along : across) - y;
                var distance = (dx * dx) + (dy * dy);
                var reach = item.IsBubble ? Math.Max(radius, size / 2) : radius;
                if ((distance <= reach * reach) && (distance <= nearest))
                {
                    nearest = distance;
                    result = (s, i);
                }
            }
        }

        return result;
    }

    public static int FindNearestIndex(CartesianLayout layout, int seriesIndex, double along) =>
        FindNearestIndexAt(layout, seriesIndex, layout.XAxis.ToValue(along));

    public static int FindNearestIndexAt(CartesianLayout layout, int seriesIndex, double value)
    {
        var x = layout.Geometries[seriesIndex].X;
        if ((x.Length == 0) || !Double.IsFinite(value))
        {
            return -1;
        }

        var index = layout.Geometries[seriesIndex].IsSorted ? FindNearestSorted(x, value) : FindNearestUnsorted(x, value);
        if (index < 0)
        {
            return -1;
        }

        var limit = layout.XAxis.Options.Kind == ChartAxisKind.Category ? 0.5 : GetHalfGap(x, index, value);
        return Math.Abs(x[index] - value) <= limit ? index : -1;
    }

    private static int HitLegend(LegendLayout legend, double x, double y)
    {
        if (!legend.IsVisible)
        {
            return -1;
        }

        var items = legend.Items;
        var gapX = legend.IsTable ? 0 : LegendLayout.ItemGap / 2;
        var gapY = legend.IsTable ? 0 : LegendLayout.RowGap / 2;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var bounds = item.Bounds;
            if ((x >= bounds.X - gapX) && (x < bounds.Right + gapX) && (y >= bounds.Y - gapY) && (y < bounds.Bottom + gapY))
            {
                return item.SeriesIndex;
            }
        }

        return -1;
    }

    private static int HitLegendHeader(LegendLayout legend, double x, double y)
    {
        if (!legend.IsVisible || !legend.IsTable)
        {
            return -1;
        }

        var columns = legend.HeaderColumns;
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].Contains(x, y))
            {
                return i;
            }
        }

        return -1;
    }

    private static ChartHit FindPoint(CartesianLayout layout, IReadOnlyList<CartesianSeries> series, double along, double across)
    {
        var hit = ChartHit.None;
        var nearest = Double.PositiveInfinity;
        for (var s = series.Count - 1; s >= 0; s--)
        {
            var item = series[s];
            if (!item.IsVisible || (item.Kind is ChartSeriesKind.Bar or ChartSeriesKind.RangeBar or ChartSeriesKind.Scatter))
            {
                continue;
            }

            var index = FindNearestIndex(layout, s, along);
            if (index < 0)
            {
                continue;
            }

            var value = layout.Geometries[s].End[index];
            if (!Double.IsFinite(value))
            {
                continue;
            }

            var distance = Math.Abs(across - layout.GetValueAxis(item.Axis).ToPixel(value));
            if ((distance <= PointRadius) && (distance < nearest))
            {
                nearest = distance;
                hit = new ChartHit(ChartHitKind.Item, s, index);
            }
        }

        return hit;
    }

    private static ChartHit FindBar(CartesianLayout layout, IReadOnlyList<CartesianSeries> series, double along, double across)
    {
        if (layout.BarSlotCount == 0)
        {
            return ChartHit.None;
        }

        var value = layout.XAxis.ToValue(along);
        var hit = ChartHit.None;
        var nearest = Double.PositiveInfinity;
        for (var s = series.Count - 1; s >= 0; s--)
        {
            var item = series[s];
            var geometry = layout.Geometries[s];
            if ((item.Kind is not (ChartSeriesKind.Bar or ChartSeriesKind.RangeBar)) || !item.IsVisible || (geometry.BarSlot < 0))
            {
                continue;
            }

            var index = FindColumn(layout, geometry, value);
            if (index < 0)
            {
                continue;
            }

            var (slotStart, slotEnd) = geometry.GetBarHitRange(geometry.X[index]);
            if ((value < slotStart) || (value >= slotEnd) || !Double.IsFinite(geometry.Start[index]) || !Double.IsFinite(geometry.End[index]))
            {
                continue;
            }

            var axis = layout.GetValueAxis(item.Axis);
            var start = axis.ToPixel(geometry.Start[index]);
            var end = axis.ToPixel(geometry.End[index]);
            var distance = (across >= Math.Min(start, end)) && (across <= Math.Max(start, end)) ? 0 : Math.Min(Math.Abs(across - start), Math.Abs(across - end));
            if (distance < nearest)
            {
                nearest = distance;
                hit = new ChartHit(ChartHitKind.Item, s, index);
            }
        }

        return hit;
    }

    private static ChartHit FindArea(CartesianLayout layout, IReadOnlyList<CartesianSeries> series, double along, double across)
    {
        var value = layout.XAxis.ToValue(along);
        for (var s = series.Count - 1; s >= 0; s--)
        {
            var item = series[s];
            var geometry = layout.Geometries[s];
            if ((item.Kind != ChartSeriesKind.Area) || !item.IsVisible || !geometry.IsSorted || (geometry.Count == 0))
            {
                continue;
            }

            var x = geometry.X;
            var next = x.BinarySearch(value);
            var previous = next;
            if (next < 0)
            {
                next = ~next;
                previous = next - 1;
                if ((previous < 0) || (next >= x.Length))
                {
                    continue;
                }
            }

            var ratio = x[next] > x[previous] ? (value - x[previous]) / (x[next] - x[previous]) : 0;
            var start = Lerp(geometry.Start[previous], geometry.Start[next], ratio);
            var end = Lerp(geometry.End[previous], geometry.End[next], ratio);
            if (!Double.IsFinite(start) || !Double.IsFinite(end))
            {
                continue;
            }

            var axis = layout.GetValueAxis(item.Axis);
            var low = Math.Min(axis.ToPixel(start), axis.ToPixel(end));
            var high = Math.Max(axis.ToPixel(start), axis.ToPixel(end));
            if ((across >= low) && (across <= high))
            {
                return new ChartHit(ChartHitKind.Item, s, ratio < 0.5 ? previous : next);
            }
        }

        return ChartHit.None;
    }

    private static int FindColumn(CartesianLayout layout, SeriesGeometry geometry, double value)
    {
        var x = geometry.X;
        if (x.Length == 0)
        {
            return -1;
        }

        var index = geometry.IsSorted ? FindNearestSorted(x, value) : FindNearestUnsorted(x, value);
        return (index >= 0) && (Math.Abs(x[index] - value) <= layout.BarColumnWidth / 2) ? index : -1;
    }

    private static int FindNearestSorted(ReadOnlySpan<double> x, double value)
    {
        var next = x.BinarySearch(value);
        if (next >= 0)
        {
            return next;
        }

        next = ~next;
        if (next == 0)
        {
            return 0;
        }

        if (next == x.Length)
        {
            return x.Length - 1;
        }

        return value - x[next - 1] <= x[next] - value ? next - 1 : next;
    }

    private static int FindNearestUnsorted(ReadOnlySpan<double> x, double value)
    {
        var index = -1;
        var nearest = Double.PositiveInfinity;
        for (var i = 0; i < x.Length; i++)
        {
            var distance = Math.Abs(x[i] - value);
            if (distance < nearest)
            {
                nearest = distance;
                index = i;
            }
        }

        return index;
    }

    private static double GetHalfGap(ReadOnlySpan<double> x, int index, double value)
    {
        var after = index + 1 < x.Length ? x[index + 1] - x[index] : Double.NaN;
        var before = index > 0 ? x[index] - x[index - 1] : Double.NaN;
        var gap = value >= x[index] ? after : before;
        if (!(gap > 0))
        {
            gap = value >= x[index] ? before : after;
        }

        return gap > 0 ? gap / 2 : Double.PositiveInfinity;
    }

    private static double Lerp(double from, double to, double ratio) => from + ((to - from) * ratio);
}
