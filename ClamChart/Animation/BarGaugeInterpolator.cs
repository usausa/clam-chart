namespace ClamChart.Animation;

using ClamChart.Layout;

internal sealed class BarGaugeInterpolator
{
    private const int DepartingKey = -1;

    private const double Tolerance = 1e-9;

    private List<Entry> current = [];
    private List<Entry> next = [];
    private ChartEntranceAnimation entrance;
    private double entranceStagger;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool IsMorphing { get; private set; }

    public bool IsEntrance { get; private set; }

    public double Progress { get; private set; } = 1;

    public int Count => current.Count;

    public ChartEntranceAnimation Entrance => IsEntrance ? entrance : ChartEntranceAnimation.None;

    public double OverlayAlpha => IsEntrance ? Ease(ChartProgress.Delayed(Progress, ChartProgress.OverlayDelay)) : 1;

    public double EntranceAlpha => IsEntrance && (entrance == ChartEntranceAnimation.Fade) ? Ease(Progress) : 1;

    public double Ease(double progress) => Easing.Ease(progress);

    public BarGaugeRow GetRow(int index) => current[index].Row;

    public double GetRatio(int index)
    {
        var entry = current[index];
        if (IsEntrance)
        {
            return entrance switch
            {
                ChartEntranceAnimation.Grow => entry.ToRatio * Ease(ChartProgress.ForElement(Progress, entry.Row.Slot, current.Count, entranceStagger)),
                ChartEntranceAnimation.Reveal => entry.ToRatio * Ease(Progress),
                _ => entry.ToRatio
            };
        }

        return IsMorphing ? ChartProgress.Lerp(entry.FromRatio, entry.ToRatio, Ease(Progress)) : entry.ToRatio;
    }

    public double GetSlot(int index)
    {
        var entry = current[index];
        return IsMorphing ? ChartProgress.Lerp(entry.FromSlot, entry.ToSlot, Ease(Progress)) : entry.ToSlot;
    }

    public double GetAlpha(int index)
    {
        var entry = current[index];
        return IsMorphing ? ChartProgress.Lerp(entry.FromAlpha, entry.ToAlpha, Ease(Progress)) : entry.ToAlpha;
    }

    public void Complete(BarGaugeLayout layout)
    {
        IsMorphing = false;
        IsEntrance = false;
        Progress = 1;
        Fill(current, layout.Rows);
    }

    public void Retarget(BarGaugeLayout layout) => Fill(current, layout.Rows);

    public void BeginEntrance(BarGaugeLayout layout, ChartEntranceAnimation mode, double stagger)
    {
        Complete(layout);
        entrance = mode == ChartEntranceAnimation.Auto ? ChartEntranceAnimation.Grow : mode;
        entranceStagger = stagger;
        IsEntrance = true;
        Progress = 0;
    }

    public bool Begin(BarGaugeLayout layout, ReadOnlySpan<int> map, bool positional, int newCount)
    {
        var eased = Ease(Progress);
        var morphing = IsMorphing;
        IsEntrance = false;
        next.Clear();
        var rows = layout.Rows;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var match = FindMatch(row.DataIndex, map, positional, newCount);
            if (match >= 0)
            {
                var old = current[match];
                var ratio = morphing ? ChartProgress.Lerp(old.FromRatio, old.ToRatio, eased) : old.ToRatio;
                var slot = morphing ? ChartProgress.Lerp(old.FromSlot, old.ToSlot, eased) : old.ToSlot;
                var alpha = morphing ? ChartProgress.Lerp(old.FromAlpha, old.ToAlpha, eased) : old.ToAlpha;
                next.Add(new Entry(row, row.DataIndex, ratio, row.Ratio, slot, row.Slot, alpha, 1));
                current[match] = old with { Key = DepartingKey - 1 };
            }
            else
            {
                next.Add(new Entry(row, row.DataIndex, 0, row.Ratio, row.Slot, row.Slot, 0, 1));
            }
        }

        foreach (var old in current)
        {
            if (old.Key >= DepartingKey)
            {
                var ratio = morphing ? ChartProgress.Lerp(old.FromRatio, old.ToRatio, eased) : old.ToRatio;
                var slot = morphing ? ChartProgress.Lerp(old.FromSlot, old.ToSlot, eased) : old.ToSlot;
                var alpha = morphing ? ChartProgress.Lerp(old.FromAlpha, old.ToAlpha, eased) : old.ToAlpha;
                next.Add(new Entry(old.Row, DepartingKey, ratio, 0, slot, slot, alpha, 0));
            }
        }

        (current, next) = (next, current);
        IsMorphing = IsMoving();
        if (!IsMorphing)
        {
            Complete(layout);
            return false;
        }

        Progress = 0;
        return true;
    }

    public void Apply(double progress) => Progress = progress;

    private static void Fill(List<Entry> entries, IReadOnlyList<BarGaugeRow> rows)
    {
        entries.Clear();
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            entries.Add(new Entry(row, row.DataIndex, row.Ratio, row.Ratio, row.Slot, row.Slot, 1, 1));
        }
    }

    private static int Remap(int key, ReadOnlySpan<int> map, bool positional, int newCount)
    {
        if (key < 0)
        {
            return DepartingKey;
        }

        if (positional)
        {
            return key < newCount ? key : DepartingKey;
        }

        return (key < map.Length) && (map[key] >= 0) ? map[key] : DepartingKey;
    }

    private int FindMatch(int dataIndex, ReadOnlySpan<int> map, bool positional, int newCount)
    {
        for (var i = 0; i < current.Count; i++)
        {
            var key = current[i].Key;
            if ((key >= 0) && (Remap(key, map, positional, newCount) == dataIndex))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsMoving()
    {
        foreach (var entry in current)
        {
            if ((Math.Abs(entry.FromRatio - entry.ToRatio) > Tolerance) || (Math.Abs(entry.FromSlot - entry.ToSlot) > Tolerance) || (Math.Abs(entry.FromAlpha - entry.ToAlpha) > Tolerance))
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct Entry(BarGaugeRow Row, int Key, double FromRatio, double ToRatio, double FromSlot, double ToSlot, double FromAlpha, double ToAlpha);
}
