namespace ClamChart.Tests;

using SkiaSharp;

public sealed class ChartAxisStyleTests
{
    private static readonly SKColor Red = new(255, 0, 0);

    private static readonly SKColor Blue = new(0, 0, 255);

    [Fact]
    public void AxisCarriesItsOwnDefaults()
    {
        // Arrange
        var axis = new NumericAxis();

        // Act & Assert
        Assert.Equal(11d, axis.LabelFontSize);
        Assert.Equal(Color.FromArgb("#C3C2B7"), axis.LineColor);
        Assert.Equal(Color.FromArgb("#E1E0D9"), axis.GridLineColor);
        Assert.Equal(0d, axis.GridLineThickness);
    }

    [Fact]
    public void InvalidAxisValuesThrowWhenAssigned()
    {
        // Arrange
        var axis = new NumericAxis();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.LabelFontSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.LabelFontSize = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.GridLineThickness = -1);
    }

    [Fact]
    public void EachAxisDrawsItsOwnGridLines()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.XAxis = new CategoryAxis { GridLinesVisible = true, GridLineColor = Colors.Red, GridLineThickness = 2 };
        view.YAxis = new NumericAxis { GridLinesVisible = true, GridLineColor = Colors.Blue, GridLineThickness = 2 };
        view.Series.Add(new LineSeries { ItemsSource = new List<double> { 2, 5, 3 } });

        // Act
        using var bitmap = view.Paint(360, 240);

        // Assert
        Assert.True(Contains(bitmap, Red), "縦の罫線が X 軸の色で描かれていない");
        Assert.True(Contains(bitmap, Blue), "横の罫線が Y 軸の色で描かれていない");
    }

    [Fact]
    public void LabelFontSizeOfTheAxisChangesTheLayout()
    {
        // Arrange
        using var small = new TestClamChartView();
        using var large = new TestClamChartView();
        small.YAxis = new NumericAxis { LabelFontSize = 8 };
        large.YAxis = new NumericAxis { LabelFontSize = 28 };
        small.Series.Add(new LineSeries { ItemsSource = new List<double> { 1000, 2000, 3000 } });
        large.Series.Add(new LineSeries { ItemsSource = new List<double> { 1000, 2000, 3000 } });

        // Act
        using var smallBitmap = small.Paint(360, 240);
        using var largeBitmap = large.Paint(360, 240);

        // Assert
        Assert.True(InkedColumns(smallBitmap) > InkedColumns(largeBitmap), "大きい文字のほうが描画域が狭くなっていない");
    }

    private static bool Contains(SKBitmap bitmap, SKColor color)
    {
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == color)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int InkedColumns(SKBitmap bitmap)
    {
        var count = 0;
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }
}
