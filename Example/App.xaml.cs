namespace Example;

using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1724
public sealed partial class App
{
    private readonly IServiceProvider serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        // Light theme based application
        Current!.UserAppTheme = AppTheme.Light;

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(serviceProvider.GetRequiredService<MainPage>());
    }
}
#pragma warning restore CA1724
