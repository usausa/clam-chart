namespace ClamChart.Animation;

using ClamChart.Data;
using ClamChart.Layout;

internal sealed class PieInterpolator : IDisposable
{
    public const int OthersKey = -2;

    public const int DepartingKey = -1;

    private const double Tolerance = 1e-9;

    private PieEntries current = new();
    private PieEntries next = new();
    private int[] keys = [];
    private int[] matches = [];
    private bool[] used = [];
    private ChartEntranceAnimation entrance;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool IsMorphing { get; private set; }

    public bool IsEntrance { get; private set; }

    public double Progress { get; private set; } = 1;

    public int Count => current.Count;

    public ChartEntranceAnimation Entrance => IsEntrance ? entrance : ChartEntranceAnimation.None;

    public double OverlayAlpha => IsEntrance ? Ease(ChartProgress.Delayed(Progress, ChartProgress.OverlayDelay)) : 1;

    public double EntranceAlpha => IsEntrance && (entrance == ChartEntranceAnimation.Fade) ? Ease(Progress) : 1;

    public double RevealLimit => IsEntrance && (entrance == ChartEntranceAnimation.Reveal) ? Ease(Progress) : 1;

    public double ColorProgress => IsMorphing ? Ease(Progress) : 1;

    public void Dispose()
    {
        current.Dispose();
        next.Dispose();
        Return(ref keys);
        Return(ref matches);
        Return(ref used);
    }

    public double Ease(double progress) => Easing.Ease(progress);

    public (double Start, double End) GetSlice(int index)
    {
        var start = current.Start[index];
        var end = current.End[index];
        if (IsEntrance && (entrance == ChartEntranceAnimation.Grow))
        {
            var eased = Ease(Progress);
            return (start * eased, end * eased);
        }

        return (start, end);
    }

    public int GetKey(int index) => current.Key[index];

    public int GetTarget(int index) => current.Target[index];

    public PieColorSource GetColor(int index) => current.ToColor[index];

    public PieColorSource GetPreviousColor(int index) => current.FromColor[index];

    public int FindKey(int key)
    {
        if (key < 0)
        {
            return -1;
        }

        for (var i = 0; i < current.Count; i++)
        {
            if (current.Key[i] == key)
            {
                return i;
            }
        }

        return -1;
    }

    public void Capture(PieLayout layout)
    {
        if (!IsMorphing && !IsEntrance)
        {
            current.Fill(layout.Slices);
        }
    }

    public bool Begin(PieLayout target, ReadOnlySpan<int> map, bool positional, int newCount)
    {
        IsEntrance = false;
        var source = current;
        var slices = target.Slices;
        Ensure(ref keys, source.Count);
        Ensure(ref used, source.Count);
        Ensure(ref matches, slices.Count);
        for (var d = 0; d < source.Count; d++)
        {
            keys[d] = Remap(source.Key[d], map, positional, newCount);
            used[d] = false;
        }

        for (var t = 0; t < slices.Count; t++)
        {
            matches[t] = -1;
            var key = GetKey(slices[t]);
            for (var d = 0; d < source.Count; d++)
            {
                if (!used[d] && (keys[d] != DepartingKey) && (keys[d] == key))
                {
                    matches[t] = d;
                    used[d] = true;
                    break;
                }
            }
        }

        var built = next;
        built.Reset(source.Count + slices.Count);
        var fromEnd = 0d;
        var toEnd = 0d;
        var last = -1;
        for (var t = 0; t < slices.Count; t++)
        {
            var slice = slices[t];
            var color = slice.ColorSource;
            var d = matches[t];
            if (d >= 0)
            {
                for (var j = last + 1; j < d; j++)
                {
                    if (!used[j])
                    {
                        used[j] = true;
                        AddDeparting(built, source, j, toEnd);
                    }
                }

                last = Math.Max(last, d);
                built.Add(source.Start[d], source.End[d], slice.Start, slice.End, GetKey(slice), t, source.ToColor[d], color);
                fromEnd = source.End[d];
            }
            else
            {
                built.Add(fromEnd, fromEnd, slice.Start, slice.End, GetKey(slice), t, color, color);
            }

            toEnd = slice.End;
        }

        for (var j = 0; j < source.Count; j++)
        {
            if (!used[j])
            {
                AddDeparting(built, source, j, toEnd);
            }
        }

        next = current;
        current = built;
        IsMorphing = current.IsMoving(Tolerance);
        if (!IsMorphing)
        {
            Complete(target);
            return false;
        }

        Apply(0);
        return true;
    }

    public void Retarget(PieLayout target)
    {
        if (!IsMorphing)
        {
            current.Fill(target.Slices);
        }
    }

    public void BeginEntrance(PieLayout target, ChartEntranceAnimation mode)
    {
        Complete(target);
        entrance = mode == ChartEntranceAnimation.Auto ? ChartEntranceAnimation.Grow : mode;
        IsEntrance = true;
        Progress = 0;
    }

    public void Apply(double progress)
    {
        Progress = progress;
        if (IsMorphing)
        {
            current.Apply(Ease(progress));
        }
    }

    public void Complete(PieLayout target)
    {
        IsMorphing = false;
        IsEntrance = false;
        Progress = 1;
        current.Fill(target.Slices);
    }

    private static int GetKey(PieSlice slice) => slice.IsOthers ? OthersKey : slice.DataIndex;

    private static int Remap(int key, ReadOnlySpan<int> map, bool positional, int newCount)
    {
        if (key < 0)
        {
            return key == OthersKey ? OthersKey : DepartingKey;
        }

        if (positional)
        {
            return key < newCount ? key : DepartingKey;
        }

        return (key < map.Length) && (map[key] >= 0) ? map[key] : DepartingKey;
    }

    private static void AddDeparting(PieEntries built, PieEntries source, int index, double position) =>
        built.Add(source.Start[index], source.End[index], position, position, DepartingKey, -1, source.ToColor[index], source.ToColor[index]);

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

    private sealed class PieEntries : IDisposable
    {
        private double[] fromStart = [];
        private double[] fromEnd = [];
        private double[] toStart = [];
        private double[] toEnd = [];

        public int Count { get; private set; }

        public double[] Start { get; private set; } = [];

        public double[] End { get; private set; } = [];

        public int[] Key { get; private set; } = [];

        public int[] Target { get; private set; } = [];

        public PieColorSource[] FromColor { get; private set; } = [];

        public PieColorSource[] ToColor { get; private set; } = [];

        public void Dispose()
        {
            Release();
            Count = 0;
        }

        public void Reset(int capacity)
        {
            Count = 0;
            if (Start.Length >= capacity)
            {
                return;
            }

            Release();
            fromStart = DataSnapshot.Rent<double>(capacity);
            fromEnd = DataSnapshot.Rent<double>(capacity);
            toStart = DataSnapshot.Rent<double>(capacity);
            toEnd = DataSnapshot.Rent<double>(capacity);
            Start = DataSnapshot.Rent<double>(capacity);
            End = DataSnapshot.Rent<double>(capacity);
            Key = DataSnapshot.Rent<int>(capacity);
            Target = DataSnapshot.Rent<int>(capacity);
            FromColor = DataSnapshot.Rent<PieColorSource>(capacity);
            ToColor = DataSnapshot.Rent<PieColorSource>(capacity);
        }

        public void Add(double startFrom, double endFrom, double startTo, double endTo, int key, int target, PieColorSource colorFrom, PieColorSource colorTo)
        {
            var i = Count++;
            fromStart[i] = startFrom;
            fromEnd[i] = endFrom;
            toStart[i] = startTo;
            toEnd[i] = endTo;
            Start[i] = startFrom;
            End[i] = endFrom;
            Key[i] = key;
            Target[i] = target;
            FromColor[i] = colorFrom;
            ToColor[i] = colorTo;
        }

        public void Fill(IReadOnlyList<PieSlice> slices)
        {
            Reset(slices.Count);
            for (var i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var color = slice.ColorSource;
                Add(slice.Start, slice.End, slice.Start, slice.End, GetKey(slice), i, color, color);
            }
        }

        public bool IsMoving(double tolerance)
        {
            for (var i = 0; i < Count; i++)
            {
                if ((Math.Abs(fromStart[i] - toStart[i]) > tolerance) || (Math.Abs(fromEnd[i] - toEnd[i]) > tolerance) || (FromColor[i] != ToColor[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public void Apply(double progress)
        {
            for (var i = 0; i < Count; i++)
            {
                Start[i] = ChartProgress.Lerp(fromStart[i], toStart[i], progress);
                End[i] = ChartProgress.Lerp(fromEnd[i], toEnd[i], progress);
            }
        }

        private void Release()
        {
            DataSnapshot.Return(fromStart);
            DataSnapshot.Return(fromEnd);
            DataSnapshot.Return(toStart);
            DataSnapshot.Return(toEnd);
            DataSnapshot.Return(Start);
            DataSnapshot.Return(End);
            DataSnapshot.Return(Key);
            DataSnapshot.Return(Target);
            DataSnapshot.Return(FromColor);
            DataSnapshot.Return(ToColor);
            fromStart = [];
            fromEnd = [];
            toStart = [];
            toEnd = [];
            Start = [];
            End = [];
            Key = [];
            Target = [];
            FromColor = [];
            ToColor = [];
        }
    }
}
