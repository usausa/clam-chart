namespace Example.Modules;

using Example.Shell;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public abstract class AppViewModelBase : ExtendViewModelBase, INavigatorAware, INavigationEventSupportAsync, INotifySupportAsync<ShellEvent>
{
    public INavigator Navigator { get; set; } = default!;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        System.Diagnostics.Debug.WriteLine($"{GetType()} is Disposed");
    }

    public virtual Task OnNavigatingFromAsync(INavigationContext context) => Task.CompletedTask;

    public virtual Task OnNavigatingToAsync(INavigationContext context) => Task.CompletedTask;

    public virtual Task OnNavigatedToAsync(INavigationContext context) => Task.CompletedTask;

    public async Task NavigatorNotifyAsync(ShellEvent parameter)
    {
        var task = parameter switch
        {
            ShellEvent.Back => OnNotifyBackAsync(),
            ShellEvent.Function1 => OnNotifyFunction1(),
            ShellEvent.Function2 => OnNotifyFunction2(),
            ShellEvent.Function3 => OnNotifyFunction3(),
            ShellEvent.Function4 => OnNotifyFunction4(),
            _ => Task.CompletedTask
        };

        await task.ConfigureAwait(true);
    }

    protected virtual Task OnNotifyBackAsync() => Task.CompletedTask;

    protected virtual Task OnNotifyFunction1() => Task.CompletedTask;

    protected virtual Task OnNotifyFunction2() => Task.CompletedTask;

    protected virtual Task OnNotifyFunction3() => Task.CompletedTask;

    protected virtual Task OnNotifyFunction4() => Task.CompletedTask;
}
