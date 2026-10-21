namespace ClamChart.Tests;

using Microsoft.Maui;

internal sealed class TestViewHandler(object? platformView) : IViewHandler
{
    public int Redraws { get; private set; }

    public bool HasContainer { get; set; }

    public object? ContainerView => null;

    public object? PlatformView => platformView;

    public IView? VirtualView { get; private set; }

    IElement? IElementHandler.VirtualView => VirtualView;

    public IMauiContext? MauiContext => null;

    public Size GetDesiredSize(double widthConstraint, double heightConstraint) => Size.Zero;

    public void PlatformArrange(Rect frame)
    {
    }

    public void SetMauiContext(IMauiContext mauiContext)
    {
    }

    public void SetVirtualView(IElement view) => VirtualView = (IView)view;

    public void UpdateValue(string property)
    {
    }

    public void Invoke(string command, object? args = null)
    {
        if (command == "InvalidateSurface")
        {
            Redraws++;
        }
    }

    public void DisconnectHandler()
    {
    }
}
