namespace Example.Modules.Chart;

public sealed class ThresholdRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public required string Name { get; init; }

    public required ObservableCollection<double> Values { get; init; }

    public double? NormalMinimum { get; init; }

    public double? NormalMaximum { get; init; }

    public ChartSparklineEmphasis Emphasis { get; init; }

    public IList<ChartThreshold> Thresholds { get; init; } = [];

    public ChartThresholdsMode ThresholdsMode { get; init; }

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

    public void Push(double value)
    {
        Values.Add(value);
        Values.RemoveAt(0);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Last)));
    }
}
