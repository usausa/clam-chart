namespace ClamChart.Tests.Animation;

using ClamChart.Animation;

public sealed class SeriesInterpolationTests
{
    [Fact]
    public void ValuesMorphFromTheShownStateToTheTarget()
    {
        // Arrange
        using var previous = new SeriesInterpolation();
        using var next = new SeriesInterpolation();
        using var before = Line([0, 1, 2], [10, 20, 30]);
        using var after = Line([0, 1, 2], [30, 20, 10]);
        previous.SetIdentity(before, 1);

        // Act
        next.BuildMorph(previous, [], true, after, SeriesCollapse.None);
        next.Apply(0.5);

        // Assert
        Assert.True(next.IsMoving);
        Assert.Equal([20d, 20, 20], next.Display.End.ToArray());
        Assert.Equal([0, 1, 2], Enumerable.Range(0, 3).Select(next.GetUnionIndex));
    }

    [Fact]
    public void SameStateDoesNotMove()
    {
        // Arrange
        using var previous = new SeriesInterpolation();
        using var next = new SeriesInterpolation();
        using var geometry = Line([0, 1], [5, 6]);
        previous.SetIdentity(geometry, 1);

        // Act
        next.BuildMorph(previous, [], true, geometry, SeriesCollapse.None);

        // Assert
        Assert.False(next.IsMoving);
    }

    [Fact]
    public void StreamingFlowsTheHeadOutAndGrowsTheTailFromTheLastPoint()
    {
        // Arrange
        using var previous = new SeriesInterpolation();
        using var next = new SeriesInterpolation();
        using var before = Line([0, 1, 2], [10, 20, 30]);
        using var after = Line([0, 1, 2], [20, 30, 40]);
        previous.SetIdentity(before, 1);

        // Act
        next.BuildMorph(previous, [-1, 0, 1], false, after, SeriesCollapse.None);
        next.Apply(0);
        var (startX, startEnd) = (next.Display.X.ToArray(), next.Display.End.ToArray());
        next.Apply(1);

        // Assert
        Assert.Equal(4, next.Display.Count);
        Assert.Equal([0d, 1, 2, 2], startX);
        Assert.Equal([10d, 20, 30, 30], startEnd);
        Assert.Equal([-1d, 0, 1, 2], next.Display.X.ToArray());
        Assert.Equal([10d, 20, 30, 40], next.Display.End.ToArray());
        Assert.Equal([-1, 0, 1, 2], Enumerable.Range(0, 4).Select(next.GetTargetIndex));
        Assert.Equal([1, 2, 3], Enumerable.Range(0, 3).Select(next.GetUnionIndex));
    }

    [Fact]
    public void RemovedLinePointIsAbsorbedAndRemovedBarShrinks()
    {
        // Arrange
        using var previous = new SeriesInterpolation();
        using var line = new SeriesInterpolation();
        using var bar = new SeriesInterpolation();
        using var before = Line([0, 1, 2], [10, 20, 30]);
        using var after = Line([0, 1], [10, 30]);
        previous.SetIdentity(before, 1);

        // Act
        line.BuildMorph(previous, [0, -1, 1], false, after, SeriesCollapse.None);
        bar.BuildMorph(previous, [0, -1, 1], false, after, SeriesCollapse.Base);
        line.Apply(1);
        bar.Apply(1);

        // Assert
        Assert.Equal([0d, 0, 1], line.Display.X.ToArray());
        Assert.Equal([10d, 10, 30], line.Display.End.ToArray());
        Assert.Equal(20, bar.Display.Start[1]);
        Assert.Equal(20, bar.Display.End[1]);
    }

    [Fact]
    public void InsertedBarGrowsFromItsBase()
    {
        // Arrange
        using var previous = new SeriesInterpolation();
        using var next = new SeriesInterpolation();
        using var before = Bars([0, 1], [10, 20]);
        using var after = Bars([0, 1, 2], [10, 20, 30]);
        previous.SetIdentity(before, 1);

        // Act
        next.BuildMorph(previous, [0, 1], false, after, SeriesCollapse.Base);
        next.Apply(0);

        // Assert
        Assert.Equal(3, next.Display.Count);
        Assert.Equal(2, next.Display.X[2]);
        Assert.Equal(0, next.Display.End[2]);
    }

    [Fact]
    public void DepartingPointKeepsFlowingWhenInterrupted()
    {
        // Arrange
        using var first = new SeriesInterpolation();
        using var second = new SeriesInterpolation();
        using var third = new SeriesInterpolation();
        using var before = Line([0, 1, 2], [10, 20, 30]);
        using var middle = Line([0, 1, 2], [20, 30, 40]);
        using var after = Line([0, 1, 2], [30, 40, 50]);
        first.SetIdentity(before, 1);
        second.BuildMorph(first, [-1, 0, 1], false, middle, SeriesCollapse.None);
        second.Apply(0.5);

        // Act
        third.BuildMorph(second, [-1, 0, 1], false, after, SeriesCollapse.None);
        third.Apply(0);
        var start = third.Display.X.ToArray();
        third.Apply(1);

        // Assert
        Assert.Equal([-0.5, 0.5, 1.5, 2, 2], start);
        Assert.Equal(-2, third.Display.X[0]);
        Assert.Equal(-1, third.Display.X[1]);
        Assert.Equal([-1, -1, 0, 1, 2], Enumerable.Range(0, 5).Select(third.GetTargetIndex));
    }

    [Fact]
    public void HiddenLineFadesAndHiddenBarCollapses()
    {
        // Arrange
        using var previous = new SeriesInterpolation();
        using var line = new SeriesInterpolation();
        using var bar = new SeriesInterpolation();
        using var geometry = Bars([0, 1], [10, 20]);
        previous.SetIdentity(geometry, 1);

        // Act
        line.BuildHide(previous, SeriesCollapse.None);
        bar.BuildHide(previous, SeriesCollapse.Base);
        line.Apply(1);
        bar.Apply(1);

        // Assert
        Assert.Equal(0, line.Opacity);
        Assert.Equal([10d, 20], line.Display.End.ToArray());
        Assert.Equal(1, bar.Opacity);
        Assert.Equal([0d, 0], bar.Display.End.ToArray());
    }

    [Fact]
    public void ShownBarGrowsAndShownLineFadesIn()
    {
        // Arrange
        using var line = new SeriesInterpolation();
        using var bar = new SeriesInterpolation();
        using var geometry = Bars([0, 1], [10, 20]);

        // Act
        line.BuildShow(geometry, SeriesCollapse.None);
        bar.BuildShow(geometry, SeriesCollapse.Base);

        // Assert
        Assert.Equal(0, line.Opacity);
        Assert.Equal([10d, 20], line.Display.End.ToArray());
        Assert.Equal(1, bar.Opacity);
        Assert.Equal([0d, 0], bar.Display.End.ToArray());
        Assert.True(line.IsMoving);
        Assert.True(bar.IsMoving);
    }

    [Fact]
    public void RangesCollapseToTheirMiddle()
    {
        // Arrange
        using var previous = new SeriesInterpolation();
        using var hidden = new SeriesInterpolation();
        using var shown = new SeriesInterpolation();
        using var inserted = new SeriesInterpolation();
        using var before = Create([0, 1], [10, 20], [30, 40]);
        using var after = Create([0, 1, 2], [10, 20, 0], [30, 40, 50]);
        previous.SetIdentity(before, 1);

        // Act
        hidden.BuildHide(previous, SeriesCollapse.Middle);
        hidden.Apply(1);
        shown.BuildShow(before, SeriesCollapse.Middle);
        inserted.BuildMorph(previous, [0, 1], false, after, SeriesCollapse.Middle);
        inserted.Apply(0);

        // Assert
        Assert.Equal([20d, 30], hidden.Display.Start.ToArray());
        Assert.Equal([20d, 30], hidden.Display.End.ToArray());
        Assert.Equal(1, hidden.Opacity);
        Assert.Equal([20d, 30], shown.Display.Start.ToArray());
        Assert.Equal([20d, 30], shown.Display.End.ToArray());
        Assert.Equal(25, inserted.Display.Start[2]);
        Assert.Equal(25, inserted.Display.End[2]);
    }

    private static SeriesGeometry Line(double[] x, double[] y) => Create(x, y, y);

    private static SeriesGeometry Bars(double[] x, double[] y) => Create(x, new double[y.Length], y);

    private static SeriesGeometry Create(double[] x, double[] start, double[] end)
    {
        var geometry = new SeriesGeometry();
        geometry.Reset(x.Length);
        x.CopyTo(geometry.X);
        start.CopyTo(geometry.Start);
        end.CopyTo(geometry.End);
        end.CopyTo(geometry.Value);
        geometry.Outer.Fill(true);
        return geometry;
    }
}
