namespace ClamChart.Tests.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class StateTimelineLayoutTests
{
    private const int Precision = 9;

    private static readonly TestTextMeasurer Measurer = new();

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    private static readonly ChartStyle Style = new();

    private static readonly DateTime Origin = new(2026, 10, 22, 0, 0, 0);

    [Fact]
    public void StatesLastUntilTheNextStart()
    {
        // Arrange
        StateTimelineEntry[] entries = [Text(0, "web", 0, "Running"), Text(1, "web", 2, "Stopped"), Text(2, "web", 3, "Running")];

        // Act
        var layout = Layout(entries);

        // Assert
        Assert.Equal([(0d, 2d), (2d, 3d)], layout.Segments.Take(2).Select(static x => (Hours(x.Start), Hours(x.End))));
        Assert.True(Double.IsPositiveInfinity(layout.Segments[2].End));
        Assert.Equal(["Running", "Stopped", "Running"], Enumerable.Range(0, 3).Select(layout.GetText));
    }

    [Fact]
    public void EqualStatesNextToEachOtherAreMerged()
    {
        // Arrange
        StateTimelineEntry[] entries = [Text(0, "web", 0, "Running"), Text(1, "web", 1, "Running"), Text(2, "web", 2, "Stopped")];

        // Act
        var merged = Layout(entries);
        var separate = Layout(entries, new StateTimelineLayoutOptions { MergeValues = false });

        // Assert
        Assert.Equal(2, merged.Segments.Count);
        Assert.Equal(2, Hours(merged.Segments[0].End));
        Assert.Equal(0, merged.Segments[0].DataIndex);
        Assert.Equal(3, separate.Segments.Count);
    }

    [Fact]
    public void EndsAndMissingStatesLeaveGaps()
    {
        // Arrange
        StateTimelineEntry[] entries =
        [
            new(0, "web", Ticks(0), Ticks(1), "Running", Double.NaN),
            Text(1, "web", 2, "Running"),
            Text(2, "web", 3, null),
            Text(3, "web", 4, "Stopped"),
            new(4, "web", Ticks(5), Ticks(9), "Running", Double.NaN),
            Text(5, "web", 6, "Stopped")
        ];

        // Act
        var layout = Layout(entries);

        // Assert
        Assert.Equal([(0d, 1d), (2d, 3d), (4d, 5d), (5d, 6d)], layout.Segments.Take(4).Select(static x => (Hours(x.Start), Hours(x.End))));
        Assert.Equal(5, layout.Segments.Count);
    }

    [Fact]
    public void RowsFollowTheFirstAppearance()
    {
        // Arrange
        StateTimelineEntry[] entries = [Text(0, "db", 1, "Running"), Text(1, "web", 0, "Running"), Text(2, "db", 0, "Stopped")];

        // Act
        var layout = Layout(entries);

        // Assert
        Assert.Equal(["db", "web"], layout.RowNames);
        Assert.Equal([0, 0, 1], layout.Segments.Select(static x => x.Row));
        Assert.Equal(["Stopped", "Running", "Running"], Enumerable.Range(0, 3).Select(layout.GetText));
    }

    [Fact]
    public void MappingsGiveTheTextAndColor()
    {
        // Arrange
        StateTimelineEntry[] entries = [Text(0, null, 0, "run"), Text(1, null, 1, "stop"), Text(2, null, 2, "idle")];
        var options = new StateTimelineLayoutOptions
        {
            Mappings =
            [
                new ChartValueMapping { Value = "run", Text = "Running", Level = ChartThresholdLevel.Good },
                new ChartValueMapping { Value = "stop", Color = Colors.Red }
            ]
        };

        // Act
        var layout = Layout(entries, options);

        // Assert
        Assert.Equal(["Running", "stop", "idle"], Enumerable.Range(0, 3).Select(layout.GetText));
        Assert.Equal([Style.GoodColor.ToSKColor(), SKColors.Red, Style.GoodColor.ToSKColor()], Enumerable.Range(0, 3).Select(layout.GetColor));
        Assert.Equal(["Running", "stop", "idle"], layout.LegendTexts);
    }

    [Fact]
    public void NumbersUseTheGreenYellowRedScaleOfEachRow()
    {
        // Arrange
        StateTimelineEntry[] entries = [Number(0, "a", 0, 10), Number(1, "a", 1, 20), Number(2, "a", 2, 30), Number(3, "b", 0, 1)];
        var scale = new[] { Style.GoodColor, Style.WarningColor, Style.CriticalColor };

        // Act
        var layout = Layout(entries);

        // Assert
        Assert.Equal(Style.GoodColor.ToSKColor(), layout.GetColor(0));
        Assert.Equal(HeatmapPalette.GetColor(scale, 0.5), layout.GetColor(1));
        Assert.Equal(Style.CriticalColor.ToSKColor(), layout.GetColor(2));
        Assert.Equal(Style.GoodColor.ToSKColor(), layout.GetColor(3));
        Assert.Equal(["10", "20", "30", "1"], Enumerable.Range(0, 4).Select(layout.GetText));
        Assert.Empty(layout.LegendTexts);
    }

    [Fact]
    public void ThresholdsTurnNumbersIntoSteps()
    {
        // Arrange
        StateTimelineEntry[] entries = [Number(0, null, 0, 20), Number(1, null, 1, 40), Number(2, null, 2, 70), Number(3, null, 3, 90)];
        var options = new StateTimelineLayoutOptions
        {
            Thresholds =
            [
                new ChartThreshold { Value = 85, Level = ChartThresholdLevel.Critical },
                new ChartThreshold { Level = ChartThresholdLevel.Good },
                new ChartThreshold { Value = 60, Level = ChartThresholdLevel.Warning }
            ]
        };

        // Act
        var layout = Layout(entries, options);
        var separate = Layout(entries, options with { MergeValues = false });

        // Assert
        Assert.Equal(["< 60", "60+", "85+"], Enumerable.Range(0, 3).Select(layout.GetText));
        Assert.Equal(2, Hours(layout.Segments[0].End));
        Assert.Equal(Style.WarningColor.ToSKColor(), layout.GetColor(1));
        Assert.Equal(["< 60", "60+", "85+"], layout.LegendTexts);
        Assert.Equal(["20", "40", "70", "90"], Enumerable.Range(0, 4).Select(separate.GetText));
        Assert.Equal(Style.GoodColor.ToSKColor(), separate.GetColor(1));
    }

    [Fact]
    public void RowsShareThePlotHeight()
    {
        // Arrange
        StateTimelineEntry[] entries = [Text(0, "a", 0, "x"), Text(1, "b", 0, "x"), Text(2, "c", 0, "x")];

        // Act
        var layout = Layout(entries);
        var single = Layout([Text(0, null, 0, "x")]);

        // Assert
        var height = layout.Plot.Height;
        Assert.Equal(height * 0.3, layout.RowHeight, Precision);
        Assert.Equal((height * 0.3) + (height * 0.05), layout.RowPitch, Precision);
        Assert.Equal(layout.Plot.Bottom, layout.GetRowTop(2) + layout.RowHeight, Precision);
        Assert.Equal(single.Plot.Height * 0.9, single.RowHeight, Precision);
        Assert.Empty(single.RowLabels);
        Assert.Equal(["a", "b", "c"], layout.RowLabels.Select(static x => x.Text));
    }

    [Fact]
    public void TheRangeFollowsTheDataUnlessFixed()
    {
        // Arrange
        StateTimelineEntry[] entries = [Text(0, null, 1, "x"), new(1, null, Ticks(2), Ticks(5), "y", Double.NaN), Text(2, null, 3, "z")];

        // Act
        var layout = Layout(entries);
        var fixedRange = Layout(entries, new StateTimelineLayoutOptions { Minimum = Ticks(0), Maximum = Ticks(10) });

        // Assert
        Assert.Equal(1, Hours(layout.Axis.Scale.Minimum));
        Assert.Equal(3, Hours(layout.Axis.Scale.Maximum));
        Assert.Equal(0, Hours(fixedRange.Axis.Scale.Minimum));
        Assert.Equal(10, Hours(fixedRange.Axis.Scale.Maximum));
    }

    [Fact]
    public void SegmentsAreFoundByRowAndTime()
    {
        // Arrange
        StateTimelineEntry[] entries = [Text(0, "a", 0, "x"), Text(1, "a", 2, "y"), Text(2, "b", 1, "z")];

        // Act
        var layout = Layout(entries);

        // Assert
        Assert.Equal(1, layout.FindSegment(0, Ticks(2.5), Ticks(3)));
        Assert.Equal(-1, layout.FindSegment(1, Ticks(0.5), Ticks(3)));
        Assert.Equal(1, layout.FindRow(layout.GetRowTop(1) + 1));
        Assert.Equal(-1, layout.FindRow(layout.GetRowTop(1) - 1));
        Assert.Equal(1, layout.FindNearestRow(layout.Plot.Bottom + 50));
        Assert.Equal(1, layout.GetLastSegment(0));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(250, "250ms")]
    [InlineData(90 * 1000, "1m 30s")]
    [InlineData(2.5 * 3600 * 1000, "2h 30m")]
    [InlineData(27 * 3600 * 1000, "1d 3h")]
    [InlineData(10 * 24 * 3600 * 1000, "1w 3d")]
    public void DurationsAreWrittenLikeGrafana(double milliseconds, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, StateTimelineLayout.FormatDuration(milliseconds * TimeSpan.TicksPerMillisecond));
    }

    private static double Ticks(double hours) => Origin.AddHours(hours).Ticks;

    private static double Hours(double ticks) => (ticks - Origin.Ticks) / TimeSpan.TicksPerHour;

    private static StateTimelineEntry Text(int index, string? row, double hours, string? state) => new(index, row, Ticks(hours), Double.NaN, state, Double.NaN);

    private static StateTimelineEntry Number(int index, string? row, double hours, double value) => new(index, row, Ticks(hours), Double.NaN, null, value);

    private static StateTimelineLayout Layout(StateTimelineEntry[] entries, StateTimelineLayoutOptions? options = null)
    {
        var layout = new StateTimelineLayout();
        layout.Update(300, 200, new Thickness(0), Style, Culture, options ?? new StateTimelineLayoutOptions(), entries, Measurer);
        return layout;
    }
}
