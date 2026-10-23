namespace ClamChart.Tests;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamBarGaugeViewRenderingTests
{
    private const float Scale = 2;

    private static readonly SKColor Blue = new(0x2A, 0x78, 0xD6);

    private static readonly SKColor Good = new(0x56, 0xA6, 0x4B);

    private static readonly SKColor Warning = new(0xF2, 0xCC, 0x0C);

    private static readonly SKColor Critical = new(0xE0, 0x2F, 0x44);

    private static readonly SKColor Track = new(0, 0, 0, 18);

    [Fact]
    public void BasicFillsTheValueAndMarksTheEnd()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 50));

        // Act
        using var bitmap = view.Paint(300, 8);

        // Assert
        Assert.Equal(Blue.WithAlpha(89), GetPixel(bitmap, 75, 4), ColorComparer.Loose);
        Assert.Equal(Blue, GetPixel(bitmap, 149, 4), ColorComparer.Loose);
        Assert.Equal(Track, GetPixel(bitmap, 151, 4), ColorComparer.Loose);
        Assert.Equal(Track, GetPixel(bitmap, 225, 4), ColorComparer.Loose);
    }

    [Fact]
    public void TheTrackCanBeHiddenOrRecolored()
    {
        // Arrange
        using var hidden = Gauge(new ChartEntry("A", 50));
        hidden.TrackVisible = false;
        using var colored = Gauge(new ChartEntry("A", 50));
        colored.ChartStyle = new ChartStyle { TrackColor = Colors.Red };

        // Act
        using var first = hidden.Paint(300, 8);
        using var second = colored.Paint(300, 8);

        // Assert
        Assert.Equal(0, GetPixel(first, 225, 4).Alpha);
        Assert.Equal(SKColors.Red, GetPixel(second, 225, 4), ColorComparer.Loose);
        Assert.Equal(Blue.WithAlpha(89), GetPixel(second, 75, 4), ColorComparer.Loose);
    }

    [Fact]
    public void GradientBlendsTheThresholdColorsUpToTheValue()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 90));
        view.DisplayMode = ChartBarGaugeMode.Gradient;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning });

        // Act
        using var bitmap = view.Paint(300, 8);

        // Assert
        Assert.Equal(Good, GetPixel(bitmap, 45, 4), ColorComparer.Loose);
        Assert.Equal(Warning, GetPixel(bitmap, 266, 4), ColorComparer.Loose);
        Assert.Equal(Track, GetPixel(bitmap, 285, 4), ColorComparer.Loose);
    }

    [Fact]
    public void SegmentedLightsTheCellsUpToTheValue()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 50));
        view.DisplayMode = ChartBarGaugeMode.Segmented;

        // Act
        using var bitmap = view.Paint(300, 8);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(25, layout.CellCount);
        Assert.InRange(GetPixel(bitmap, CellCenter(layout, 0), 4).Alpha, 238, 246);
        Assert.InRange(GetPixel(bitmap, CellCenter(layout, 12), 4).Alpha, 238, 246);
        Assert.Equal(Blue.WithAlpha(46), GetPixel(bitmap, CellCenter(layout, 13), 4), ColorComparer.Loose);
        Assert.Equal(Blue.WithAlpha(46), GetPixel(bitmap, CellCenter(layout, 24), 4), ColorComparer.Loose);
        Assert.Equal(0, GetPixel(bitmap, CellCenter(layout, 0) + (layout.CellSize / 2) + 1, 4).Alpha);
    }

    [Fact]
    public void SegmentedCellsTakeTheColorOfTheirStart()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 100));
        view.DisplayMode = ChartBarGaugeMode.Segmented;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 50, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(300, 8);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(Good.WithAlpha(242), GetPixel(bitmap, CellCenter(layout, 12), 4), ColorComparer.Loose);
        Assert.Equal(Critical.WithAlpha(242), GetPixel(bitmap, CellCenter(layout, 13), 4), ColorComparer.Loose);
    }

    [Fact]
    public void TheTargetIsAThinLineInThePrimaryText()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 50, Target: 75));

        // Act
        using var bitmap = view.Paint(300, 8);
        view.TargetVisible = false;
        using var hidden = view.Paint(300, 8);

        // Assert
        Assert.Equal(new SKColor(0x0B, 0x0B, 0x0B), GetPixel(bitmap, 225, 4), ColorComparer.Loose);
        Assert.Equal(Track, GetPixel(bitmap, 228, 4), ColorComparer.Loose);
        Assert.Equal(Track, GetPixel(hidden, 225, 4), ColorComparer.Loose);
    }

    [Fact]
    public void TheValueTakesTheColorOfTheBar()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 50));
        view.ValueVisible = true;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 40, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(300, 40);

        // Assert
        var bounds = view.ChartLayout.GetValueBounds(0);
        Assert.True(CountColor(bitmap, bounds, Critical) > 0);
        Assert.Equal(0, CountColor(bitmap, bounds, Blue));
        Assert.Equal(Critical.WithAlpha(89), GetPixel(bitmap, 10, view.ChartLayout.GetTrackBounds(0).CenterY), ColorComparer.Loose);
    }

    [Fact]
    public void TheValueCanUseThePrimaryText()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 50));
        Assert.Equal(ChartBarGaugeValueColorMode.Bar, view.ValueColorMode);
        view.ValueVisible = true;
        view.ValueColorMode = ChartBarGaugeValueColorMode.Text;
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 40, Level = ChartThresholdLevel.Critical });

        // Act
        using var bitmap = view.Paint(300, 40);

        // Assert
        var bounds = view.ChartLayout.GetValueBounds(0);
        Assert.True(CountColor(bitmap, bounds, ChartStyles.Light.PrimaryTextColor.ToSKColor()) > 0);
        Assert.Equal(0, CountColor(bitmap, bounds, Critical));
        Assert.Equal(Critical.WithAlpha(89), GetPixel(bitmap, 10, view.ChartLayout.GetTrackBounds(0).CenterY), ColorComparer.Loose);
    }

    [Fact]
    public void LabelsUseTheSecondaryText()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("Tokyo", 50));
        view.LabelPosition = ChartBarGaugeLabelPosition.Top;

        // Act
        using var bitmap = view.Paint(300, 40);

        // Assert
        Assert.True(CountColor(bitmap, view.ChartLayout.GetLabelBounds(0), new SKColor(0x52, 0x51, 0x4E)) > 0);
    }

    [Fact]
    public void VerticalBarsGrowUpward()
    {
        // Arrange
        using var view = Gauge(new ChartEntry("A", 50));
        view.Orientation = ChartOrientation.Vertical;

        // Act
        using var bitmap = view.Paint(100, 200);

        // Assert
        Assert.Equal(new ChartRect(46, 0, 8, 200), view.ChartLayout.TrackTemplate);
        Assert.Equal(Blue.WithAlpha(89), GetPixel(bitmap, 50, 150), ColorComparer.Loose);
        Assert.Equal(Blue, GetPixel(bitmap, 50, 101), ColorComparer.Loose);
        Assert.Equal(Track, GetPixel(bitmap, 50, 50), ColorComparer.Loose);
    }

    [Fact]
    public void EmptyTextIsShownWithoutRows()
    {
        // Arrange
        using var view = new TestClamBarGaugeView { ItemsSource = new List<ChartEntry>(), EmptyText = "No data" };

        // Act
        using var bitmap = view.Paint(300, 80);

        // Assert
        Assert.True(CountColor(bitmap, new ChartRect(0, 0, 300, 80), new SKColor(0x89, 0x87, 0x81)) > 0);
    }

    [Theory]
    [InlineData(ChartBarGaugeMode.Basic)]
    [InlineData(ChartBarGaugeMode.Gradient)]
    [InlineData(ChartBarGaugeMode.Segmented)]
    public void FramesDoNotAllocate(ChartBarGaugeMode mode)
    {
        // Arrange
        using var view = new TestClamBarGaugeView
        {
            AnimationDriver = new TestAnimationDriver(),
            ItemsSource = new List<ChartEntry> { new("Tokyo", 82, Target: 90), new("Osaka", 58), new("Nagoya", 31, Maximum: 50) },
            EntranceAnimation = ChartEntranceAnimation.None,
            DisplayMode = mode,
            LabelPosition = ChartBarGaugeLabelPosition.Left,
            ValueMode = ChartBarGaugeValueMode.ValueOfMaximum
        };
        view.Thresholds.Add(new ChartThreshold { Level = ChartThresholdLevel.Good });
        view.Thresholds.Add(new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning });
        view.Thresholds.Add(new ChartThreshold { Value = 80, Level = ChartThresholdLevel.Critical });
        using var bitmap = view.Paint(360, 120);
        using var surface = SKSurface.Create(new SKImageInfo(720, 240));
        var canvas = surface.Canvas;
        view.Render(canvas, 360, 120);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 4; i++)
        {
            view.Render(canvas, 360, 120);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.Equal(0, allocated);
    }

    private static TestClamBarGaugeView Gauge(ChartEntry entry)
    {
        var view = new TestClamBarGaugeView
        {
            AnimationDriver = new TestAnimationDriver(),
            ItemsSource = new List<ChartEntry> { entry },
            EntranceAnimation = ChartEntranceAnimation.None,
            ContentPadding = new Thickness(0),
            LabelPosition = ChartBarGaugeLabelPosition.None,
            ValueVisible = false
        };
        view.OnAttached();
        return view;
    }

    private static double CellCenter(BarGaugeLayout layout, int index) =>
        layout.TrackTemplate.X + (index * (layout.CellSize + layout.CellGap)) + (layout.CellSize / 2);

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static int CountColor(SKBitmap bitmap, ChartRect bounds, SKColor color)
    {
        var count = 0;
        for (var y = (int)(bounds.Y * Scale); y < (int)(bounds.Bottom * Scale); y++)
        {
            for (var x = (int)(bounds.X * Scale); x < (int)(bounds.Right * Scale); x++)
            {
                if (ColorComparer.Loose.Equals(bitmap.GetPixel(x, y), color))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private sealed class ColorComparer : IEqualityComparer<SKColor>
    {
        public static ColorComparer Loose { get; } = new();

        public bool Equals(SKColor x, SKColor y) =>
            (Math.Abs(x.Red - y.Red) <= 12) && (Math.Abs(x.Green - y.Green) <= 12) && (Math.Abs(x.Blue - y.Blue) <= 12) && (Math.Abs(x.Alpha - y.Alpha) <= 2);

        public int GetHashCode(SKColor obj) => obj.GetHashCode();
    }
}
