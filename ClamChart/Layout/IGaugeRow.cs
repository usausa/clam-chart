namespace ClamChart.Layout;

internal interface IGaugeRow
{
    int DataIndex { get; }

    int Slot { get; }

    double Ratio { get; }
}
