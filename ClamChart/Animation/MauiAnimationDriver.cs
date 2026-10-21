namespace ClamChart.Animation;

internal sealed class MauiAnimationDriver : IChartAnimationDriver
{
    private readonly VisualElement owner;

    public MauiAnimationDriver(VisualElement owner)
    {
        this.owner = owner;
    }

    public bool CanStart => (owner.Handler is not null) && owner.IsVisible;

    public void Start(string name, uint length, Action<double> frame, Action<double, bool> finished, Func<bool>? repeat = null) =>
        owner.Animate(name, frame, 0, 1, 16, length, Easing.Linear, finished, repeat);

    public void Abort(string name) => owner.AbortAnimation(name);
}
