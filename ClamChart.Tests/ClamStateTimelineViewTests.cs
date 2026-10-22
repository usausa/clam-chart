namespace ClamChart.Tests;

using ClamChart.Tests.Data;

public sealed class ClamStateTimelineViewTests
{
    private static readonly DateTime Origin = new(2026, 10, 22, 0, 0, 0);

    [Fact]
    public void DefaultsFollowGrafana()
    {
        // Arrange
        using var view = new TestClamStateTimelineView();

        // Assert
        Assert.True(view.MergeValues);
        Assert.Equal(ChartVisibilityMode.Auto, view.ShowValue);
        Assert.Equal(ChartValueAlignment.Left, view.AlignValue);
        Assert.Equal(0.9, view.RowHeight);
        Assert.Equal(0.7, view.FillOpacity);
        Assert.Equal(0, view.LineWidth);
        Assert.Equal(ChartLegendPosition.Bottom, view.LegendPosition);
        Assert.Equal(ChartTrackballMode.None, view.TrackballMode);
        Assert.Empty(view.ValueMappings);
        Assert.Empty(view.Thresholds);
    }

    [Fact]
    public void InvalidValuesAreRejected()
    {
        // Arrange
        using var view = new TestClamStateTimelineView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => view.RowHeight = 1.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.RowHeight = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.FillOpacity = -0.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.LineWidth = -1);
        Assert.Throws<ArgumentNullException>(() => view.ValueMappings = null!);
        Assert.Throws<ArgumentNullException>(() => view.Thresholds = null!);
    }

    [Fact]
    public void ItemsAreReadWithTheKeys()
    {
        // Arrange
        using var view = Servers();

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        var layout = view.ChartLayout;
        Assert.Equal(["web", "db"], layout.RowNames);
        Assert.Equal(["Running", "Stopped", "Running", "Running"], Enumerable.Range(0, 4).Select(layout.GetText));
        Assert.Equal(4, view.RenderedPointCount);
    }

    [Fact]
    public void MappingChangesAreFollowed()
    {
        // Arrange
        using var view = Servers();
        view.OnAttached();
        using var first = view.Paint(300, 200);

        // Act
        view.ValueMappings.Add(new ChartValueMapping { Value = "Stopped", Text = "Down", Level = ChartThresholdLevel.Critical });
        using var second = view.Paint(300, 200);

        // Assert
        Assert.Equal("Down", view.ChartLayout.GetText(1));
    }

    [Fact]
    public void MissingKeysAreReportedOnce()
    {
        // Arrange
        using var capture = new TraceCapture();
        using var view = new TestClamStateTimelineView { ValueAccessors = TestStateItem.Accessors, StartKey = "start", ItemsSource = Items() };
        var message = "The state timeline needs StartKey and StateKey.";
        var before = capture.Count(message);

        // Act
        using var first = view.Paint(300, 200);
        view.ItemsSource = Items();
        using var second = view.Paint(300, 200);

        // Assert
        Assert.Equal(before + 1, capture.Count(message));
        Assert.Equal(0, view.RenderedPointCount);
    }

    [Fact]
    public void SummaryNamesTheLastStateOfEachRow()
    {
        // Arrange
        using var view = Servers();
        view.Culture = CultureInfo.InvariantCulture;

        // Act
        using var bitmap = view.Paint(300, 200);

        // Assert
        Assert.Equal("web: last Running; db: last Running", SemanticProperties.GetDescription(view));
    }

    private static List<TestStateItem> Items() =>
    [
        new("web", Origin, "Running"),
        new("web", Origin.AddHours(2), "Stopped"),
        new("db", Origin, "Running"),
        new("web", Origin.AddHours(3), "Running")
    ];

    private static TestClamStateTimelineView Servers() =>
        new()
        {
            ValueAccessors = TestStateItem.Accessors,
            RowKey = "server",
            StartKey = "start",
            EndKey = "end",
            StateKey = "state",
            ItemsSource = Items()
        };
}
