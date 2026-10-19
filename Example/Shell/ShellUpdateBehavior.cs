namespace Example.Shell;

using Smart.Maui.Interactivity;

public sealed class ShellUpdateBehavior : BehaviorBase<ContentPage>
{
    public static readonly BindableProperty NavigatorProperty = BindableProperty.Create(
        nameof(Navigator),
        typeof(INavigator),
        typeof(ShellUpdateBehavior),
        propertyChanged: HandlePropertyChanged);

    public INavigator? Navigator
    {
        get => (INavigator)GetValue(NavigatorProperty);
        set => SetValue(NavigatorProperty, value);
    }

    protected override void OnDetachingFrom(ContentPage bindable)
    {
        if (Navigator is not null)
        {
            Navigator.Navigating -= NavigatorOnNavigating;
            Navigator.Exited -= NavigatorOnExited;
        }

        base.OnDetachingFrom(bindable);
    }

    private static void HandlePropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        ((ShellUpdateBehavior)bindable).OnNavigatorPropertyChanged(oldValue as INavigator, newValue as INavigator);
    }

    private void OnNavigatorPropertyChanged(INavigator? oldValue, INavigator? newValue)
    {
        if (newValue == oldValue)
        {
            return;
        }

        if (oldValue is not null)
        {
            oldValue.Navigating -= NavigatorOnNavigating;
            oldValue.Exited -= NavigatorOnExited;
        }

        if (newValue is not null)
        {
            newValue.Navigating += NavigatorOnNavigating;
            newValue.Exited += NavigatorOnExited;
        }
    }

    private void NavigatorOnNavigating(object? sender, Smart.Navigation.NavigationEventArgs e)
    {
        UpdateShell(e.ToView as Element);
    }

    private void NavigatorOnExited(object? sender, EventArgs e)
    {
        UpdateShell(null);
    }

    private void UpdateShell(BindableObject? view)
    {
        if (AssociatedObject?.BindingContext is IShellControl shell)
        {
            ShellProperty.UpdateShellControl(shell, view);
        }
    }
}
