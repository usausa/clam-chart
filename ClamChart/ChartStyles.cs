namespace ClamChart;

public static class ChartStyles
{
    public static ChartStyle Light { get; } = new();

    public static ChartStyle Dark { get; } = new()
    {
        IsDark = true,
        PrimaryTextColor = Color.FromArgb("#F0EFEC"),
        SecondaryTextColor = Color.FromArgb("#C3C2B7"),
        AxisLineColor = Color.FromArgb("#383835"),
        GridLineColor = Color.FromArgb("#2C2C2A"),
        TooltipBackground = Color.FromArgb("#F01F1F1D"),
        TooltipTextColor = Color.FromArgb("#F0EFEC"),
        TooltipBorderColor = Color.FromArgb("#1AF0EFEC"),
        PressedOverlayColor = Color.FromArgb("#14FFFFFF"),
        SkeletonColor = Color.FromArgb("#232321"),
        SkeletonHighlightColor = Color.FromArgb("#2C2C2A"),
        Palette = ChartPalettes.DefaultDark,
        OtherColor = Color.FromArgb("#5C5B56"),
        GoodColor = Color.FromArgb("#73BF69"),
        WarningColor = Color.FromArgb("#FADE2A"),
        SeriousColor = Color.FromArgb("#FF9830"),
        CriticalColor = Color.FromArgb("#F2495C"),
        PositiveTextColor = Color.FromArgb("#73BF69"),
        NegativeTextColor = Color.FromArgb("#F2495C")
    };
}
