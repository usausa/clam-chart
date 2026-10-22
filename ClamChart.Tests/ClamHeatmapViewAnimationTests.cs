namespace ClamChart.Tests;

using SkiaSharp;

[Collection(nameof(ClamChartSettings))]
public sealed class ClamHeatmapViewAnimationTests
{
    private const string Data = "ClamHeatmapViewData";

    private const string Selection = "ClamHeatmapViewSelection";

    private const float Scale = 2;

    [Fact]
    public void FirstDataFadesInFromTheLeft()
    {
        // Arrange
        using var view = Chart(Entries(100, 100, 100, 100));
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 100);
        driver.Frame(Data, 0.5);
        using var middle = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(ChartEntranceAnimation.Fade, view.Interpolator.Entrance);
        var left = GetPixel(middle, Center(layout, 0)).Alpha;
        var right = GetPixel(middle, Center(layout, 3)).Alpha;
        Assert.InRange(left, 1, 254);
        Assert.True(left > right);

        // Act
        driver.Finish(Data);
        using var last = view.Paint(300, 100);

        // Assert
        Assert.Equal(255, GetPixel(last, Center(layout, 3)).Alpha);
    }

    [Fact]
    public void GrowStartsTheCellsFromTheirCenters()
    {
        // Arrange
        using var view = Chart(Entries(100, 100));
        view.EntranceAnimation = ChartEntranceAnimation.Grow;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 100);
        driver.Frame(Data, 0.3);
        using var middle = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        var cell = layout.GetCellBounds(0);
        Assert.Equal(255, GetPixel(middle, Center(layout, 0)).Alpha);
        Assert.Equal(0, GetPixel(middle, cell.X + 2, cell.Y + 2).Alpha);
    }

    [Fact]
    public void RevealSweepsFromTheLeft()
    {
        // Arrange
        using var view = Chart(Entries(100, 100));
        view.EntranceAnimation = ChartEntranceAnimation.Reveal;
        var driver = Driver(view);

        // Act
        using var first = view.Paint(300, 100);
        driver.Frame(Data, 0.4);
        using var middle = view.Paint(300, 100);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(255, GetPixel(middle, Center(layout, 0)).Alpha);
        Assert.Equal(0, GetPixel(middle, Center(layout, 1)).Alpha);
    }

    [Fact]
    public void UpdateMovesTheValuesAndTheRange()
    {
        // Arrange
        var items = new ObservableCollection<ChartEntry>([new("A", 0), new("B", 10)]);
        using var view = Quiet(items);
        var driver = Driver(view);
        using var first = view.Paint(300, 100);

        // Act
        items[1] = new ChartEntry("B", 20);
        using var second = view.Paint(300, 100);
        driver.Frame(Data, 0.5);

        // Assert
        Assert.Equal([(Data, 450u)], driver.Starts);
        Assert.Equal(15, view.Interpolator.GetValue(1));
        Assert.Equal(15, view.Interpolator.Maximum);
        Assert.Equal(20, view.ChartLayout.Maximum);
    }

    [Fact]
    public void NewColumnsReplaceTheCellsWithoutMorphing()
    {
        // Arrange
        var items = new ObservableCollection<ChartEntry>([new("A", 0), new("B", 10)]);
        using var view = Quiet(items);
        var driver = Driver(view);
        using var first = view.Paint(300, 100);

        // Act
        items.Add(new ChartEntry("C", 5));
        using var second = view.Paint(300, 100);

        // Assert
        Assert.Empty(driver.Starts);
        Assert.False(view.Interpolator.IsMorphing);
        Assert.Equal(5, view.Interpolator.GetValue(2));
    }

    [Fact]
    public void SelectionChangesOverAShortAnimation()
    {
        // Arrange
        using var view = Quiet(Entries(1, 2, 3));
        view.SelectionMode = ChartSelectionMode.Item;
        var driver = Driver(view);
        using var first = view.Paint(300, 100);

        // Act
        view.SelectedIndex = 1;

        // Assert
        Assert.Equal([(Selection, 150u)], driver.Starts);
        Assert.True(view.IsAnimating);

        // Act
        driver.Finish(Selection);

        // Assert
        Assert.False(view.IsAnimating);
    }

    private static TestClamHeatmapView Chart(IEnumerable items)
    {
        var view = new TestClamHeatmapView { AnimationDriver = new TestAnimationDriver(), AnimationEasing = Easing.Linear, ItemsSource = items };
        view.OnAttached();
        return view;
    }

    private static TestClamHeatmapView Quiet(IEnumerable items)
    {
        var view = Chart(items);
        view.EntranceAnimation = ChartEntranceAnimation.None;
        return view;
    }

    private static TestAnimationDriver Driver(TestClamHeatmapView view) => (TestAnimationDriver)view.AnimationDriver;

    private static List<ChartEntry> Entries(params double[] values) =>
        [.. values.Select(static (value, index) => new ChartEntry($"C{index}", value))];

    private static (double X, double Y) Center(HeatmapLayout layout, int cell)
    {
        var bounds = layout.GetCellBounds(cell);
        return (bounds.X + ((bounds.Width - layout.Gap) / 2), bounds.Y + ((bounds.Height - layout.Gap) / 2));
    }

    private static SKColor GetPixel(SKBitmap bitmap, double x, double y) => bitmap.GetPixel((int)(x * Scale), (int)(y * Scale));

    private static SKColor GetPixel(SKBitmap bitmap, (double X, double Y) point) => GetPixel(bitmap, point.X, point.Y);
}
