namespace ClamChart.Tests;

using System.Runtime.InteropServices;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ChartDrawingRadialBarTests
{
    private const float Scale = 2;

    private const double Size = 160;

    private static readonly ChartStyle Style = new();

    [Fact]
    public void RingsMatchTheView()
    {
        // Arrange
        using var view = Rings(72, 45, 90);

        // Act & Assert
        AssertSameAsView(view, [72, 45, 90], new RadialBarDrawOptions());
    }

    [Fact]
    public void RoundEndsOnAHalfCircleMatchTheView()
    {
        // Arrange
        using var view = Rings(60, 30);
        view.StartAngle = 270;
        view.SweepAngle = 180;
        view.CapStyle = ChartCapStyle.Round;

        // Act & Assert
        AssertSameAsView(view, [60, 30], new RadialBarDrawOptions { StartAngle = 270, SweepAngle = 180, CapStyle = ChartCapStyle.Round });
    }

    [Fact]
    public void GradientWithThresholdsMatchesTheView()
    {
        // Arrange
        using var view = Rings(85, 40);
        view.DisplayMode = ChartRadialBarMode.Gradient;
        view.CapStyle = ChartCapStyle.Round;
        AddThresholds(view.Thresholds);

        // Act & Assert
        AssertSameAsView(view, [85, 40], new RadialBarDrawOptions { DisplayMode = ChartRadialBarMode.Gradient, CapStyle = ChartCapStyle.Round, Thresholds = Thresholds() });
    }

    [Fact]
    public void DarkGradientMatchesTheView()
    {
        // Arrange
        using var view = Rings(65, 20);
        view.ChartStyle = ChartStyles.Dark;
        view.DisplayMode = ChartRadialBarMode.Gradient;

        // Act & Assert
        AssertSameAsView(view, [65, 20], new RadialBarDrawOptions { DisplayMode = ChartRadialBarMode.Gradient, IsDark = true });
    }

    [Fact]
    public void SegmentsMatchTheView()
    {
        // Arrange
        using var view = Rings(70, 30);
        view.DisplayMode = ChartRadialBarMode.Segmented;
        view.SegmentCount = 16;
        AddThresholds(view.Thresholds);

        // Act & Assert
        AssertSameAsView(view, [70, 30], new RadialBarDrawOptions { DisplayMode = ChartRadialBarMode.Segmented, SegmentCount = 16, Thresholds = Thresholds() });
    }

    [Fact]
    public void WrappedValuesMatchTheView()
    {
        // Arrange
        using var basic = Rings(130);
        basic.OverflowMode = ChartOverflowMode.Wrap;
        using var gradient = Rings(130);
        gradient.OverflowMode = ChartOverflowMode.Wrap;
        gradient.DisplayMode = ChartRadialBarMode.Gradient;

        // Act & Assert
        AssertSameAsView(basic, [130], new RadialBarDrawOptions { OverflowMode = ChartOverflowMode.Wrap });
        AssertSameAsView(gradient, [130], new RadialBarDrawOptions { OverflowMode = ChartOverflowMode.Wrap, DisplayMode = ChartRadialBarMode.Gradient });
    }

    [Theory]
    [InlineData(ChartRadialBarMode.Basic)]
    [InlineData(ChartRadialBarMode.Gradient)]
    [InlineData(ChartRadialBarMode.Segmented)]
    public void GlowMatchesTheView(ChartRadialBarMode mode)
    {
        // Arrange
        using var view = Rings(70, 35);
        view.BarGlow = true;
        view.DisplayMode = mode;
        view.CapStyle = ChartCapStyle.Round;

        // Act & Assert
        AssertSameAsView(view, [70, 35], new RadialBarDrawOptions { BarGlow = true, DisplayMode = mode, CapStyle = ChartCapStyle.Round });
    }

    [Theory]
    [InlineData(ChartRadialBarMode.Basic, false)]
    [InlineData(ChartRadialBarMode.Gradient, false)]
    [InlineData(ChartRadialBarMode.Segmented, false)]
    [InlineData(ChartRadialBarMode.Basic, true)]
    [InlineData(ChartRadialBarMode.Gradient, true)]
    public void ThresholdMarkersMatchTheView(ChartRadialBarMode mode, bool glow)
    {
        // Arrange
        using var view = Rings(70, 35);
        view.DisplayMode = mode;
        view.BarGlow = glow;
        view.ThresholdMarkersVisible = true;
        AddThresholds(view.Thresholds);

        // Act & Assert
        AssertSameAsView(view, [70, 35], new RadialBarDrawOptions { DisplayMode = mode, BarGlow = glow, ThresholdMarkersVisible = true, Thresholds = Thresholds() });
    }

    [Fact]
    public void ThresholdMarkersOnAHalfCircleMatchTheView()
    {
        // Arrange
        using var view = Rings(60, 30);
        view.StartAngle = 270;
        view.SweepAngle = 180;
        view.ThresholdMarkersVisible = true;
        AddThresholds(view.Thresholds);

        // Act & Assert
        AssertSameAsView(view, [60, 30], new RadialBarDrawOptions { StartAngle = 270, SweepAngle = 180, ThresholdMarkersVisible = true, Thresholds = Thresholds() });
    }

    [Fact]
    public void SizesAndTrackMatchTheView()
    {
        // Arrange
        using var sized = Rings(50, 25, 75);
        sized.RingThickness = 10;
        sized.RingSpacing = 6;
        sized.InnerRadius = 0.2;
        sized.ChartStyle = new ChartStyle { TrackColor = Colors.Red };
        using var hidden = Rings(50, Double.NaN);
        hidden.TrackVisible = false;

        // Act & Assert
        AssertSameAsView(sized, [50, 25, 75], new RadialBarDrawOptions { Thickness = 10, Spacing = 6, InnerRadius = 0.2f, TrackColor = SKColors.Red });
        AssertSameAsView(hidden, [50, Double.NaN], new RadialBarDrawOptions { TrackVisible = false });
    }

    [Fact]
    public void TheHoleInsideTheRingsIsReturned()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);

        // Act
        var one = ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 100, 100), [50], new RadialBarDrawOptions { Thickness = 10 });
        var two = ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 100, 100), [50, 50], new RadialBarDrawOptions { Thickness = 10, Spacing = 5 });

        // Assert
        Assert.Equal(50, one.MidX, 0.001f);
        Assert.Equal(50, one.MidY, 0.001f);
        Assert.Equal(40 * MathF.Sqrt(2), one.Width, 0.001f);
        Assert.Equal(25 * MathF.Sqrt(2), two.Width, 0.001f);
    }

    [Fact]
    public void ColorsAreTakenInOrderAndTheRestAreGray()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 100, 100), [100, 100], new RadialBarDrawOptions { Thickness = 10, Spacing = 0, Colors = [SKColors.Red] });

        // Assert
        Assert.Equal(SKColors.Red, bitmap.GetPixel(50, 5));
        Assert.Equal(new SKColor(0xB5, 0xB3, 0xAA), bitmap.GetPixel(50, 15));
    }

    [Fact]
    public void NothingIsDrawnWithoutRoom()
    {
        // Arrange
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear();

        // Act
        var empty = ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 100, 100), [], new RadialBarDrawOptions());
        var flat = ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 100, 0), [50], new RadialBarDrawOptions());
        var thin = ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 100, 100), [50], new RadialBarDrawOptions { Thickness = 0 });
        var closed = ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 100, 100), [50], new RadialBarDrawOptions { SweepAngle = Single.NaN });

        // Assert
        Assert.True(empty.IsEmpty);
        Assert.True(flat.IsEmpty);
        Assert.True(thin.IsEmpty);
        Assert.True(closed.IsEmpty);
        Assert.All(bitmap.Pixels, static x => Assert.Equal(0, x.Alpha));
    }

    [Fact]
    public async Task RingsCanBeDrawnOnOtherThreads()
    {
        // Arrange
        var options = new RadialBarDrawOptions { DisplayMode = ChartRadialBarMode.Gradient, BarGlow = true, Thresholds = Thresholds() };
        using var expected = Draw(120, 120, canvas => ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 120, 120), [80, 40], options));

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RunOnThread(() => Draw(120, 120, canvas => ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, 120, 120), [80, 40], options))))).ConfigureAwait(true);

        // Assert
        foreach (var actual in results)
        {
            using (actual)
            {
                Assert.Equal(0, CountDifferences(expected, actual));
            }
        }
    }

    private static TestClamRadialBarView Rings(params double[] values)
    {
        var view = new TestClamRadialBarView
        {
            AnimationDriver = new TestAnimationDriver(),
            ItemsSource = values.Select(static (x, i) => new ChartEntry($"R{i}", x)).ToList(),
            EntranceAnimation = ChartEntranceAnimation.None,
            Density = ChartDensity.Full,
            ContentPadding = new Thickness(0),
            CenterValueMode = ChartCenterValueMode.None,
            LegendVisible = false
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

    private static void AssertSameAsView(TestClamRadialBarView view, double[] values, RadialBarDrawOptions options)
    {
        using var expected = view.Paint(Size, Size, Scale);
        using var actual = Draw(Size, Size, canvas => ChartDrawing.DrawRadialBar(canvas, new SKRect(0, 0, (float)Size, (float)Size), values, options));
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
