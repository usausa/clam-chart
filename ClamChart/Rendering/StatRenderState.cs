namespace ClamChart.Rendering;

using ClamChart.Animation;

internal readonly record struct StatRenderState(ChartStatColorMode ColorMode, Color? StateColor, Color? Fill, Color SeriesColor, ChartCountUp CountUp, ChartValueFormatting Formatting, bool IsEmptyValue, Color AccentTextColor, double GradientStrength, double Pulse = 1);
