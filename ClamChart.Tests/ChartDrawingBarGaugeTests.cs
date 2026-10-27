namespace ClamChart.Tests;

using System.Runtime.InteropServices;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ChartDrawingBarGaugeTests
{
    private const float Scale = 2;

    private static readonly ChartStyle Style = new();

    [Theory]
    [InlineData(ChartBarGaugeMode.Basic, ChartFillMode.Solid)]
    [InlineData(ChartBarGaugeMode.Basic, ChartFillMode.Gradient)]
    [InlineData(ChartBarGaugeMode.Segmented, ChartFillMode.Solid)]
    public void BarsMatchTheView(ChartBarGaugeMode mode, ChartFillMode fill)
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 85), new ChartEntry("B", 40), new ChartEntry("C", Double.NaN));
        view.DisplayMode = mode;
        view.FillMode = fill;
        AddThresholds(view.Thresholds);

        // Act & Assert
        AssertSameAsView(view, 300, 48, [85, 40, Double.NaN], default, new BarGaugeDrawOptions { DisplayMode = mode, FillMode = fill, Thresholds = Thresholds() });
    }

    [Theory]
    [InlineData(ChartBarGaugeMode.Basic, ChartFillMode.Solid)]
    [InlineData(ChartBarGaugeMode.Basic, ChartFillMode.Gradient)]
    [InlineData(ChartBarGaugeMode.Segmented, ChartFillMode.Solid)]
    public void ColumnsMatchTheView(ChartBarGaugeMode mode, ChartFillMode fill)
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 70), new ChartEntry("B", 20), new ChartEntry("C", 100));
        view.Orientation = ChartOrientation.Vertical;
        view.DisplayMode = mode;
        view.FillMode = fill;

        // Act & Assert
        AssertSameAsView(view, 48, 200, [70, 20, 100], default, new BarGaugeDrawOptions { Orientation = ChartOrientation.Vertical, DisplayMode = mode, FillMode = fill });
    }

    [Fact]
    public void TargetsMatchTheView()
    {
        // Arrange
        using var bars = Gauge(new ChartEntry("A", 50, Target: 70), new ChartEntry("B", 90, Target: 60));
        using var columns = Gauge(new ChartEntry("A", 50, Target: 70), new ChartEntry("B", 90, Target: 60));
        columns.Orientation = ChartOrientation.Vertical;

        // Act & Assert
        AssertSameAsView(bars, 300, 28, [50, 90], [70, 60], new BarGaugeDrawOptions());
        AssertSameAsView(columns, 28, 200, [50, 90], [70, 60], new BarGaugeDrawOptions { Orientation = ChartOrientation.Vertical });
    }

    [Fact]
    public void ColorsAndTrackMatchTheView()
    {
        // Arrange
        using var dark = Gauge(new ChartEntry("A", 60, Target: 80));
        dark.ApplyStyle(ChartStyles.Dark);
        using var colored = Gauge(new ChartEntry("A", 60));
        colored.Color = Colors.Orange;
        colored.ApplyStyle(new ChartStyle { TrackColor = Colors.Red });
        using var hidden = Gauge(new ChartEntry("A", 60));
        hidden.TrackVisible = false;

        // Act & Assert
        AssertSameAsView(dark, 300, 8, [60], [80], new BarGaugeDrawOptions { IsDark = true });
        AssertSameAsView(colored, 300, 8, [60], default, new BarGaugeDrawOptions { Color = Colors.Orange.ToSKColor(), TrackColor = SKColors.Red });
        AssertSameAsView(hidden, 300, 8, [60], default, new BarGaugeDrawOptions { TrackVisible = false });
    }

    [Fact]
    public void TheTargetStaysInsideTheBounds()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 20);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 5, 100, 15), [20], [50], new BarGaugeDrawOptions { TrackVisible = false });

        // Assert
        Assert.Equal(new SKColor(0x0B, 0x0B, 0x0B), bitmap.GetPixel(50, 10));
        Assert.Equal(0, bitmap.GetPixel(50, 3).Alpha);
        Assert.Equal(0, bitmap.GetPixel(50, 16).Alpha);
    }

    [Fact]
    public void NothingIsDrawnWithoutRoom()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 20);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 0, 100, 20), [], new BarGaugeDrawOptions());
        ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 0, 0, 20), [50], new BarGaugeDrawOptions());
        ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 0, 100, 20), [50, 50], new BarGaugeDrawOptions { Spacing = 20 });

        // Assert
        Assert.All(bitmap.Pixels, static x => Assert.Equal(0, x.Alpha));
    }

    [Fact]
    public async Task BarsCanBeDrawnOnOtherThreads()
    {
        // Arrange
        var options = new BarGaugeDrawOptions { DisplayMode = ChartBarGaugeMode.Segmented, Thresholds = Thresholds() };
        using var expected = Draw(200, 28, canvas => ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 0, 200, 28), [85, 40], [60, 60], options));

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RunOnThread(() => Draw(200, 28, canvas => ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 0, 200, 28), [85, 40], [60, 60], options))))).ConfigureAwait(true);

        // Assert
        foreach (var actual in results)
        {
            using (actual)
            {
                Assert.Equal(0, CountDifferences(expected, actual));
            }
        }
    }

    private static TestClamBarGaugeView Gauge(params ChartEntry[] entries)
    {
        var view = new TestClamBarGaugeView
        {
            AnimationDriver = new TestAnimationDriver(),
            ItemsSource = entries.ToList(),
            EntranceAnimation = ChartEntranceAnimation.None,
            Density = ChartDensity.Full,
            ContentPadding = new Thickness(0),
            LabelPosition = ChartBarGaugeLabelPosition.None,
            ValueVisible = false
        };
        view.OnAttached();
        return view;
    }

    private static void AddThresholds(IList<ChartThreshold> thresholds)
    {
        thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        thresholds.Add(new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Warning });
        thresholds.Add(new ChartThreshold { Value = 80, Level = ChartThresholdLevel.Critical });
    }

    private static ChartDrawingThreshold[] Thresholds() =>
    [
        new(Double.NegativeInfinity, Style.GoodColor.ToSKColor()),
        new(50, Style.WarningColor.ToSKColor()),
        new(80, Style.CriticalColor.ToSKColor())
    ];

    private static void AssertSameAsView(TestClamBarGaugeView view, double width, double height, double[] values, double[]? targets, BarGaugeDrawOptions options)
    {
        using var expected = view.Paint(width, height, Scale);
        using var actual = Draw(width, height, canvas => ChartDrawing.DrawBarGauge(canvas, new SKRect(0, 0, (float)width, (float)height), values, targets, options));
        Assert.Equal(0, CountDifferences(expected, actual));
    }

    private static SKBitmap Draw(double width, double height, Action<SKCanvas> draw)
    {
        var info = new SKImageInfo((int)Math.Ceiling(width * Scale), (int)Math.Ceiling(height * Scale), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear();
        canvas.Scale(Scale, Scale);
        draw(canvas);
        using var image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static Task<SKBitmap> RunOnThread(Func<SKBitmap> draw) =>
        Task.Factory.StartNew(draw, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static int CountDifferences(SKBitmap expected, SKBitmap actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        var first = MemoryMarshal.Cast<byte, uint>(expected.GetPixelSpan());
        var second = MemoryMarshal.Cast<byte, uint>(actual.GetPixelSpan());
        var count = 0;
        for (var i = 0; i < first.Length; i++)
        {
            if (first[i] != second[i])
            {
                count++;
            }
        }

        return count;
    }
}
