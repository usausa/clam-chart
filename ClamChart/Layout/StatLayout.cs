namespace ClamChart.Layout;

internal sealed class StatLayout
{
    public const double CardPadding = 12;

    public const double HorizontalRatio = 2.5;

    public const double RowGap = 2;

    public const double TrendGap = 6;

    public const double MinTrendHeight = 16;

    public const double RightTrendRatio = 0.4;

    public const double BackgroundTrendRatio = 0.6;

    public const double TitleShare = 0.4;

    public const double ValueGap = 8;

    public const double IconGap = 4;

    public const double ArrowGap = 3;

    public const double ArrowWidthRatio = 0.6;

    public const double CaptionGap = 6;

    public const double MinCaptionWidth = 12;

    public const double UnitFontRatio = 0.5;

    public const double UnitGapRatio = 0.25;

    public const double MinFontSize = 6;

    public const double BandWidth = 4;

    private const int FitAttempts = 6;

    public ChartRect Bounds { get; private set; }

    public ChartRect Content { get; private set; }

    public ChartStatLayoutMode Mode { get; private set; } = ChartStatLayoutMode.Vertical;

    public ChartRect BandBounds { get; private set; }

    public ChartThresholdLevel IconLevel { get; private set; }

    public ChartRect IconBounds { get; private set; }

    public string? Title { get; private set; }

    public double TitleFontSize { get; private set; }

    public ChartRect TitleBounds { get; private set; }

    public string? ValueText { get; private set; }

    public double ValueFontSize { get; private set; }

    public ChartRect ValueBounds { get; private set; }

    public double ValueMaxWidth { get; private set; }

    public string? Unit { get; private set; }

    public double UnitFontSize { get; private set; }

    public double UnitGap { get; private set; }

    public ChartRect UnitBounds { get; private set; }

    public StatDelta Delta { get; private set; }

    public double DeltaFontSize { get; private set; }

    public ChartRect ArrowBounds { get; private set; }

    public ChartRect DeltaBounds { get; private set; }

    public string? Caption { get; private set; }

    public ChartRect CaptionBounds { get; private set; }

    public ChartTrendPlacement TrendPlacement { get; private set; }

    public ChartRect TrendBounds { get; private set; }

    public bool HasTrend => !TrendBounds.IsEmpty;

    public static double GetUnitFontSize(double valueFontSize, double titleFontSize) =>
        RoundSize(Math.Max(Math.Min(titleFontSize, valueFontSize), valueFontSize * UnitFontRatio));

    public void Update(double width, double height, Thickness padding, ChartStyle style, StatLayoutOptions options, IChartTextMeasurer measurer)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);
        Bounds = new ChartRect(0, 0, width, height);
        Content = new ChartRect(padding.Left, padding.Top, Math.Max(0, width - padding.HorizontalThickness), Math.Max(0, height - padding.VerticalThickness));
        Mode = ResolveMode(options.LayoutMode, width, height);
        TrendPlacement = options.TrendPlacement;
        BandBounds = options.ShowBand ? new ChartRect(0, 0, Math.Min(BandWidth, width), height) : default;
        IconLevel = options.IconLevel;
        Title = String.IsNullOrEmpty(options.Title) ? null : options.Title;
        TitleFontSize = style.LegendFontSize;
        DeltaFontSize = style.LegendFontSize;
        ValueText = String.IsNullOrEmpty(options.ValueText) ? null : options.ValueText;
        Unit = (ValueText is not null) && !String.IsNullOrEmpty(options.Unit) ? options.Unit : null;
        Delta = options.Delta;
        Caption = Delta.IsVisible && !String.IsNullOrEmpty(options.Caption) ? options.Caption : null;
        ResetBounds();

        var text = Content;
        if (options.HasTrend && (TrendPlacement == ChartTrendPlacement.Right))
        {
            var trendWidth = Content.Width * RightTrendRatio;
            TrendBounds = new ChartRect(Content.Right - trendWidth, Content.Y, trendWidth, Content.Height);
            text = text with { Width = Math.Max(0, Content.Width - trendWidth - TrendGap) };
        }
        else if (options.HasTrend && (TrendPlacement == ChartTrendPlacement.Background))
        {
            var trendHeight = height * BackgroundTrendRatio;
            TrendBounds = new ChartRect(0, height - trendHeight, width, trendHeight);
        }

        var bottomTrend = options.HasTrend && (TrendPlacement == ChartTrendPlacement.Bottom);
        var iconSize = IconLevel != ChartThresholdLevel.None ? TitleFontSize : 0;
        var iconBlock = iconSize > 0 ? iconSize + (Title is not null ? IconGap : 0) : 0;
        var titleHeight = (Title is not null) || (iconSize > 0) ? measurer.GetLineHeight(TitleFontSize) : 0;
        var titleWidth = Title is null ? 0 : measurer.Measure(Title, TitleFontSize, Double.PositiveInfinity);
        var deltaWidth = PrepareDelta(text.Width, measurer, out var arrowWidth, out var captionWidth);
        var deltaHeight = Delta.IsVisible ? measurer.GetLineHeight(DeltaFontSize) : 0;
        var horizontal = Mode == ChartStatLayoutMode.Horizontal;
        var widthLimit = horizontal && (titleHeight > 0) ? text.Width - Math.Min(titleWidth + iconBlock, text.Width * TitleShare) - ValueGap : text.Width;

        var size = 0d;
        if (ValueText is not null)
        {
            var maximum = options.ValueFontSize;
            var minimum = Math.Min(options.MinValueFontSize, maximum);
            var heightSize = FitHeight(GetHeightLimit(), maximum, measurer);
            if ((heightSize < minimum) && bottomTrend)
            {
                bottomTrend = false;
                heightSize = FitHeight(GetHeightLimit(), maximum, measurer);
            }

            if ((heightSize < minimum) && Delta.IsVisible)
            {
                Delta = default;
                Caption = null;
                deltaHeight = 0;
                heightSize = FitHeight(GetHeightLimit(), maximum, measurer);
            }

            var widthSize = FitWidth(ValueText, Unit, widthLimit, maximum, measurer);
            if ((widthSize < minimum) && (Unit is not null))
            {
                Unit = null;
                widthSize = FitWidth(ValueText, Unit, widthLimit, maximum, measurer);
            }

            size = Math.Min(heightSize, widthSize);
            if (size <= 0)
            {
                ValueText = null;
                Unit = null;
                size = 0;
            }
        }

        ValueFontSize = size;
        UnitFontSize = Unit is not null ? GetUnitFontSize(size, TitleFontSize) : 0;
        UnitGap = Unit is not null ? UnitFontSize * UnitGapRatio : 0;
        var valueWidth = ValueText is not null ? measurer.Measure(ValueText, size, Double.PositiveInfinity) : 0;
        var unitWidth = Unit is not null ? measurer.Measure(Unit, UnitFontSize, Double.PositiveInfinity) : 0;
        var valueHeight = ValueText is not null ? measurer.GetLineHeight(size) : 0;
        var unitBlock = Unit is not null ? UnitGap + unitWidth : 0;
        ValueMaxWidth = Math.Max(0, widthLimit - unitBlock);

        var y = Content.Y;
        if (horizontal)
        {
            var rowHeight = Math.Max(titleHeight, valueHeight);
            var unitX = text.Right - unitWidth;
            var valueX = text.Right - unitBlock - valueWidth;
            ValueBounds = ValueText is not null ? new ChartRect(valueX, y, valueWidth, valueHeight) : default;
            UnitBounds = Unit is not null ? new ChartRect(unitX, y, unitWidth, valueHeight) : default;
            var titleRight = ValueText is not null ? valueX - ValueGap : text.Right;
            PlaceTitle(text.X, y, titleRight - text.X, titleHeight, iconSize, iconBlock);
            y += rowHeight;
        }
        else
        {
            if (titleHeight > 0)
            {
                PlaceTitle(text.X, y, text.Width, titleHeight, iconSize, iconBlock);
                y += titleHeight + RowGap;
            }

            ValueBounds = ValueText is not null ? new ChartRect(text.X, y, valueWidth, valueHeight) : default;
            UnitBounds = Unit is not null ? new ChartRect(text.X + valueWidth + UnitGap, y, unitWidth, valueHeight) : default;
            y += valueHeight;
        }

        if (Delta.Text is { } deltaText)
        {
            y += RowGap;
            PlaceDelta(deltaText, horizontal ? text.Right - deltaWidth : text.X, y, arrowWidth, captionWidth, deltaHeight, measurer);
            y += deltaHeight;
        }

        if (bottomTrend)
        {
            var top = y + TrendGap;
            var trendHeight = Content.Bottom - top;
            TrendBounds = trendHeight >= MinTrendHeight ? new ChartRect(Content.X, top, Content.Width, trendHeight) : default;
        }

        double GetHeightLimit()
        {
            var fixedHeight = (deltaHeight > 0 ? RowGap + deltaHeight : 0) + (bottomTrend ? TrendGap + MinTrendHeight : 0);
            if (!horizontal && (titleHeight > 0))
            {
                fixedHeight += titleHeight + RowGap;
            }

            return Content.Height - fixedHeight;
        }
    }

    private static ChartStatLayoutMode ResolveMode(ChartStatLayoutMode mode, double width, double height)
    {
        if (mode != ChartStatLayoutMode.Auto)
        {
            return mode;
        }

        return (width > 0) && (width >= height * HorizontalRatio) ? ChartStatLayoutMode.Horizontal : ChartStatLayoutMode.Vertical;
    }

    private static double RoundSize(double size) => Math.Floor(size * 2) / 2;

    private static double MeasureBlock(string value, string? unit, double size, double titleFontSize, IChartTextMeasurer measurer)
    {
        var width = measurer.Measure(value, size, Double.PositiveInfinity);
        if (unit is null)
        {
            return width;
        }

        var unitSize = GetUnitFontSize(size, titleFontSize);
        return width + (unitSize * UnitGapRatio) + measurer.Measure(unit, unitSize, Double.PositiveInfinity);
    }

    private static double FitHeight(double heightLimit, double maximum, IChartTextMeasurer measurer)
    {
        var size = RoundSize(maximum);
        var floor = Math.Min(MinFontSize, size);
        for (var attempt = 0; attempt < FitAttempts; attempt++)
        {
            if ((size <= 0) || (size < floor) || (heightLimit <= 0))
            {
                return 0;
            }

            var height = measurer.GetLineHeight(size);
            if (height <= heightLimit)
            {
                return size;
            }

            var next = RoundSize(size * heightLimit / height);
            size = next < size ? next : size - 0.5;
        }

        return 0;
    }

    private double FitWidth(string value, string? unit, double widthLimit, double maximum, IChartTextMeasurer measurer)
    {
        var size = RoundSize(maximum);
        var floor = Math.Min(MinFontSize, size);
        for (var attempt = 0; attempt < FitAttempts; attempt++)
        {
            if ((size <= 0) || (size < floor) || (widthLimit <= 0))
            {
                return 0;
            }

            var width = MeasureBlock(value, unit, size, TitleFontSize, measurer);
            if (width <= widthLimit)
            {
                return size;
            }

            var next = RoundSize(size * widthLimit / width);
            size = next < size ? next : size - 0.5;
        }

        return 0;
    }

    private double PrepareDelta(double width, IChartTextMeasurer measurer, out double arrowWidth, out double captionWidth)
    {
        arrowWidth = 0;
        captionWidth = 0;
        if (Delta.Text is not { } text)
        {
            return 0;
        }

        arrowWidth = Delta.Direction != 0 ? DeltaFontSize * ArrowWidthRatio : 0;
        var needed = (arrowWidth > 0 ? arrowWidth + ArrowGap : 0) + measurer.Measure(text, DeltaFontSize, Double.PositiveInfinity);
        if (needed > width)
        {
            Delta = default;
            Caption = null;
            arrowWidth = 0;
            return 0;
        }

        if (Caption is { } caption)
        {
            var available = width - needed - CaptionGap;
            if (available < MinCaptionWidth)
            {
                Caption = null;
            }
            else
            {
                captionWidth = measurer.Measure(caption, DeltaFontSize, available);
                needed += CaptionGap + captionWidth;
            }
        }

        return needed;
    }

    private void PlaceTitle(double x, double y, double width, double height, double iconSize, double iconBlock)
    {
        IconBounds = iconSize > 0 ? new ChartRect(x, y, iconSize, height) : default;
        TitleBounds = Title is not null ? new ChartRect(x + iconBlock, y, Math.Max(0, width - iconBlock), height) : default;
    }

    private void PlaceDelta(string value, double x, double y, double arrowWidth, double captionWidth, double height, IChartTextMeasurer measurer)
    {
        if (arrowWidth > 0)
        {
            ArrowBounds = new ChartRect(x, y, arrowWidth, height);
            x += arrowWidth + ArrowGap;
        }

        var deltaWidth = measurer.Measure(value, DeltaFontSize, Double.PositiveInfinity);
        DeltaBounds = new ChartRect(x, y, deltaWidth, height);
        if (Caption is not null)
        {
            CaptionBounds = new ChartRect(x + deltaWidth + CaptionGap, y, captionWidth, height);
        }
    }

    private void ResetBounds()
    {
        IconBounds = default;
        TitleBounds = default;
        ValueBounds = default;
        UnitBounds = default;
        ArrowBounds = default;
        DeltaBounds = default;
        CaptionBounds = default;
        TrendBounds = default;
    }
}
