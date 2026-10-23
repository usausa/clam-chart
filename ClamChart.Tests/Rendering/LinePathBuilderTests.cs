namespace ClamChart.Tests.Rendering;

using SkiaSharp;

public sealed class LinePathBuilderTests
{
    [Fact]
    public void StepHoldsTheValueUntilTheNextPoint()
    {
        // Arrange
        using var builder = new SKPathBuilder();

        // Act
        LinePathBuilder.AddLine(builder, [0f, 10, 20], [5f, 1, 3], [0f, 0, 0], ChartLineInterpolation.Step, false);
        using var path = builder.Detach();

        // Assert
        Assert.Equal([new SKPoint(0, 5), new SKPoint(10, 5), new SKPoint(10, 1), new SKPoint(20, 1), new SKPoint(20, 3)], path.Points);
    }

    [Fact]
    public void StepBeforeMovesToTheNextValueFirst()
    {
        // Arrange
        using var builder = new SKPathBuilder();

        // Act
        LinePathBuilder.AddLine(builder, [0f, 10, 20], [5f, 1, 3], [0f, 0, 0], ChartLineInterpolation.StepBefore, false);
        using var path = builder.Detach();

        // Assert
        Assert.Equal([new SKPoint(0, 5), new SKPoint(0, 1), new SKPoint(10, 1), new SKPoint(10, 3), new SKPoint(20, 3)], path.Points);
    }

    [Fact]
    public void ReversedStepBeforeTracesTheSameShapeBack()
    {
        // Arrange
        using var builder = new SKPathBuilder();
        builder.MoveTo(0, 0);

        // Act
        LinePathBuilder.AddReversedLine(builder, [0f, 10, 20], [5f, 1, 3], [0f, 0, 0], ChartLineInterpolation.StepBefore, false);
        using var path = builder.Detach();

        // Assert
        Assert.Equal([new SKPoint(0, 0), new SKPoint(20, 3), new SKPoint(10, 3), new SKPoint(10, 1), new SKPoint(0, 1), new SKPoint(0, 5)], path.Points);
    }

    [Fact]
    public void HorizontalLinesSwapTheCoordinates()
    {
        // Arrange
        using var builder = new SKPathBuilder();

        // Act
        LinePathBuilder.AddLine(builder, [0f, 10], [5f, 1], [0f, 0], ChartLineInterpolation.Linear, true);
        using var path = builder.Detach();

        // Assert
        Assert.Equal([new SKPoint(5, 0), new SKPoint(1, 10)], path.Points);
    }

    [Fact]
    public void ReversedStepTracesTheSameShapeBack()
    {
        // Arrange
        using var builder = new SKPathBuilder();
        builder.MoveTo(0, 0);

        // Act
        LinePathBuilder.AddReversedLine(builder, [0f, 10, 20], [5f, 1, 3], [0f, 0, 0], ChartLineInterpolation.Step, false);
        using var path = builder.Detach();

        // Assert
        Assert.Equal([new SKPoint(0, 0), new SKPoint(20, 3), new SKPoint(20, 1), new SKPoint(10, 1), new SKPoint(10, 5), new SKPoint(0, 5)], path.Points);
    }

    [Fact]
    public void SinglePointIsADot()
    {
        // Arrange
        using var builder = new SKPathBuilder();

        // Act
        LinePathBuilder.AddLine(builder, [4f], [6f], [0f], ChartLineInterpolation.Smooth, false);
        using var path = builder.Detach();

        // Assert
        Assert.Equal([new SKPoint(4, 6), new SKPoint(4, 6)], path.Points);
    }

    [Fact]
    public void TangentsAreFlatAtTurnsAndPlateaus()
    {
        // Arrange
        var tangents = new float[5];

        // Act
        LinePathBuilder.ComputeTangents([0f, 1, 2, 3, 4], [0f, 2, 2, 1, 3], tangents);

        // Assert
        Assert.Equal(2, tangents[0]);
        Assert.Equal(0, tangents[1]);
        Assert.Equal(0, tangents[2]);
        Assert.Equal(0, tangents[3]);
        Assert.Equal(2, tangents[4]);
    }

    [Fact]
    public void SmoothCurveStaysBetweenItsPoints()
    {
        // Arrange
        float[] along = [0, 10, 20, 30, 40];
        float[] across = [0, 1, 30, 31, 32];
        var tangents = new float[along.Length];

        // Act
        LinePathBuilder.ComputeTangents(along, across, tangents);

        // Assert
        for (var i = 0; i < along.Length - 1; i++)
        {
            var third = (along[i + 1] - along[i]) / 3;
            var control1 = across[i] + (tangents[i] * third);
            var control2 = across[i + 1] - (tangents[i + 1] * third);
            for (var t = 0.05f; t < 1; t += 0.05f)
            {
                var u = 1 - t;
                var value = (u * u * u * across[i]) + (3 * u * u * t * control1) + (3 * u * t * t * control2) + (t * t * t * across[i + 1]);
                Assert.InRange(value, across[i] - 0.0001f, across[i + 1] + 0.0001f);
            }
        }
    }
}
