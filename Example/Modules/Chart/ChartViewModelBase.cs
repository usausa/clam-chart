namespace Example.Modules.Chart;

public abstract class ChartViewModelBase : AppViewModelBase
{
    protected override Task OnNotifyBackAsync() => Navigator.ForwardAsync(ViewId.ChartMenu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();
}
