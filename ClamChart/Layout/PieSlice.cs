namespace ClamChart.Layout;

internal readonly record struct PieSlice(
    int DataIndex,
    double Value,
    double Start,
    double End,
    int ColorRank,
    Color? Color,
    string? Label,
    string PercentText,
    string ValueText,
    string? InsideText,
    double InsideWidth)
{
    public bool IsOthers => DataIndex < 0;

    public PieColorSource ColorSource => new(ColorRank, Color, IsOthers);
}
