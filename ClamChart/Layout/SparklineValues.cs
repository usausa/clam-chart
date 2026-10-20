namespace ClamChart.Layout;

internal readonly struct SparklineValues(IReadOnlyList<double>? list, SeriesGeometry? geometry)
{
    public int Count => list?.Count ?? geometry?.Count ?? 0;

    public double this[int index] => list is not null ? list[index] : geometry is not null ? geometry.End[index] : Double.NaN;
}
