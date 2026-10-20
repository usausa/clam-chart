namespace ClamChart.Rendering;

using SkiaSharp;

internal static class ChartColor
{
    private static readonly SKColor DarkText = new(0x0B, 0x0B, 0x0B);

    private static readonly double DarkTextLuminance = GetLuminance(DarkText);

    public static SKColor Fade(SKColor color, double alpha) =>
        alpha >= 1 ? color : color.WithAlpha((byte)Math.Round(color.Alpha * Math.Clamp(alpha, 0, 1)));

    public static SKColor Lerp(SKColor from, SKColor to, double progress) =>
        new(Lerp(from.Red, to.Red, progress), Lerp(from.Green, to.Green, progress), Lerp(from.Blue, to.Blue, progress), Lerp(from.Alpha, to.Alpha, progress));

    public static SKColor GetContrastColor(SKColor background)
    {
        var luminance = GetLuminance(background);
        return (luminance + 0.05) / (DarkTextLuminance + 0.05) >= 1.05 / (luminance + 0.05) ? DarkText : SKColors.White;
    }

    private static byte Lerp(byte from, byte to, double progress) => (byte)Math.Round(from + ((to - from) * Math.Clamp(progress, 0, 1)));

    private static double GetLuminance(SKColor color) =>
        (0.2126 * ToLinear(color.Red)) + (0.7152 * ToLinear(color.Green)) + (0.0722 * ToLinear(color.Blue));

    private static double ToLinear(byte channel)
    {
        var value = channel / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
