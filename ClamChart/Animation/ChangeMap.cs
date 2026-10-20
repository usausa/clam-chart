namespace ClamChart.Animation;

using ClamChart.Data;

internal static class ChangeMap
{
    public static bool TryBuild(ChartChangeLog log, int oldCount, int newCount, Span<int> map)
    {
        if (log.IsReset)
        {
            return false;
        }

        for (var i = 0; i < oldCount; i++)
        {
            map[i] = i;
        }

        var changes = log.Changes;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var c = 0; c < changes.Count; c++)
        {
            var change = changes[c];
            var end = change.Index + change.Count;
            switch (change.Kind)
            {
                case ChartChangeKind.Add:
                    for (var i = 0; i < oldCount; i++)
                    {
                        if (map[i] >= change.Index)
                        {
                            map[i] += change.Count;
                        }
                    }

                    break;
                case ChartChangeKind.Remove:
                    for (var i = 0; i < oldCount; i++)
                    {
                        if (map[i] >= end)
                        {
                            map[i] -= change.Count;
                        }
                        else if (map[i] >= change.Index)
                        {
                            map[i] = -1;
                        }
                    }

                    break;
                case ChartChangeKind.Move:
                    return false;
            }
        }

        for (var i = 0; i < oldCount; i++)
        {
            if (map[i] >= newCount)
            {
                return false;
            }
        }

        return true;
    }

    public static void Compose(Span<int> first, ReadOnlySpan<int> second)
    {
        for (var i = 0; i < first.Length; i++)
        {
            var index = first[i];
            first[i] = (index >= 0) && (index < second.Length) ? second[index] : -1;
        }
    }
}
