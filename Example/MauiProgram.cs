namespace Example;

using BunnyTail.DependencyInjection;

using Example.Modules;

using SkiaSharp.Views.Maui.Controls.Hosting;

using Smart.Mvvm.Resolver;

public static partial class MauiProgram
{
    public static MauiApp CreateMauiApp() =>
        MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureLogging()
            .UseSkiaSharp()
            .ConfigureComponents()
            .BuildApplication();

    // ------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------

    private static MauiAppBuilder ConfigureLogging(this MauiAppBuilder builder)
    {
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder;
    }

    // ------------------------------------------------------------
    // Components
    // ------------------------------------------------------------

    private static MauiAppBuilder ConfigureComponents(this MauiAppBuilder builder)
    {
        // Source generated service provider
        builder.ConfigureContainer(new GeneratedServiceProviderFactory());

        var services = builder.Services;

        // Convention based views and view models (source generated)
        services.AddViews();

        // Navigator
        services.AddNavigator(static (_, config) =>
        {
            config.UseMauiNavigationProvider();
            config.UseIdViewMapper(static m => m.AutoRegister(ViewSource()));
        });

        return builder;
    }

    // Views and view models are registered by naming convention (source generated)
    [ComponentRegistration(Lifetime.Transient, "Page$", Namespace = "Example")]
    [ComponentRegistration(Lifetime.Transient, "View$", Namespace = "Example.Modules")]
    [ComponentRegistration(Lifetime.Transient, "ViewModel$", Namespace = "Example")]
    public static partial IServiceCollection AddViews(this IServiceCollection services);

    // ------------------------------------------------------------
    // Build
    // ------------------------------------------------------------

    private static MauiApp BuildApplication(this MauiAppBuilder builder)
    {
        var app = builder.Build();

        // Setup provider
        ResolveProvider.Default.Provider = app.Services;

        return app;
    }

    // ------------------------------------------------------------
    // Navigation
    // ------------------------------------------------------------

    [ViewSource]
    public static partial IEnumerable<KeyValuePair<ViewId, Type>> ViewSource();
}
