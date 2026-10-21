namespace ClamChart.Animation;

internal interface IChartAnimationDriver
{
    bool CanStart { get; }

    void Start(string name, uint length, Action<double> frame, Action<double, bool> finished, Func<bool>? repeat = null);

    void Abort(string name);
}
