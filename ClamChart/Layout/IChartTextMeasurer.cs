namespace ClamChart.Layout;

internal interface IChartTextMeasurer
{
    double Measure(string text, double fontSize, double maxWidth);

    double GetLineHeight(double fontSize);
}
