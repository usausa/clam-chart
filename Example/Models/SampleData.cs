namespace Example.Models;

using System.Security.Cryptography;

public static class SampleData
{
    public static readonly string[] Months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    public static double Next(double minimum, double maximum) =>
        minimum + ((maximum - minimum) * RandomNumberGenerator.GetInt32(0, 10001) / 10000);

    public static double[] Walk(int count, double start, double step, double minimum)
    {
        var values = new double[count];
        var value = start;
        for (var i = 0; i < count; i++)
        {
            value = Math.Max(minimum, value + Next(-step, step));
            values[i] = Math.Round(value, 1);
        }

        return values;
    }
}
