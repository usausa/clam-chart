namespace ClamChart.Tests.Layout;

internal sealed class TestTextMeasurer : IChartTextMeasurer
{
    public const double CharacterRatio = 0.5;

    public double Measure(string text, double fontSize, double maxWidth) => Math.Min(text.Length * fontSize * CharacterRatio, maxWidth);

    public double GetLineHeight(double fontSize) => fontSize + 2;
}
