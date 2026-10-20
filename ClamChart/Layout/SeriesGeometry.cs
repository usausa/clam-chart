namespace ClamChart.Layout;

using ClamChart.Data;

internal sealed class SeriesGeometry : IDisposable
{
    private double[] x = [];
    private double[] start = [];
    private double[] end = [];
    private double[] value = [];
    private bool[] outer = [];

    public int Count { get; private set; }

    public bool IsStacked { get; set; }

    public int BarSlot { get; set; } = -1;

    public double BarOffset { get; private set; }

    public double BarWidth { get; private set; }

    public double BarHitStart { get; private set; }

    public double BarHitEnd { get; private set; }

    public bool IsSorted { get; private set; }

    public Span<double> X => x.AsSpan(0, Count);

    public Span<double> Start => start.AsSpan(0, Count);

    public Span<double> End => end.AsSpan(0, Count);

    public Span<double> Value => value.AsSpan(0, Count);

    public Span<bool> Outer => outer.AsSpan(0, Count);

    public void Dispose()
    {
        Release();
        Count = 0;
    }

    public void Reset(int count)
    {
        if (x.Length < count)
        {
            Release();
            x = DataSnapshot.Rent<double>(count);
            start = DataSnapshot.Rent<double>(count);
            end = DataSnapshot.Rent<double>(count);
            value = DataSnapshot.Rent<double>(count);
            outer = DataSnapshot.Rent<bool>(count);
        }

        Count = count;
        IsStacked = false;
        BarSlot = -1;
        PlaceBar(0, 0, 0, 0);
        IsSorted = false;
    }

    public void CopyShape(SeriesGeometry source)
    {
        IsStacked = source.IsStacked;
        BarSlot = source.BarSlot;
        PlaceBar(source.BarOffset, source.BarWidth, source.BarHitStart, source.BarHitEnd);
        IsSorted = source.IsSorted;
    }

    public void PlaceBar(double offset, double width, double hitStart, double hitEnd)
    {
        BarOffset = offset;
        BarWidth = width;
        BarHitStart = hitStart;
        BarHitEnd = hitEnd;
    }

    public (double Start, double End) GetBarRange(double position) =>
        (position + BarOffset - (BarWidth / 2), position + BarOffset + (BarWidth / 2));

    public (double Start, double End) GetBarHitRange(double position) =>
        (position + BarHitStart, position + BarHitEnd);

    public void UpdateSorted()
    {
        var values = X;
        for (var i = 1; i < values.Length; i++)
        {
            if (!(values[i] >= values[i - 1]))
            {
                IsSorted = false;
                return;
            }
        }

        IsSorted = true;
    }

    private void Release()
    {
        DataSnapshot.Return(x);
        DataSnapshot.Return(start);
        DataSnapshot.Return(end);
        DataSnapshot.Return(value);
        DataSnapshot.Return(outer);
        x = [];
        start = [];
        end = [];
        value = [];
        outer = [];
    }
}
