namespace ClamChart.Tests;

internal static class ChartViewStyleExtensions
{
    public static T ApplyStyle<T>(this T view, ChartStyle style)
        where T : ChartViewBase
    {
        view.FontFamily = style.FontFamily;
        view.PrimaryTextColor = style.PrimaryTextColor;
        view.SecondaryTextColor = style.SecondaryTextColor;
        view.MutedTextColor = style.MutedTextColor;
        view.TextHaloColor = style.TextHaloColor;
        view.TextHaloWidth = style.TextHaloWidth;
        view.LegendFontSize = style.LegendFontSize;
        view.LegendMarkerSize = style.LegendMarkerSize;
        view.DataLabelFontSize = style.DataLabelFontSize;
        view.TooltipBackground = style.TooltipBackground;
        view.TooltipTextColor = style.TooltipTextColor;
        view.TooltipBorderColor = style.TooltipBorderColor;
        view.TooltipFontSize = style.TooltipFontSize;
        view.TooltipCornerRadius = style.TooltipCornerRadius;
        view.TrackballLineColor = style.TrackballLineColor;
        view.TrackColor = style.TrackColor;
        view.DimmedOpacity = style.DimmedOpacity;
        view.PressedOverlayColor = style.PressedOverlayColor;
        view.SkeletonColor = style.SkeletonColor;
        view.SkeletonHighlightColor = style.SkeletonHighlightColor;
        view.OtherColor = style.OtherColor;
        view.GoodColor = style.GoodColor;
        view.WarningColor = style.WarningColor;
        view.SeriousColor = style.SeriousColor;
        view.CriticalColor = style.CriticalColor;
        view.MarkerKnockout = style.MarkerKnockout;
        view.SegmentGap = style.SegmentGap;
        view.CompactSize = style.CompactSize;
        view.MinimalSize = style.MinimalSize;
        view.Palette = style.Palette;
        return view;
    }
}
