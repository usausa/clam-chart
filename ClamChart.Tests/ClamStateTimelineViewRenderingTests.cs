namespace ClamChart.Tests;

using ClamChart.Tests.Data;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class ClamStateTimelineViewRenderingTests
{
    private const float Scale = 2;

    private static readonly DateTime Origin = new(2026, 10, 22, 0, 0, 0);

    private static readonly SKColor Good = new(0x56, 0xA6, 0x4B);

    private static readonly SKColor Critical = new(0xE0, 0x2F, 0x44);

    [Fact]
    public void StatesFillTheirRowsAtSeventyPercent()
    {
        // Arrange
        using var view = Servers();
        view.ShowValue = ChartVisibilityMode.Never;

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        Assert.Equal(Good.WithAlpha(179), GetPixel(bitmap, Point(view, 0, 1)), ColorComparer.Loose);
        Assert.Equal(Critical.WithAlpha(179), GetPixel(bitmap, Point(view, 0, 2.5)), ColorComparer.Loose);
        Assert.Equal(0, GetPixel(bitmap, Point(view, 1, 0.5)).Alpha);
    }

    [Fact]
    public void LinesOutlineTheStatesInTheSolidColor()
    {
        // Arrange
        using var view = Servers();
        view.ShowValue = ChartVisibilityMode.Never;
        view.LineWidth = 2;

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        var layout = view.ChartLayout;
        var point = Point(view, 0, 1);
        Assert.Equal(Good, GetPixel(bitmap, point.X, layout.GetRowTop(0) + 0.5), ColorComparer.Loose);
        Assert.Equal(Good.WithAlpha(179), GetPixel(bitmap, point), ColorComparer.Loose);
    }

    [Fact]
    public void ValuesAreWrittenOnlyWhereTheyFit()
    {
        // Arrange
        using var auto = Servers();
        using var never = Servers();
        never.ShowValue = ChartVisibilityMode.Never;

        // Act
        using var first = auto.Paint(300, 200);
        using var second = never.Paint(300, 200);

        // Assert
        var layout = auto.ChartLayout;
        var row = new ChartRect(layout.Plot.X, layout.GetRowTop(0), layout.Plot.Width / 2, layout.RowHeight);
        Assert.True(CountOther(first, row, Good.WithAlpha(179)) > 0);
        Assert.Equal(0, CountOther(second, row, Good.WithAlpha(179)));
    }

    [Fact]
    public void TrackingHighlightsTheStateAndDescribesIt()
    {
        // Arrange
        using var view = Servers();
        view.Culture = CultureInfo.InvariantCulture;
        view.ShowValue = ChartVisibilityMode.Never;
        view.TrackballMode = ChartTrackballMode.Touch;
        using var first = view.Paint(300, 200);

        // Act
        view.HandleTouch(1, SKTouchAction.Pressed, Point(view, 0, 2.5), 0);
        using var bitmap = view.Paint(300, 200);

        // Assert
        var highlighted = GetPixel(bitmap, Point(view, 0, 2.2));
        Assert.True(highlighted.Alpha > 179);
        Assert.Equal("02:00 – 03:00", view.TooltipHeader);
        Assert.Equal(["web Stopped", "1h"], view.TooltipTexts);
    }

    [Fact]
    public void TheLegendListsTheStates()
    {
        // Arrange
        using var view = Servers();
        view.LegendVisible = true;

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        var legend = view.ChartLayout.Legend;
        Assert.Equal(["Running", "Stopped"], view.ChartLayout.LegendTexts);
        var marker = legend.Items[1].Marker;
        Assert.Equal(Critical, GetPixel(bitmap, marker.CenterX, marker.CenterY), ColorComparer.Loose);
    }

    [Fact]
    public void FramesDoNotAllocate()
    {
        // Arrange
        using var view = Servers();
        view.LegendVisible = true;
        view.LineWidth = 1;
        using var bitmap = view.Paint(300, 200);
        using var surface = SKSurface.Create(new SKImageInfo(600, 400));
        Render(view, surface.Canvas);

        // Act
        var allocated = ChartAllocation.Measure(() =>
        {
            for (var i = 0; i < 4; i++)
            {
                Render(view, surface.Canvas);
            }
        });

        // Assert
        Assert.Equal(0, allocated);
    }

    private static TestClamStateTimelineView Servers()
    {
        var view = new TestClamStateTimelineView
        {
            AnimationDriver = new TestAnimationDriver(),
            EntranceAnimation = ChartEntranceAnimation.None,
            ContentPadding = new Thickness(0),
            SeparatorColor = Colors.Transparent,
            ValueAccessors = TestStateItem.Accessors,
            RowKey = "server",
            StartKey = "start",
            StateKey = "state",
            Maximum = Origin.AddHours(4),
            ItemsSource = new List<TestStateItem>
            {
                new("web", Origin, "Running"),
                new("web", Origin.AddHours(2), "Stopped"),
                new("web", Origin.AddHours(3), "Running"),
                new("db", Origin.AddHours(1), "Running")
            }
        };
        view.ValueMappings.Add(new ChartValueMapping { Value = "Running", Level = ChartThresholdLevel.Good });
        view.ValueMappings.Add(new ChartValueMapping { Value = "Stopped", Level = ChartThresholdLevel.Critical });
        view.OnAttached();
        return view;
    }

    private static Point Point(TestClamStateTimelineView view, int row, double hours)
    {
        var layout = view.ChartLayout;
        var scale = layout.Axis.Scale;
        var x = layout.Plot.X + ((Origin.AddHours(hours).Ticks - scale.Minimum) / scale.Span * layout.Plot.Width);
        return new Point(x, layout.GetRowTop(row) + (layout.RowHeight / 2));
    }

    private static void Render(TestClamStateTimelineView view, SKCanvas canvas)
    {
        canvas.Save();
        canvas.Scale(Scale);
        view.Render(canvas, 300, 200);
        canvas.Restore();
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static SKColor GetPixel(SKBitmap bitmap, Point point) => GetPixel(bitmap, point.X, point.Y);

    private static int CountOther(SKBitmap bitmap, ChartRect bounds, SKColor color)
    {
        var count = 0;
        for (var y = (int)((bounds.Y + 2) * Scale); y < (int)((bounds.Bottom - 2) * Scale); y++)
        {
            for (var x = (int)((bounds.X + 2) * Scale); x < (int)((bounds.Right - 2) * Scale); x++)
            {
                if (!ColorComparer.Loose.Equals(bitmap.GetPixel(x, y), color))
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
