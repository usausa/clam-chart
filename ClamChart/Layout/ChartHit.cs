namespace ClamChart.Layout;

internal readonly record struct ChartHit(ChartHitKind Kind, int SeriesIndex = -1, int Index = -1)
{
    public static ChartHit None => new(ChartHitKind.None);
}
