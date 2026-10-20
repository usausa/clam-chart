namespace ClamChart.Rendering;

internal readonly record struct PieSelection(int Previous, int Current, double Progress, double Dimmed, double Explode)
{
    public double Alpha(int key) => Blend(Dims(Previous, key) ? Dimmed : 1, Dims(Current, key) ? Dimmed : 1);

    public double Offset(int key)
    {
        if (key < 0)
        {
            return 0;
        }

        var current = key == Current ? Progress : 0;
        var previous = key == Previous ? 1 - Progress : 0;
        return Explode * (current + previous);
    }

    private static bool Dims(int selected, int key) => (selected >= 0) && (key != selected);

    private double Blend(double from, double to) => from + ((to - from) * Progress);
}
