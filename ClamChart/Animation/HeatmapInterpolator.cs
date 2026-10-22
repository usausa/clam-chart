namespace ClamChart.Animation;

using ClamChart.Layout;

internal sealed class HeatmapInterpolator
{
    private double[] from = [];
    private double[] to = [];
    private double[] values = [];
    private string?[] rowNames = [];
    private string?[] columnNames = [];
    private double fromMinimum;
    private double fromMaximum = 1;
    private double toMinimum;
    private double toMaximum = 1;
    private int rowCount;
    private int columnCount;
    private ChartHeatmapLayoutMode mode;
    private DateTime start;
    private bool captured;
    private bool staggered;
    private ChartEntranceAnimation entrance;

    public Easing Easing { get; set; } = Easing.Linear;

    public bool IsMorphing { get; private set; }

    public bool IsEntrance { get; private set; }

    public double Progress { get; private set; } = 1;

    public double Minimum { get; private set; }

    public double Maximum { get; private set; } = 1;

    public ChartEntranceAnimation Entrance => IsEntrance ? entrance : ChartEntranceAnimation.None;

    public double RevealRatio => Entrance == ChartEntranceAnimation.Reveal ? Ease(Progress) : 1;

    public double Ease(double progress) => Easing.Ease(progress);

    public double GetValue(int cell) => cell < rowCount * columnCount ? values[cell] : Double.NaN;

    public double GetAlpha(int column, int count) =>
        Entrance == ChartEntranceAnimation.Fade ? Ease(staggered ? ChartProgress.ForElement(Progress, column, count) : Progress) : 1;

    public double GetScale(int column, int count) =>
        Entrance == ChartEntranceAnimation.Grow ? Ease(ChartProgress.ForElement(Progress, column, count)) : 1;

    public void Capture()
    {
        var size = rowCount * columnCount;
        Ensure(ref from, size);
        Array.Copy(values, from, size);
        fromMinimum = Minimum;
        fromMaximum = Maximum;
        captured = true;
    }

    public bool Begin(HeatmapLayout target)
    {
        IsEntrance = false;
        if (!captured || !HasSameShape(target))
        {
            Complete(target);
            return false;
        }

        captured = false;
        var size = rowCount * columnCount;
        Ensure(ref to, size);
        var moving = !fromMinimum.Equals(target.Minimum) || !fromMaximum.Equals(target.Maximum);
        for (var i = 0; i < size; i++)
        {
            to[i] = target.GetValue(i);
            moving |= !from[i].Equals(to[i]);
        }

        toMinimum = target.Minimum;
        toMaximum = target.Maximum;
        if (!moving)
        {
            Complete(target);
            return false;
        }

        IsMorphing = true;
        Apply(0);
        return true;
    }

    public void BeginEntrance(HeatmapLayout target, ChartEntranceAnimation animation)
    {
        Complete(target);
        staggered = animation == ChartEntranceAnimation.Auto;
        entrance = staggered ? ChartEntranceAnimation.Fade : animation;
        IsEntrance = true;
        Progress = 0;
    }

    public void Retarget(HeatmapLayout target)
    {
        var progress = Progress;
        var animation = entrance;
        Complete(target);
        entrance = animation;
        IsEntrance = true;
        Progress = progress;
    }

    public void Apply(double progress)
    {
        Progress = progress;
        if (!IsMorphing)
        {
            return;
        }

        var eased = Ease(progress);
        var size = rowCount * columnCount;
        for (var i = 0; i < size; i++)
        {
            values[i] = Double.IsFinite(from[i]) && Double.IsFinite(to[i]) ? ChartProgress.Lerp(from[i], to[i], eased) : to[i];
        }

        Minimum = ChartProgress.Lerp(fromMinimum, toMinimum, eased);
        Maximum = ChartProgress.Lerp(fromMaximum, toMaximum, eased);
    }

    public void Complete(HeatmapLayout target)
    {
        IsMorphing = false;
        IsEntrance = false;
        Progress = 1;
        captured = false;
        mode = target.Mode;
        rowCount = target.RowCount;
        columnCount = target.ColumnCount;
        start = target.CalendarStart;
        var size = rowCount * columnCount;
        Ensure(ref values, size);
        for (var i = 0; i < size; i++)
        {
            values[i] = target.IsPresent(i) ? target.GetValue(i) : Double.NaN;
        }

        Copy(target.RowNames, ref rowNames);
        Copy(target.ColumnNames, ref columnNames);
        Minimum = target.Minimum;
        Maximum = target.Maximum;
    }

    private static void Ensure<T>(ref T[] buffer, int count)
    {
        if (buffer.Length < count)
        {
            buffer = new T[count];
        }
    }

    private static void Copy(IReadOnlyList<string?> source, ref string?[] destination)
    {
        Ensure(ref destination, source.Count);
        for (var i = 0; i < source.Count; i++)
        {
            destination[i] = source[i];
        }
    }

    private static bool HasSameNames(IReadOnlyList<string?> source, string?[] names)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (!String.Equals(source[i], names[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasSameShape(HeatmapLayout target) =>
        (target.Mode == mode) && (target.RowCount == rowCount) && (target.ColumnCount == columnCount) && (target.CalendarStart == start) &&
        HasSameNames(target.RowNames, rowNames) && HasSameNames(target.ColumnNames, columnNames);
}
