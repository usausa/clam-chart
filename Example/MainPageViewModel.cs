namespace Example;

using Example.Modules;
using Example.Shell;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public sealed partial class MainPageViewModel : ExtendViewModelBase, IShellControl, IAppLifecycle
{
    private bool destroying;

    public INavigator Navigator { get; }

    [ObservableProperty]
    public partial string Title { get; set; } = default!;

    [ObservableProperty]
    public partial bool HeaderVisible { get; set; }

    [ObservableProperty]
    public partial bool FunctionVisible { get; set; }

    [ObservableProperty]
    public partial string Function1Text { get; set; } = default!;
    [ObservableProperty]
    public partial string Function2Text { get; set; } = default!;
    [ObservableProperty]
    public partial string Function3Text { get; set; } = default!;
    [ObservableProperty]
    public partial string Function4Text { get; set; } = default!;

    [ObservableProperty]
    public partial bool Function1Enabled { get; set; }
    [ObservableProperty]
    public partial bool Function2Enabled { get; set; }
    [ObservableProperty]
    public partial bool Function3Enabled { get; set; }
    [ObservableProperty]
    public partial bool Function4Enabled { get; set; }

    public IObserveCommand Function1Command { get; }
    public IObserveCommand Function2Command { get; }
    public IObserveCommand Function3Command { get; }
    public IObserveCommand Function4Command { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public MainPageViewModel(INavigator navigator)
    {
        Navigator = navigator;

        Function1Command = MakeAsyncCommand(() => Navigator.NotifyAsync(ShellEvent.Function1), () => Function1Enabled);
        Function2Command = MakeAsyncCommand(() => Navigator.NotifyAsync(ShellEvent.Function2), () => Function2Enabled);
        Function3Command = MakeAsyncCommand(() => Navigator.NotifyAsync(ShellEvent.Function3), () => Function3Enabled);
        Function4Command = MakeAsyncCommand(() => Navigator.NotifyAsync(ShellEvent.Function4), () => Function4Enabled);
    }

    //--------------------------------------------------------------------------------
    // Lifecycle
    //--------------------------------------------------------------------------------

    // ReSharper disable once AsyncVoidMethod
    public async void OnCreated()
    {
        // Guard for the case where the Activity is recreated while initialization is still in progress
        if (destroying)
        {
            return;
        }

        Navigator.Exit();
        await Navigator.ForwardAsync(ViewId.ChartMenu);
    }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
    }

    public void OnStopped()
    {
    }

    public void OnResumed()
    {
    }

    public void OnDestroying()
    {
        destroying = true;
    }
}
