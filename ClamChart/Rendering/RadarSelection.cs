namespace ClamChart.Rendering;

internal readonly record struct RadarSelection(int Previous, int Current, double Progress, double Dimmed)
{
    public double Alpha(int series) => Blend(Dims(Previous, series) ? Dimmed : 1, Dims(Current, series) ? Dimmed : 1);

    private static bool Dims(int selected, int series) => (selected >= 0) && (series != selected);

    private double Blend(double from, double to) => from + ((to - from) * Progress);
}
