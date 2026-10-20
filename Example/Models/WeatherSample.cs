namespace Example.Models;

public sealed class WeatherSample
{
    public static ChartAccessorCollection<WeatherSample> Accessors { get; } = new()
    {
        { "time", static x => x.Time },
        { "temperature", static x => x.Temperature },
        { "humidity", static x => x.Humidity }
    };

    public DateTime Time { get; init; }

    public double Temperature { get; init; }

    public double Humidity { get; init; }
}
