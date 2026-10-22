namespace Example.Modules.Chart;

using SkiaSharp.Views.Maui;

internal static class DrawingWidgets
{
    private const float Square = 160;

    private const float HalfHeight = 96;

    private const float Wide = 340;

    private const float GoalsHeight = 84;

    private const float TrendHeight = 64;

    private const float Inset = 4;

    private const float BarSpacing = 12;

    private const float ValueWidth = 56;

    private const float LabelSize = 13;

    private const float TrendSize = 22;

    private const float CenterRatio = 0.42f;

    private const float GaugeRatio = 0.3f;

    public static byte[] Storage(double percent, ChartRadialBarMode mode, bool dark, float scale) =>
        Render(Square, Square, scale, canvas =>
        {
            var options = new RadialBarDrawOptions { DisplayMode = mode, CapStyle = ChartCapStyle.Round, Thickness = 14, SegmentCount = 24, BarGlow = true, IsDark = dark };
            var hole = ChartDrawing.DrawRadialBar(canvas, new SKRect(Inset, Inset, Square - Inset, Square - Inset), [percent], options);
            DrawCenter(canvas, hole, FormatPercent(percent), GetStyle(dark).PrimaryTextColor.ToSKColor());
        });

    public static byte[] Activity(double[] values, ChartRadialBarMode mode, bool dark, float scale) =>
        Render(Square, Square, scale, canvas =>
        {
            var style = GetStyle(dark);
            var options = new RadialBarDrawOptions
            {
                DisplayMode = mode,
                CapStyle = ChartCapStyle.Round,
                Thickness = 16,
                Spacing = 3,
                SegmentCount = 30,
                OverflowMode = ChartOverflowMode.Wrap,
                Colors = [style.CriticalColor.ToSKColor(), style.GoodColor.ToSKColor(), style.Palette[0].ToSKColor()],
                IsDark = dark
            };
            ChartDrawing.DrawRadialBar(canvas, new SKRect(Inset, Inset, Square - Inset, Square - Inset), values, options);
        });

    public static byte[] Cpu(double percent, ChartRadialBarMode mode, bool dark, float scale) =>
        Render(Square, HalfHeight, scale, canvas =>
        {
            var style = GetStyle(dark);
            var options = new RadialBarDrawOptions
            {
                StartAngle = 270,
                SweepAngle = 180,
                DisplayMode = mode,
                Thickness = 18,
                SegmentCount = 16,
                Thresholds = GetThresholds(style),
                IsDark = dark
            };
            var hole = ChartDrawing.DrawRadialBar(canvas, new SKRect(Inset, Inset, Square - Inset, HalfHeight - Inset), [percent], options);
            DrawGauge(canvas, hole, FormatPercent(percent), style.PrimaryTextColor.ToSKColor());
        });

    public static byte[] Columns(double[] values, ChartBarGaugeMode mode, bool dark, float scale) =>
        Render(Square, Square, scale, canvas =>
        {
            var options = new BarGaugeDrawOptions
            {
                Orientation = ChartOrientation.Vertical,
                DisplayMode = mode,
                Spacing = 8,
                SegmentWidth = 8,
                Color = GetStyle(dark).Palette[2].ToSKColor(),
                IsDark = dark
            };
            ChartDrawing.DrawBarGauge(canvas, new SKRect(Inset, Inset, Square - Inset, Square - Inset), values, options);
        });

    public static byte[] Goals(double[] values, double[] targets, ChartBarGaugeMode mode, bool dark, float scale) =>
        Render(Wide, GoalsHeight, scale, canvas =>
        {
            var style = GetStyle(dark);
            var options = new BarGaugeDrawOptions { DisplayMode = mode, Spacing = BarSpacing, SegmentWidth = 10, Thresholds = GetThresholds(style), IsDark = dark };
            ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 0, Wide - ValueWidth, GoalsHeight), values, targets, options);
            using var typeface = CreateTypeface();
            using var font = CreateFont(typeface, LabelSize);
            using var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.Color = style.SecondaryTextColor.ToSKColor();
            var size = (GoalsHeight - (BarSpacing * (values.Length - 1))) / values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                var middle = (i * (size + BarSpacing)) + (size / 2);
                canvas.DrawText(FormatPercent(values[i]), Wide, GetBaseline(font, middle), SKTextAlign.Right, font, paint);
            }
        });

    public static byte[] Trend(double[] values, bool dark, float scale) =>
        Render(Wide, TrendHeight, scale, canvas =>
        {
            var style = GetStyle(dark);
            var options = new SparklineDrawOptions { Kind = ChartSparklineKind.Area, Color = style.Palette[0].ToSKColor(), Smooth = true };
            ChartDrawing.DrawSparkline(canvas, new SKRect(0, Inset, Wide - ValueWidth - 8, TrendHeight - Inset), values, options);
            using var typeface = CreateTypeface();
            using var font = CreateFont(typeface, TrendSize);
            using var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.Color = style.PrimaryTextColor.ToSKColor();
            canvas.DrawText(values[^1].ToString("0.0", CultureInfo.InvariantCulture), Wide, GetBaseline(font, TrendHeight / 2), SKTextAlign.Right, font, paint);
        });

    private static ChartStyle GetStyle(bool dark) => dark ? ChartStyles.Dark : ChartStyles.Light;

    private static ChartDrawingThreshold[] GetThresholds(ChartStyle style) =>
    [
        new(Double.NegativeInfinity, style.GoodColor.ToSKColor()),
        new(60, style.WarningColor.ToSKColor()),
        new(85, style.CriticalColor.ToSKColor())
    ];

    private static string FormatPercent(double value) => value.ToString("0", CultureInfo.InvariantCulture) + "%";

    private static SKTypeface CreateTypeface() => SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold);

    private static SKFont CreateFont(SKTypeface typeface, float size) =>
        new(typeface, size) { LinearMetrics = true, Subpixel = true, Edging = SKFontEdging.Antialias };

    private static float GetBaseline(SKFont font, float middle)
    {
        font.GetFontMetrics(out var metrics);
        return middle - ((metrics.Ascent + metrics.Descent) / 2);
    }

    private static void DrawCenter(SKCanvas canvas, SKRect hole, string text, SKColor color)
    {
        if (hole.IsEmpty)
        {
            return;
        }

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface, hole.Height * CenterRatio);
        var width = font.MeasureText(text);
        if (width > hole.Width)
        {
            font.Size *= hole.Width / width;
        }

        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = color;
        canvas.DrawText(text, hole.MidX, GetBaseline(font, hole.MidY), SKTextAlign.Center, font, paint);
    }

    private static void DrawGauge(SKCanvas canvas, SKRect hole, string text, SKColor color)
    {
        if (hole.IsEmpty)
        {
            return;
        }

        using var typeface = CreateTypeface();
        using var font = CreateFont(typeface, hole.Width * GaugeRatio);
        font.GetFontMetrics(out var metrics);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = color;
        canvas.DrawText(text, hole.MidX, hole.MidY - metrics.Descent, SKTextAlign.Center, font, paint);
    }

    private static byte[] Render(float width, float height, float scale, Action<SKCanvas> draw)
    {
        var info = new SKImageInfo((int)Math.Ceiling(width * scale), (int)Math.Ceiling(height * scale), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        draw(canvas);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
