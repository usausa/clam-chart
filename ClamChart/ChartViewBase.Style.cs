namespace ClamChart;

public abstract partial class ChartViewBase
{
    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(ChartViewBase), "sans-serif", propertyChanged: OnStyleChanged);
    public static readonly BindableProperty PrimaryTextColorProperty = BindableProperty.Create(nameof(PrimaryTextColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#0B0B0B"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SecondaryTextColorProperty = BindableProperty.Create(nameof(SecondaryTextColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#52514E"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty MutedTextColorProperty = BindableProperty.Create(nameof(MutedTextColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#898781"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TextHaloColorProperty = BindableProperty.Create(nameof(TextHaloColor), typeof(Color), typeof(ChartViewBase), Colors.Transparent, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TextHaloWidthProperty = BindableProperty.Create(nameof(TextHaloWidth), typeof(double), typeof(ChartViewBase), 0d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(TextHaloWidth)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty LegendFontSizeProperty = BindableProperty.Create(nameof(LegendFontSize), typeof(double), typeof(ChartViewBase), 12d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(LegendFontSize)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty LegendMarkerSizeProperty = BindableProperty.Create(nameof(LegendMarkerSize), typeof(double), typeof(ChartViewBase), 8d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(LegendMarkerSize)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty DataLabelFontSizeProperty = BindableProperty.Create(nameof(DataLabelFontSize), typeof(double), typeof(ChartViewBase), 11d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(DataLabelFontSize)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty TooltipBackgroundProperty = BindableProperty.Create(nameof(TooltipBackground), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#F0FCFCFB"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TooltipTextColorProperty = BindableProperty.Create(nameof(TooltipTextColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#0B0B0B"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TooltipBorderColorProperty = BindableProperty.Create(nameof(TooltipBorderColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#1A0B0B0B"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TooltipFontSizeProperty = BindableProperty.Create(nameof(TooltipFontSize), typeof(double), typeof(ChartViewBase), 12d, validateValue: static (_, value) => ChartAxis.ValidatePositive(value, nameof(TooltipFontSize)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty TooltipCornerRadiusProperty = BindableProperty.Create(nameof(TooltipCornerRadius), typeof(double), typeof(ChartViewBase), 0d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(TooltipCornerRadius)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TrackballLineColorProperty = BindableProperty.Create(nameof(TrackballLineColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#898781"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#12000000"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty DimmedOpacityProperty = BindableProperty.Create(nameof(DimmedOpacity), typeof(double), typeof(ChartViewBase), 0.35d, validateValue: static (_, value) => ValidateOpacity(value, nameof(DimmedOpacity)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty PressedOverlayColorProperty = BindableProperty.Create(nameof(PressedOverlayColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#0F000000"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SkeletonColorProperty = BindableProperty.Create(nameof(SkeletonColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#ECEBE6"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SkeletonHighlightColorProperty = BindableProperty.Create(nameof(SkeletonHighlightColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#F6F5F2"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty OtherColorProperty = BindableProperty.Create(nameof(OtherColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#B5B3AA"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty GoodColorProperty = BindableProperty.Create(nameof(GoodColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#56A64B"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty WarningColorProperty = BindableProperty.Create(nameof(WarningColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#F2CC0C"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SeriousColorProperty = BindableProperty.Create(nameof(SeriousColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#FF780A"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty CriticalColorProperty = BindableProperty.Create(nameof(CriticalColor), typeof(Color), typeof(ChartViewBase), Color.FromArgb("#E02F44"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty MarkerKnockoutProperty = BindableProperty.Create(nameof(MarkerKnockout), typeof(double), typeof(ChartViewBase), 2d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(MarkerKnockout)), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SegmentGapProperty = BindableProperty.Create(nameof(SegmentGap), typeof(double), typeof(ChartViewBase), 2d, validateValue: static (_, value) => ChartSeries.ValidateSize(value, nameof(SegmentGap)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty FillModeProperty = BindableProperty.Create(nameof(FillMode), typeof(ChartFillMode), typeof(ChartViewBase), ChartFillMode.Solid, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty CompactSizeProperty = BindableProperty.Create(nameof(CompactSize), typeof(Size), typeof(ChartViewBase), new Size(320, 200), validateValue: static (_, value) => ValidateDensitySize(value, nameof(CompactSize)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty MinimalSizeProperty = BindableProperty.Create(nameof(MinimalSize), typeof(Size), typeof(ChartViewBase), new Size(160, 96), validateValue: static (_, value) => ValidateDensitySize(value, nameof(MinimalSize)), propertyChanged: OnStyleChanged);

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public Color PrimaryTextColor
    {
        get => (Color)GetValue(PrimaryTextColorProperty);
        set => SetValue(PrimaryTextColorProperty, value);
    }

    public Color SecondaryTextColor
    {
        get => (Color)GetValue(SecondaryTextColorProperty);
        set => SetValue(SecondaryTextColorProperty, value);
    }

    public Color MutedTextColor
    {
        get => (Color)GetValue(MutedTextColorProperty);
        set => SetValue(MutedTextColorProperty, value);
    }

    public Color TextHaloColor
    {
        get => (Color)GetValue(TextHaloColorProperty);
        set => SetValue(TextHaloColorProperty, value);
    }

    public double TextHaloWidth
    {
        get => (double)GetValue(TextHaloWidthProperty);
        set => SetValue(TextHaloWidthProperty, value);
    }

    public double LegendFontSize
    {
        get => (double)GetValue(LegendFontSizeProperty);
        set => SetValue(LegendFontSizeProperty, value);
    }

    public double LegendMarkerSize
    {
        get => (double)GetValue(LegendMarkerSizeProperty);
        set => SetValue(LegendMarkerSizeProperty, value);
    }

    public double DataLabelFontSize
    {
        get => (double)GetValue(DataLabelFontSizeProperty);
        set => SetValue(DataLabelFontSizeProperty, value);
    }

    public Color TooltipBackground
    {
        get => (Color)GetValue(TooltipBackgroundProperty);
        set => SetValue(TooltipBackgroundProperty, value);
    }

    public Color TooltipTextColor
    {
        get => (Color)GetValue(TooltipTextColorProperty);
        set => SetValue(TooltipTextColorProperty, value);
    }

    public Color TooltipBorderColor
    {
        get => (Color)GetValue(TooltipBorderColorProperty);
        set => SetValue(TooltipBorderColorProperty, value);
    }

    public double TooltipFontSize
    {
        get => (double)GetValue(TooltipFontSizeProperty);
        set => SetValue(TooltipFontSizeProperty, value);
    }

    public double TooltipCornerRadius
    {
        get => (double)GetValue(TooltipCornerRadiusProperty);
        set => SetValue(TooltipCornerRadiusProperty, value);
    }

    public Color TrackballLineColor
    {
        get => (Color)GetValue(TrackballLineColorProperty);
        set => SetValue(TrackballLineColorProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public double DimmedOpacity
    {
        get => (double)GetValue(DimmedOpacityProperty);
        set => SetValue(DimmedOpacityProperty, value);
    }

    public Color PressedOverlayColor
    {
        get => (Color)GetValue(PressedOverlayColorProperty);
        set => SetValue(PressedOverlayColorProperty, value);
    }

    public Color SkeletonColor
    {
        get => (Color)GetValue(SkeletonColorProperty);
        set => SetValue(SkeletonColorProperty, value);
    }

    public Color SkeletonHighlightColor
    {
        get => (Color)GetValue(SkeletonHighlightColorProperty);
        set => SetValue(SkeletonHighlightColorProperty, value);
    }

    public Color OtherColor
    {
        get => (Color)GetValue(OtherColorProperty);
        set => SetValue(OtherColorProperty, value);
    }

    public Color GoodColor
    {
        get => (Color)GetValue(GoodColorProperty);
        set => SetValue(GoodColorProperty, value);
    }

    public Color WarningColor
    {
        get => (Color)GetValue(WarningColorProperty);
        set => SetValue(WarningColorProperty, value);
    }

    public Color SeriousColor
    {
        get => (Color)GetValue(SeriousColorProperty);
        set => SetValue(SeriousColorProperty, value);
    }

    public Color CriticalColor
    {
        get => (Color)GetValue(CriticalColorProperty);
        set => SetValue(CriticalColorProperty, value);
    }

    public double MarkerKnockout
    {
        get => (double)GetValue(MarkerKnockoutProperty);
        set => SetValue(MarkerKnockoutProperty, value);
    }

    public double SegmentGap
    {
        get => (double)GetValue(SegmentGapProperty);
        set => SetValue(SegmentGapProperty, value);
    }

    public ChartFillMode FillMode
    {
        get => (ChartFillMode)GetValue(FillModeProperty);
        set => SetValue(FillModeProperty, value);
    }

    public Size CompactSize
    {
        get => (Size)GetValue(CompactSizeProperty);
        set => SetValue(CompactSizeProperty, value);
    }

    public Size MinimalSize
    {
        get => (Size)GetValue(MinimalSizeProperty);
        set => SetValue(MinimalSizeProperty, value);
    }

    private protected virtual ChartStyle ApplyStyle(ChartStyle style) => style;

    private ChartStyle BuildStyle() =>
        ApplyStyle(new ChartStyle
        {
            FontFamily = FontFamily,
            PrimaryTextColor = PrimaryTextColor,
            SecondaryTextColor = SecondaryTextColor,
            MutedTextColor = MutedTextColor,
            TextHaloColor = TextHaloColor,
            TextHaloWidth = TextHaloWidth,
            LegendFontSize = LegendFontSize,
            LegendMarkerSize = LegendMarkerSize,
            DataLabelFontSize = DataLabelFontSize,
            TooltipBackground = TooltipBackground,
            TooltipTextColor = TooltipTextColor,
            TooltipBorderColor = TooltipBorderColor,
            TooltipFontSize = TooltipFontSize,
            TooltipCornerRadius = TooltipCornerRadius,
            TrackballLineColor = TrackballLineColor,
            TrackColor = TrackColor,
            DimmedOpacity = DimmedOpacity,
            PressedOverlayColor = PressedOverlayColor,
            SkeletonColor = SkeletonColor,
            SkeletonHighlightColor = SkeletonHighlightColor,
            OtherColor = OtherColor,
            GoodColor = GoodColor,
            WarningColor = WarningColor,
            SeriousColor = SeriousColor,
            CriticalColor = CriticalColor,
            MarkerKnockout = MarkerKnockout,
            SegmentGap = SegmentGap,
            FillMode = FillMode,
            CompactSize = CompactSize,
            MinimalSize = MinimalSize,
            Palette = Palette
        });

    internal static bool ValidateOpacity(object? value, string name) =>
        (value is not double opacity) || ((opacity >= 0) && (opacity <= 1)) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be from 0 to 1.");

    internal static bool ValidateDensitySize(object? value, string name) =>
        (value is not Size size) || (Double.IsFinite(size.Width) && Double.IsFinite(size.Height) && (size.Width >= 0) && (size.Height >= 0)) ? true : throw new ArgumentOutOfRangeException(name, value, "The width and the height must be finite values of zero or more.");
}
