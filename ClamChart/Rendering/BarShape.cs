namespace ClamChart.Rendering;

using SkiaSharp;

internal readonly record struct BarShape(SKRect Rect, BarEdge Edge, bool Rounded);
