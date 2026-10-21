namespace ClamChart.Rendering;

internal static class LttbDownsampler
{
    public const float PointsPerUnit = 2;

    public static int Reduce(Span<float> along, Span<float> across, Span<float> baseline, int count)
    {
        var write = 0;
        var start = 0;
        while (start < count)
        {
            if (!IsPoint(along, across, start))
            {
                if ((write > 0) && IsPoint(along, across, write - 1))
                {
                    Copy(along, across, baseline, start, write++);
                }

                start++;
                continue;
            }

            var end = start;
            while ((end < count) && IsPoint(along, across, end))
            {
                end++;
            }

            var length = end - start;
            var target = Math.Max(3, (int)Math.Ceiling(Math.Abs(along[end - 1] - along[start]) * PointsPerUnit));
            if (length <= target)
            {
                for (var i = start; i < end; i++)
                {
                    Copy(along, across, baseline, i, write++);
                }
            }
            else
            {
                write = ReduceRun(along, across, baseline, start, length, target, write);
            }

            start = end;
        }

        return write;
    }

    private static bool IsPoint(Span<float> along, Span<float> across, int index) =>
        Single.IsFinite(along[index]) && Single.IsFinite(across[index]);

    private static void Copy(Span<float> along, Span<float> across, Span<float> baseline, int from, int to)
    {
        along[to] = along[from];
        across[to] = across[from];
        baseline[to] = baseline[from];
    }

    private static int ReduceRun(Span<float> along, Span<float> across, Span<float> baseline, int start, int length, int target, int write)
    {
        var last = start + length - 1;
        var bucket = (double)(length - 2) / (target - 2);
        var previousAlong = along[start];
        var previousAcross = across[start];
        Copy(along, across, baseline, start, write++);
        for (var i = 0; i < target - 2; i++)
        {
            var rangeStart = start + 1 + (int)Math.Floor(i * bucket);
            var rangeEnd = Math.Min(start + 1 + (int)Math.Floor((i + 1) * bucket), last);
            var nextEnd = Math.Min(start + 1 + (int)Math.Floor((i + 2) * bucket), last);
            var averageAlong = 0d;
            var averageAcross = 0d;
            if (nextEnd > rangeEnd)
            {
                for (var j = rangeEnd; j < nextEnd; j++)
                {
                    averageAlong += along[j];
                    averageAcross += across[j];
                }

                averageAlong /= nextEnd - rangeEnd;
                averageAcross /= nextEnd - rangeEnd;
            }
            else
            {
                averageAlong = along[last];
                averageAcross = across[last];
            }

            var best = rangeStart;
            var largest = -1d;
            for (var j = rangeStart; j < rangeEnd; j++)
            {
                var area = Math.Abs(((previousAlong - averageAlong) * (across[j] - previousAcross)) - ((previousAlong - along[j]) * (averageAcross - previousAcross)));
                if (area > largest)
                {
                    largest = area;
                    best = j;
                }
            }

            previousAlong = along[best];
            previousAcross = across[best];
            Copy(along, across, baseline, best, write++);
        }

        Copy(along, across, baseline, last, write++);
        return write;
    }
}
