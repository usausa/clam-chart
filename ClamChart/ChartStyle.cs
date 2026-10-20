namespace ClamChart;

public sealed record ChartStyle
{
    public string FontFamily { get; set; } = "sans-serif";

    public Color PrimaryTextColor { get; set; } = Color.FromArgb("#0B0B0B");

    public Color SecondaryTextColor { get; set; } = Color.FromArgb("#52514E");

    public Color MutedTextColor { get; set; } = Color.FromArgb("#898781");

    public Color TextHaloColor { get; set; } = Colors.Transparent;

    public double TextHaloWidth { get; set; }

    public double AxisLabelFontSize { get; set; } = 11;

    public double AxisTitleFontSize { get; set; } = 11;

    public Color AxisLineColor { get; set; } = Color.FromArgb("#C3C2B7");

    public Color GridLineColor { get; set; } = Color.FromArgb("#E1E0D9");

    public double GridLineThickness { get; set; }

    public double LegendFontSize { get; set; } = 12;

    public double LegendMarkerSize { get; set; } = 8;

    public double DataLabelFontSize { get; set; } = 11;

    public Color TooltipBackground { get; set; } = Color.FromArgb("#F0FCFCFB");

    public Color TooltipTextColor { get; set; } = Color.FromArgb("#0B0B0B");

    public Color TooltipBorderColor { get; set; } = Color.FromArgb("#1A0B0B0B");

    public double TooltipFontSize { get; set; } = 12;

    public double TooltipCornerRadius { get; set; } = 6;

    public Color TrackballLineColor { get; set; } = Color.FromArgb("#898781");

    public Color? TrackColor { get; set; }

    public double DimmedOpacity { get; set; } = 0.35;

    public Color PressedOverlayColor { get; set; } = Color.FromArgb("#0F000000");

    public Color SkeletonColor { get; set; } = Color.FromArgb("#ECEBE6");

    public Color SkeletonHighlightColor { get; set; } = Color.FromArgb("#F6F5F2");

    public IReadOnlyList<Color> Palette { get; set; } = ChartPalettes.DefaultLight;

    public Color OtherColor { get; set; } = Color.FromArgb("#B5B3AA");

    public Color GoodColor { get; set; } = Color.FromArgb("#0CA30C");

    public Color WarningColor { get; set; } = Color.FromArgb("#FAB219");

    public Color SeriousColor { get; set; } = Color.FromArgb("#EC835A");

    public Color CriticalColor { get; set; } = Color.FromArgb("#D03B3B");

    public Color PositiveTextColor { get; set; } = Color.FromArgb("#006300");

    public Color NegativeTextColor { get; set; } = Color.FromArgb("#D03B3B");

    public double LineThickness { get; set; } = 2;

    public double AreaFillOpacity { get; set; } = 0.1;

    public double BarCornerRadius { get; set; } = 4;

    public double MaxBarWidth { get; set; } = 24;

    public double MarkerSize { get; set; } = 8;

    public double MarkerKnockout { get; set; } = 2;

    public double SegmentGap { get; set; } = 2;

    public Size CompactSize { get; set; } = new(320, 200);

    public Size MinimalSize { get; set; } = new(160, 96);

    internal static bool ValidateValue(object? value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ChartStyle));
        ((ChartStyle)value).Validate();
        return true;
    }

    internal void Validate()
    {
        if (!IsPositive(AxisLabelFontSize) || !IsPositive(AxisTitleFontSize) || !IsPositive(LegendFontSize) || !IsPositive(DataLabelFontSize) || !IsPositive(TooltipFontSize))
        {
            throw new ArgumentException("Font sizes must be positive finite values.");
        }

        if (!IsPositive(LegendMarkerSize) || !IsPositive(LineThickness) || !IsPositive(MaxBarWidth) || !IsPositive(MarkerSize))
        {
            throw new ArgumentException("Line thickness, marker sizes and the bar width must be positive finite values.");
        }

        if (!IsNonNegative(TextHaloWidth) || !IsNonNegative(GridLineThickness) || !IsNonNegative(TooltipCornerRadius) || !IsNonNegative(BarCornerRadius) || !IsNonNegative(MarkerKnockout) || !IsNonNegative(SegmentGap))
        {
            throw new ArgumentException("Widths, radii and gaps must be finite values of zero or more.");
        }

        if (!IsOpacity(DimmedOpacity) || !IsOpacity(AreaFillOpacity))
        {
            throw new ArgumentException("Opacities must be values from 0 to 1.");
        }

        if (!IsSize(CompactSize) || !IsSize(MinimalSize) || (MinimalSize.Width > CompactSize.Width) || (MinimalSize.Height > CompactSize.Height))
        {
            throw new ArgumentException("Density sizes must be finite sizes of zero or more and the minimal size must not exceed the compact size.");
        }
    }

    private static bool IsPositive(double value) => Double.IsFinite(value) && (value > 0);

    private static bool IsNonNegative(double value) => Double.IsFinite(value) && (value >= 0);

    private static bool IsOpacity(double value) => (value >= 0) && (value <= 1);

    private static bool IsSize(Size value) => IsNonNegative(value.Width) && IsNonNegative(value.Height);
}
