namespace ClamChart.Tests;

public sealed class ChartDelegateBindingTests
{
    private static readonly Func<double, string> Fixed = static value => $"F:{value:F1}";

    [Fact]
    public void DelegateIsAppliedThroughAStyleSetter()
    {
        // Arrange
        var style = new Style(typeof(ClamStatView)) { ApplyToDerivedTypes = true };
        style.Setters.Add(new Setter { Property = ClamStatView.ValueFormatterProperty, Value = Fixed });
        using var view = new ClamStatView();

        // Act
        view.Style = style;

        // Assert
        Assert.Same(Fixed, view.ValueFormatter);
    }

    [Fact]
    public void DelegateIsAppliedThroughABinding()
    {
        // Arrange
        using var view = new ClamStatView();
        view.BindingContext = new FormatterSource();

        // Act
        view.SetBinding(ClamStatView.ValueFormatterProperty, static (FormatterSource source) => source.Formatter);

        // Assert
        Assert.Same(Fixed, view.ValueFormatter);
    }

    [Fact]
    public void DelegateOfTheSeriesFollowsADynamicResource()
    {
        // Arrange
        using var view = new TestClamChartView();
        view.Resources = new ResourceDictionary { { "Formatter", Fixed } };
        var series = new LineSeries();
        view.Series.Add(series);

        // Act
        series.SetDynamicResource(ChartSeries.ValueFormatterProperty, "Formatter");

        // Assert
        Assert.Same(Fixed, series.ValueFormatter);
    }

    [Fact]
    public void SettingADelegateUpdatesTheLayout()
    {
        // Arrange
        using var view = new TestChartView();
        view.Paint(200, 100);

        // Act
        view.AutomationSummaryFormatter = static _ => "summary";
        view.Paint(200, 100);

        // Assert
        Assert.Equal(2, view.LayoutUpdates);
    }

    internal sealed class FormatterSource
    {
        public Func<double, string> Formatter { get; } = Fixed;
    }
}
