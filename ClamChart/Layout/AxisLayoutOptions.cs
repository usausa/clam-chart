namespace ClamChart.Layout;

using ClamChart.Rendering;

internal sealed record AxisLayoutOptions
{
    public ChartAxisKind Kind { get; init; }

    public bool IsVisible { get; init; } = true;

    public bool LabelsVisible { get; init; } = true;

    public string? Title { get; init; }

    public ChartOrientation TitleOrientation { get; init; } = ChartOrientation.Horizontal;

    public int? LabelMaxLength { get; init; }

    public AxisRangeOptions Range { get; init; } = new();

    public ChartTickAlignment TickAlignment { get; init; }

    public TimeInterval? TimeInterval { get; init; }

    public ChartDateTimeIntervalType MinimumTimeUnit { get; init; }

    public DayOfWeek FirstDayOfWeek { get; init; }

    public ChartLabelPlacement LabelPlacement { get; init; }

    public int? MaxLabelCount { get; init; }

    public bool IsInversed { get; init; }

    public ChartAxisPosition Position { get; init; }

    public ChartTextFormat LabelFormat { get; init; } = ChartTextFormat.Default;

    public IValueConverter? LabelConverter { get; init; }

    public Func<double, string>? LabelFormatter { get; init; }

    public Func<string, string>? CategoryFormatter { get; init; }

    public bool GridLinesVisible { get; init; }
}
