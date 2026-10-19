namespace Example.Modules.Chart;

public sealed class ChartMenuViewModel : AppViewModelBase
{
    public IObserveCommand ForwardCommand { get; }

    public ChartMenuViewModel()
    {
        ForwardCommand = MakeAsyncCommand<ViewId>(x => Navigator.ForwardAsync(x));
    }

    protected override Task OnNotifyBackAsync()
    {
        AndroidHelper.MoveTaskToBack();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();
}
