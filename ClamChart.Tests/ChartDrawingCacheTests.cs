namespace ClamChart.Tests;

using SkiaSharp;

public sealed class ChartDrawingCacheTests
{
    private const int Size = 200;

    private static readonly double[] Values = [3, 8, 5, 12, 9, 14, 7, 11, 6, 15, 10, 13];

    private static readonly double[] Gauges = [42, 77, 18];

    private static readonly ChartDrawingThreshold[] Thresholds = [new(Double.NegativeInfinity, SKColors.Green), new(60, SKColors.Orange), new(80, SKColors.Red)];

    public static TheoryData<string> ScenarioNames => [.. Scenarios.Keys];

    private static readonly Dictionary<string, Action<SKCanvas, ChartDrawingCache?>> Scenarios = new()
    {
        ["Line"] = static (canvas, cache) => ChartDrawing.DrawSparkline(canvas, Bounds, Values, new SparklineDrawOptions(), cache),
        ["Smooth"] = static (canvas, cache) => ChartDrawing.DrawSparkline(canvas, Bounds, Values, new SparklineDrawOptions { Smooth = true, Markers = ChartSparklineMarkers.High | ChartSparklineMarkers.Low }, cache),
        ["Area"] = static (canvas, cache) => ChartDrawing.DrawSparkline(canvas, Bounds, Values, new SparklineDrawOptions { Kind = ChartSparklineKind.Area }, cache),
        ["Bar"] = static (canvas, cache) => ChartDrawing.DrawSparkline(canvas, Bounds, Values, new SparklineDrawOptions { Kind = ChartSparklineKind.Bar }, cache),
        ["WinLoss"] = static (canvas, cache) => ChartDrawing.DrawSparkline(canvas, Bounds, Values, new SparklineDrawOptions { Kind = ChartSparklineKind.WinLoss }, cache),
        ["BarGauge"] = static (canvas, cache) => ChartDrawing.DrawBarGauge(canvas, Bounds, Gauges, new BarGaugeDrawOptions { Thresholds = Thresholds }, cache),
        ["BarGaugeTarget"] = static (canvas, cache) => ChartDrawing.DrawBarGauge(canvas, Bounds, Gauges, [50, 50, 50], new BarGaugeDrawOptions { Orientation = ChartOrientation.Vertical }, cache),
        ["BarGaugeSegmented"] = static (canvas, cache) => ChartDrawing.DrawBarGauge(canvas, Bounds, Gauges, new BarGaugeDrawOptions { DisplayMode = ChartBarGaugeMode.Segmented }, cache),
        ["BarGaugeGradient"] = static (canvas, cache) => ChartDrawing.DrawBarGauge(canvas, Bounds, Gauges, new BarGaugeDrawOptions { DisplayMode = ChartBarGaugeMode.Gradient, Thresholds = Thresholds }, cache),
        ["RadialBar"] = static (canvas, cache) => ChartDrawing.DrawRadialBar(canvas, Bounds, Gauges, new RadialBarDrawOptions { CapStyle = ChartCapStyle.Round }, cache),
        ["RadialBarGlow"] = static (canvas, cache) => ChartDrawing.DrawRadialBar(canvas, Bounds, Gauges, new RadialBarDrawOptions { BarGlow = true, CapStyle = ChartCapStyle.Round }, cache),
        ["RadialBarSegmented"] = static (canvas, cache) => ChartDrawing.DrawRadialBar(canvas, Bounds, Gauges, new RadialBarDrawOptions { DisplayMode = ChartRadialBarMode.Segmented, BarGlow = true }, cache),
        ["RadialBarGradient"] = static (canvas, cache) => ChartDrawing.DrawRadialBar(canvas, Bounds, Gauges, new RadialBarDrawOptions { DisplayMode = ChartRadialBarMode.Gradient, BarGlow = true, Thresholds = Thresholds, ThresholdMarkersVisible = true }, cache)
    };

    private static SKRect Bounds => new(10, 10, Size - 10, Size - 10);

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void CacheDrawsTheSamePixels(string name)
    {
        // Arrange
        var scenario = Scenarios[name];
        using var cache = new ChartDrawingCache();

        // Act
        var plain = Render(scenario, null);
        var cached = Render(scenario, cache);

        // Assert
        Assert.Equal(plain, cached);
    }

    [Fact]
    public void OneCacheIsUsedForEveryDrawingInARow()
    {
        // Arrange
        using var cache = new ChartDrawingCache();
        var names = Scenarios.Keys.ToArray();

        // Act
        var forward = new byte[names.Length][];
        for (var i = 0; i < names.Length; i++)
        {
            forward[i] = Render(Scenarios[names[i]], cache);
        }

        var backward = new byte[names.Length][];
        for (var i = names.Length - 1; i >= 0; i--)
        {
            backward[i] = Render(Scenarios[names[i]], cache);
        }

        // Assert
        for (var i = 0; i < names.Length; i++)
        {
            var plain = Render(Scenarios[names[i]], null);
            Assert.Equal(plain, forward[i]);
            Assert.Equal(plain, backward[i]);
        }
    }

    [Fact]
    public void CacheRemovesThePerCallAllocation()
    {
        // Arrange
        using var cache = new ChartDrawingCache();
        using var bitmap = new SKBitmap(Size, Size);
        using var canvas = new SKCanvas(bitmap);

        // Act
        var line = Measure(canvas, Scenarios["Line"], null);
        var cachedLine = Measure(canvas, Scenarios["Line"], cache);
        var winLoss = Measure(canvas, Scenarios["WinLoss"], null);
        var cachedWinLoss = Measure(canvas, Scenarios["WinLoss"], cache);
        var gauge = Measure(canvas, Scenarios["BarGauge"], null);
        var cachedGauge = Measure(canvas, Scenarios["BarGauge"], cache);
        var glow = Measure(canvas, Scenarios["RadialBarGlow"], null);
        var cachedGlow = Measure(canvas, Scenarios["RadialBarGlow"], cache);

        // Assert
        Assert.True(cachedLine < line, $"line {cachedLine} < {line}");
        Assert.Equal(0, cachedWinLoss);
        Assert.True(winLoss > 0);
        Assert.Equal(0, cachedGauge);
        Assert.True(gauge > 0);
        Assert.True(cachedGlow < glow, $"glow {cachedGlow} < {glow}");
    }

    [Fact]
    public void CacheKeepsTheBlurOfTheLastThickness()
    {
        // Arrange
        using var cache = new ChartDrawingCache();
        using var bitmap = new SKBitmap(Size, Size);
        using var canvas = new SKCanvas(bitmap);
        var thin = new RadialBarDrawOptions { BarGlow = true, Thickness = 6 };
        var thick = new RadialBarDrawOptions { BarGlow = true, Thickness = 16 };

        // Act
        var same = Measure(canvas, (target, scratch) => ChartDrawing.DrawRadialBar(target, Bounds, Gauges, thin, scratch), cache);
        ChartDrawing.DrawRadialBar(canvas, Bounds, Gauges, thick, cache);
        var changed = Measure(canvas, (target, scratch) =>
        {
            ChartDrawing.DrawRadialBar(target, Bounds, Gauges, thin, scratch);
            ChartDrawing.DrawRadialBar(target, Bounds, Gauges, thick, scratch);
        }, cache);

        // Assert
        Assert.Equal(0, same);
        Assert.True(changed > 0);
    }

    private static byte[] Render(Action<SKCanvas, ChartDrawingCache?> scenario, ChartDrawingCache? cache)
    {
        using var bitmap = new SKBitmap(Size, Size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        scenario(canvas, cache);
        return bitmap.Bytes;
    }

    private static long Measure(SKCanvas canvas, Action<SKCanvas, ChartDrawingCache?> scenario, ChartDrawingCache? cache)
    {
        scenario(canvas, cache);
        scenario(canvas, cache);
        var before = GC.GetAllocatedBytesForCurrentThread();
        scenario(canvas, cache);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
