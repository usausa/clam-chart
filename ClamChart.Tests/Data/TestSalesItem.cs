namespace ClamChart.Tests.Data;

internal sealed class TestSalesItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Month { get; init; } = String.Empty;

    public double Sales
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sales)));
        }
    }

    public double? Target { get; init; }

    public int Count { get; init; }

    public DateTime Date { get; init; }

    public DateTimeOffset Time { get; init; }

    public Color? Color { get; init; }

    public static ChartAccessorCollection<TestSalesItem> Accessors { get; } = new()
    {
        { "month", static x => x.Month },
        { "sales", static x => x.Sales },
        { "target", static x => x.Target },
        { "count", static x => x.Count },
        { "date", static x => x.Date },
        { "time", static x => x.Time },
        { "color", static x => x.Color }
    };
}
