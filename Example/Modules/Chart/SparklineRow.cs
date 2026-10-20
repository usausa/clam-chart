namespace Example.Modules.Chart;

public sealed class SparklineRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public required string Name { get; init; }

    public required IReadOnlyList<double> Values { get; init; }

    public double Last => Values[^1];

    public ChartSparklineKind Kind
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Kind)));
        }
    }

    public ChartSparklineMarkers Markers
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Markers)));
        }
    }
}
