namespace ClamChart.Layout;

internal static class LabelThinning
{
    public static void Select(ReadOnlySpan<LabelExtent> labels, double gap, int maxCount, List<int> selected)
    {
        selected.Clear();
        if ((labels.Length == 0) || (maxCount <= 0))
        {
            return;
        }

        var last = labels.Length - 1;
        if ((last == 0) || (maxCount == 1))
        {
            selected.Add(0);
            return;
        }

        var stride = 1;
        while ((stride <= last) && !TrySelect(labels, gap, maxCount, stride, selected))
        {
            stride++;
        }

        if (stride > last)
        {
            selected.Clear();
            selected.Add(0);
            return;
        }

        if ((last % stride) == 0)
        {
            return;
        }

        var limit = Math.Min(stride * 2, last);
        for (var even = stride + 1; even <= limit; even++)
        {
            if (((last % even) == 0) && TrySelect(labels, gap, maxCount, even, selected))
            {
                return;
            }
        }

        TrySelect(labels, gap, maxCount, stride, selected);
    }

    private static bool TrySelect(ReadOnlySpan<LabelExtent> labels, double gap, int maxCount, int stride, List<int> selected)
    {
        selected.Clear();
        var last = labels.Length - 1;
        for (var index = 0; index < last; index += stride)
        {
            selected.Add(index);
        }

        if ((selected.Count > 1) && ((selected.Count >= maxCount) || Overlaps(labels[selected[^1]], labels[last], gap)))
        {
            selected[^1] = last;
        }
        else
        {
            selected.Add(last);
        }

        if (selected.Count > maxCount)
        {
            return false;
        }

        for (var i = 1; i < selected.Count; i++)
        {
            if (Overlaps(labels[selected[i - 1]], labels[selected[i]], gap))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Overlaps(LabelExtent first, LabelExtent second, double gap) =>
        (Math.Max(first.Start, second.Start) - Math.Min(first.End, second.End)) < gap;
}
