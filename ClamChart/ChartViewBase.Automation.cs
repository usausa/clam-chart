namespace ClamChart;

using System.Globalization;

using ClamChart.Rendering;

public abstract partial class ChartViewBase
{
    public static readonly BindableProperty AutomationSummaryFormatterProperty = BindableProperty.Create(nameof(AutomationSummaryFormatter), typeof(Func<ChartViewBase, string>), typeof(ChartViewBase), propertyChanged: OnLayoutInputChanged);

    private string? automationText;

    public Func<ChartViewBase, string?>? AutomationSummaryFormatter
    {
        get => (Func<ChartViewBase, string?>?)GetValue(AutomationSummaryFormatterProperty);
        set => SetValue(AutomationSummaryFormatterProperty, value);
    }

    internal virtual string? CreateAutomationSummary(ChartSummaryText text) => null;

    private void UpdateAutomationSummary()
    {
        var current = SemanticProperties.GetDescription(this);
        if ((current is not null) && !String.Equals(current, automationText, StringComparison.Ordinal))
        {
            return;
        }

        var text = AutomationSummaryFormatter is { } formatter ? formatter(this) : CreateAutomationSummary(new ChartSummaryText(Culture ?? CultureInfo.CurrentUICulture, Culture ?? CultureInfo.CurrentCulture));
        if (String.Equals(text, current, StringComparison.Ordinal))
        {
            return;
        }

        automationText = text;
        SemanticProperties.SetDescription(this, text);
    }
}
