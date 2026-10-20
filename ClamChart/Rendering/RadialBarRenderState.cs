namespace ClamChart.Rendering;

internal readonly record struct RadialBarRenderState(IReadOnlyList<Color> Palette, IList<ChartThreshold> Thresholds, bool TrackVisible);
