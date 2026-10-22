namespace ClamChart.Rendering;

using SkiaSharp;

internal static class HeatmapPalette
{
    public const int Steps = 64;

    public static void Build(IReadOnlyList<Color> stops, SKColor[] destination)
    {
        var last = destination.Length - 1;
        for (var i = 0; i <= last; i++)
        {
            var t = last > 0 ? (double)i / last : 0;
            destination[i] = new SKColor(Interpolate(stops, t, 0), Interpolate(stops, t, 1), Interpolate(stops, t, 2), Interpolate(stops, t, 3));
        }
    }

    public static int GetIndex(double value, double minimum, double maximum, int count)
    {
        var range = maximum - minimum;
        if (!(range > 0))
        {
            range = 1;
        }

        if (value < minimum)
        {
            return 0;
        }

        return value > maximum ? count - 1 : Math.Min(count - 1, (int)Math.Floor(count * (value - minimum) / range));
    }

    private static byte Interpolate(IReadOnlyList<Color> stops, double t, int channel)
    {
        var n = stops.Count - 1;
        int i;
        if (t <= 0)
        {
            t = 0;
            i = 0;
        }
        else if (t >= 1)
        {
            t = 1;
            i = n - 1;
        }
        else
        {
            i = (int)Math.Floor(t * n);
        }

        var v1 = GetChannel(stops[i], channel);
        var v2 = GetChannel(stops[i + 1], channel);
        var v0 = i > 0 ? GetChannel(stops[i - 1], channel) : (2 * v1) - v2;
        var v3 = i < n - 1 ? GetChannel(stops[i + 2], channel) : (2 * v2) - v1;
        return (byte)Math.Clamp(Math.Floor(Basis((t - ((double)i / n)) * n, v0, v1, v2, v3) + 0.5), 0, 255);
    }

    private static double Basis(double t1, double v0, double v1, double v2, double v3)
    {
        var t2 = t1 * t1;
        var t3 = t2 * t1;
        return (((1 - (3 * t1) + (3 * t2) - t3) * v0) + ((4 - (6 * t2) + (3 * t3)) * v1) + ((1 + (3 * t1) + (3 * t2) - (3 * t3)) * v2) + (t3 * v3)) / 6;
    }

    private static double GetChannel(Color color, int channel) => Math.Round(channel switch
    {
        0 => color.Red,
        1 => color.Green,
        2 => color.Blue,
        _ => color.Alpha
    } * 255d);
}
