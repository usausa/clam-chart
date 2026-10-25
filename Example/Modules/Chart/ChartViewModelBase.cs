namespace Example.Modules.Chart;

public abstract partial class ChartViewModelBase : AppViewModelBase
{
    [ObservableProperty]
    public partial bool IsSettingOpen { get; set; }

    protected override Task OnNotifyBackAsync()
    {
        if (IsSettingOpen)
        {
            IsSettingOpen = false;
            return Task.CompletedTask;
        }

        return Navigator.ForwardAsync(ViewId.ChartMenu);
    }

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction2()
    {
        IsSettingOpen = !IsSettingOpen;
        return Task.CompletedTask;
    }
}
