namespace ClamChart.Animation;

using ClamChart.Data;
using ClamChart.Layout;

internal sealed class SeriesInterpolation : IDisposable
{
    private const int Matched = 0;
    private const int Inserted = 1;
    private const int Departing = 2;

    private int[] targets = [];
    private int[] unions = [];
    private int[] kinds = [];
    private bool[] matched = [];
    private double[] fromX = [];
    private double[] fromStart = [];
    private double[] fromEnd = [];
    private double[] toX = [];
    private double[] toStart = [];
    private double[] toEnd = [];
    private double fromOpacity = 1;
    private double toOpacity = 1;

    public SeriesGeometry Display { get; } = new();

    public int TargetCount { get; private set; }

    public double Opacity { get; private set; } = 1;

    public bool IsMoving { get; private set; }

    public void Dispose()
    {
        Display.Dispose();
        Return(ref targets);
        Return(ref unions);
        Return(ref kinds);
        Return(ref matched);
        Return(ref fromX);
        Return(ref fromStart);
        Return(ref fromEnd);
        Return(ref toX);
        Return(ref toStart);
        Return(ref toEnd);
    }

    public int GetUnionIndex(int target) => (target >= 0) && (target < TargetCount) ? unions[target] : -1;

    public int GetTargetIndex(int union) => (union >= 0) && (union < Display.Count) ? targets[union] : -1;

    public void SetIdentity(SeriesGeometry target, double opacity)
    {
        var count = target.Count;
        Prepare(count, count, target);
        var x = target.X;
        var start = target.Start;
        var end = target.End;
        for (var i = 0; i < count; i++)
        {
            targets[i] = i;
            unions[i] = i;
            fromX[i] = x[i];
            toX[i] = x[i];
            fromStart[i] = start[i];
            toStart[i] = start[i];
            fromEnd[i] = end[i];
            toEnd[i] = end[i];
        }

        target.Value.CopyTo(Display.Value);
        target.Size.CopyTo(Display.Size);
        target.Outer.CopyTo(Display.Outer);
        fromOpacity = opacity;
        toOpacity = opacity;
        IsMoving = false;
        Apply(1);
    }

    public void BuildMorph(SeriesInterpolation previous, ReadOnlySpan<int> map, bool identity, SeriesGeometry target, SeriesCollapse collapse)
    {
        var count = target.Count;
        var previousCount = previous.Display.Count;
        EnsureMatched(count);
        var hits = 0;
        for (var u = 0; u < previousCount; u++)
        {
            var index = Map(previous.targets[u], map, identity, count);
            if ((index >= 0) && !matched[index])
            {
                matched[index] = true;
                hits++;
            }
        }

        Prepare(previousCount + (count - hits), count, target);
        var position = 0;
        var nextNew = 0;
        var display = previous.Display;
        for (var u = 0; u < previousCount; u++)
        {
            var index = Map(previous.targets[u], map, identity, count);
            if ((index >= 0) && (index < nextNew))
            {
                index = -1;
            }

            if (index >= 0)
            {
                for (; nextNew < index; nextNew++)
                {
                    if (!matched[nextNew])
                    {
                        EmitTarget(position++, nextNew, target, Inserted);
                    }
                }

                EmitTarget(position, index, target, Matched);
                SetFrom(position++, display.X[u], display.Start[u], display.End[u]);
                nextNew = index + 1;
            }
            else
            {
                targets[position] = -1;
                kinds[position] = Departing;
                SetFrom(position, display.X[u], display.Start[u], display.End[u]);
                Display.Value[position] = display.Value[u];
                Display.Size[position] = display.Size[u];
                Display.Outer[position] = display.Outer[u];
                position++;
            }
        }

        for (; nextNew < count; nextNew++)
        {
            if (!matched[nextNew])
            {
                EmitTarget(position++, nextNew, target, Inserted);
            }
        }

        Resize(position, target);
        PlaceInserted(collapse);
        PlaceDeparting(collapse);
        fromOpacity = previous.Opacity;
        toOpacity = 1;
        Finish();
    }

    public void BuildHide(SeriesInterpolation previous, SeriesCollapse collapse)
    {
        var display = previous.Display;
        var count = display.Count;
        Prepare(count, 0, display);
        for (var u = 0; u < count; u++)
        {
            targets[u] = -1;
            kinds[u] = Departing;
            SetFrom(u, display.X[u], display.Start[u], display.End[u]);
            toX[u] = display.X[u];
            (toStart[u], toEnd[u]) = Collapse(collapse, display.Start[u], display.End[u]);
        }

        display.Value.CopyTo(Display.Value);
        display.Size.CopyTo(Display.Size);
        display.Outer.CopyTo(Display.Outer);
        fromOpacity = previous.Opacity;
        toOpacity = collapse == SeriesCollapse.None ? 0 : 1;
        Finish();
    }

    public void BuildShow(SeriesGeometry target, SeriesCollapse collapse)
    {
        SetIdentity(target, 1);
        if (collapse == SeriesCollapse.None)
        {
            fromOpacity = 0;
        }
        else
        {
            for (var i = 0; i < target.Count; i++)
            {
                (fromStart[i], fromEnd[i]) = Collapse(collapse, fromStart[i], fromEnd[i]);
            }
        }

        Finish();
    }

    public void Apply(double progress)
    {
        var x = Display.X;
        var start = Display.Start;
        var end = Display.End;
        for (var i = 0; i < x.Length; i++)
        {
            x[i] = ChartProgress.Lerp(fromX[i], toX[i], progress);
            start[i] = ChartProgress.Lerp(fromStart[i], toStart[i], progress);
            end[i] = ChartProgress.Lerp(fromEnd[i], toEnd[i], progress);
        }

        Opacity = ChartProgress.Lerp(fromOpacity, toOpacity, progress);
    }

    private static int Map(int index, ReadOnlySpan<int> map, bool identity, int count)
    {
        if (index < 0)
        {
            return -1;
        }

        if (identity)
        {
            return index < count ? index : -1;
        }

        return index < map.Length ? map[index] : -1;
    }

    private static bool Differs(double from, double to) => !from.Equals(to);

    private static (double Start, double End) Collapse(SeriesCollapse collapse, double start, double end) => collapse switch
    {
        SeriesCollapse.Base => (start, start),
        SeriesCollapse.Middle => ((start + end) / 2, (start + end) / 2),
        _ => (start, end)
    };

    private static void Ensure<T>(ref T[] buffer, int count)
    {
        if (buffer.Length < count)
        {
            Return(ref buffer);
            buffer = DataSnapshot.Rent<T>(count);
        }
    }

    private static void Return<T>(ref T[] buffer)
    {
        DataSnapshot.Return(buffer);
        buffer = [];
    }

    private void EnsureMatched(int count)
    {
        Ensure(ref matched, count);
        Array.Clear(matched, 0, count);
    }

    private void Prepare(int unionCount, int targetCount, SeriesGeometry shape)
    {
        Ensure(ref targets, unionCount);
        Ensure(ref kinds, unionCount);
        Ensure(ref fromX, unionCount);
        Ensure(ref fromStart, unionCount);
        Ensure(ref fromEnd, unionCount);
        Ensure(ref toX, unionCount);
        Ensure(ref toStart, unionCount);
        Ensure(ref toEnd, unionCount);
        Ensure(ref unions, targetCount);
        Array.Fill(unions, -1, 0, targetCount);
        TargetCount = targetCount;
        Display.Reset(unionCount);
        Display.CopyShape(shape);
    }

    private void Resize(int count, SeriesGeometry shape)
    {
        if (count != Display.Count)
        {
            Display.Reset(count);
            Display.CopyShape(shape);
        }
    }

    private void EmitTarget(int position, int index, SeriesGeometry target, int kind)
    {
        targets[position] = index;
        unions[index] = position;
        kinds[position] = kind;
        toX[position] = target.X[index];
        toStart[position] = target.Start[index];
        toEnd[position] = target.End[index];
        Display.Value[position] = target.Value[index];
        Display.Size[position] = target.Size[index];
        Display.Outer[position] = target.Outer[index];
    }

    private void SetFrom(int position, double x, double start, double end)
    {
        fromX[position] = x;
        fromStart[position] = start;
        fromEnd[position] = end;
    }

    private void PlaceInserted(SeriesCollapse collapse)
    {
        var count = Display.Count;
        for (var k = 0; k < count; k++)
        {
            if (kinds[k] != Inserted)
            {
                continue;
            }

            if (collapse != SeriesCollapse.None)
            {
                var (start, end) = Collapse(collapse, toStart[k], toEnd[k]);
                SetFrom(k, toX[k], start, end);
                continue;
            }

            var source = k > 0 ? k - 1 : FindNext(k);
            if (source < 0)
            {
                SetFrom(k, toX[k], toStart[k], toEnd[k]);
            }
            else
            {
                SetFrom(k, fromX[source], fromStart[source], fromEnd[source]);
            }
        }
    }

    private void PlaceDeparting(SeriesCollapse collapse)
    {
        var count = Display.Count;
        var previousKept = -1;
        for (var k = 0; k < count; k++)
        {
            if (kinds[k] != Departing)
            {
                previousKept = k;
                continue;
            }

            var neighbor = FindKept(k + 1, 1);
            if (neighbor < 0)
            {
                neighbor = previousKept;
            }

            var shift = neighbor >= 0 ? toX[neighbor] - fromX[neighbor] : 0;
            if ((previousKept < 0) || (collapse != SeriesCollapse.None))
            {
                toX[k] = fromX[k] + shift;
                (toStart[k], toEnd[k]) = previousKept < 0 ? (fromStart[k], fromEnd[k]) : Collapse(collapse, fromStart[k], fromEnd[k]);
            }
            else
            {
                toX[k] = toX[previousKept];
                toStart[k] = toStart[previousKept];
                toEnd[k] = toEnd[previousKept];
            }
        }
    }

    private int FindNext(int position)
    {
        for (var k = position + 1; k < Display.Count; k++)
        {
            if (kinds[k] != Inserted)
            {
                return k;
            }
        }

        return -1;
    }

    private int FindKept(int position, int step)
    {
        for (var k = position; (k >= 0) && (k < Display.Count); k += step)
        {
            if (kinds[k] != Departing)
            {
                return k;
            }
        }

        return -1;
    }

    private void Finish()
    {
        var moving = Differs(fromOpacity, toOpacity);
        for (var i = 0; (i < Display.Count) && !moving; i++)
        {
            moving = Differs(fromX[i], toX[i]) || Differs(fromStart[i], toStart[i]) || Differs(fromEnd[i], toEnd[i]);
        }

        IsMoving = moving;
        Apply(0);
    }
}
