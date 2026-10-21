namespace ClamChart.Rendering;

internal readonly record struct CartesianRenderState(ChartThresholdsMode ThresholdsMode, IReadOnlyList<ChartAnnotation> Annotations, ChartDownsampling Downsampling = ChartDownsampling.None, double LivePhase = Double.NaN);
