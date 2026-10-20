namespace ClamChart.Rendering;

internal readonly record struct BarGaugeRenderState(Color BaseColor, IList<ChartThreshold> Thresholds, bool TrackVisible, bool TargetVisible);
