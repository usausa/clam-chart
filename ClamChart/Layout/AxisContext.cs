namespace ClamChart.Layout;

using System.Globalization;

internal readonly record struct AxisContext(bool Visible, bool TitleVisible, bool IncludeZero, bool ValueAxis, int? MaxLabelCount, CultureInfo Culture);
