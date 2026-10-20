namespace ClamChart.Tests.Layout;

internal sealed class TestSeriesSet : IDisposable
{
    private static readonly ChartValueReader Reader = new(new ChartValueOptions());

    private readonly List<CartesianSeries> series = [];
    private readonly List<SeriesGeometry> geometries = [];

    public IReadOnlyList<CartesianSeries> Series => series;

    public CartesianSeries this[int index] => series[index];

    public void Dispose()
    {
        foreach (var item in series)
        {
            item.Data.Dispose();
        }

        foreach (var geometry in geometries)
        {
            geometry.Dispose();
        }
    }

    public CartesianSeries AddValues(ChartSeriesKind kind, double[] values, string? stackGroup = null, ChartAxisTarget axis = ChartAxisTarget.Primary, bool visible = true, string? title = null) =>
        Add(DataSnapshotBuilder.Build(values, Reader, ChartEmptyPointMode.Gap), kind, stackGroup, axis, visible, title);

    public CartesianSeries AddEntries(ChartSeriesKind kind, (string? Label, double Value)[] entries, string? stackGroup = null, ChartAxisTarget axis = ChartAxisTarget.Primary, bool visible = true, string? title = null) =>
        Add(DataSnapshotBuilder.Build(entries.Select(static x => new ChartEntry(x.Label, x.Value)).ToList(), Reader, ChartEmptyPointMode.Gap), kind, stackGroup, axis, visible, title);

    public CartesianSeries AddPoints(ChartSeriesKind kind, (double X, double Y)[] points, string? stackGroup = null, ChartAxisTarget axis = ChartAxisTarget.Primary, bool visible = true, string? title = null) =>
        Add(DataSnapshotBuilder.Build(points.Select(static x => new ChartPoint(x.X, x.Y)).ToList(), Reader, ChartEmptyPointMode.Gap), kind, stackGroup, axis, visible, title);

    public CartesianSeries AddTimes(ChartSeriesKind kind, (DateTime X, double Y)[] points, string? title = null) =>
        Add(DataSnapshotBuilder.Build(points.Select(static x => new ChartTimePoint(x.X, x.Y)).ToList(), Reader, ChartEmptyPointMode.Gap), kind, null, ChartAxisTarget.Primary, true, title);

    public void Replace(int index, CartesianSeries item) => series[index] = item;

    public IReadOnlyList<SeriesGeometry> CreateGeometries()
    {
        foreach (var item in series)
        {
            var geometry = new SeriesGeometry();
            geometries.Add(geometry);
            geometry.Reset(item.Data.Count);
            item.Data.X.CopyTo(geometry.X);
        }

        return geometries;
    }

    private CartesianSeries Add(DataSnapshot data, ChartSeriesKind kind, string? stackGroup, ChartAxisTarget axis, bool visible, string? title)
    {
        var item = new CartesianSeries { Data = data, Kind = kind, StackGroup = stackGroup, Axis = axis, IsVisible = visible, Title = title };
        series.Add(item);
        return item;
    }
}
