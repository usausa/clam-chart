namespace ClamChart.Layout;

using ClamChart.Data;

internal sealed class CategoryMerger
{
    private readonly Dictionary<string, int> indexes = [with(StringComparer.Ordinal)];
    private readonly List<string?> categories = [];
    private int nullIndex = -1;

    public IReadOnlyList<string?> Categories => categories;

    public void Merge(IReadOnlyList<CartesianSeries> series, IReadOnlyList<SeriesGeometry> geometries)
    {
        indexes.Clear();
        categories.Clear();
        nullIndex = -1;
        for (var s = 0; s < series.Count; s++)
        {
            var data = series[s].Data;
            if (data.XKind == ChartXKind.Category)
            {
                var x = geometries[s].X;
                for (var i = 0; i < x.Length; i++)
                {
                    x[i] = GetIndex(data.GetCategory(i));
                }
            }
        }

        for (var s = 0; s < series.Count; s++)
        {
            if (series[s].Data.XKind != ChartXKind.Category)
            {
                var x = geometries[s].X;
                for (var i = 0; i < x.Length; i++)
                {
                    x[i] = i;
                }

                while (categories.Count < x.Length)
                {
                    categories.Add(null);
                }
            }
        }
    }

    private int GetIndex(string? category)
    {
        if (category is null)
        {
            if (nullIndex < 0)
            {
                nullIndex = categories.Count;
                categories.Add(null);
            }

            return nullIndex;
        }

        if (!indexes.TryGetValue(category, out var index))
        {
            index = categories.Count;
            indexes.Add(category, index);
            categories.Add(category);
        }

        return index;
    }
}
