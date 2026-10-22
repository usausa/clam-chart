namespace ClamChart.Data;

internal sealed record ChartValueOptions
{
    public IChartAccessorProvider? Accessors { get; init; }

    public string? XKey { get; init; }

    public string? YKey { get; init; }

    public string? LabelKey { get; init; }

    public string? ColorKey { get; init; }

    public string? MaximumKey { get; init; }

    public string? TargetKey { get; init; }

    public string? LowKey { get; init; }

    public string? SizeKey { get; init; }

    public Func<object, double?>? ValueSelector { get; init; }

    public Func<object, double?>? LowSelector { get; init; }

    public Func<object, double?>? SizeSelector { get; init; }

    public Func<object, string?>? LabelSelector { get; init; }
}
