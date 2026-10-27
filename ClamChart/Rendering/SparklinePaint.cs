namespace ClamChart.Rendering;

using SkiaSharp;

internal readonly record struct SparklinePaint(SKColor Color, SKColor Negative, SKColor Area, double WeakOpacity, bool FlatArea = false)
{
    public bool HasEmphasis => WeakOpacity < 1;
}
